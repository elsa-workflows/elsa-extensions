using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.Activities.PullRequests;

/// <summary>
/// Retrieves details of a pull request in Azure DevOps.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.PullRequests",
    "Azure DevOps Pull Requests",
    "Retrieves details of a pull request in Azure DevOps.",
    DisplayName = "Get Pull Request")]
[UsedImplicitly]
public class GetPullRequest : AzureDevOpsActivity
{
    /// <summary>
    /// The pull request ID.
    /// </summary>
    [Input(Description = "The pull request ID.")]
    public Input<int> PullRequestId { get; set; } = null!;

    /// <summary>
    /// The project name or ID (optional; can improve performance).
    /// </summary>
    [Input(Description = "The project name or ID (optional; can improve performance).")]
    public Input<string?> Project { get; set; } = null!;

    /// <summary>
    /// The retrieved pull request.
    /// </summary>
    [Output(Description = "The retrieved pull request.")]
    public Output<GitPullRequest> RetrievedPullRequest { get; set; } = null!;

    /// <inheritdoc />
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var pullRequestId = context.Get(PullRequestId);
        var (idOk, idErr) = ActivityInputValidation.TryValidatePositive(pullRequestId, nameof(PullRequestId));
        if (!idOk) { context.AddExecutionLogEntry("Precondition Failed", idErr); return new ValueTask<bool>(false); }
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var pullRequestId = context.Get(PullRequestId);
        var project = context.Get(Project);
        var connection = GetConnection(context);
        var gitClient = connection.GetClient<GitHttpClient>();
        GitPullRequest pr;
        if (!string.IsNullOrEmpty(project))
            pr = await gitClient.GetPullRequestByIdAsync(project, pullRequestId, cancellationToken: context.CancellationToken);
        else
            pr = await gitClient.GetPullRequestByIdAsync(pullRequestId, cancellationToken: context.CancellationToken);
        context.Set(RetrievedPullRequest, pr);
        await context.CompleteActivityAsync();
    }
}
