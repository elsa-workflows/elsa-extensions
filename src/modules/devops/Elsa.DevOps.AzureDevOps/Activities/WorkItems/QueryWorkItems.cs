using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.Activities.WorkItems;

/// <summary>
/// Queries work items in Azure DevOps using WIQL.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.WorkItems",
    "Azure DevOps Work Items",
    "Queries work items in Azure DevOps using WIQL.",
    DisplayName = "Query Work Items")]
[UsedImplicitly]
public class QueryWorkItems : AzureDevOpsActivity
{
    /// <summary>
    /// The project name or ID.
    /// </summary>
    [Input(Description = "The project name or ID.")]
    public Input<string> Project { get; set; } = null!;

    /// <summary>
    /// The WIQL query string (e.g. SELECT [System.Id] FROM WorkItems WHERE [System.State] = 'Active').
    /// </summary>
    [Input(Description = "The WIQL query string.", UIHint = InputUIHints.MultiLine)]
    public Input<string> Query { get; set; } = null!;

    /// <summary>
    /// The maximum number of work items to return. Optional.
    /// </summary>
    [Input(Description = "The maximum number of work items to return. Optional.")]
    public Input<int?> Top { get; set; } = null!;

    /// <summary>
    /// The list of work items matching the query.
    /// </summary>
    [Output(Description = "The list of work items matching the query.")]
    public Output<IEnumerable<WorkItem>> WorkItems { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = context.Get(Project);
        var query = context.Get(Query);
        var top = context.Get(Top);
        ActivityInputValidation.ThrowIfNullOrEmpty(project, nameof(Project));
        ActivityInputValidation.ThrowIfNullOrEmpty(query, nameof(Query));
        if (top.HasValue && top.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(Top), top, "'Top' must be greater than zero when specified.");
        var connection = GetConnection(context);
        var witClient = connection.GetClient<WorkItemTrackingHttpClient>();
        var wiql = new Wiql { Query = query! };
        var queryResult = await witClient.QueryByWiqlAsync(wiql, null, top, null, context.CancellationToken);
        var workItemRefs = queryResult?.WorkItems;
        if (workItemRefs == null || !workItemRefs.Any())
        {
            context.Set(WorkItems, Array.Empty<WorkItem>());
            return;
        }
        var ids = workItemRefs.Select(wi => wi.Id).ToArray();
        var workItems = await witClient.GetWorkItemsAsync(project!, ids, null, null, null, null, null, context.CancellationToken);
        context.Set(WorkItems, workItems ?? (IEnumerable<WorkItem>)Array.Empty<WorkItem>());
    }
}
