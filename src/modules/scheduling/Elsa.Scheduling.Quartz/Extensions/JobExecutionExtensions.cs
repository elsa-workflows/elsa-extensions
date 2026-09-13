using System.Globalization;
using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Jobs;
using Quartz;
using QuartzScheduler = global::Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz;

internal static class JobExecutionExtensions
{
    /// <summary>
    /// Attempts to schedule a retry for the current job execution. Callers should return from their <c>catch</c>
    /// block when this returns <c>true</c>; when it returns <c>false</c>, retries are exhausted and the caller is
    /// responsible for logging its own workflow-specific error message.
    /// </summary>
    /// <param name="retryScheduler">The retry scheduler.</param>
    /// <param name="context">The Quartz job execution context.</param>
    /// <param name="exception">The exception the job failed with.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>True if a retry was scheduled; otherwise, false.</returns>
    public static async Task<bool> TryScheduleRetryAsync(this IQuartzJobRetryScheduler retryScheduler, IJobExecutionContext context, Exception exception, CancellationToken cancellationToken = default)
    {
        // The retry scheduler logs the scheduled retry, including the attempt number and delay.
        return await retryScheduler.ScheduleRetryAsync(context, exception, cancellationToken);
    }

    /// <summary>
    /// Removes a trigger whose workflow graph is no longer available. A retry may have been acquired before an
    /// explicit unschedule and reschedule of the same task key; in that case, only remove the original trigger when
    /// its schedule generation still belongs to the retry that acquired it.
    /// </summary>
    /// <param name="context">The Quartz job execution context.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    public static Task UnscheduleAfterWorkflowGraphNotFoundAsync(this IJobExecutionContext context, CancellationToken cancellationToken = default) =>
        context.UnscheduleAfterWorkflowGraphNotFoundAsync(null, cancellationToken);

    /// <summary>
    /// Removes a graph-not-found trigger under the optional per-original coordinator.
    /// </summary>
    /// <param name="context">The Quartz job execution context.</param>
    /// <param name="scheduleCoordinator">The optional per-original schedule coordinator.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    public static async Task UnscheduleAfterWorkflowGraphNotFoundAsync(this IJobExecutionContext context, IQuartzScheduleCoordinator? scheduleCoordinator, CancellationToken cancellationToken = default)
    {
        var originalTriggerKey = QuartzTriggerKeys.GetOriginalTriggerKey(context.Trigger);
        var action = async (CancellationToken token) =>
        {
            var isRetryTrigger = QuartzTriggerKeys.IsRetryTrigger(context.Trigger);
            var scheduleGeneration = QuartzTriggerKeys.GetScheduleGeneration(context.Trigger);
            var originalTrigger = await context.Scheduler.GetTrigger(originalTriggerKey, token);

            if (!isRetryTrigger)
            {
                // The original trigger key is shared by every schedule generation. If this execution was acquired
                // before an unschedule+reschedule, deleting it now would remove the replacement generation. A null
                // result is the normal one-shot case after Quartz has completed the firing, so it is safe to retain
                // the existing best-effort unschedule in that case.
                if (originalTrigger != null && !HasSameScheduleGeneration(context.Trigger, originalTrigger))
                {
                    await UnschedulePendingRetriesAsync(context.Scheduler, originalTriggerKey, scheduleGeneration, includeLegacy: false, cancellationToken: token);
                    return;
                }

                await context.Scheduler.UnscheduleJob(context.Trigger.Key, token);

                // A generated schedule can have a pending retry even when this graph-not-found failure came from the
                // original occurrence. Remove only this generation's retry chain; never touch a replacement key.
                if (!string.Equals(scheduleGeneration, QuartzJobDataKeys.LegacyScheduleGeneration, StringComparison.Ordinal) || IsRecurringTrigger(context.Trigger))
                    await UnschedulePendingRetriesAsync(context.Scheduler, originalTriggerKey, scheduleGeneration, includeLegacy: true, cancellationToken: token);

                return;
            }

            // The retry trigger itself has a generation-specific key, so removing it is safe. The original trigger
            // still needs the compare-and-delete guard below because its key is shared across generations.
            await context.Scheduler.UnscheduleJob(context.Trigger.Key, token);

            if (originalTrigger == null)
                return;

            if (!HasSameScheduleGeneration(context.Trigger, originalTrigger))
                return;

            await context.Scheduler.UnscheduleJob(originalTriggerKey, token);
        };

        if (scheduleCoordinator == null)
            await action(cancellationToken);
        else
            await scheduleCoordinator.ExecuteAsync(originalTriggerKey, action, cancellationToken);
    }

