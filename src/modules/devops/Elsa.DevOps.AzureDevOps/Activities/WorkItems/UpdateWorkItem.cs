using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.UIHints.Dictionary;
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
    /// Any other fields to set, keyed by reference name. This is the way to write fields that have no dedicated input,
    /// including custom fields.
    /// </summary>
    [Input(
        Description = "Any other fields to set, keyed by reference name (e.g. System.State, Microsoft.VSTS.Common.Priority, Custom.MyField). Values are written as-is; leave empty to change nothing.",
        UIHint = InputUIHints.Dictionary,
        EvaluatorType = typeof(DictionaryValueEvaluator))]
    public Input<IDictionary<string, object>?> Fields { get; set; } = null!;

    /// <summary>
    /// The updated work item.
    /// </summary>
    [Output(Description = "The updated work item.")]
    public Output<WorkItem> UpdatedWorkItem { get; set; } = null!;

    /// <inheritdoc />
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var workItemId = context.Get(WorkItemId);
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidatePositive(workItemId, nameof(WorkItemId)));
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var workItemId = context.Get(WorkItemId);
        var title = context.Get(Title);
        var description = context.Get(Description);
        var fields = context.Get(Fields);
        var document = new JsonPatchDocument();
        var patchedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(title))
        {
            document.Add(new JsonPatchOperation
            {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Replace,
                Path = "/fields/System.Title",
                Value = title
            });
            patchedPaths.Add("/fields/System.Title");
        }
        if (description != null)
        {
            document.Add(new JsonPatchOperation
            {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Replace,
                Path = "/fields/System.Description",
                Value = description
            });
            patchedPaths.Add("/fields/System.Description");
        }
        if (fields != null)
        {
            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field.Key))
                    continue;
                // A field the dedicated inputs already cover would produce a second operation on the same path, which
                // Azure DevOps rejects. The dedicated input wins.
                var path = $"/fields/{field.Key.Trim()}";
                if (!patchedPaths.Add(path))
                {
                    context.AddExecutionLogEntry("Info", $"Ignoring '{field.Key}' from Fields because a dedicated input already sets it.");
                    continue;
                }
                document.Add(new JsonPatchOperation
                {
                    // Add rather than Replace: on a work item field Azure DevOps treats add as an upsert, so this also
                    // works for a custom field that has no value yet.
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                    Path = path,
                    Value = WorkItemFieldValues.Normalize(field.Value)
                });
            }
        }
        var connection = await GetConnectionAsync(context);
        var client = connection.GetClient<Microsoft.TeamFoundation.WorkItemTracking.WebApi.WorkItemTrackingHttpClient>();

        if (document.Count == 0)
        {
            var existing = await client.GetWorkItemAsync(workItemId, cancellationToken: context.CancellationToken);
            context.Set(UpdatedWorkItem, existing);
            await context.CompleteActivityAsync();
            return;
        }

        var workItem = await client.UpdateWorkItemAsync(document, workItemId, cancellationToken: context.CancellationToken);
        context.Set(UpdatedWorkItem, workItem);
        await context.CompleteActivityAsync();
    }
}
