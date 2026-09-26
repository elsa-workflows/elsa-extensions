using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.DevOps.AzureDevOps.Services.Polling;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.Activities.Builds;

[Activity(
    "Elsa.AzureDevOps.Builds",
    "Azure DevOps Builds",
    "Polls Azure DevOps build changes and dispatches matching triggers.",
    DisplayName = "Poll Build Changes")]
[UsedImplicitly]
public class PollAzureDevOpsBuilds : AzureDevOpsPollActivity
{
    public const string CheckpointsVariableName = "AzureDevOpsBuildPollingCheckpoints";

    protected override async ValueTask<IReadOnlyList<AzureDevOpsPollingEvent>> PollAsync(ActivityExecutionContext context)
    {
        AzureDevOpsBuildPollingDispatcher dispatcher = context.GetRequiredService<AzureDevOpsBuildPollingDispatcher>();
        BuildPollingOptions pollingOptions = context.GetRequiredService<IOptions<AzureDevOpsPollingOptions>>().Value.Builds
            .With(GetSharedOverrides(context));
        BuildPollingCheckpoints? checkpoints = context.GetVariable<BuildPollingCheckpoints>(CheckpointsVariableName);
        AzureDevOpsBuildPollingResult result = await dispatcher.DispatchAsync(pollingOptions, checkpoints, context.CancellationToken).ConfigureAwait(false);
        context.SetVariable(CheckpointsVariableName, result.Checkpoints);

        return result.Events;
    }
}
