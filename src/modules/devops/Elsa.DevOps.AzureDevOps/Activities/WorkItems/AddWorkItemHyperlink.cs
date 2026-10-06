using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Elsa.DevOps.AzureDevOps.Activities.WorkItems;

/// <summary>
/// Attaches a URL to a work item in Azure DevOps.
/// </summary>
/// <remarks>
/// A hyperlink is a relation rather than a field, so it cannot travel in the field patch document that
/// <see cref="UpdateWorkItem"/> sends. It is kept apart from <see cref="AddWorkItemRelation"/> because it needs a URL
/// and no link type, where that activity needs a target work item and a link type; one activity for both would mean
/// inputs that only apply half the time.
/// </remarks>
[Activity(
    "Elsa.AzureDevOps.WorkItems",
    "Azure DevOps Work Items",
    "Attaches a URL to a work item in Azure DevOps.",
    DisplayName = "Add Work Item Hyperlink")]
[UsedImplicitly]
public class AddWorkItemHyperlink : AzureDevOpsActivity
{
    /// <summary>
    /// The work item the hyperlink is added to.
    /// </summary>
    [Input(Description = "The work item ID the hyperlink is added to.")]
    public Input<int> WorkItemId { get; set; } = null!;

    /// <summary>
    /// The URL to attach.
    /// </summary>
    [Input(Description = "The URL to attach to the work item.")]
    public Input<string> Url { get; set; } = null!;

    /// <summary>
    /// A comment describing what the URL points at. Optional.
    /// </summary>
    [Input(Description = "A comment describing what the URL points at. Optional.")]
    public Input<string?> Comment { get; set; } = null!;

    /// <summary>
    /// The work item the hyperlink was added to.
    /// </summary>
    [Output(Description = "The work item the hyperlink was added to.")]
    public Output<WorkItem> UpdatedWorkItem { get; set; } = null!;

    /// <inheritdoc />
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var workItemId = context.Get(WorkItemId);
        var url = context.Get(Url);
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidatePositive(workItemId, nameof(WorkItemId)));
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateUri(url, nameof(Url)));
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var workItemId = context.Get(WorkItemId);
        var url = context.Get(Url)!.Trim();
        var comment = context.Get(Comment);
        var connection = await GetConnectionAsync(context);
        var client = connection.GetClient<Microsoft.TeamFoundation.WorkItemTracking.WebApi.WorkItemTrackingHttpClient>();

        // Read before write, so a re-run or a retry after a timeout skips instead of faulting on a duplicate link.
        var workItem = await client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations, cancellationToken: context.CancellationToken);

        if (WorkItemRelations.HasHyperlink(workItem, url))
        {
            context.AddExecutionLogEntry("Info", $"Work item {workItemId} already links to '{url}'; nothing to add.");
            context.Set(UpdatedWorkItem, workItem);
            await context.CompleteActivityAsync();
            return;
        }

        var document = new JsonPatchDocument
        {
            new JsonPatchOperation
            {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                Path = WorkItemRelations.AddRelationPath,
                Value = WorkItemRelations.CreateRelationValue(WorkItemRelations.HyperlinkReferenceName, url, comment)
            }
        };
        var updated = await client.UpdateWorkItemAsync(document, workItemId, cancellationToken: context.CancellationToken);
        context.Set(UpdatedWorkItem, updated);
        await context.CompleteActivityAsync();
    }
}
