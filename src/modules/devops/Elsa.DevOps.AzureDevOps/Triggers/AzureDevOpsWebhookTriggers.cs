using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Models;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System.Globalization;

namespace Elsa.DevOps.AzureDevOps.Triggers;

[Activity("Elsa.AzureDevOps.Builds", "Azure DevOps Builds", "Wait for a completed Azure DevOps build.", DisplayName = "Build Completed", Kind = ActivityKind.Trigger)]
public sealed class BuildCompletedTrigger : AzureDevOpsWebhookTrigger<Build> { protected override string WebhookEventType => AzureDevOpsWebhookEventTypes.BuildCompleted; }

[Activity("Elsa.AzureDevOps.Builds", "Azure DevOps Builds", "Wait for an Azure DevOps build in progress.", DisplayName = "Build In Progress", Kind = ActivityKind.Trigger)]
public sealed class BuildInProgressTrigger : AzureDevOpsWebhookTrigger<Build> { protected override string WebhookEventType => AzureDevOpsWebhookEventTypes.BuildStarted; }

[Activity("Elsa.AzureDevOps.Builds", "Azure DevOps Builds", "Wait for a queued Azure DevOps build.", DisplayName = "Build Queued", Kind = ActivityKind.Trigger)]
public sealed class BuildQueuedTrigger : AzureDevOpsWebhookTrigger<Build> { protected override string WebhookEventType => AzureDevOpsWebhookEventTypes.BuildQueued; }

[Activity("Elsa.AzureDevOps.Repositories", "Azure DevOps Repositories", "Wait for an Azure Repos push.", DisplayName = "Code Pushed", Kind = ActivityKind.Trigger)]
public sealed class CodePushedTrigger : AzureDevOpsWebhookTrigger<JsonElement> { protected override string WebhookEventType => AzureDevOpsWebhookEventTypes.CodePushed; }

[Activity("Elsa.AzureDevOps.PullRequests", "Azure DevOps Pull Requests", "Wait for a created Azure DevOps pull request.", DisplayName = "Pull Request Created", Kind = ActivityKind.Trigger)]
public sealed class PullRequestCreatedTrigger : AzureDevOpsWebhookTrigger<GitPullRequest> { protected override string WebhookEventType => AzureDevOpsWebhookEventTypes.PullRequestCreated; }

/// <summary>
/// Base class for the pull request triggers that can be restricted to one pull request.
/// </summary>
/// <remarks>
/// <para>
/// Only Updated and Merged offer the filter. Created does not, because there is nothing to wait for: a workflow cannot
/// know the id of a pull request that does not exist yet, and a filter no trigger can be configured with would only
/// add a stimulus per delivery that never matches.
/// </para>
/// <para>
/// Without the filter, a workflow waiting for the pull request it had just created was resumed by every merge in the
/// project, and by then the trigger had completed - which is not a state a workflow can go back to waiting from. A
/// flow decision downstream cannot repair that; the filter has to be in the bookmark, which is what an arriving
/// delivery is matched against.
/// </para>
/// </remarks>
public abstract class AzureDevOpsPullRequestIdentityTrigger : AzureDevOpsWebhookTrigger<GitPullRequest>
{
    [Input(Description = "Optional Azure DevOps pull request ID filter. Leave empty to listen to every pull request in the project.")]
    public Input<int?> PullRequestId { get; set; } = default!;

    /// <inheritdoc />
    protected override string? GetPullRequestId(ActivityExecutionContext context) =>
        context.Get(PullRequestId)?.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override string? GetPullRequestId(TriggerIndexingContext context) =>
        context.Get(PullRequestId)?.ToString(CultureInfo.InvariantCulture);
}

[Activity("Elsa.AzureDevOps.PullRequests", "Azure DevOps Pull Requests", "Wait for an updated Azure DevOps pull request.", DisplayName = "Pull Request Updated", Kind = ActivityKind.Trigger)]
public sealed class PullRequestUpdatedTrigger : AzureDevOpsPullRequestIdentityTrigger { protected override string WebhookEventType => AzureDevOpsWebhookEventTypes.PullRequestUpdated; }

[Activity("Elsa.AzureDevOps.PullRequests", "Azure DevOps Pull Requests", "Wait for a merged Azure DevOps pull request.", DisplayName = "Pull Request Merged", Kind = ActivityKind.Trigger)]
public sealed class PullRequestMergedTrigger : AzureDevOpsPullRequestIdentityTrigger { protected override string WebhookEventType => AzureDevOpsWebhookEventTypes.PullRequestMerged; }

[Activity("Elsa.AzureDevOps.WorkItems", "Azure DevOps Work Items", "Wait for a created Azure DevOps work item.", DisplayName = "Work Item Created", Kind = ActivityKind.Trigger)]
public abstract class AzureDevOpsWorkItemTrigger : AzureDevOpsWebhookTrigger<WorkItem>
{
    [Input(Description = "Optional Azure DevOps work item type filter.")]
    public Input<string?> WorkItemType { get; set; } = default!;

