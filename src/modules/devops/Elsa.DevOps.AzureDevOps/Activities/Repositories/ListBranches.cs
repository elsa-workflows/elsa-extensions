using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.Activities.Repositories;

/// <summary>
/// Lists branches in an Azure DevOps Git repository.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.Repositories",
    "Azure DevOps Repositories",
    "Lists branches in an Azure DevOps Git repository.",
    DisplayName = "List Branches")]
[UsedImplicitly]
public class ListBranches : AzureDevOpsActivity
{
    /// <summary>
    /// The project name or ID.
    /// </summary>
    [Input(Description = "The project name or ID.")]
    public Input<string> Project { get; set; } = null!;

    /// <summary>
    /// The repository name or ID.
    /// </summary>
    [Input(Description = "The repository name or ID.")]
    public Input<string> RepositoryName { get; set; } = null!;

    /// <summary>
    /// The list of branches.
    /// </summary>
    [Output(Description = "The list of branches.")]
    public Output<IEnumerable<GitBranchStats>> Branches { get; set; } = null!;

    /// <inheritdoc />
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var project = context.Get(Project);
        var repositoryName = context.Get(RepositoryName);
        var (projectOk, projectErr) = ActivityInputValidation.TryValidateRequired(project, nameof(Project));
        if (!projectOk) { context.AddExecutionLogEntry("Precondition Failed", projectErr); return new ValueTask<bool>(false); }
        var (repoOk, repoErr) = ActivityInputValidation.TryValidateRequired(repositoryName, nameof(RepositoryName));
        if (!repoOk) { context.AddExecutionLogEntry("Precondition Failed", repoErr); return new ValueTask<bool>(false); }
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = context.Get(Project)!;
        var repositoryName = context.Get(RepositoryName)!;
        var connection = GetConnection(context);
        var gitClient = connection.GetClient<GitHttpClient>();
        var branches = await gitClient.GetBranchesAsync(project, repositoryName, null, null, context.CancellationToken);
        context.Set(Branches, branches ?? (IEnumerable<GitBranchStats>)Array.Empty<GitBranchStats>());
    }
}
