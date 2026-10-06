using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.Activities.WorkItems;

/// <summary>
/// Adds a comment to the discussion of a work item in Azure DevOps.
/// </summary>
/// <remarks>
/// Comments are not work item fields, so they cannot travel in the field patch document that <see cref="UpdateWorkItem"/>
/// sends. They live behind their own endpoint, which is why this is a separate activity.
/// </remarks>
[Activity(
    "Elsa.AzureDevOps.WorkItems",
    "Azure DevOps Work Items",
    "Adds a comment to the discussion of a work item in Azure DevOps.",
    DisplayName = "Add Work Item Comment")]
[UsedImplicitly]
public class AddWorkItemComment : AzureDevOpsActivity
{
    /// <summary>
    /// The project name or ID.
    /// </summary>
    [Input(Description = ProjectDescription)]
    public Input<string> Project { get; set; } = null!;

    /// <summary>
    /// The work item ID to comment on.
    /// </summary>
    [Input(Description = "The work item ID to comment on.")]
    public Input<int> WorkItemId { get; set; } = null!;

    /// <summary>
    /// The comment text.
    /// </summary>
    [Input(Description = "The comment text, in the selected format.", UIHint = InputUIHints.MultiLine)]
    public Input<string> Text { get; set; } = null!;

    /// <summary>
    /// The format the comment text is written in.
    /// </summary>
    [Input(Description = "The format the comment text is written in. Defaults to Markdown.")]
    public Input<CommentFormat> Format { get; set; } = null!;

    /// <summary>
    /// The comment that was added.
    /// </summary>
    [Output(Description = "The comment that was added.")]
    public Output<Comment> AddedComment { get; set; } = null!;

    /// <inheritdoc />
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var project = ResolveProject(context, context.Get(Project));
        var workItemId = context.Get(WorkItemId);
        var text = context.Get(Text);
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateRequired(project, nameof(Project)));
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidatePositive(workItemId, nameof(WorkItemId)));
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateRequired(text, nameof(Text)));
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = ResolveProject(context, context.Get(Project))!;
        var workItemId = context.Get(WorkItemId);
        var text = context.Get(Text)!;
        var format = context.Get(Format);
        var request = new CommentCreate { Text = text };
        var connection = await GetConnectionAsync(context);
        var client = connection.GetClient<Microsoft.TeamFoundation.WorkItemTracking.WebApi.WorkItemTrackingHttpClient>();
        var comment = await client.AddWorkItemCommentAsync(request, project, workItemId, format, cancellationToken: context.CancellationToken);
        context.Set(AddedComment, comment);
        await context.CompleteActivityAsync();
    }
}
