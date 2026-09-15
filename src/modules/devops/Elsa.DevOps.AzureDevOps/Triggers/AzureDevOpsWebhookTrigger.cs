using Elsa.DevOps.AzureDevOps.Bookmarks;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using System.Text.Json;

namespace Elsa.DevOps.AzureDevOps.Triggers;

/// <summary>
/// Base class for Azure DevOps Service Hook triggers.
/// </summary>
public abstract class AzureDevOpsWebhookTrigger<TPayload> : Trigger<TPayload>
{
    private const string InputKey = "Message";

    /// <summary>
    /// The Azure DevOps Service Hook event type handled by this trigger.
    /// </summary>
    protected abstract string WebhookEventType { get; }

    protected virtual string? GetWorkItemId(ActivityExecutionContext context) => null;

    protected virtual string? GetWorkItemType(ActivityExecutionContext context) => null;

    protected virtual string? GetWorkItemId(TriggerIndexingContext context) => null;

    protected virtual string? GetWorkItemType(TriggerIndexingContext context) => null;

    /// <summary>
    /// The pull request this trigger listens to, or <c>null</c> to listen to every pull request in the project. Only
    /// the triggers deriving from <c>AzureDevOpsPullRequestIdentityTrigger</c> answer with a value.
    /// </summary>
    protected virtual string? GetPullRequestId(ActivityExecutionContext context) => null;

    /// <inheritdoc cref="GetPullRequestId(ActivityExecutionContext)"/>
    protected virtual string? GetPullRequestId(TriggerIndexingContext context) => null;

    /// <summary>
    /// The Azure DevOps project this trigger listens to, either its ID or its name. Falls back to the configured
    /// default project when left empty.
    /// </summary>
    [Input(Description = "The Azure DevOps project ID or name. Falls back to the configured default project (AzureDevOps:DefaultProject) when left empty.")]
    public Input<string> ProjectId { get; set; } = default!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (TryGetMessage(context, out AzureDevOpsWebhookEvent? message))
            return CompleteWithMessageAsync(context, message!);

        context.CreateBookmark(GetBookmarkPayload(context), ResumeAsync, includeActivityInstanceId: false);
        return default;
    }

    protected override object GetTriggerPayload(TriggerIndexingContext context) =>
        CreateBookmark(
            context.ExpressionExecutionContext.GetRequiredService<AzureDevOpsProjectResolver>().Resolve(context.Get(ProjectId)),
            GetWorkItemId(context),
            GetWorkItemType(context),
            GetPullRequestId(context));

    private bool TryGetMessage(ActivityExecutionContext context, out AzureDevOpsWebhookEvent? message)
    {
        if (context.WorkflowInput.TryGetValue(InputKey, out object? input) && input is AzureDevOpsWebhookEvent webhookEvent)
        {
            message = webhookEvent;
            return string.Equals(webhookEvent.EventType, WebhookEventType, StringComparison.OrdinalIgnoreCase);
        }

        message = null;
        return false;
    }

    /// <summary>
    /// Sets whatever this trigger exposes beyond <see cref="Trigger{T}.Result"/>. Called once the payload has been
    /// converted and before the activity completes.
    /// </summary>
    /// <remarks>
    /// The base trigger's job is the payload, which is the same for every event. A specific event can carry something
    /// alongside it - a comment, for instance - or want it under a name of its own, and this is where that goes,
    /// rather than by widening the payload type and breaking every workflow already bound to it.
    /// </remarks>
    /// <param name="payload">The already-converted payload, so an override does not have to deserialize it again.</param>
    protected virtual void SetAdditionalOutputs(ActivityExecutionContext context, AzureDevOpsWebhookEvent message, TPayload payload)
    {
    }

    private ValueTask CompleteWithMessageAsync(ActivityExecutionContext context, AzureDevOpsWebhookEvent message)
    {
        TPayload payload = ConvertPayload(message);

        context.Set(Result, payload);
        SetAdditionalOutputs(context, message, payload);
        context.WorkflowInput.Remove(InputKey);
        return context.CompleteActivityAsync();
    }

    /// <summary>
    /// The options the delivered payload is read with.
    /// </summary>
    /// <remarks>
    /// Case-insensitive, and that is the whole point of naming them. A Service Hook resource is camelCase and the
    /// WebApi models are PascalCase, so the default options bind nothing at all: the workflow received a work item with
    /// no id and no fields, no error anywhere, and the first sign of it was an activity downstream refusing a work item
    /// id of zero. The poller never showed it, because there the payload is already a typed object and this conversion
    /// does not run.
    /// </remarks>
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// The delivered payload as JSON, whichever shape it arrived in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The controller puts a <see cref="JsonElement"/> in the event, but a trigger that was already waiting does not
    /// receive that object. It is resumed from a bookmark, and the event reaches it through the instance state: Elsa
    /// writes the stimulus input into the instance and reads it back before the bookmark callback runs. The event
    /// survives - the record is rebuilt - but <see cref="AzureDevOpsWebhookEvent.Payload"/> is declared
    /// <c>object</c>, and the state serializer rebuilds a JSON object as an <c>ExpandoObject</c>, never as the
    /// <see cref="JsonElement"/> that went in.
    /// </para>
    /// <para>
    /// So anything that reads the payload has to go through here rather than test for a <see cref="JsonElement"/>: a
    /// type test passes on delivery and fails on resume, which is the worst of both, because the trigger that is
    /// resumed is the one a workflow was actually waiting on.
    /// </para>
    /// </remarks>
    protected static JsonElement AsJsonElement(object? payload) =>
        payload as JsonElement? ?? JsonSerializer.SerializeToElement(payload);

    /// <summary>
    /// Converts the delivered payload into the value this trigger hands to the workflow.
    /// </summary>
    /// <remarks>
    /// Overridable because an event can carry its resource in a shape of its own. A work item update, for instance,
    /// describes the update rather than the work item, and the work item is one level down - see
    /// <c>AzureDevOpsWorkItemTrigger</c>.
    /// </remarks>
    protected virtual TPayload ConvertPayload(AzureDevOpsWebhookEvent message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Payload is TPayload typedPayload)
            return typedPayload;

        TPayload? payload = AsJsonElement(message.Payload).Deserialize<TPayload>(PayloadSerializerOptions);
        return payload ?? throw new InvalidOperationException($"The Azure DevOps webhook payload for '{message.EventType}' could not be converted to {typeof(TPayload).Name}.");
    }

    private object GetBookmarkPayload(ActivityExecutionContext context) =>
        CreateBookmark(
            context.GetRequiredService<AzureDevOpsProjectResolver>().Resolve(context.Get(ProjectId)),
            GetWorkItemId(context),
            GetWorkItemType(context),
            GetPullRequestId(context));

    private AzureDevOpsWebhookBookmark CreateBookmark(string? projectId, string? workItemId, string? workItemType, string? pullRequestId)
    {
        // The project is mandatory: without it a trigger would listen to every project in the organization.
        string project = Normalize(projectId)
            ?? throw new InvalidOperationException($"ProjectId must be provided for {GetType().Name}, either on the activity or as the default project in configuration.");

        return new AzureDevOpsWebhookBookmark(WebhookEventType, project, Normalize(workItemId), Normalize(workItemType), Normalize(pullRequestId));
    }

    private ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        if (!TryGetMessage(context, out AzureDevOpsWebhookEvent? message))
            throw new InvalidOperationException($"An Azure DevOps {WebhookEventType} webhook event was not received.");

        return CompleteWithMessageAsync(context, message!);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
