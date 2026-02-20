using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Elsa.DevOps.AzureDevOps.Activities.WorkItems;

/// <summary>
/// Creates a work item in Azure DevOps.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.WorkItems",
    "Azure DevOps Work Items",
    "Creates a work item in Azure DevOps.",
    DisplayName = "Create Work Item")]
[UsedImplicitly]
public class CreateWorkItem : AzureDevOpsActivity
{
    /// <summary>
    /// The project name or ID.
    /// </summary>
    [Input(Description = "The project name or ID.")]
    public Input<string> Project { get; set; } = null!;

    /// <summary>
    /// The work item type (e.g. Task, Bug, User Story).
    /// </summary>
    [Input(Description = "The work item type (e.g. Task, Bug, User Story).")]
    public Input<string> WorkItemType { get; set; } = null!;

    /// <summary>
    /// The title of the work item.
    /// </summary>
    [Input(Description = "The title of the work item.")]
    public Input<string> Title { get; set; } = null!;

    /// <summary>
    /// The description of the work item. Optional.
    /// </summary>
    [Input(Description = "The description of the work item. Optional.")]
    public Input<string?> Description { get; set; } = null!;

    /// <summary>
    /// The created work item.
    /// </summary>
    [Output(Description = "The created work item.")]
    public Output<WorkItem> CreatedWorkItem { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = context.Get(Project)!;
        var workItemType = context.Get(WorkItemType)!;
        var title = context.Get(Title)!;
        var description = context.Get(Description);
        var document = new JsonPatchDocument();
        document.Add(new JsonPatchOperation
        {
            Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
            Path = "/fields/System.Title",
            Value = title
        });
        if (!string.IsNullOrEmpty(description))
        {
            document.Add(new JsonPatchOperation
            {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                Path = "/fields/System.Description",
                Value = description
            });
        }
        var connection = GetConnection(context);
        var witClient = connection.GetClient<Microsoft.TeamFoundation.WorkItemTracking.WebApi.WorkItemTrackingHttpClient>();
        var workItem = await witClient.CreateWorkItemAsync(document, project, workItemType, null, null, null, null, context.CancellationToken);
        context.Set(CreatedWorkItem, workItem);
    }
}
