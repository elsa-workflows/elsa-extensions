namespace Elsa.DevOps.AzureDevOps.Events;

/// <summary>
/// Event types emitted by Azure DevOps Service Hooks.
/// </summary>
public static class AzureDevOpsWebhookEventTypes
{
    public const string BuildCompleted = "build.complete";
    public const string BuildStarted = "build.inProgress";
    public const string BuildQueued = "build.queued";
    public const string CodePushed = "git.push";
    public const string PullRequestCreated = "git.pullrequest.created";
    public const string PullRequestUpdated = "git.pullrequest.updated";
    public const string PullRequestMerged = "git.pullrequest.merged";
    public const string WorkItemCreated = "workitem.created";
    public const string WorkItemUpdated = "workitem.updated";
    public const string WorkItemDeleted = "workitem.deleted";

    /// <summary>
    /// The comment event, under the name this package indexes it by.
    /// </summary>
    /// <remarks>
    /// Not the name Azure DevOps sends - that is <see cref="WorkItemCommentedDelivered"/>, and the difference cost a
    /// production investigation. It stays as it is because it is what every published trigger is already indexed with
    /// and what every suspended instance is already waiting on: renaming it would silently strand both until each
    /// workflow was republished. <see cref="Normalize"/> closes the gap on the way in instead.
    /// </remarks>
    public const string WorkItemCommented = "workitem.commentedOn";

    /// <summary>
    /// What Azure DevOps actually puts in <c>eventType</c> when someone comments on a work item. The subscription is
    /// labelled "Work item commented on" in the portal, which is where the name below came from; the delivery says
    /// <c>workitem.commented</c>.
    /// </summary>
    public const string WorkItemCommentedDelivered = "workitem.commented";

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        [WorkItemCommentedDelivered] = WorkItemCommented
    };

    /// <summary>
    /// Returns the name this package knows an event type by, given whatever Azure DevOps called it.
    /// </summary>
    /// <remarks>
    /// Applied once, where the two routes into this package meet, so that everything downstream - the trigger lookup,
    /// the bookmarks, and the event handed to the workflow - agrees on one spelling. Normalizing at only some of those
    /// is worse than not normalizing at all: the stimulus then reaches a trigger that compares the event to its own
    /// name, finds a different one, and suspends instead of running.
    /// </remarks>
    public static string Normalize(string eventType) =>
        eventType != null && Aliases.TryGetValue(eventType, out string? canonical) ? canonical : eventType!;

    public static IReadOnlyCollection<string> All { get; } =
    [
        BuildCompleted,
        BuildStarted,
        BuildQueued,
        CodePushed,
        PullRequestCreated,
        PullRequestUpdated,
        PullRequestMerged,
        WorkItemCreated,
        WorkItemUpdated,
        WorkItemDeleted,
        WorkItemCommented
    ];
}
