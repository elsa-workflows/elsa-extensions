using Elsa.DevOps.AzureDevOps.Activities.PullRequests;
using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Services.Polling;
using Elsa.Workflows;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.RuntimeWorkflows;

/// <summary>
/// System workflow that polls Azure DevOps pull request changes.
/// </summary>
public class AzureDevOpsPullRequestPollingWorkflow(IOptions<AzureDevOpsPollingOptions> options) : AzureDevOpsPollingWorkflowBase
{
    /// <summary>
    /// The event that triggers a poll outside of the schedule.
    /// </summary>
    public const string ManualPollEventName = "AzureDevOpsPolling:PullRequests:Manual";

    protected override string DefinitionId => AzureDevOpsPollingWorkflowNames.PullRequests;

    protected override TimeSpan Interval => options.Value.ResolveInterval(options.Value.PullRequests);

    protected override string ManualEventName => ManualPollEventName;

    protected override string PollDisplayText => "Poll pull request changes";

    protected override Activity CreatePollActivity() => new PollAzureDevOpsPullRequests();

    protected override IEnumerable<Variable> CreateVariables() =>
    [
        new Variable<PullRequestPollingCheckpoints?>(PollAzureDevOpsPullRequests.CheckpointsVariableName, null)
        {
            StorageDriverType = typeof(WorkflowInstanceStorageDriver),
        }
    ];
}
