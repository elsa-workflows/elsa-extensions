using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.Build.WebApi;

namespace Elsa.DevOps.AzureDevOps.Activities.Builds;

/// <summary>
/// Queues a build in Azure DevOps.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.Builds",
    "Azure DevOps Builds",
    "Queues a build in Azure DevOps.",
    DisplayName = "Queue Build")]
[UsedImplicitly]
public class QueueBuild : AzureDevOpsActivity
{
    /// <summary>
    /// The project name or ID.
    /// </summary>
    [Input(Description = "The project name or ID.")]
    public Input<string> Project { get; set; } = null!;

    /// <summary>
    /// The build definition ID.
    /// </summary>
    [Input(Description = "The build definition ID.")]
    public Input<int> DefinitionId { get; set; } = null!;

    /// <summary>
    /// The source branch to build. Optional (e.g. refs/heads/main).
    /// </summary>
    [Input(Description = "The source branch to build. Optional.")]
    public Input<string?> SourceBranch { get; set; } = null!;

    /// <summary>
    /// The queued build.
    /// </summary>
    [Output(Description = "The queued build.")]
    public Output<Build> QueuedBuild { get; set; } = null!;

    /// <inheritdoc />
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var project = context.Get(Project);
        var definitionId = context.Get(DefinitionId);
        var (projectOk, projectErr) = ActivityInputValidation.TryValidateRequired(project, nameof(Project));
        if (!projectOk) { context.AddExecutionLogEntry("Precondition Failed", projectErr); return new ValueTask<bool>(false); }
        var (idOk, idErr) = ActivityInputValidation.TryValidatePositive(definitionId, nameof(DefinitionId));
        if (!idOk) { context.AddExecutionLogEntry("Precondition Failed", idErr); return new ValueTask<bool>(false); }
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = context.Get(Project)!;
        var definitionId = context.Get(DefinitionId);
        var sourceBranch = context.Get(SourceBranch);
        var build = new Build
        {
            Definition = new BuildDefinitionReference { Id = definitionId },
            SourceBranch = sourceBranch ?? "refs/heads/main"
        };
        var connection = GetConnection(context);
        var buildClient = connection.GetClient<BuildHttpClient>();
        var queued = await buildClient.QueueBuildAsync(build, project, cancellationToken: context.CancellationToken);
        context.Set(QueuedBuild, queued);
        await context.CompleteActivityAsync();
    }
}
