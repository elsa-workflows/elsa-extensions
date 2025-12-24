using Quartz;

namespace Elsa.Scheduling.Quartz.Contracts;

/// <summary>
/// Schedules retries for Quartz jobs.
/// </summary>
public interface IQuartzJobRetryScheduler
{
    /// <summary>
    /// Reschedules the trigger of the current job for a retry using the configured retry delay.
    /// </summary>
    Task ScheduleRetryAsync(IJobExecutionContext context, CancellationToken cancellationToken = default);
}
