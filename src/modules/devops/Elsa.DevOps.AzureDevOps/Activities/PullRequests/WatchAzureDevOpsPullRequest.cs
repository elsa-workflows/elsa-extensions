using Elsa.DevOps.AzureDevOps.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using JetBrains.Annotations;

namespace Elsa.DevOps.AzureDevOps.Activities.PullRequests;

/// <summary>
/// Re-reads the watched pull request and dispatches an update when it changed. Leaves through <c>Closed</c> once the
/// pull request is no longer active, which is what ends the watcher.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.PullRequests",
    "Azure DevOps Pull Requests",
    "Reports a change to the watched pull request and whether it is still open.",
    DisplayName = "Watch Pull Request")]
[FlowNode(ActiveOutcome, ClosedOutcome)]
[UsedImplicitly]
public class WatchAzureDevOpsPullRequest : CodeActivity
{
    public const string ActiveOutcome = "Active";
    public const string ClosedOutcome = "Closed";

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string project = context.GetVariable<string>(InitializePullRequestWatch.ProjectVariableName)!;
        string repositoryId = context.GetVariable<string>(InitializePullRequestWatch.RepositoryIdVariableName)!;
        int pullRequestId = context.GetVariable<int>(InitializePullRequestWatch.PullRequestIdVariableName);
        string? fingerprint = context.GetVariable<string>(InitializePullRequestWatch.FingerprintVariableName);
        // Recorded when watching started, so every check re-reads the pull request over the connection the poll found
        // it on. Both are null for a watcher of the configured organization.
        string? organizationUrl = context.GetVariable<string>(InitializePullRequestWatch.OrganizationUrlVariableName);
        string? tokenSecretName = context.GetVariable<string>(InitializePullRequestWatch.TokenSecretNameVariableName);

        AzureDevOpsPullRequestWatcher watcher = context.GetRequiredService<AzureDevOpsPullRequestWatcher>();
        PullRequestWatchResult result = await watcher
            .CheckAsync(
                new PullRequestWatchTarget(project, repositoryId, pullRequestId, organizationUrl, tokenSecretName),
                fingerprint,
                context.CancellationToken)
            .ConfigureAwait(false);
        context.SetVariable(InitializePullRequestWatch.FingerprintVariableName, result.Fingerprint);

        if (result.Changed)
        {
            context.AddExecutionLogEntry(
                eventName: "AzureDevOpsPollingEvent",
                message: $"Triggered git.pullrequest.updated for pull request {pullRequestId} in project {project}.",
                source: nameof(WatchAzureDevOpsPullRequest));
        }

        await context.CompleteActivityWithOutcomesAsync(result.Active ? ActiveOutcome : ClosedOutcome).ConfigureAwait(false);
    }
}
