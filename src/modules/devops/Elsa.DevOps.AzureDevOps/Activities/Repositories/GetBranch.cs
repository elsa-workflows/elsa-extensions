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
    [Input(Description = ProjectDescription)]
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
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var project = ResolveProject(context, context.Get(Project));
        var repositoryName = context.Get(RepositoryName);
        var branchName = context.Get(BranchName);
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateRequired(project, nameof(Project)));
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateRequired(repositoryName, nameof(RepositoryName)));
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateRequired(branchName, nameof(BranchName)));
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = ResolveProject(context, context.Get(Project))!;
        var repositoryName = context.Get(RepositoryName)!;
        var branchName = context.Get(BranchName)!;
        var connection = await GetConnectionAsync(context);
        var gitClient = connection.GetClient<GitHttpClient>();
        var branches = await gitClient.GetBranchesAsync(project, repositoryName, cancellationToken: context.CancellationToken);
        var normalizedName = branchName.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase) ? branchName : "refs/heads/" + branchName;
        var branch = branches?.FirstOrDefault(b =>
            string.Equals(b.Name, branchName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(b.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
        context.Set(Branch, branch);
        await context.CompleteActivityAsync();
    }
}
