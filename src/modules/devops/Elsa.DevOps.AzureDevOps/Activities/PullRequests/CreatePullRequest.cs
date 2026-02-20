using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.Activities.PullRequests;

/// <summary>
/// Creates a pull request in Azure DevOps.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.PullRequests",
    "Azure DevOps Pull Requests",
    "Creates a pull request in Azure DevOps.",
    DisplayName = "Create Pull Request")]
[UsedImplicitly]
public class CreatePullRequest : AzureDevOpsActivity
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
    /// The source branch (e.g. refs/heads/feature or feature).
    /// </summary>
    [Input(Description = "The source branch (e.g. refs/heads/feature or feature).")]
    public Input<string> SourceBranch { get; set; } = null!;

    /// <summary>
    /// The target branch (e.g. refs/heads/main or main).
    /// </summary>
    [Input(Description = "The target branch (e.g. refs/heads/main or main).")]
    public Input<string> TargetBranch { get; set; } = null!;

    /// <summary>
    /// The pull request title.
    /// </summary>
    [Input(Description = "The pull request title.")]
    public Input<string> Title { get; set; } = null!;

    /// <summary>
    /// The pull request description. Optional.
    /// </summary>
    [Input(Description = "The pull request description. Optional.", UIHint = InputUIHints.MultiLine)]
    public Input<string?> Description { get; set; } = null!;

    /// <summary>
    /// The created pull request.
    /// </summary>
    [Output(Description = "The created pull request.")]
    public Output<GitPullRequest> CreatedPullRequest { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = context.Get(Project);
        var repositoryName = context.Get(RepositoryName);
        var sourceBranchRaw = context.Get(SourceBranch);
        var targetBranchRaw = context.Get(TargetBranch);
        var title = context.Get(Title);
        ActivityInputValidation.ThrowIfNullOrEmpty(project, nameof(Project));
        ActivityInputValidation.ThrowIfNullOrEmpty(repositoryName, nameof(RepositoryName));
        ActivityInputValidation.ThrowIfNullOrEmpty(sourceBranchRaw, nameof(SourceBranch));
        ActivityInputValidation.ThrowIfNullOrEmpty(targetBranchRaw, nameof(TargetBranch));
        ActivityInputValidation.ThrowIfNullOrEmpty(title, nameof(Title));
        var sourceBranch = NormalizeRef(sourceBranchRaw!);
        var targetBranch = NormalizeRef(targetBranchRaw!);
        var description = context.Get(Description);
        var pr = new GitPullRequest
        {
            SourceRefName = sourceBranch,
            TargetRefName = targetBranch,
            Title = title!,
            Description = description ?? string.Empty
        };
        var connection = GetConnection(context);
        var gitClient = connection.GetClient<GitHttpClient>();
        var created = await gitClient.CreatePullRequestAsync(pr, project!, repositoryName!, null, null, context.CancellationToken);
        context.Set(CreatedPullRequest, created);
    }

    private static string NormalizeRef(string refName)
    {
        if (string.IsNullOrWhiteSpace(refName)) return refName;
        refName = refName.Trim();
        if (refName.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase)) return refName;
        return "refs/heads/" + refName;
    }
}
