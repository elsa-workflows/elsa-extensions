using System.Globalization;
using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Jobs;
using Quartz;

namespace Elsa.Scheduling.Quartz;

internal static class JobExecutionExtensions
{
    /// <summary>
    /// Gets the number of retries that have already been scheduled for the currently executing trigger. Returns 0 when
    /// the trigger is the original one, i.e. when the current execution is not a retry.
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
            long longValue => (int)longValue,
            string stringValue when int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue) => parsedValue,
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