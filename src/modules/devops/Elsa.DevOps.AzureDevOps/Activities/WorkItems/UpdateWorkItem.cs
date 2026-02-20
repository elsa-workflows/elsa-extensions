using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Elsa.DevOps.AzureDevOps.Activities.WorkItems;

/// <summary>
/// Updates a work item in Azure DevOps.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.WorkItems",
    "Azure DevOps Work Items",
    "Updates a work item in Azure DevOps.",
    DisplayName = "Update Work Item")]
[UsedImplicitly]
public class UpdateWorkItem : AzureDevOpsActivity
{
    /// <summary>
    /// The work item ID to update.
    /// </summary>
    [Input(Description = "The work item ID to update.")]
    public Input<int> WorkItemId { get; set; } = null!;

    /// <summary>
    /// The updated title. Optional; omit to leave unchanged.
    /// </summary>
    [Input(Description = "The updated title. Optional; omit to leave unchanged.")]
    public Input<string?> Title { get; set; } = null!;

    /// <summary>
    /// The updated description. Optional; omit to leave unchanged.
    /// </summary>
    [Input(Description = "The updated description. Optional; omit to leave unchanged.", UIHint = InputUIHints.MultiLine)]
    public Input<string?> Description { get; set; } = null!;

    /// <summary>
    /// The updated work item.
    /// </summary>
    [Output(Description = "The updated work item.")]
    public Output<WorkItem> UpdatedWorkItem { get; set; } = null!;

    /// <inheritdoc />
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var workItemId = context.Get(WorkItemId);
        var (idOk, idErr) = ActivityInputValidation.TryValidatePositive(workItemId, nameof(WorkItemId));
        if (!idOk) { context.AddExecutionLogEntry("Precondition Failed", idErr); return new ValueTask<bool>(false); }
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var workItemId = context.Get(WorkItemId);
        var title = context.Get(Title);
        var description = context.Get(Description);
        var document = new JsonPatchDocument();
        if (!string.IsNullOrEmpty(title))
        {
            document.Add(new JsonPatchOperation
            {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Replace,
                Path = "/fields/System.Title",
                Value = title
            });
        }
        if (description != null)
        {
            document.Add(new JsonPatchOperation
            {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Replace,
                Path = "/fields/System.Description",
                Value = description
            });
        }
        if (document.Count == 0)
        {
            var connection = GetConnection(context);
            var witClient = connection.GetClient<Microsoft.TeamFoundation.WorkItemTracking.WebApi.WorkItemTrackingHttpClient>();
            var existing = await witClient.GetWorkItemAsync(workItemId, null, null, null, context.CancellationToken);
            context.Set(UpdatedWorkItem, existing);
            return;
        }
        var conn = GetConnection(context);
        var client = conn.GetClient<Microsoft.TeamFoundation.WorkItemTracking.WebApi.WorkItemTrackingHttpClient>();
        var workItem = await client.UpdateWorkItemAsync(document, workItemId, null, null, null, null, context.CancellationToken);
        context.Set(UpdatedWorkItem, workItem);
    }
}
