using Elsa.Workflows;
using Elsa.Workflows.Models;

namespace Elsa.DevOps.AzureDevOps.Activities;

/// <summary>
/// Generic base class inherited by all Azure DevOps trigger activities.
/// </summary>
public abstract class AzureDevOpsTriggerActivity : AzureDevOpsActivity
{
    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Implementation depends on Azure DevOps Service Hooks / WebHook support
        throw new NotImplementedException("Event subscription requires WebHook implementation.");
    }
}
