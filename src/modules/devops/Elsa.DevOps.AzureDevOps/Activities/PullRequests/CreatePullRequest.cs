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
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var project = context.Get(Project);
        var repositoryName = context.Get(RepositoryName);
        var sourceBranch = context.Get(SourceBranch);
        var targetBranch = context.Get(TargetBranch);
        var title = context.Get(Title);
        var (projectOk, projectErr) = ActivityInputValidation.TryValidateRequired(project, nameof(Project));
        if (!projectOk) { context.AddExecutionLogEntry("Precondition Failed", projectErr); return new ValueTask<bool>(false); }
        var (repoOk, repoErr) = ActivityInputValidation.TryValidateRequired(repositoryName, nameof(RepositoryName));
        if (!repoOk) { context.AddExecutionLogEntry("Precondition Failed", repoErr); return new ValueTask<bool>(false); }
        var (srcOk, srcErr) = ActivityInputValidation.TryValidateRequired(sourceBranch, nameof(SourceBranch));
        if (!srcOk) { context.AddExecutionLogEntry("Precondition Failed", srcErr); return new ValueTask<bool>(false); }
        var (tgtOk, tgtErr) = ActivityInputValidation.TryValidateRequired(targetBranch, nameof(TargetBranch));
        if (!tgtOk) { context.AddExecutionLogEntry("Precondition Failed", tgtErr); return new ValueTask<bool>(false); }
        var (titleOk, titleErr) = ActivityInputValidation.TryValidateRequired(title, nameof(Title));
        if (!titleOk) { context.AddExecutionLogEntry("Precondition Failed", titleErr); return new ValueTask<bool>(false); }
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = context.Get(Project)!;
        var repositoryName = context.Get(RepositoryName)!;
        var sourceBranch = NormalizeRef(context.Get(SourceBranch)!);
        var targetBranch = NormalizeRef(context.Get(TargetBranch)!);
        var title = context.Get(Title)!;
        var description = context.Get(Description);
        var pr = new GitPullRequest
        {
            SourceRefName = sourceBranch,
            TargetRefName = targetBranch,
            Title = title,
            Description = description ?? string.Empty
        };
        var connection = GetConnection(context);
        var gitClient = connection.GetClient<GitHttpClient>();
        var created = await gitClient.CreatePullRequestAsync(pr, project, repositoryName, null, null, context.CancellationToken);
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