    private static bool HasSameScheduleGeneration(ITrigger left, ITrigger right) =>
        string.Equals(
            QuartzTriggerKeys.GetScheduleGeneration(left),
            QuartzTriggerKeys.GetScheduleGeneration(right),
            StringComparison.Ordinal);

    private static async Task UnschedulePendingRetriesAsync(QuartzScheduler scheduler, TriggerKey originalTriggerKey, string scheduleGeneration, bool includeLegacy, CancellationToken cancellationToken)
    {
        await scheduler.UnscheduleJob(QuartzTriggerKeys.GetRetryTriggerKey(originalTriggerKey, scheduleGeneration), cancellationToken);

        if (includeLegacy && !string.Equals(scheduleGeneration, QuartzJobDataKeys.LegacyScheduleGeneration, StringComparison.Ordinal))
            await scheduler.UnscheduleJob(QuartzTriggerKeys.GetRetryTriggerKey(originalTriggerKey), cancellationToken);
    }

    private static bool IsRecurringTrigger(ITrigger trigger) => trigger switch
    {
        ICronTrigger => true,
        ISimpleTrigger simpleTrigger => simpleTrigger.RepeatCount != 0,
        _ => false
    };

    /// <summary>
    /// Gets the number of retries that have already been scheduled for the currently executing trigger. Returns 0 when
    /// the trigger is the original schedule, i.e. when the current execution is not a retry.
    /// </summary>
    /// <param name="context">The Quartz job execution context.</param>
    public static int GetRetryAttempt(this IJobExecutionContext context)
    {
        var jobDataMap = context.Trigger.JobDataMap;

        if (jobDataMap == null || !jobDataMap.TryGetValue(QuartzJobDataKeys.RetryAttempt, out var value))
            return 0;

        // The attempt is written as a string so that job stores using properties-only serialization can persist it,
        // but a numeric value is accepted as well.
        return value switch
        {
            int intValue => intValue,
            long longValue => (int)Math.Clamp(longValue, int.MinValue, int.MaxValue),
            string stringValue when long.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue) => (int)Math.Clamp(parsedValue, int.MinValue, int.MaxValue),
            _ => 0
        };
    }

    public static async Task<Tenant?> GetTenantAsync(this IJobExecutionContext context, ITenantFinder tenantFinder)
    {
        if(!context.MergedJobDataMap.ContainsKey("TenantId"))
            return null;
        
        if(!context.MergedJobDataMap.TryGetString("TenantId", out var tenantId))
            return null;
        
        if (string.IsNullOrWhiteSpace(tenantId))
            return null;
        
        return await tenantFinder.FindByIdAsync(tenantId, context.CancellationToken);
    }

    /// <summary>
    /// Executes delete job if allowed
    /// </summary>
    /// <param name="context">The Quartz job execution context.</param>
    /// <param name="jobKey">The Quartz job key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task DeleteJob(this IJobExecutionContext context, JobKey jobKey, CancellationToken cancellationToken = default)
    {
        if (IsJobAllowedToBeDeleted(jobKey.Name))
            await context.Scheduler.DeleteJob(jobKey, cancellationToken);
    }

    /// <summary>
    /// Checks if the job is allowed to be deleted by name
    /// </summary>
    /// <param name="jobName">Name of the job to check</param>
    /// <returns>False if the job is one of the required ones, otherwise true</returns>
    private static bool IsJobAllowedToBeDeleted(string jobName)
    {
        return jobName != nameof(ResumeWorkflowJob)
               && jobName != nameof(RunWorkflowJob);
    }
}
