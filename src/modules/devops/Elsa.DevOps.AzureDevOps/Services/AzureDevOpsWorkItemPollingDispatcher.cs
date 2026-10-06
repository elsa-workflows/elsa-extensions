using System.Globalization;
using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services.Polling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// The deletion events one poll dispatched, and the recycle bin contents it saw.
/// </summary>
/// <param name="SeenIds">
/// The recycle bin contents this poll actually read, minus the entries whose dispatch failed, or <c>null</c> when it
/// did not read the bin at all (the family or the deleted-work-item feature is off, the project/organization/token
/// could not be resolved, or the read itself failed). Collapsing that to an empty collection would tell the next poll
/// "the bin was empty", and every entry already sitting in it would then look newly deleted and get replayed. Holding
/// a failed entry back is the mirror image: it stays unseen, so the next poll retries exactly it.
/// </param>
public sealed record AzureDevOpsDeletedPollingResult(
    IReadOnlyList<AzureDevOpsPollingEvent> Events,
    IReadOnlyCollection<int>? SeenIds);

/// <summary>
/// Polls Azure DevOps work items and dispatches matching Elsa trigger stimuli.
/// </summary>
public class AzureDevOpsWorkItemPollingDispatcher(
    AzureDevOpsConnectionFactory connectionFactory,
    AzureDevOpsPollingCredentialResolver credentialResolver,
    AzureDevOpsWebhookEventHandler eventHandler,
    AzureDevOpsOrganizationUrlResolver organizationUrlResolver,
    AzureDevOpsProjectResolver projectResolver,
    ILogger<AzureDevOpsWorkItemPollingDispatcher> logger,
    IOptions<AzureDevOpsPollingOptions> options)
{
    private static readonly string[] Fields =
    [
        "System.Id",
        "System.CreatedDate",
        "System.ChangedDate",
        "System.TeamProject",
        "System.WorkItemType",
        "System.CommentCount"
    ];

    /// <summary>
    /// Adding a comment bumps System.ChangedDate, so a comment is indistinguishable from a field change on timestamp
    /// alone. A change is attributed to a comment when it lands no later than the newest comment plus this margin.
    /// </summary>
    private static readonly TimeSpan CommentAttributionMargin = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The recycle bin detail endpoint caps the number of IDs it accepts per call.
    /// </summary>
    private const int DeletedWorkItemBatchSize = 200;

    /// <param name="pollingOptions">
    /// What this poll runs with: the configured family with the poll activity's inputs laid over it. The master switch
    /// is read from configuration instead, because switching polling off altogether is the operator's call, not a
    /// workflow's. The family's own switch is deliberately not consulted: it decides whether this family's polling
    /// workflow is started at host start, not whether a poll that something asked for anyway - the manual event, or a
    /// workflow carrying the poll activity - may run.
    /// </param>
    public async Task<AzureDevOpsPollingResult> DispatchAsync(
        WorkItemPollingOptions pollingOptions,
        DateTimeOffset checkpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pollingOptions);

        List<AzureDevOpsPollingEvent> triggeredEvents = [];

        if (!options.Value.Enabled)
            return new(triggeredEvents, checkpoint);

        string? project = projectResolver.Resolve(pollingOptions.Project);
        string? organizationUrl = organizationUrlResolver.Resolve(pollingOptions.OrganizationUrl);

        if (organizationUrl == null || project == null)
            return new(triggeredEvents, checkpoint);

        string? token = await credentialResolver.GetTokenAsync(pollingOptions, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("Azure DevOps work item polling is enabled but no token or token secret is configured.");
            return new(triggeredEvents, checkpoint);
        }

        VssConnection connection = connectionFactory.GetConnection(organizationUrl, token);
        WorkItemTrackingHttpClient client = connection.GetClient<WorkItemTrackingHttpClient>();
        string timestamp = checkpoint.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        Wiql wiql = new()
        {
            Query =
                $"""
                 SELECT [System.Id]
                 FROM WorkItems
                 WHERE [System.TeamProject] = @project
                 AND [System.ChangedDate] > '{timestamp}'
                 ORDER BY [System.ChangedDate] ASC
                 """
        };

        List<WorkItem> workItems;

        try
        {
            WorkItemQueryResult? queryResult = await client.QueryByWiqlAsync(
                wiql,
                project,
                timePrecision: true,
                top: pollingOptions.Top,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            int[] ids = queryResult?.WorkItems?.Select(x => x.Id).Distinct().ToArray() ?? [];

            if (ids.Length == 0)
                return new(triggeredEvents, checkpoint);

            workItems = await client.GetWorkItemsAsync(project, ids, fields: Fields, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The checkpoint stays put: nothing was read, so nothing should be skipped next time. Every other family
            // holds its checkpoint on a failed read the same way.
            logger.LogWarning(ex, "Reading changed Azure DevOps work items of project {Project} failed; leaving its checkpoint unchanged.", project);
            return new(triggeredEvents, checkpoint);
        }

        Dictionary<int, PostedComment> newestComments = await GetNewestCommentsAsync(client, project, workItems, checkpoint, cancellationToken).ConfigureAwait(false);
        DateTimeOffset lastSeen = checkpoint;

        foreach (WorkItem workItem in workItems.OrderBy(GetChangedDate))
        {
            DateTimeOffset createdDate = GetDateTimeOffset(workItem, "System.CreatedDate");
            DateTimeOffset changedDate = GetChangedDate(workItem);
            PostedComment? newestComment = workItem.Id != null && newestComments.TryGetValue(workItem.Id.Value, out PostedComment? value)
                ? value
                : null;

            if (createdDate >= checkpoint)
            {
                await SendAsync(AzureDevOpsWebhookEventTypes.WorkItemCreated, workItem, project, cancellationToken).ConfigureAwait(false);
                triggeredEvents.Add(CreatePollingEvent(AzureDevOpsWebhookEventTypes.WorkItemCreated, workItem, project));
            }

            if (newestComment != null)
            {
                // The comment travels with the event: the trigger would otherwise have to recover it from the work
                // item, which this poller never asked Azure DevOps for the history of.
                await SendAsync(AzureDevOpsWebhookEventTypes.WorkItemCommented, workItem, project, cancellationToken, newestComment).ConfigureAwait(false);
                triggeredEvents.Add(CreatePollingEvent(AzureDevOpsWebhookEventTypes.WorkItemCommented, workItem, project));
            }

            // A comment bumps the changed date as well, so only report an update when the item changed after the
            // newest comment; otherwise every comment would also show up as an update.
            bool changedByComment = newestComment?.CreatedOn != null && changedDate <= newestComment.CreatedOn.Value + CommentAttributionMargin;

            if (changedDate >= checkpoint && !changedByComment)
            {
                await SendAsync(AzureDevOpsWebhookEventTypes.WorkItemUpdated, workItem, project, cancellationToken).ConfigureAwait(false);
                triggeredEvents.Add(CreatePollingEvent(AzureDevOpsWebhookEventTypes.WorkItemUpdated, workItem, project));
            }

            if (changedDate > lastSeen)
                lastSeen = changedDate;
        }

        return new(triggeredEvents, lastSeen);
    }

    /// <summary>
    /// Dispatches a deletion event for every work item that entered the recycle bin since the previous poll.
    /// </summary>
    /// <param name="pollingOptions">
    /// What this poll runs with: the configured family with the poll activity's inputs laid over it.
    /// </param>
    /// <param name="seenIds">The recycle bin contents of the previous poll, or <c>null</c> when none has run yet.</param>
    public async Task<AzureDevOpsDeletedPollingResult> DispatchDeletedAsync(
        WorkItemPollingOptions pollingOptions,
        IReadOnlyCollection<int>? seenIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pollingOptions);

        List<AzureDevOpsPollingEvent> triggeredEvents = [];

        if (!options.Value.Enabled || !pollingOptions.IncludeDeleted)
            return new(triggeredEvents, seenIds);

        string? project = projectResolver.Resolve(pollingOptions.Project);
        string? organizationUrl = organizationUrlResolver.Resolve(pollingOptions.OrganizationUrl);

        if (organizationUrl == null || project == null)
            return new(triggeredEvents, seenIds);

        string? token = await credentialResolver.GetTokenAsync(pollingOptions, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(token))
            return new(triggeredEvents, seenIds);

        VssConnection connection = connectionFactory.GetConnection(organizationUrl, token);
        WorkItemTrackingHttpClient client = connection.GetClient<WorkItemTrackingHttpClient>();
        int[] current;

        try
        {
            // Shallow references are IDs only, which keeps the per-poll cost flat however large the bin grows.
            List<WorkItemDeleteShallowReference> references = await client
                .GetDeletedWorkItemShallowReferencesAsync(project, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            current = [.. references.Where(reference => reference.Id != null).Select(reference => reference.Id!.Value)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Leaving the seen set untouched means the next poll compares against the same baseline, so nothing is
            // lost and nothing is replayed.
            logger.LogWarning(ex, "Reading the Azure DevOps recycle bin of project {Project} failed; leaving the deleted work item baseline unchanged.", project);
            return new(triggeredEvents, seenIds);
        }

        IReadOnlyList<int> newlyDeleted = DeletedWorkItems.GetNewlyDeleted(seenIds, current);
        HashSet<int> undispatched = [];

        foreach (int[] batch in newlyDeleted.Chunk(DeletedWorkItemBatchSize))
        {
            // Tracked per ID rather than per batch: a SendAsync failing part-way leaves the IDs before it genuinely
            // dispatched, and putting those back would report them a second time.
            HashSet<int> pending = [.. batch];

            try
            {
                List<WorkItemDeleteReference> deleted = await client
                    .GetDeletedWorkItemsAsync(project, batch, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                foreach (WorkItemDeleteReference reference in deleted)
                {
                    WorkItem workItem = DeletedWorkItems.ToWorkItem(reference, project);
                    await SendAsync(AzureDevOpsWebhookEventTypes.WorkItemDeleted, workItem, project, cancellationToken).ConfigureAwait(false);
                    triggeredEvents.Add(CreatePollingEvent(AzureDevOpsWebhookEventTypes.WorkItemDeleted, workItem, project));

                    // Only reached once the deletion above is fully handled, so a failure below leaves it out of the
                    // retry set. IDs the detail read simply did not return stay pending and, on a successful batch,
                    // count as seen: the bin no longer describes them, so there is nothing left to report.
                    if (reference.Id != null)
                        pending.Remove(reference.Id.Value);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Held back from the baseline below, so the next poll sees these IDs as newly deleted again and
                // retries exactly them: a failed dispatch is not proof that nothing happened, which is the discipline
                // every other family follows.
                undispatched.UnionWith(pending);
                logger.LogWarning(ex, "Reporting {Count} deleted Azure DevOps work item(s) of project {Project} failed; they are retried on the next poll.", pending.Count, project);
            }
        }

        IReadOnlyCollection<int> baseline = undispatched.Count == 0
            ? current
            : [.. current.Where(id => !undispatched.Contains(id))];

        return new(triggeredEvents, baseline);
    }

    /// <summary>
    /// Returns, per work item, its newest comment when that comment was added since the checkpoint. Work items
    /// without a new comment are absent from the result.
    /// </summary>
    /// <remarks>
    /// The whole comment is kept rather than only its timestamp. The timestamp is what decides whether to raise the
    /// event; the rest is what the trigger hands to the workflow, and this call is the only place on the polling path
    /// where it is available.
    /// </remarks>
    private async Task<Dictionary<int, PostedComment>> GetNewestCommentsAsync(
        WorkItemTrackingHttpClient client,
        string project,
        IEnumerable<WorkItem> workItems,
        DateTimeOffset checkpoint,
        CancellationToken cancellationToken)
    {
        Dictionary<int, PostedComment> newestComments = [];

        // Comments are read per work item, so System.CommentCount is used to skip the ones that cannot have any.
        foreach (WorkItem workItem in workItems.Where(workItem => workItem.Id != null && GetCommentCount(workItem) > 0))
        {
            int id = workItem.Id!.Value;

            try
            {
                CommentList commentList = await client
                    .GetCommentsAsync(project, id, top: 1, order: CommentSortOrder.Desc, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                Comment? newest = commentList?.Comments?.FirstOrDefault();

                if (newest == null)
                    continue;

                DateTimeOffset createdDate = new(DateTime.SpecifyKind(newest.CreatedDate, DateTimeKind.Utc));

                if (createdDate < checkpoint)
                    continue;

                // A comment with no text still counts as a comment for the purpose of raising the event and of not
                // reporting the same change as an update, so the timestamp is kept even when the mapping yields
                // nothing to hand over.
                newestComments[id] = WorkItemCommentReader.FromApi(newest)
                    ?? new PostedComment(string.Empty, newest.Id == 0 ? null : newest.Id, CreatedOn: createdDate);
            }
            catch (Exception ex)
            {
                // Without comment data the change simply reports as an update, which is the behaviour without this call.
                logger.LogWarning(ex, "Failed to read comments of Azure DevOps work item {WorkItemId}; its change is reported as an update.", id);
            }
        }

        return newestComments;
    }

    private static int GetCommentCount(WorkItem workItem) =>
        workItem.Fields != null && workItem.Fields.TryGetValue("System.CommentCount", out object? value) && value != null
            ? Convert.ToInt32(value, CultureInfo.InvariantCulture)
            : 0;

    private static AzureDevOpsPollingEvent CreatePollingEvent(string eventType, WorkItem workItem, string project)
    {
        string? workItemType = workItem.Fields.TryGetValue("System.WorkItemType", out object? value)
            ? value?.ToString()
            : null;
        string id = workItem.Id?.ToString(CultureInfo.InvariantCulture) ?? "(unknown)";

        return new AzureDevOpsPollingEvent(eventType, project, workItem.Id?.ToString(CultureInfo.InvariantCulture), $"work item {id} ({workItemType ?? "(unknown)"})");
    }

    private async Task SendAsync(string eventType, WorkItem workItem, string projectId, CancellationToken cancellationToken, PostedComment? comment = null)
    {
        // Reuse the webhook handler so polled events produce the exact same bookmark variants as Service Hook events;
        // sending a stimulus without a bookmark payload never matches an indexed trigger.
        AzureDevOpsWebhookEvent message = new(eventType, workItem, projectId, Comment: comment);
        await eventHandler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
    }

    private static DateTimeOffset GetChangedDate(WorkItem workItem) => GetDateTimeOffset(workItem, "System.ChangedDate");

    private static DateTimeOffset GetDateTimeOffset(WorkItem workItem, string fieldName)
    {
        if (!workItem.Fields.TryGetValue(fieldName, out object? value) || value == null)
            return DateTimeOffset.MinValue;

        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt),
            string s when DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dto) => dto,
            _ => DateTimeOffset.MinValue
        };
    }
}
