using Elsa.Scheduling.Quartz.Jobs;
using Quartz;

namespace Elsa.Scheduling.Quartz.Handlers;

public static class QuartzDeleteJobHandler
{
    public static async Task DeleteJob(this IJobExecutionContext context, JobKey jobKey, CancellationToken cancellationToken = default)
    {
        if (IsJobAllowedToBeDeleted(jobKey.Name))
            await context.Scheduler.DeleteJob(jobKey, cancellationToken);
    }

    private static bool IsJobAllowedToBeDeleted(string jobName)
    {
        return jobName != nameof(ResumeWorkflowJob)
            && jobName != nameof(RunWorkflowJob);
    }
}