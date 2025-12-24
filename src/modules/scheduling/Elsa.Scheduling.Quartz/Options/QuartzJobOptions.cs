namespace Elsa.Scheduling.Quartz.Options;

/// <summary>
/// Options for Quartz job execution behavior.
/// </summary>
public class QuartzJobOptions
{
    /// <summary>
    /// The delay in seconds before rescheduling a job after a transient failure.
    /// </summary>
    public TimeSpan TransientExceptionRetryDelay { get; set; } = TimeSpan.FromSeconds(10);
}
