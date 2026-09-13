using System.Globalization;
using Elsa.Common;
using Elsa.Resilience;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Models;
using Elsa.Scheduling.Quartz.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using QuartzScheduler = global::Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.Services;

/// <summary>
/// Default implementation of <see cref="IQuartzJobRetryScheduler"/>. Rather than retrying in-process, each retry is
/// scheduled as a separate one-shot Quartz trigger for the same job, so that a pending retry does not occupy a Quartz
/// worker thread while waiting, the original schedule (cron or repeating) is left intact, and the retry survives an
/// application restart when Quartz is configured with a persistent job store (with the default in-memory store,
/// pending retries are lost on restart).
/// </summary>
public class QuartzJobRetryScheduler(
    ISystemClock systemClock,
    IOptions<QuartzJobOptions> options,
    IQuartzRetryDelayCalculator delayCalculator,
    ITransientExceptionDetector transientExceptionDetector,
    ILogger<QuartzJobRetryScheduler> logger) : IQuartzJobRetryScheduler
{
    /// <inheritdoc />
    public bool IsRetryable(Exception exception)
    {
        var isRetryable = options.Value.IsRetryable;
        return isRetryable != null ? isRetryable(exception) : transientExceptionDetector.IsTransient(exception);
    }

    /// <inheritdoc />
    public async Task<bool> ScheduleRetryAsync(IJobExecutionContext context, Exception exception, CancellationToken cancellationToken = default)
    {
        var jobOptions = options.Value;
        var jobKey = context.JobDetail.Key;

        if (!jobOptions.RetryEnabled)
        {
            logger.LogDebug("Retries are disabled. Not scheduling a retry for job {JobKey}", jobKey);
            return false;
        }

        var attemptsMade = Math.Max(context.GetRetryAttempt(), 0);

        if (attemptsMade >= jobOptions.MaxRetryAttempts)
            return false;

        var attemptNumber = attemptsMade + 1;
        var delay = GetDelay(context, exception, attemptNumber, jobOptions);
        var retryTrigger = CreateRetryTrigger(context, attemptNumber, delay);

        logger.LogWarning(
            exception,
            "Job {JobKey} failed with a retryable error. Scheduling retry {AttemptNumber} of {MaxRetryAttempts} in {RetryDelay}",
            jobKey,
            attemptNumber,
            jobOptions.MaxRetryAttempts,
            delay);

        // Replace any existing pending retry rather than the firing trigger, so a cron or repeating schedule keeps
        // its next fire times. Unschedule-then-schedule covers both the first retry and a later attempt that reuses
        // the same derived key.
        await context.Scheduler.UnscheduleJob(retryTrigger.Key, cancellationToken);

        try
        {
            await context.Scheduler.ScheduleJob(retryTrigger, cancellationToken);
        }
        catch (JobPersistenceException e) when (e.InnerException is ObjectAlreadyExistsException)
        {
            // Another concurrent execution won the race to create the deterministic retry key. The retry is already
            // scheduled, so report success to the job and avoid turning an idempotent operation into a failed attempt.
            logger.LogDebug("Retry trigger {RetryTriggerKey} already exists for job {JobKey}; keeping the existing retry", retryTrigger.Key, jobKey);
        }
        catch (ObjectAlreadyExistsException)
        {
            // See the wrapped exception case above. Quartz may expose the duplicate directly depending on the store.
            logger.LogDebug("Retry trigger {RetryTriggerKey} already exists for job {JobKey}; keeping the existing retry", retryTrigger.Key, jobKey);
        }

        // An explicit UnscheduleAsync removes the original trigger before it removes the derived retry trigger. If
        // that operation overlaps this schedule, the retry can be created after its removal step. Re-checking the
        // original after creating a retry closes that ordering gap without serializing unrelated workflows. A missing
        // one-shot trigger is expected after it fires, so only recurring chains use this fence. The generation token
        // also prevents a retry from an old schedule from surviving an unschedule+reschedule of the same task key.
        if (IsOriginalRecurring(retryTrigger) && !await IsCurrentRecurringScheduleAsync(context.Scheduler, retryTrigger, cancellationToken))
        {
            await context.Scheduler.UnscheduleJob(retryTrigger.Key, cancellationToken);
            logger.LogDebug("Original trigger {OriginalTriggerKey} was removed or replaced while retry {RetryTriggerKey} was being scheduled; removing the retry", QuartzTriggerKeys.GetOriginalTriggerKey(retryTrigger), retryTrigger.Key);
            return true;
        }

        return true;
    }

    private static bool IsOriginalRecurring(ITrigger retryTrigger)
    {
        if (!retryTrigger.JobDataMap.TryGetValue(QuartzJobDataKeys.RetryOriginalIsRecurring, out var value))
            return false;

        return value switch
        {
            bool boolValue => boolValue,
            string stringValue => bool.TryParse(stringValue, out var parsedValue) && parsedValue,
            _ => false
        };
    }

    private TimeSpan GetDelay(IJobExecutionContext context, Exception exception, int attemptNumber, QuartzJobOptions jobOptions)
    {
        var computedDelay = delayCalculator.CalculateDelay(attemptNumber, jobOptions);
        var delayGenerator = jobOptions.DelayGenerator;

        if (delayGenerator == null)
            return computedDelay;

        var retryContext = new QuartzJobRetryContext
        {
            JobExecutionContext = context,
            Exception = exception,
            AttemptNumber = attemptNumber,
            MaxRetryAttempts = jobOptions.MaxRetryAttempts,
            ComputedDelay = computedDelay
        };

        var customDelay = delayGenerator(retryContext);

        if (customDelay == null)
            return computedDelay;

        return customDelay.Value > TimeSpan.Zero ? customDelay.Value : TimeSpan.Zero;
    }

    private ITrigger CreateRetryTrigger(IJobExecutionContext context, int attemptNumber, TimeSpan delay)
    {
        // Carry over the job data of the failed trigger so that the workflow inputs it carries are not lost. Use a
        // derived key so the original trigger (and its cron / repeating schedule) is left in place.
        var jobDataMap = new JobDataMap();
        var triggerJobDataMap = context.Trigger.JobDataMap;

        if (triggerJobDataMap != null)
            jobDataMap.PutAll(triggerJobDataMap);

        var originalTriggerKey = QuartzTriggerKeys.GetOriginalTriggerKey(context.Trigger);
        var scheduleGeneration = GetScheduleGeneration(context.Trigger);
        jobDataMap[QuartzJobDataKeys.RetryAttempt] = attemptNumber.ToString(CultureInfo.InvariantCulture);
        jobDataMap[QuartzJobDataKeys.RetryTrigger] = bool.TrueString;
        jobDataMap[QuartzJobDataKeys.RetryOriginalTriggerName] = originalTriggerKey.Name;
        jobDataMap[QuartzJobDataKeys.RetryOriginalTriggerGroup] = originalTriggerKey.Group;
        jobDataMap[QuartzJobDataKeys.RetryScheduleGeneration] = scheduleGeneration;

        if (!jobDataMap.ContainsKey(QuartzJobDataKeys.RetryOriginalIsRecurring))
            jobDataMap[QuartzJobDataKeys.RetryOriginalIsRecurring] = IsRecurringTrigger(context.Trigger).ToString();

        var now = systemClock.UtcNow;
        var startAt = delay >= DateTimeOffset.MaxValue - now ? DateTimeOffset.MaxValue : now.Add(delay);

        return TriggerBuilder.Create()
            .ForJob(context.JobDetail.Key)
            .WithIdentity(QuartzTriggerKeys.GetRetryTriggerKey(originalTriggerKey, scheduleGeneration))
            .UsingJobData(jobDataMap)
            .StartAt(startAt)
            .Build();
    }

    private static bool IsRecurringTrigger(ITrigger trigger) => trigger switch
    {
        ICronTrigger => true,
        ISimpleTrigger simpleTrigger => simpleTrigger.RepeatCount != 0,
        _ => false
    };

    private static string GetScheduleGeneration(ITrigger trigger) =>
        trigger.JobDataMap.TryGetValue(QuartzJobDataKeys.RetryScheduleGeneration, out var value) && value != null
            ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? QuartzJobDataKeys.LegacyScheduleGeneration
            : QuartzJobDataKeys.LegacyScheduleGeneration;

    private static async Task<bool> IsCurrentRecurringScheduleAsync(QuartzScheduler scheduler, ITrigger retryTrigger, CancellationToken cancellationToken)
    {
        var originalTrigger = await scheduler.GetTrigger(QuartzTriggerKeys.GetOriginalTriggerKey(retryTrigger), cancellationToken);

        if (originalTrigger == null)
            return false;

        return string.Equals(
            GetScheduleGeneration(retryTrigger),
            GetScheduleGeneration(originalTrigger),
            StringComparison.Ordinal);
    }
}
