using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.Build.WebApi;

namespace Elsa.DevOps.AzureDevOps.Activities.Builds;

/// <summary>
/// Retrieves a build from Azure DevOps.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.Builds",
    "Azure DevOps Builds",
    "Retrieves a build from Azure DevOps.",
    DisplayName = "Get Build")]
[UsedImplicitly]
public class GetBuild : AzureDevOpsActivity
{
    /// <summary>
    /// The project name or ID.
    /// </summary>
    [Input(Description = ProjectDescription)]
    public Input<string> Project { get; set; } = null!;

    /// <summary>
    /// The build ID.
    /// </summary>
    [Input(Description = "The build ID.")]
    public Input<int> BuildId { get; set; } = null!;

    /// <summary>
    /// The retrieved build.
    /// </summary>
    [Output(Description = "The retrieved build.")]
    public Output<Build> RetrievedBuild { get; set; } = null!;

    /// <inheritdoc />
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var project = ResolveProject(context, context.Get(Project));
        var buildId = context.Get(BuildId);
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateRequired(project, nameof(Project)));
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidatePositive(buildId, nameof(BuildId)));
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = ResolveProject(context, context.Get(Project))!;
        var buildId = context.Get(BuildId);
        var connection = await GetConnectionAsync(context);
        var buildClient = connection.GetClient<BuildHttpClient>();
        var build = await buildClient.GetBuildAsync(project, buildId, cancellationToken: context.CancellationToken);
        context.Set(RetrievedBuild, build);
        await context.CompleteActivityAsync();
    }
}