    /// <summary>
    /// The work item the event is about.
    /// </summary>
    /// <remarks>
    /// The same value as <see cref="Trigger{T}.Result"/>, under a name that says what it is. <c>Result</c> comes from
    /// the base activity and cannot be renamed without orphaning every workflow definition already bound to it, so it
    /// keeps being set and this is offered next to it.
    /// </remarks>
    [Output(Description = "The work item the event is about. The same value as Result, under a name that says what it is.")]
    public Output<WorkItem> WorkItem { get; set; } = default!;

    /// <summary>
    /// The state of the work item, as a plain string.
    /// </summary>
    /// <remarks>
    /// Offered next to <see cref="WorkItem"/> because a workflow binding cannot navigate into the work item's field
    /// dictionary; a decision that waits for a state needs it as a value.
    /// </remarks>
    [Output(Description = "The state of the work item, for example Active or Closed.")]
    public Output<string> State { get; set; } = default!;

    protected override string? GetWorkItemType(ActivityExecutionContext context) => context.Get(WorkItemType);

    protected override string? GetWorkItemType(TriggerIndexingContext context) => context.Get(WorkItemType);

    /// <summary>
    /// Finds the work item in the delivered resource, which is not always the resource itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <c>workitem.updated</c> delivery describes the update: its <c>id</c> is the revision number, its <c>fields</c>
    /// hold only what changed and hold it as <c>oldValue</c>/<c>newValue</c> pairs rather than as values, and the work
    /// item itself sits under <c>revision</c>. Reading the resource directly therefore yields a work item numbered
    /// after the revision - 7 rather than 36018 - with a field dictionary no workflow can use. Created, deleted and
    /// commented deliveries do carry the work item as the resource, and fall through unchanged.
    /// </para>
    /// <para>
    /// The poller is unaffected either way: it hands over a real <see cref="WorkItem"/> that it read from the API, so
    /// none of this runs. That asymmetry is why the shape went unnoticed until a webhook actually reached a workflow.
    /// </para>
    /// <para>
    /// The resource is read through <see cref="AzureDevOpsWebhookTrigger{TPayload}.AsJsonElement"/> and not by testing
    /// for a <see cref="JsonElement"/>. A trigger that was already waiting is resumed from the instance state, which
    /// hands the payload back as an expando; a type test therefore held on the delivery that started a workflow and
    /// failed on the delivery that resumed one, and everything below was skipped for exactly the events a workflow was
    /// waiting on.
    /// </para>
    /// </remarks>
    protected override WorkItem ConvertPayload(AzureDevOpsWebhookEvent message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Payload is WorkItem polled)
            return polled;

        JsonElement resource = AsJsonElement(message.Payload);

        if (resource.ValueKind != JsonValueKind.Object)
            return base.ConvertPayload(message);

        bool hasRevision = resource.TryGetProperty("revision", out JsonElement revision) && revision.ValueKind == JsonValueKind.Object;
        WorkItem workItem = base.ConvertPayload(message with { Payload = WithoutUpdateRelations(hasRevision ? revision : resource) });

        // Overwritten rather than filled in when there is no revision to read: the id on an update resource is not
        // missing, it is the revision number, so leaving it would hand the workflow work item 7 instead of 36018. A
        // revision is already numbered after the work item, and is only corrected if it somehow carried no id at all.
        if (ReadWorkItemId(resource) is { } workItemId && (!hasRevision || workItem.Id == null))
            workItem.Id = workItemId;

        return workItem;
    }

    /// <summary>
    /// Drops a <c>relations</c> that describes a change rather than a work item's relations.
    /// </summary>
    /// <remarks>
    /// A work item spells <c>relations</c> as a list of links. An update resource spells it as an
    /// added/removed/updated object, and there is no revision to read it from when the subscription sends minimal
    /// resource details - so on those deliveries the update itself is what gets read as a work item, and a serializer
    /// asked for a list of links given an object stops the conversion dead. It threw for the whole payload, which is
    /// why an update that moved a link produced no work item at all rather than a work item without its links.
    /// Dropped rather than mapped: what a revision added is not what the work item now has, and a workflow that reads
    /// <c>Relations</c> should see nothing before it sees a partial answer it cannot tell apart from a complete one.
    /// </remarks>
    private static JsonElement WithoutUpdateRelations(JsonElement resource)
    {
        if (!resource.TryGetProperty("relations", out JsonElement relations) || relations.ValueKind is JsonValueKind.Array or JsonValueKind.Null)
            return resource;

        JsonObject? copy = JsonSerializer.Deserialize<JsonObject>(resource);

        if (copy == null)
            return resource;

        copy.Remove("relations");
        return JsonSerializer.SerializeToElement(copy);
    }

    private static int? ReadWorkItemId(JsonElement resource) =>
        resource.TryGetProperty("workItemId", out JsonElement workItemId)
        && workItemId.ValueKind == JsonValueKind.Number
        && workItemId.TryGetInt32(out int value)
            ? value
            : null;

    /// <inheritdoc />
    protected override void SetAdditionalOutputs(ActivityExecutionContext context, AzureDevOpsWebhookEvent message, WorkItem payload)
    {
        ArgumentNullException.ThrowIfNull(context);

        base.SetAdditionalOutputs(context, message, payload);
        context.Set(WorkItem, payload);

        // An empty string rather than null, so a flow decision comparing the state does not have to guard for null
        // before it can compare.
        context.Set(State, WorkItemFieldReader.Text(payload, "System.State") ?? string.Empty);
    }
}

