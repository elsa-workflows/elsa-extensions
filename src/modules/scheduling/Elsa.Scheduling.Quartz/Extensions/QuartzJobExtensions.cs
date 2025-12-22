using Elsa.Scheduling.Quartz.Options;
using Microsoft.Extensions.Options;
using Quartz;

namespace Elsa.Scheduling.Quartz;

/// <summary>
/// Extension methods for Quartz job execution context.
/// </summary>
public static class QuartzJobExtensions
{
    /// <summary>
    /// Reschedules the current job to run after a configured delay for transient retry.
    /// </summary>
    /// <param name="context">The job execution context.</param>
    /// <param name="options">The Quartz job options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task RescheduleForTransientRetryAsync(this IJobExecutionContext context, IOptions<QuartzJobOptions> options, CancellationToken cancellationToken = default)
    {
        var delaySeconds = options.Value.TransientRetryDelaySeconds;
        var trigger = TriggerBuilder.Create()
            .ForJob(context.JobDetail.Key)
            .StartAt(DateTimeOffset.UtcNow.AddSeconds(delaySeconds))
            .Build();

        await context.Scheduler.RescheduleJob(context.Trigger.Key, trigger, cancellationToken);
    }
}
