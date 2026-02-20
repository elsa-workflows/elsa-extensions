using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.Activities.PullRequests;

/// <summary>
/// Lists pull requests in an Azure DevOps Git repository.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.PullRequests",
    "Azure DevOps Pull Requests",
    "Lists pull requests in an Azure DevOps Git repository.",
    DisplayName = "List Pull Requests")]
[UsedImplicitly]
public class ListPullRequests : AzureDevOpsActivity
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
    /// The pull request status filter (e.g. Active, Completed, All). Optional.
    /// </summary>
    [Input(Description = "The pull request status filter (e.g. Active, Completed, All). Optional.")]
    public Input<string?> Status { get; set; } = null!;

    /// <summary>
    /// The number of pull requests to retrieve. Optional.
    /// </summary>
    [Input(Description = "The number of pull requests to retrieve. Optional.")]
    public Input<int?> Top { get; set; } = null!;

    /// <summary>
    /// The number of pull requests to skip. Optional.
    /// </summary>
    [Input(Description = "The number of pull requests to skip. Optional.")]
    public Input<int?> Skip { get; set; } = null!;

    /// <summary>
    /// The list of pull requests.
    /// </summary>
    [Output(Description = "The list of pull requests.")]
    public Output<IEnumerable<GitPullRequest>> PullRequests { get; set; } = null!;

    /// <inheritdoc />
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var project = context.Get(Project);
        var repositoryName = context.Get(RepositoryName);
        var top = context.Get(Top);
        var skip = context.Get(Skip);
        var (projectOk, projectErr) = ActivityInputValidation.TryValidateRequired(project, nameof(Project));
        if (!projectOk) { context.AddExecutionLogEntry("Precondition Failed", projectErr); return new ValueTask<bool>(false); }
        var (repoOk, repoErr) = ActivityInputValidation.TryValidateRequired(repositoryName, nameof(RepositoryName));
        if (!repoOk) { context.AddExecutionLogEntry("Precondition Failed", repoErr); return new ValueTask<bool>(false); }
        if (top is < 0) { context.AddExecutionLogEntry("Precondition Failed", "'Top' must be non-negative when specified."); return new ValueTask<bool>(false); }
        if (skip is < 0) { context.AddExecutionLogEntry("Precondition Failed", "'Skip' must be non-negative when specified."); return new ValueTask<bool>(false); }
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = context.Get(Project)!;
        var repositoryName = context.Get(RepositoryName)!;
        var statusInput = context.Get(Status);
        var top = context.Get(Top);
        var skip = context.Get(Skip);
        var connection = GetConnection(context);
        var gitClient = connection.GetClient<GitHttpClient>();
        var searchCriteria = new GitPullRequestSearchCriteria();
        if (!string.IsNullOrEmpty(statusInput) && Enum.TryParse<PullRequestStatus>(statusInput, true, out var status))
            searchCriteria.Status = status;
        var list = await gitClient.GetPullRequestsAsync(project, repositoryName, searchCriteria, null, skip, top, null, context.CancellationToken);
        context.Set(PullRequests, list ?? (IEnumerable<GitPullRequest>)Array.Empty<GitPullRequest>());
    }
}
