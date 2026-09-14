using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.Build.WebApi;

namespace Elsa.DevOps.AzureDevOps.Activities.Builds;

/// <summary>
/// Lists builds in an Azure DevOps project.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.Builds",
    "Azure DevOps Builds",
    "Lists builds in an Azure DevOps project.",
    DisplayName = "List Builds")]
[UsedImplicitly]
public class ListBuilds : AzureDevOpsActivity
{
    /// <summary>
    /// The project name or ID.
    /// </summary>
    [Input(Description = ProjectDescription)]
    public Input<string> Project { get; set; } = null!;

    /// <summary>
    /// The definition ID to filter by. Optional.
    /// </summary>
    [Input(Description = "The definition ID to filter by. Optional.")]
    public Input<int?> DefinitionId { get; set; } = null!;

    /// <summary>
    /// The maximum number of builds to return. Optional.
    /// </summary>
    [Input(Description = "The maximum number of builds to return. Optional.")]
    public Input<int?> Top { get; set; } = null!;

    /// <summary>
    /// The list of builds.
    /// </summary>
    [Output(Description = "The list of builds.")]
    public Output<IEnumerable<Build>> Builds { get; set; } = null!;

    /// <inheritdoc />
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var project = ResolveProject(context, context.Get(Project));
        var definitionId = context.Get(DefinitionId);
        var top = context.Get(Top);
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateRequired(project, nameof(Project)));
        if (definitionId.HasValue) ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidatePositive(definitionId.Value, nameof(DefinitionId)));
        ActivityInputValidation.ThrowIfInvalid(context, (top is not <= 0, "'Top' must be greater than zero when specified."));
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = ResolveProject(context, context.Get(Project))!;
        var definitionId = context.Get(DefinitionId);
        var top = context.Get(Top);
        var connection = await GetConnectionAsync(context);
        var buildClient = connection.GetClient<BuildHttpClient>();
        var definitions = definitionId.HasValue ? new[] { definitionId.Value } : null;
        var builds = await buildClient.GetBuildsAsync(
            project,
            definitions: definitions,
            top: top,
            cancellationToken: context.CancellationToken);
        context.Set(Builds, builds ?? (IEnumerable<Build>)Array.Empty<Build>());
        await context.CompleteActivityAsync();
    }
}
