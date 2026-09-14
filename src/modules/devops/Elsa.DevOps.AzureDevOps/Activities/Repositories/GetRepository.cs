using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.Activities.Repositories;

/// <summary>
/// Retrieves details of a Git repository in Azure DevOps.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.Repositories",
    "Azure DevOps Repositories",
    "Retrieves details of a Git repository in Azure DevOps.",
    DisplayName = "Get Repository")]
[UsedImplicitly]
public class GetRepository : AzureDevOpsActivity
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
    /// The retrieved repository.
    /// </summary>
    [Output(Description = "The retrieved repository.")]
    public Output<GitRepository> RetrievedRepository { get; set; } = null!;

    /// <inheritdoc />
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var project = ResolveProject(context, context.Get(Project));
        var repositoryName = context.Get(RepositoryName);
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateRequired(project, nameof(Project)));
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateRequired(repositoryName, nameof(RepositoryName)));
        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var project = ResolveProject(context, context.Get(Project))!;
        var repositoryName = context.Get(RepositoryName)!;
        var connection = await GetConnectionAsync(context);
        var gitClient = connection.GetClient<GitHttpClient>();
        var repository = await gitClient.GetRepositoryAsync(project, repositoryName, cancellationToken: context.CancellationToken);
        context.Set(RetrievedRepository, repository);
        await context.CompleteActivityAsync();
    }
}
