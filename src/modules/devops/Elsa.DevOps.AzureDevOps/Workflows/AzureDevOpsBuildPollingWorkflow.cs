using Elsa.DevOps.AzureDevOps.Activities.Builds;
using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Services.Polling;
using Elsa.Workflows;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.RuntimeWorkflows;

/// <summary>
/// System workflow that polls Azure DevOps build changes.
/// </summary>
public class AzureDevOpsBuildPollingWorkflow(IOptions<AzureDevOpsPollingOptions> options) : AzureDevOpsPollingWorkflowBase
{
    /// <summary>
    /// The event that triggers a poll outside of the schedule.
    /// </summary>
    public const string ManualPollEventName = "AzureDevOpsPolling:Builds:Manual";

    protected override string DefinitionId => AzureDevOpsPollingWorkflowNames.Builds;

    protected override TimeSpan Interval => options.Value.ResolveInterval(options.Value.Builds);

    protected override string ManualEventName => ManualPollEventName;

    protected override string PollDisplayText => "Poll build changes";

    protected override Activity CreatePollActivity() => new PollAzureDevOpsBuilds();

    protected override IEnumerable<Variable> CreateVariables() =>
    [
        new Variable<BuildPollingCheckpoints?>(PollAzureDevOpsBuilds.CheckpointsVariableName, null)
        {
            StorageDriverType = typeof(WorkflowInstanceStorageDriver),
        }
    ];
}
