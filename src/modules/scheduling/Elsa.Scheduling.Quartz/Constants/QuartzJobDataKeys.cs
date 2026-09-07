namespace Elsa.Scheduling.Quartz;

/// <summary>
/// Well-known keys written by this module into a Quartz <see cref="global::Quartz.JobDataMap"/>.
/// </summary>
public static class QuartzJobDataKeys
{
    /// <summary>
    /// The key under which the one-based retry attempt number is stored on a rescheduled trigger. The value is stored
    /// as a string so that job stores configured with <c>quartz.jobStore.useProperties</c> can persist it.
    /// </summary>
    public const string RetryAttempt = "Elsa.Scheduling.Quartz:RetryAttempt";
}
