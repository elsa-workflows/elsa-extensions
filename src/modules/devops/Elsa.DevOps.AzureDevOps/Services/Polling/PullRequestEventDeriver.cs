using Elsa.DevOps.AzureDevOps.Events;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.Services.Polling;

/// <summary>
/// One pull request event that polling found, with the timestamp its checkpoint advances to.
/// </summary>
public sealed record PullRequestPollingEvent(string EventType, GitPullRequest PullRequest, DateTimeOffset Timestamp);

/// <summary>
/// Turns the two pull request queries of one poll into the events to dispatch. Kept free of the API client so the
/// rules can be tested without a connection.
/// </summary>
public static class PullRequestEventDeriver
{
    /// <summary>
    /// Returns the events to dispatch, oldest first. <paramref name="created"/> holds the result of the creation
    /// query and <paramref name="closed"/> that of the closure query; a pull request opened and completed inside one
    /// interval appears in both.
    /// </summary>
    public static IReadOnlyList<PullRequestPollingEvent> Derive(
        IEnumerable<GitPullRequest> created,
        IEnumerable<GitPullRequest> closed,
        PullRequestPollingCheckpoints checkpoints)
    {
        ArgumentNullException.ThrowIfNull(created);
        ArgumentNullException.ThrowIfNull(closed);
        ArgumentNullException.ThrowIfNull(checkpoints);

        List<PullRequestPollingEvent> events = [];

        foreach (GitPullRequest pullRequest in created)
        {
            DateTimeOffset creationDate = ToUtc(pullRequest.CreationDate);

            if (creationDate > checkpoints.LastCreated)
                events.Add(new PullRequestPollingEvent(AzureDevOpsWebhookEventTypes.PullRequestCreated, pullRequest, creationDate));
        }

        foreach (GitPullRequest pullRequest in closed)
        {
            DateTimeOffset closedDate = ToUtc(pullRequest.ClosedDate);

            // Abandoning closes a pull request too, so the status is what separates a merge from a withdrawal.
            if (pullRequest.Status == PullRequestStatus.Completed && closedDate > checkpoints.LastClosed)
                events.Add(new PullRequestPollingEvent(AzureDevOpsWebhookEventTypes.PullRequestMerged, pullRequest, closedDate));
        }

        return [.. events.OrderBy(@event => @event.Timestamp)];
    }

    /// <summary>
    /// Moves the checkpoint belonging to the event that was just dispatched. Applied per event rather than per poll,
    /// so a failure keeps the events already handled from being repeated.
    /// </summary>
    public static void Advance(PullRequestPollingCheckpoints checkpoints, PullRequestPollingEvent @event)
    {
        ArgumentNullException.ThrowIfNull(checkpoints);
        ArgumentNullException.ThrowIfNull(@event);

        if (@event.EventType == AzureDevOpsWebhookEventTypes.PullRequestCreated)
        {
            if (@event.Timestamp > checkpoints.LastCreated)
                checkpoints.LastCreated = @event.Timestamp;
        }
        else if (@event.Timestamp > checkpoints.LastClosed)
        {
            checkpoints.LastClosed = @event.Timestamp;
        }
    }

    private static DateTimeOffset ToUtc(DateTime value) =>
        value == default ? DateTimeOffset.MinValue : new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
