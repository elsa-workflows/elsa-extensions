using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Elsa.DevOps.AzureDevOps.Activities.WorkItems;

/// <summary>
/// Links a work item to another work item in Azure DevOps.
/// </summary>
/// <remarks>
/// A relation is not a work item field, so it cannot travel in the field patch document that <see cref="UpdateWorkItem"/>
/// sends: it is an operation on <c>/relations/-</c> whose value is an object rather than a scalar. That is why this is a
/// separate activity, for the same reason <see cref="AddWorkItemComment"/> is.
/// </remarks>
[Activity(
    "Elsa.AzureDevOps.WorkItems",
    "Azure DevOps Work Items",
    "Links a work item to another work item in Azure DevOps.",
    DisplayName = "Add Work Item Relation")]
[UsedImplicitly]
public class AddWorkItemRelation : AzureDevOpsActivity
{
    /// <summary>
    /// The work item the relation is added to.
    /// </summary>
    [Input(Description = "The work item ID the relation is added to.")]
    public Input<int> WorkItemId { get; set; } = null!;

    /// <summary>
    /// The work item to link to.
    /// </summary>
    [Input(Description = "The work item ID to link to.")]
    public Input<int> TargetWorkItemId { get; set; } = null!;

    /// <summary>
    /// The kind of link to create.
    /// </summary>
    [Input(Description = "The kind of link to create, named after the role the target plays: 'Parent' makes the target the parent of the work item. Defaults to Related. Affects and Affected By exist only in CMMI processes.")]
    public Input<WorkItemLinkType> LinkType { get; set; } = null!;

    /// <summary>
    /// A comment describing why the two are linked. Optional.
    /// </summary>
    [Input(Description = "A comment describing why the two are linked. Optional.")]
    public Input<string?> Comment { get; set; } = null!;

    /// <summary>
    /// The work item the relation was added to.
    /// </summary>
    [Output(Description = "The work item the relation was added to.")]
    public Output<WorkItem> UpdatedWorkItem { get; set; } = null!;

    /// <inheritdoc />
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var workItemId = context.Get(WorkItemId);
        var targetWorkItemId = context.Get(TargetWorkItemId);
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidatePositive(workItemId, nameof(WorkItemId)));
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidatePositive(targetWorkItemId, nameof(TargetWorkItemId)));
        // Azure DevOps refuses a work item linked to itself, and says so far less clearly than this does.
        ActivityInputValidation.ThrowIfInvalid(
            context,
            (targetWorkItemId != workItemId,
                $"'{nameof(TargetWorkItemId)}' must differ from '{nameof(WorkItemId)}'; a work item cannot be linked to itself."));
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var workItemId = context.Get(WorkItemId);
        var targetWorkItemId = context.Get(TargetWorkItemId);
        var referenceName = WorkItemRelations.ToReferenceName(context.Get(LinkType));
        var comment = context.Get(Comment);
        var connection = await GetConnectionAsync(context);
        var client = connection.GetClient<Microsoft.TeamFoundation.WorkItemTracking.WebApi.WorkItemTrackingHttpClient>();

        // Two reads before the write, where the package guideline is one remote call per activity. That guideline is
        // about scope â€” this activity still writes to a single endpoint â€” and the reads are what make the write
        // idempotent, so a re-run or a retry after a timeout skips instead of faulting on a duplicate relation.
        var target = await client.GetWorkItemAsync(targetWorkItemId, cancellationToken: context.CancellationToken);
        var workItem = await client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations, cancellationToken: context.CancellationToken);

        if (WorkItemRelations.HasWorkItemRelation(workItem, referenceName, targetWorkItemId))
        {
            context.AddExecutionLogEntry("Info", $"Work item {workItemId} is already linked to work item {targetWorkItemId} as '{context.Get(LinkType)}'; nothing to add.");
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
                Value = WorkItemRelations.CreateRelationValue(referenceName, target.Url, comment)
            }
        };
        var updated = await client.UpdateWorkItemAsync(document, workItemId, cancellationToken: context.CancellationToken);
        context.Set(UpdatedWorkItem, updated);
        await context.CompleteActivityAsync();
    }
}
