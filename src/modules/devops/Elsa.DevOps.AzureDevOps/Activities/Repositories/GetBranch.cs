using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.Activities.Repositories;

/// <summary>
/// Retrieves details of a specific branch in an Azure DevOps Git repository.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.Repositories",
    "Azure DevOps Repositories",
    "Retrieves details of a specific branch in an Azure DevOps Git repository.",
    DisplayName = "Get Branch")]
[UsedImplicitly]
public class GetBranch : AzureDevOpsActivity
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
    /// The branch name (e.g. refs/heads/main or main).
    /// </summary>
    [Input(Description = "The branch name (e.g. refs/heads/main or main).")]
    public Input<string> BranchName { get; set; } = null!;

    /// <summary>
    /// The retrieved branch statistics.
    /// </summary>
    [Output(Description = "The retrieved branch statistics.")]
    public Output<GitBranchStats?> Branch { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = context.Get(Project);
        var repositoryName = context.Get(RepositoryName);
        var branchName = context.Get(BranchName);
        ActivityInputValidation.ThrowIfNullOrEmpty(project, nameof(Project));
        ActivityInputValidation.ThrowIfNullOrEmpty(repositoryName, nameof(RepositoryName));
        ActivityInputValidation.ThrowIfNullOrEmpty(branchName, nameof(BranchName));
        var connection = GetConnection(context);
        var gitClient = connection.GetClient<GitHttpClient>();
        var branches = await gitClient.GetBranchesAsync(project!, repositoryName!, null, null, context.CancellationToken);
        var normalizedName = branchName!.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase) ? branchName : "refs/heads/" + branchName;
        var branch = branches?.FirstOrDefault(b =>
            string.Equals(b.Name, branchName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(b.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
        context.Set(Branch, branch);
    }
}
