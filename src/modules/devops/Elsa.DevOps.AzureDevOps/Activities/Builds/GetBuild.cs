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
    [Input(Description = "The project name or ID.")]
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
        var project = context.Get(Project);
        var buildId = context.Get(BuildId);
        var (projectOk, projectErr) = ActivityInputValidation.TryValidateRequired(project, nameof(Project));
        if (!projectOk) { context.AddExecutionLogEntry("Precondition Failed", projectErr); return new ValueTask<bool>(false); }
        var (idOk, idErr) = ActivityInputValidation.TryValidatePositive(buildId, nameof(BuildId));
        if (!idOk) { context.AddExecutionLogEntry("Precondition Failed", idErr); return new ValueTask<bool>(false); }
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = context.Get(Project)!;
        var buildId = context.Get(BuildId);
        var connection = GetConnection(context);
        var buildClient = connection.GetClient<BuildHttpClient>();
        var build = await buildClient.GetBuildAsync(project, buildId, null, null, context.CancellationToken);
        context.Set(RetrievedBuild, build);
        await context.CompleteActivityAsync();
    }
}
