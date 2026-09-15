using Elsa.DevOps.AzureDevOps.Activities.WorkItems;
using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.Workflows;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.RuntimeWorkflows;

/// <summary>
/// System workflow that polls Azure DevOps work item changes.
/// </summary>
public class AzureDevOpsWorkItemPollingWorkflow(IOptions<AzureDevOpsPollingOptions> options) : AzureDevOpsPollingWorkflowBase
{
    /// <summary>
    /// The event that triggers a poll outside of the schedule.
    /// </summary>
    public const string ManualPollEventName = "AzureDevOpsPolling:Manual";

    protected override string DefinitionId => AzureDevOpsPollingWorkflowNames.WorkItems;

    protected override TimeSpan Interval => options.Value.ResolveInterval(options.Value.WorkItems);

    protected override string ManualEventName => ManualPollEventName;

    protected override string PollDisplayText => "Poll work item changes";

    protected override Activity CreatePollActivity() => new PollAzureDevOpsWorkItems();

    protected override IEnumerable<Variable> CreateVariables() =>
    [
        new Variable<DateTimeOffset?>(PollAzureDevOpsWorkItems.LastSeenVariableName, null)
        {
            StorageDriverType = typeof(WorkflowInstanceStorageDriver),
        },
        new Variable<List<int>?>(PollAzureDevOpsWorkItems.DeletedIdsVariableName, null)
        {
            StorageDriverType = typeof(WorkflowInstanceStorageDriver),
        }
    ];
}
