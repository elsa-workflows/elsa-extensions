using Quartz;

namespace Elsa.Scheduling.Quartz;

/// <summary>
/// Conventions for Quartz trigger keys used by this module. Retry triggers keep the original schedule intact by using
/// a derived key rather than replacing the firing trigger.
/// </summary>
public static class QuartzTriggerKeys
{
    /// <summary>
    /// Suffix appended to an original trigger name to form the key of its one-shot retry trigger.
    /// </summary>
    public const string RetrySuffix = "-retry";

    /// <summary>
    /// Returns the key of the one-shot retry trigger that belongs to <paramref name="triggerKey"/>. When
    /// <paramref name="triggerKey"/> is already a retry trigger, it is returned unchanged so that subsequent retries
    /// keep the same addressable key.
    /// </summary>
    public static TriggerKey GetRetryTriggerKey(TriggerKey triggerKey)
    {
        if (IsRetryTrigger(triggerKey))
            return triggerKey;

        return new TriggerKey(triggerKey.Name + RetrySuffix, triggerKey.Group);
    }

    /// <summary>
    /// Returns whether <paramref name="triggerKey"/> is a derived retry trigger rather than the original schedule.
    /// </summary>
    public static bool IsRetryTrigger(TriggerKey triggerKey) =>
        triggerKey.Name.EndsWith(RetrySuffix, StringComparison.Ordinal);
}
