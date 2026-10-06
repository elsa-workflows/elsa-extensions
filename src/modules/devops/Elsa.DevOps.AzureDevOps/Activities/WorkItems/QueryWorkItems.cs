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
    [Input(Description = ProjectDescription)]
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
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var project = ResolveProject(context, context.Get(Project));
        var query = context.Get(Query);
        var top = context.Get(Top);
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateRequired(project, nameof(Project)));
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateRequired(query, nameof(Query)));
        ActivityInputValidation.ThrowIfInvalid(context, (top is not <= 0, "'Top' must be greater than zero when specified."));
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = ResolveProject(context, context.Get(Project))!;
        var query = context.Get(Query)!;
        var top = context.Get(Top);
        var connection = await GetConnectionAsync(context);
        var witClient = connection.GetClient<WorkItemTrackingHttpClient>();
        var wiql = new Wiql { Query = query };
        var queryResult = await witClient.QueryByWiqlAsync(wiql, project, top: top, cancellationToken: context.CancellationToken);
        var workItemRefs = queryResult?.WorkItems;
        if (workItemRefs == null || !workItemRefs.Any())
        {
            context.Set(WorkItems, Array.Empty<WorkItem>());
            await context.CompleteActivityAsync();
            return;
        }
        var ids = workItemRefs.Select(wi => wi.Id).ToArray();
        var workItems = await witClient.GetWorkItemsAsync(project, ids, cancellationToken: context.CancellationToken);
        context.Set(WorkItems, workItems ?? (IEnumerable<WorkItem>)Array.Empty<WorkItem>());
        await context.CompleteActivityAsync();
    }
}
