using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Jobs;
using Quartz;

namespace Elsa.Scheduling.Quartz;

internal static class JobExecutionExtensions
{
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