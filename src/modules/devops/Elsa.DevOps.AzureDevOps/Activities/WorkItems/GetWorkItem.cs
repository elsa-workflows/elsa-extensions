using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.Activities.WorkItems;

/// <summary>
/// Retrieves a work item from Azure DevOps.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.WorkItems",
    "Azure DevOps Work Items",
    "Retrieves a work item from Azure DevOps.",
    DisplayName = "Get Work Item")]
[UsedImplicitly]
public class GetWorkItem : AzureDevOpsActivity
{
    /// <summary>
    /// The work item ID.
    /// </summary>
    [Input(Description = "The work item ID.")]
    public Input<int> WorkItemId { get; set; } = null!;

    /// <summary>
    /// The retrieved work item.
    /// </summary>
    [Output(Description = "The retrieved work item.")]
    public Output<WorkItem> RetrievedWorkItem { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var workItemId = context.Get(WorkItemId);
        ActivityInputValidation.ThrowIfNegativeOrZero(workItemId, nameof(WorkItemId));
        var connection = GetConnection(context);
        var witClient = connection.GetClient<Microsoft.TeamFoundation.WorkItemTracking.WebApi.WorkItemTrackingHttpClient>();
        var workItem = await witClient.GetWorkItemAsync(workItemId, null, null, null, context.CancellationToken);
        context.Set(RetrievedWorkItem, workItem);
    }
}
