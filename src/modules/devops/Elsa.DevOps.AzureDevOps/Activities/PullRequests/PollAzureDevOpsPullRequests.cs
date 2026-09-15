using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.DevOps.AzureDevOps.Services.Polling;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.Activities.PullRequests;

[Activity(
    "Elsa.AzureDevOps.PullRequests",
    "Azure DevOps Pull Requests",
    "Polls Azure DevOps pull request changes and dispatches matching triggers.",
    DisplayName = "Poll Pull Request Changes")]
[UsedImplicitly]
public class PollAzureDevOpsPullRequests : AzureDevOpsPollActivity
{
    public const string CheckpointsVariableName = "AzureDevOpsPullRequestPollingCheckpoints";

    /// <inheritdoc cref="PullRequestPollingOptions.WatchUpdates"/>
    [Input(Description = "Whether a watcher workflow is started per new pull request to report git.pullrequest.updated, which no query can produce. Falls back to the configured setting (AzureDevOps:Polling:PullRequests:WatchUpdates) when left empty. A poll of another organization only watches updates when it names TokenSecretName rather than Token, because a watcher resolves its own credential.")]
    public Input<bool?> WatchUpdates { get; set; } = null!;

    protected override async ValueTask<IReadOnlyList<AzureDevOpsPollingEvent>> PollAsync(ActivityExecutionContext context)
    {
        AzureDevOpsPullRequestPollingDispatcher dispatcher = context.GetRequiredService<AzureDevOpsPullRequestPollingDispatcher>();
        PullRequestPollingOptions pollingOptions = context.GetRequiredService<IOptions<AzureDevOpsPollingOptions>>().Value.PullRequests
            .With(new PullRequestPollingOverrides(GetSharedOverrides(context))
            {
                WatchUpdates = context.Get(WatchUpdates),
            });
        PullRequestPollingCheckpoints? checkpoints = context.GetVariable<PullRequestPollingCheckpoints>(CheckpointsVariableName);
        AzureDevOpsPullRequestPollingResult result = await dispatcher.DispatchAsync(pollingOptions, checkpoints, context.CancellationToken).ConfigureAwait(false);
        context.SetVariable(CheckpointsVariableName, result.Checkpoints);

        return result.Events;
    }
}
