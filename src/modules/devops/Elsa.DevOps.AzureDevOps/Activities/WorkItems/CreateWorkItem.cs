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
    [Input(Description = ProjectDescription)]
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
    [Input(Description = "The description of the work item. Optional.", UIHint = InputUIHints.MultiLine)]
    public Input<string?> Description { get; set; } = null!;

    /// <summary>
    /// The tags to put on the work item, separated by <c>;</c>. Optional.
    /// </summary>
    [Input(Description = "The tags to put on the work item, separated by ';'. Optional.")]
    public Input<string?> Tags { get; set; } = null!;

    /// <summary>
    /// Any other fields to set, keyed by reference name. This is the way to write fields that have no dedicated input,
    /// including custom fields.
    /// </summary>
    [Input(
        Description = "Any other fields to set, keyed by reference name (e.g. System.Tags, System.AreaPath, Custom.MyField). Values are written as-is; leave empty to set nothing.",
        UIHint = InputUIHints.Dictionary,
        EvaluatorType = typeof(DictionaryValueEvaluator))]
    public Input<IDictionary<string, object>?> Fields { get; set; } = null!;

    /// <summary>
    /// The created work item.
    /// </summary>
    [Output(Description = "The created work item.")]
    public Output<WorkItem> CreatedWorkItem { get; set; } = null!;

    /// <inheritdoc />
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var project = ResolveProject(context, context.Get(Project));
        var workItemType = context.Get(WorkItemType);
        var title = context.Get(Title);
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateRequired(project, nameof(Project)));
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateRequired(workItemType, nameof(WorkItemType)));
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateRequired(title, nameof(Title)));
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = ResolveProject(context, context.Get(Project))!;
        var workItemType = context.Get(WorkItemType)!;
        var title = context.Get(Title)!;
        var description = context.Get(Description);
        var tags = context.Get(Tags);
        var fields = context.Get(Fields);
        var document = new JsonPatchDocument();
        var patchedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/fields/System.Title" };
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
            patchedPaths.Add("/fields/System.Description");
        }
        if (!string.IsNullOrWhiteSpace(tags))
        {
            document.Add(new JsonPatchOperation
            {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                Path = "/fields/System.Tags",
                Value = tags
            });
            patchedPaths.Add("/fields/System.Tags");
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
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                    Path = path,
                    Value = WorkItemFieldValues.Normalize(field.Value)
                });
            }
        }
        var connection = await GetConnectionAsync(context);
        var witClient = connection.GetClient<Microsoft.TeamFoundation.WorkItemTracking.WebApi.WorkItemTrackingHttpClient>();
        var workItem = await witClient.CreateWorkItemAsync(document, project, workItemType, cancellationToken: context.CancellationToken);
        context.Set(CreatedWorkItem, workItem);
        await context.CompleteActivityAsync();
    }
}
