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
    /// Schedules a one-shot retry trigger for the current job, using the configured backoff policy. The attempt number
    /// is carried on that retry trigger, together with the job data of the original trigger. The firing trigger is left
    /// in place so a cron or repeating schedule keeps its next fire times.
    /// </summary>
    /// <param name="context">The execution context of the attempt that just failed.</param>
    /// <param name="exception">The exception the job failed with.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>
    /// True if a retry was scheduled; false if retries are disabled or the configured maximum number of retries has
    /// been exhausted, in which case the caller is responsible for reporting the failure.
    /// </returns>
    Task<bool> ScheduleRetryAsync(IJobExecutionContext context, Exception exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a pending retry trigger for the same task, if one exists. Call this when the original (non-retry)
    /// trigger fires so a later scheduled occurrence does not also run a leftover retry. A no-op when the current
    /// execution is itself a retry.
    /// </summary>
    Task CancelPendingRetryAsync(IJobExecutionContext context, CancellationToken cancellationToken = default);
}
