using Quartz;

namespace Elsa.Scheduling.Quartz.Contracts;

/// <summary>
/// Schedules retries for Quartz jobs.
/// </summary>
public interface IQuartzJobRetryScheduler
{
    /// <summary>
    /// Determines whether a job that failed with the specified exception should be retried.
    /// </summary>
    /// <param name="exception">The exception the job failed with.</param>
    /// <returns>True if the exception is worth retrying; otherwise, false.</returns>
    bool IsRetryable(Exception exception);

    /// <summary>
    /// Reschedules the trigger of the current job for a retry, using the configured backoff policy. The attempt number
    /// is carried over on the rescheduled trigger, together with the job data of the original trigger.
    /// </summary>
    /// <param name="context">The execution context of the attempt that just failed.</param>
    /// <param name="exception">The exception the job failed with.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>
    /// True if a retry was scheduled; false if retries are disabled or the configured maximum number of retries has
    /// been exhausted, in which case the caller is responsible for reporting the failure.
    /// </returns>
    Task<bool> ScheduleRetryAsync(IJobExecutionContext context, Exception exception, CancellationToken cancellationToken = default);
}