public abstract class AzureDevOpsWorkItemIdentityTrigger : AzureDevOpsWorkItemTrigger
{
    [Input(Description = "Optional Azure DevOps work item ID filter.")]
    public Input<int?> WorkItemId { get; set; } = default!;

    protected override string? GetWorkItemId(ActivityExecutionContext context) =>
        context.Get(WorkItemId)?.ToString(CultureInfo.InvariantCulture);

    protected override string? GetWorkItemId(TriggerIndexingContext context) =>
        context.Get(WorkItemId)?.ToString(CultureInfo.InvariantCulture);
}

[Activity("Elsa.AzureDevOps.WorkItems", "Azure DevOps Work Items", "Wait for a created Azure DevOps work item.", DisplayName = "Work Item Created", Kind = ActivityKind.Trigger)]
public sealed class WorkItemCreatedTrigger : AzureDevOpsWorkItemTrigger { protected override string WebhookEventType => AzureDevOpsWebhookEventTypes.WorkItemCreated; }

[Activity("Elsa.AzureDevOps.WorkItems", "Azure DevOps Work Items", "Wait for an updated Azure DevOps work item.", DisplayName = "Work Item Updated", Kind = ActivityKind.Trigger)]
public sealed class WorkItemUpdatedTrigger : AzureDevOpsWorkItemIdentityTrigger { protected override string WebhookEventType => AzureDevOpsWebhookEventTypes.WorkItemUpdated; }

[Activity("Elsa.AzureDevOps.WorkItems", "Azure DevOps Work Items", "Wait for a deleted Azure DevOps work item.", DisplayName = "Work Item Deleted", Kind = ActivityKind.Trigger)]
public sealed class WorkItemDeletedTrigger : AzureDevOpsWorkItemIdentityTrigger { protected override string WebhookEventType => AzureDevOpsWebhookEventTypes.WorkItemDeleted; }

/// <summary>
/// Waits for a comment on an Azure DevOps work item, and hands over both the work item and what was said.
/// </summary>
/// <remarks>
/// The comment travels alongside the work item rather than replacing it as the result, so a workflow already bound to
/// the work item keeps working. Which source it comes from depends on how the event arrived: the poller has read the
/// real comment through the API and knows its id, a Service Hook carries only what is in the payload. See
/// <see cref="WorkItemCommentReader"/>.
/// </remarks>
[Activity("Elsa.AzureDevOps.WorkItems", "Azure DevOps Work Items", "Wait for a comment on an Azure DevOps work item.", DisplayName = "Work Item Commented", Kind = ActivityKind.Trigger)]
public sealed class WorkItemCommentedTrigger : AzureDevOpsWorkItemIdentityTrigger
{
    protected override string WebhookEventType => AzureDevOpsWebhookEventTypes.WorkItemCommented;

    /// <summary>
    /// The text of the comment.
    /// </summary>
    /// <remarks>
    /// A separate output from <see cref="Comment"/> on purpose: matching on what was said is the common case - a
    /// mention, a keyword, a command - and a plain string is what a flow decision and an expression can work with
    /// most easily.
    /// </remarks>
    [Output(Description = "The text of the comment, as HTML. Empty when the event carried no comment text.")]
    public Output<string> CommentText { get; set; } = null!;

    /// <summary>
    /// The comment, with whatever the source knew about it.
    /// </summary>
    [Output(Description = "The comment, with its author and timestamp. The comment id is only filled in when the event came from polling; a Service Hook payload does not carry one.")]
    public Output<PostedComment?> Comment { get; set; } = null!;

    /// <inheritdoc />
    protected override void SetAdditionalOutputs(ActivityExecutionContext context, AzureDevOpsWebhookEvent message, WorkItem payload)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);

        base.SetAdditionalOutputs(context, message, payload);

        // What the source already read wins over what can be recovered from the payload: the poller's comment carries
        // the id and the sign-in name, which the payload does not have.
        PostedComment? comment = message.Comment ?? WorkItemCommentReader.FromPayload(message.Payload);

        context.Set(Comment, comment);
        context.Set(CommentText, comment?.Text ?? string.Empty);
    }
}
