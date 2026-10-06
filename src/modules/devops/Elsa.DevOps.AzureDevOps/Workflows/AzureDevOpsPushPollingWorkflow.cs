using Elsa.DevOps.AzureDevOps.Activities.Repositories;
using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.Workflows;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.RuntimeWorkflows;

/// <summary>
/// System workflow that polls Azure Repos pushes.
/// </summary>
public class AzureDevOpsPushPollingWorkflow(IOptions<AzureDevOpsPollingOptions> options) : AzureDevOpsPollingWorkflowBase
{
    /// <summary>
    /// The event that triggers a poll outside of the schedule.
    /// </summary>
    public const string ManualPollEventName = "AzureDevOpsPolling:Pushes:Manual";

    protected override string DefinitionId => AzureDevOpsPollingWorkflowNames.Pushes;

    protected override TimeSpan Interval => options.Value.ResolveInterval(options.Value.Pushes);

    protected override string ManualEventName => ManualPollEventName;

    protected override string PollDisplayText => "Poll pushes";

    protected override Activity CreatePollActivity() => new PollAzureDevOpsPushes();

    protected override IEnumerable<Variable> CreateVariables() =>
    [
        new Variable<Dictionary<string, DateTimeOffset>?>(PollAzureDevOpsPushes.CheckpointsVariableName, null)
        {
            StorageDriverType = typeof(WorkflowInstanceStorageDriver),
        }
    ];
}
