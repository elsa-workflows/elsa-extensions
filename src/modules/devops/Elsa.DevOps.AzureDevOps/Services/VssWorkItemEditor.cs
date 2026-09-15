using Elsa.DevOps.AzureDevOps.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <inheritdoc cref="IWorkItemEditor" />
public sealed class VssWorkItemEditor(AzureDevOpsConnectionFactory connectionFactory) : IWorkItemEditor
{
    /// <inheritdoc />
    public async ValueTask<WorkItemSnapshot> GetAsync(WorkItemRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        WorkItem workItem = await CallAsync(
                () => Client(request.OrganizationUrl, request.Token).GetWorkItemAsync(request.WorkItemId, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return WorkItemSnapshot.From(workItem, request.OrganizationUrl);
    }

    /// <inheritdoc />
    public async ValueTask<WorkItemSearchResult> SearchAsync(WorkItemQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        WorkItemTrackingHttpClient client = Client(request.OrganizationUrl, request.Token);

        // One more than asked for, so "there is more" is a fact. A WIQL result carries no total count, and the
        // alternative - running the query twice, once unbounded - costs a round trip to learn a number nobody acts on
        // beyond "narrow your filters".
        WorkItemQueryResult? result = await CallAsync(
                () => client.QueryByWiqlAsync(
                    new Wiql { Query = request.Wiql },
                    request.Project,
                    top: request.MaxResults + 1,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        int[] matched = result?.WorkItems?.Select(reference => reference.Id).ToArray() ?? [];
        bool moreAvailable = matched.Length > request.MaxResults;
        int[] ids = [.. matched.Take(request.MaxResults)];

        if (ids.Length == 0)
            return new WorkItemSearchResult([], false);

        List<WorkItem> workItems = await CallAsync(
                () => client.GetWorkItemsAsync(request.Project, ids, WorkItemRow.Fields, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        // Back into the order the query asked for. GetWorkItemsAsync does not promise to honour the order of the ids it
        // is given, and the query's ORDER BY - newest first, usually - is the only ranking the caller has.
        Dictionary<int, WorkItem> byId = workItems
            .Where(workItem => workItem.Id.HasValue)
            .ToDictionary(workItem => workItem.Id!.Value);

        WorkItemRow[] rows =
        [
            .. ids
                .Where(byId.ContainsKey)
                .Select(id => WorkItemRow.From(byId[id], request.OrganizationUrl)),
        ];

        return new WorkItemSearchResult(rows, moreAvailable);
    }

    /// <inheritdoc />
    public async ValueTask<WorkItemFieldChange> SetFieldAsync(WorkItemFieldRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        JsonPatchDocument document =
        [
            // Add rather than Replace: on a work item field Azure DevOps treats add as an upsert, so this also works for
            // a custom field that has no value yet. The same reasoning as UpdateWorkItem's Fields input.
            new JsonPatchOperation
            {
                Operation = Operation.Add,
                Path = $"/fields/{request.ReferenceName}",
                Value = request.Value,
            },
        ];

        WorkItem workItem = await CallAsync(
                () => Client(request.OrganizationUrl, request.Token).UpdateWorkItemAsync(document, request.WorkItemId, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return new WorkItemFieldChange(request.WorkItemId, request.ReferenceName, request.Value, workItem.Rev);
    }

    /// <inheritdoc />
    public async ValueTask<WorkItemCommentAdded> AddCommentAsync(WorkItemCommentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Comment comment = await CallAsync(
                () => Client(request.OrganizationUrl, request.Token).AddWorkItemCommentAsync(
                    new CommentCreate { Text = request.Text },
                    request.Project,
                    request.WorkItemId,
                    CommentFormat.Markdown,
                    cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return new WorkItemCommentAdded(request.WorkItemId, comment.Id);
    }

    private WorkItemTrackingHttpClient Client(string organizationUrl, string token)
    {
        VssConnection connection = connectionFactory.GetConnection(organizationUrl, token);

        return connection.GetClient<WorkItemTrackingHttpClient>();
    }

    /// <summary>
    /// Turns what Azure DevOps refuses into a <see cref="WorkItemEditorException"/>, message intact, and lets everything
    /// else through.
    /// </summary>
    private static async Task<T> CallAsync<T>(Func<Task<T>> call)
    {
        try
        {
            return await call().ConfigureAwait(false);
        }
        catch (VssException exception)
        {
            throw new WorkItemEditorException(exception.Message, exception);
        }
    }
}
