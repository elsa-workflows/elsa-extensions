using System.Globalization;
using System.Text.Json;
using Elsa.Common.Multitenancy;
using Elsa.DevOps.AzureDevOps.Bookmarks;
using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Converts an Azure DevOps Service Hook event into an Elsa stimulus.
/// </summary>
/// <param name="options">
/// Supplies the webhook diagnostics settings. Optional so the handler can still be constructed with nothing but what
/// it needs to do its work; without it the diagnostics stay at their defaults and write nothing.
/// </param>
/// <param name="triggerStore">The indexed triggers, read only to report what a delivery could have matched.</param>
/// <param name="bookmarkStore">The waiting bookmarks, read only to report what a delivery could have matched.</param>
/// <param name="tenantAccessor">
/// The tenant this delivery is being handled under, reported alongside the stores because it is what decides which
/// rows those stores will admit to holding.
/// </param>
public sealed class AzureDevOpsWebhookEventHandler(
    IStimulusSender stimulusSender,
    ILogger<AzureDevOpsWebhookEventHandler> logger,
    IOptions<AzureDevOpsOptions>? options = null,
    ITriggerStore? triggerStore = null,
    IBookmarkStore? bookmarkStore = null,
    ITenantAccessor? tenantAccessor = null)
{
    private const string WorkItemTypeField = "System.WorkItemType";
    private const string TeamProjectField = "System.TeamProject";

    /// <summary>
    /// The trigger every event type is dispatched to. A table rather than a switch, because the diagnostics below need
    /// the same set of trigger names to ask the store what it can see at all.
    /// </summary>
    private static readonly Dictionary<string, Type> TriggerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [AzureDevOpsWebhookEventTypes.BuildCompleted] = typeof(Triggers.BuildCompletedTrigger),
        [AzureDevOpsWebhookEventTypes.BuildStarted] = typeof(Triggers.BuildInProgressTrigger),
        [AzureDevOpsWebhookEventTypes.BuildQueued] = typeof(Triggers.BuildQueuedTrigger),
        [AzureDevOpsWebhookEventTypes.CodePushed] = typeof(Triggers.CodePushedTrigger),
        [AzureDevOpsWebhookEventTypes.PullRequestCreated] = typeof(Triggers.PullRequestCreatedTrigger),
        [AzureDevOpsWebhookEventTypes.PullRequestUpdated] = typeof(Triggers.PullRequestUpdatedTrigger),
        [AzureDevOpsWebhookEventTypes.PullRequestMerged] = typeof(Triggers.PullRequestMergedTrigger),
        [AzureDevOpsWebhookEventTypes.WorkItemCreated] = typeof(Triggers.WorkItemCreatedTrigger),
        [AzureDevOpsWebhookEventTypes.WorkItemUpdated] = typeof(Triggers.WorkItemUpdatedTrigger),
        [AzureDevOpsWebhookEventTypes.WorkItemDeleted] = typeof(Triggers.WorkItemDeletedTrigger),
        [AzureDevOpsWebhookEventTypes.WorkItemCommented] = typeof(Triggers.WorkItemCommentedTrigger)
    };

    private static readonly string[] AllTriggerActivityTypeNames =
        [.. TriggerTypes.Values.Select(ActivityTypeNameHelper.GenerateTypeName).Distinct().Order()];

    public async Task HandleAsync(AzureDevOpsWebhookEvent message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Before anything reads the type, including the copy handed to the workflow: the trigger checks the event
        // against its own name once it is resumed, so an event that reached it under a name it does not recognise
        // suspends the instance it was supposed to complete.
        message = message with { EventType = AzureDevOpsWebhookEventTypes.Normalize(message.EventType) };

        Type triggerType = TriggerTypes.TryGetValue(message.EventType, out Type? mappedTriggerType)
            ? mappedTriggerType
            : throw new InvalidOperationException($"Unsupported Azure DevOps webhook event type '{message.EventType}'.");
        string activityTypeName = ActivityTypeNameHelper.GenerateTypeName(triggerType);
        Dictionary<string, object> input = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Message"] = message
        };
        StimulusMetadata stimulusMetadata = new() { Input = input };
        IReadOnlyCollection<AzureDevOpsWebhookBookmark> bookmarks = GetBookmarks(message);

        if (bookmarks.Count == 0)
        {
            logger.LogWarning(
                "Azure DevOps {EventType} event carries no project ID or name, so no trigger can match it. No stimulus was sent.",
                message.EventType);
            return;
        }

        logger.LogInformation(
            "Azure DevOps {EventType} event received; sending {BookmarkCount} stimuli for {ActivityTypeName}.",
            message.EventType,
            bookmarks.Count,
            activityTypeName);

        await LogWhatIsWaitingAsync(activityTypeName, bookmarks, cancellationToken).ConfigureAwait(false);

        foreach (AzureDevOpsWebhookBookmark bookmark in bookmarks)
        {
            logger.LogDebug("Sending stimulus {ActivityTypeName} for bookmark {Bookmark}.", activityTypeName, bookmark);
            await stimulusSender.SendAsync(activityTypeName, bookmark, stimulusMetadata, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes the bookmarks this event produced alongside the ones the stores actually hold for the same activity, so
    /// a delivery that started nothing can be read off a single log line instead of guessed at.
    /// </summary>
    /// <remarks>
    /// A stimulus that matches nothing is silent by design - nothing has gone wrong, there is simply no workflow
    /// waiting - and that silence is indistinguishable from a workflow that failed to start. The two sides printed
    /// next to each other are what tells them apart: equal values on both sides mean something should have run, and a
    /// project ID on one side against a project name on the other is the mismatch that explains the silence.
    /// </remarks>
    private async Task LogWhatIsWaitingAsync(
        string activityTypeName,
        IReadOnlyCollection<AzureDevOpsWebhookBookmark> bookmarks,
        CancellationToken cancellationToken)
    {
        LogLevel level = options?.Value.WebhookDiagnostics.Level ?? new AzureDevOpsWebhookDiagnosticsOptions().Level;

        // Guarded rather than always read: this is two extra store queries per delivered event, which is not a price
        // worth paying when nobody is reading the result.
        if (!logger.IsEnabled(level) || triggerStore == null || bookmarkStore == null)
            return;

        try
        {
            IEnumerable<StoredTrigger> triggers = await triggerStore
                .FindManyAsync(new TriggerFilter { Names = [activityTypeName] }, cancellationToken)
                .ConfigureAwait(false);
            IEnumerable<StoredBookmark> waiting = await bookmarkStore
                .FindManyAsync(new BookmarkFilter { Names = [activityTypeName] }, cancellationToken)
                .ConfigureAwait(false);

            // The control. Empty results above have two opposite meanings that look identical: nothing is published
            // for this event, or this caller cannot see into the store at all - a tenant that did not resolve, say -
            // in which case the match was never possible however much is published. Asking the same store, from the
            // same scope, for every trigger this package owns separates the two: seeing another package trigger and
            // not this one settles it as nothing published, and seeing none of the eleven settles it as the scope.
            // Filtered by name rather than asked for everything, because that filter is the one proven to work here.
            StoredTrigger[] anyOfOurs =
            [
                .. await triggerStore
                    .FindManyAsync(new TriggerFilter { Names = [.. AllTriggerActivityTypeNames] }, cancellationToken)
                    .ConfigureAwait(false)
            ];

            logger.Log(
                level,
                "Azure DevOps {ActivityTypeName} under tenant {Tenant}: this event produced [{Produced}]; the store holds indexed triggers [{IndexedTriggers}] and waiting bookmarks [{WaitingBookmarks}] for it. From this same scope, the store holds {OurTriggerCount} Azure DevOps trigger(s) in total, named [{OurTriggerNames}]. A stimulus only reaches a workflow when one of the produced entries matches one of those. A total of zero is a different problem from nothing being published for this event, and comparing the tenant against the one a poll reports is what tells the two apart.",
                activityTypeName,
                DescribeTenant(),
                Describe(bookmarks),
                Describe(triggers.Select(trigger => trigger.Payload)),
                Describe(waiting.Select(bookmark => bookmark.Payload)),
                anyOfOurs.Length,
                anyOfOurs.Length == 0 ? "none" : string.Join(", ", anyOfOurs.Select(trigger => trigger.Name).Distinct().Order()));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Diagnostics must never be the reason a delivery fails: the stimuli below are the actual work.
            logger.LogWarning(ex, "Reporting what is waiting on {ActivityTypeName} failed; the event itself is unaffected.", activityTypeName);
        }
    }

    /// <summary>
    /// Names the tenant the current scope is on, spelling out the two values that mean something other than a tenant.
    /// </summary>
    /// <remarks>
    /// The empty string is Elsa's default tenant, not a failed resolution - which is worth saying in the log, because
    /// "Tenant with ID '' was resolved but could not be found in the tenant store" appears on every request in a
    /// deployment with no tenant records and reads like a fault. The value that would explain a store seeing different
    /// rows from one scope to the next is a real tenant id here against the default or agnostic one elsewhere.
    /// </remarks>
    private string DescribeTenant()
    {
        // Written as comparisons rather than as switch patterns: these are static fields on Elsa's Tenant type, and a
        // pattern would need them to be compile-time constants.
        string? tenantId = tenantAccessor?.TenantId;

        if (tenantId == null)
            return "(none resolved)";

        if (tenantId == Tenant.DefaultTenantId)
            return "(default)";

        return tenantId == Tenant.AgnosticTenantId ? "(agnostic, sees every tenant)" : $"'{tenantId}'";
    }

    private static string Describe(IEnumerable<object?> payloads)
    {
        string[] described = [.. payloads.Select(Describe)];
        return described.Length == 0 ? "none" : string.Join("; ", described);
    }

    private static string Describe(object? payload) => payload switch
    {
        null => "(null)",
        // The stores hand back the typed record when they can deserialize it and the raw JSON when they cannot, and
        // which of the two it is matters: an unreadable payload cannot match either.
        AzureDevOpsWebhookBookmark bookmark =>
            $"{bookmark.EventType} project '{bookmark.ProjectId}' id {bookmark.WorkItemId ?? "(any)"} type {bookmark.WorkItemType ?? "(any)"} pull request {bookmark.PullRequestId ?? "(any)"}",
        _ => JsonSerializer.Serialize(payload)
    };

    /// <summary>
    /// Builds every bookmark variant a trigger can have indexed itself with. The project is mandatory on a trigger,
    /// so it only varies over the identifiers this event is known by; the work item filters are optional and
    /// therefore produce a bookmark both with and without a value.
    /// </summary>
    private static IReadOnlyCollection<AzureDevOpsWebhookBookmark> GetBookmarks(AzureDevOpsWebhookEvent message)
    {
        (string? workItemId, string? workItemType) = GetWorkItemFilters(message);
        string[] projectIds = GetProjectIds(message).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        string?[] workItemIds = Optional(workItemId);
        string?[] workItemTypes = Optional(workItemType);
        string?[] pullRequestIds = Optional(GetPullRequestFilter(message));

        return (from projectId in projectIds
                from id in workItemIds
                from type in workItemTypes
                from pullRequestId in pullRequestIds
                select new AzureDevOpsWebhookBookmark(message.EventType, projectId, id, type, pullRequestId))
            .Distinct()
            .ToArray();
    }

    private static IEnumerable<string> GetProjectIds(AzureDevOpsWebhookEvent message)
    {
        // A webhook payload carries the project ID, while the polling dispatcher and the work item fields carry the
        // project name. A trigger can be configured with either, so every known identifier is matched.
        if (Normalize(message.ProjectId) is { } projectId)
            yield return projectId;

        if (Normalize(message.ProjectName) is { } projectName)
            yield return projectName;

        if (GetWorkItemField(message, TeamProjectField) is { } teamProject)
            yield return teamProject;
    }

    private static (string? WorkItemId, string? WorkItemType) GetWorkItemFilters(AzureDevOpsWebhookEvent message)
    {
        // Only the triggers deriving from AzureDevOpsWorkItemIdentityTrigger expose a work item ID filter.
        bool includeWorkItemId = message.EventType is AzureDevOpsWebhookEventTypes.WorkItemUpdated
            or AzureDevOpsWebhookEventTypes.WorkItemDeleted
            or AzureDevOpsWebhookEventTypes.WorkItemCommented;

        return (includeWorkItemId ? GetWorkItemId(message) : null, GetWorkItemField(message, WorkItemTypeField));
    }

    /// <summary>
    /// The pull request this event is about, for the two triggers that can be restricted to one.
    /// </summary>
    /// <remarks>
    /// Only Updated and Merged expose the filter - see <c>AzureDevOpsPullRequestIdentityTrigger</c> - so a Created
    /// delivery deliberately produces no pull request bookmark: nothing can be indexed with it, and the extra stimulus
    /// would only show up in the log as a match that was never possible.
    /// </remarks>
    private static string? GetPullRequestFilter(AzureDevOpsWebhookEvent message) =>
        message.EventType is AzureDevOpsWebhookEventTypes.PullRequestUpdated or AzureDevOpsWebhookEventTypes.PullRequestMerged
            ? GetPullRequestId(message)
            : null;

    /// <summary>
    /// Reads the pull request id from whichever shape the event arrived in.
    /// </summary>
    /// <remarks>
    /// The typed branch is the polling path, which hands over a <see cref="GitPullRequest"/> it read through the API;
    /// the JSON branch is the Service Hook, whose resource names it <c>pullRequestId</c>. Both are needed, and the
    /// JSON one is the path that reaches a deployed workflow.
    /// </remarks>
    private static string? GetPullRequestId(AzureDevOpsWebhookEvent message) => message.Payload switch
    {
        GitPullRequest pullRequest => pullRequest.PullRequestId.ToString(CultureInfo.InvariantCulture),
        JsonElement json => GetJsonValue(json, "pullRequestId"),
        _ => null
    };

    private static string? GetWorkItemId(AzureDevOpsWebhookEvent message) => message.Payload switch
    {
        WorkItem workItem => workItem.Id?.ToString(CultureInfo.InvariantCulture),
        // For workitem.updated the resource ID is the revision ID, so the explicit work item ID takes precedence.
        JsonElement json => GetJsonValue(json, "workItemId") ?? GetJsonValue(json, "id"),
        _ => null
    };

    private static string? GetWorkItemField(AzureDevOpsWebhookEvent message, string fieldName) => message.Payload switch
    {
        WorkItem workItem => Normalize(workItem.Fields != null && workItem.Fields.TryGetValue(fieldName, out object? value) ? value?.ToString() : null),
        JsonElement json => Normalize(GetJsonField(json, fieldName)),
        _ => null
    };

    private static string? GetJsonField(JsonElement resource, string fieldName)
    {
        if (GetJsonFieldFrom(resource, fieldName) is { } value)
            return value;

        // workitem.updated only carries the unchanged fields on the revision.
        return resource.TryGetProperty("revision", out JsonElement revision) && revision.ValueKind == JsonValueKind.Object
            ? GetJsonFieldFrom(revision, fieldName)
            : null;
    }

    private static string? GetJsonFieldFrom(JsonElement element, string fieldName)
    {
        if (!element.TryGetProperty("fields", out JsonElement fields) || fields.ValueKind != JsonValueKind.Object)
            return null;

        if (!fields.TryGetProperty(fieldName, out JsonElement field))
            return null;

        // On workitem.updated a changed field is an object holding oldValue/newValue instead of a plain value.
        return field.ValueKind == JsonValueKind.Object
            ? field.TryGetProperty("newValue", out JsonElement newValue) ? ToStringValue(newValue) : null
            : ToStringValue(field);
    }

    private static string? GetJsonValue(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out JsonElement property)
            ? ToStringValue(property)
            : null;

    private static string? ToStringValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => Normalize(element.GetString()),
        JsonValueKind.Number => element.GetRawText(),
        _ => null
    };

    private static string?[] Optional(string? value) => value == null ? [null] : [null, value];

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
