using System.Security.Cryptography;
using System.Text;
using Quartz;

namespace Elsa.Scheduling.Quartz;

/// <summary>
/// Conventions for Quartz trigger keys used by this module. Retry triggers keep the original schedule intact by using
/// a derived key rather than replacing the firing trigger. A reserved group and hash-based name keep retry identities
/// separate from caller-controlled task names, while the trigger data marker identifies retry triggers.
/// </summary>
public static class QuartzTriggerKeys
{
    /// <summary>
    /// Reserved Quartz group for one-shot retry triggers.
    /// </summary>
    public const string RetryGroup = "Elsa.Scheduling.Quartz:Retries";

    /// <summary>
    /// Returns the key of the one-shot retry trigger that belongs to the original <paramref name="triggerKey"/>. The
    /// key does not expose caller-controlled names and is distinct from every ordinary trigger in its tenant group.
    /// Legacy schedules without a generation token use the original deterministic key for compatibility.
    /// </summary>
    public static TriggerKey GetRetryTriggerKey(TriggerKey triggerKey)
        => GetRetryTriggerKey(triggerKey, QuartzJobDataKeys.LegacyScheduleGeneration);

    /// <summary>
    /// Returns the retry key for a specific schedule generation. Including the generation keeps an old retry from
    /// being able to remove a newer retry after an unschedule+reschedule of the same original trigger key.
    /// </summary>
    public static TriggerKey GetRetryTriggerKey(TriggerKey triggerKey, string? scheduleGeneration)
    {
        var identity = $"{triggerKey.Group}\u001F{triggerKey.Name}";
        if (!string.IsNullOrWhiteSpace(scheduleGeneration) && !string.Equals(scheduleGeneration, QuartzJobDataKeys.LegacyScheduleGeneration, StringComparison.Ordinal))
            identity += $"\u001F{scheduleGeneration}";

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))).ToLowerInvariant();
        return new TriggerKey($"retry-{hash}", RetryGroup);
    }

    /// <summary>
    /// Returns whether <paramref name="trigger"/> is a derived retry trigger rather than the original schedule.
    /// The explicit marker avoids confusing an ordinary trigger with a retry trigger.
    /// </summary>
    public static bool IsRetryTrigger(ITrigger trigger)
    {
        if (!trigger.JobDataMap.TryGetValue(QuartzJobDataKeys.RetryTrigger, out var value))
            return false;

        return value switch
        {
            bool boolValue => boolValue,
            string stringValue => bool.TryParse(stringValue, out var parsedValue) && parsedValue,
            _ => false
        };
    }

    /// <summary>
    /// Returns the original schedule's key for <paramref name="trigger"/> using the original name and group persisted
    /// in the retry trigger's data.
    /// </summary>
    public static TriggerKey GetOriginalTriggerKey(ITrigger trigger)
    {
        var triggerKey = trigger.Key;

        if (!IsRetryTrigger(trigger))
            return triggerKey;

        if (!trigger.JobDataMap.TryGetValue(QuartzJobDataKeys.RetryOriginalTriggerName, out var name) || name is not string triggerName)
            return triggerKey;

        if (!trigger.JobDataMap.TryGetValue(QuartzJobDataKeys.RetryOriginalTriggerGroup, out var group) || group is not string triggerGroup)
            return triggerKey;

        return new TriggerKey(triggerName, triggerGroup);
    }
}
