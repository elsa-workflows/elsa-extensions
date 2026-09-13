namespace Elsa.Scheduling.Quartz;

/// <summary>
/// Well-known keys written by this module into a Quartz <see cref="global::Quartz.JobDataMap"/>.
/// </summary>
public static class QuartzJobDataKeys
{
    /// <summary>
    /// The key used to mark a derived retry trigger. This marker is required because a caller may legitimately choose
    /// an original task name that ends with the retry suffix.
    /// </summary>
    public const string RetryTrigger = "Elsa.Scheduling.Quartz:RetryTrigger";

    /// <summary>
    /// The original trigger name carried by a retry trigger so later retries can reuse the same derived identity.
    /// </summary>
    public const string RetryOriginalTriggerName = "Elsa.Scheduling.Quartz:RetryOriginalTriggerName";

    /// <summary>
    /// The original trigger group carried by a retry trigger so later retries can reuse the same derived identity.
    /// </summary>
    public const string RetryOriginalTriggerGroup = "Elsa.Scheduling.Quartz:RetryOriginalTriggerGroup";

    /// <summary>
    /// The key under which the one-based retry attempt number is stored on a retry trigger. The value is stored
    /// as a string so that job stores configured with <c>quartz.jobStore.useProperties</c> can persist it.
    /// </summary>
    public const string RetryAttempt = "Elsa.Scheduling.Quartz:RetryAttempt";
}
