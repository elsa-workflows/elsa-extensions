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
    /// Schedules a retry for the current job after a delay specified in the provided Quartz job options.
    /// </summary>
    /// <param name="context">
    /// The Quartz job execution context, which provides information about the job and the current trigger.
    /// </param>
    /// <param name="options">
    /// The options object containing configuration values that determine the delay duration for the retry.
    /// </param>
    /// <param name="cancellationToken">
    /// An optional token to cancel the operation.
    /// </param>
    public static async Task ScheduleRetryAsync(this IJobExecutionContext context, IOptions<QuartzJobOptions> options, CancellationToken cancellationToken = default)
    {
        var delaySeconds = options.Value.TransientExceptionRetryDelay;
        var trigger = TriggerBuilder.Create()
            .ForJob(context.JobDetail.Key)
            .StartAt(DateTimeOffset.UtcNow.Add(delaySeconds))
            .Build();

        await context.Scheduler.RescheduleJob(context.Trigger.Key, trigger, cancellationToken);
    }
}
