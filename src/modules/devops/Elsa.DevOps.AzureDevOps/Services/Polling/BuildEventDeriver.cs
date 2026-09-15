using Elsa.DevOps.AzureDevOps.Events;
using Microsoft.TeamFoundation.Build.WebApi;

namespace Elsa.DevOps.AzureDevOps.Services.Polling;

/// <summary>
/// One build event that polling found, with the timestamp its checkpoint advances to.
/// </summary>
public sealed record BuildPollingEvent(string EventType, Build Build, DateTimeOffset Timestamp);

/// <summary>
/// Turns the three build queries of one poll into the events to dispatch. Kept free of the API client so the rules —
/// which timestamp belongs to which event, and what counts as new — can be tested without a connection.
/// </summary>
public static class BuildEventDeriver
{
    /// <summary>
    /// Returns the events to dispatch, oldest first, for one poll. The three inputs are the results of the queued,
    /// started and finished queries; the same build legitimately appears in more than one of them, and even twice in
    /// the same one — the dispatcher merges its in-flight and completed queries into the queued and started
    /// candidates, so a build finishing between those two reads can be handed in from both.
    /// </summary>
    public static IReadOnlyList<BuildPollingEvent> Derive(
        IEnumerable<Build> queued,
        IEnumerable<Build> started,
        IEnumerable<Build> finished,
        BuildPollingCheckpoints checkpoints)
    {
        ArgumentNullException.ThrowIfNull(queued);
        ArgumentNullException.ThrowIfNull(started);
        ArgumentNullException.ThrowIfNull(finished);
        ArgumentNullException.ThrowIfNull(checkpoints);

        List<BuildPollingEvent> events = [];

        Collect(events, queued, AzureDevOpsWebhookEventTypes.BuildQueued, checkpoints.LastQueued, build => build.QueueTime);
        Collect(events, started, AzureDevOpsWebhookEventTypes.BuildStarted, checkpoints.LastStarted, build => build.StartTime);
        Collect(events, finished, AzureDevOpsWebhookEventTypes.BuildCompleted, checkpoints.LastFinished, build => build.FinishTime);

        // Ordered so that a failure part-way through leaves every checkpoint at the last event actually dispatched,
        // then deduplicated by (event type, build) so a build fed in from more than one candidate list is only
        // dispatched once.
        return [.. events
            .OrderBy(@event => @event.Timestamp)
            .ThenBy(@event => Rank(@event.EventType))
            .GroupBy(@event => (@event.EventType, @event.Build.Id))
            .Select(group => group.First())];
    }

    /// <summary>
    /// Moves the checkpoint belonging to the event that was just dispatched. Applied per event rather than per poll,
    /// so a failure keeps the events already handled from being repeated.
    /// </summary>
    public static void Advance(BuildPollingCheckpoints checkpoints, BuildPollingEvent @event)
    {
        ArgumentNullException.ThrowIfNull(checkpoints);
        ArgumentNullException.ThrowIfNull(@event);

        switch (@event.EventType)
        {
            case AzureDevOpsWebhookEventTypes.BuildQueued:
                if (@event.Timestamp > checkpoints.LastQueued)
                    checkpoints.LastQueued = @event.Timestamp;
                break;
            case AzureDevOpsWebhookEventTypes.BuildStarted:
                if (@event.Timestamp > checkpoints.LastStarted)
                    checkpoints.LastStarted = @event.Timestamp;
                break;
            case AzureDevOpsWebhookEventTypes.BuildCompleted:
                if (@event.Timestamp > checkpoints.LastFinished)
                    checkpoints.LastFinished = @event.Timestamp;
                break;
        }
    }

    private static void Collect(
        List<BuildPollingEvent> events,
        IEnumerable<Build> builds,
        string eventType,
        DateTimeOffset checkpoint,
        Func<Build, DateTime?> timestampOf)
    {
        foreach (Build build in builds)
        {
            DateTimeOffset? timestamp = ToUtc(timestampOf(build));

            // Strictly greater: minTime is inclusive server-side, so the newest build of a poll returns on the next.
            if (timestamp is { } value && value > checkpoint)
                events.Add(new BuildPollingEvent(eventType, build, value));
        }
    }

    /// <summary>
    /// Orders the events of a single build the way they happened, for the case where all three share a timestamp.
    /// </summary>
    private static int Rank(string eventType) => eventType switch
    {
        AzureDevOpsWebhookEventTypes.BuildQueued => 0,
        AzureDevOpsWebhookEventTypes.BuildStarted => 1,
        _ => 2
    };

    private static DateTimeOffset? ToUtc(DateTime? value) =>
        value == null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
}
