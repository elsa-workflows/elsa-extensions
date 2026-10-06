using ElsaTimer = Elsa.Scheduling.Activities.Timer;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.ActivationValidators;

namespace Elsa.DevOps.AzureDevOps.RuntimeWorkflows;

/// <summary>
/// The shape every Azure DevOps polling workflow has: a schedule and a manual event both feeding one poll activity,
/// which loops back onto the timer.
/// </summary>
public abstract class AzureDevOpsPollingWorkflowBase : WorkflowBase
{
    protected abstract string DefinitionId { get; }

    protected abstract TimeSpan Interval { get; }

    /// <summary>
    /// The event that triggers a poll outside of the schedule.
    /// </summary>
    protected abstract string ManualEventName { get; }

    /// <summary>
    /// The label the poll activity carries on the Elsa Studio design surface.
    /// </summary>
    protected abstract string PollDisplayText { get; }

    protected abstract Activity CreatePollActivity();

    /// <summary>
    /// The variables holding this family's checkpoints. They live on the workflow instance, so a restart continues
    /// where the previous poll left off.
    /// </summary>
    protected virtual IEnumerable<Variable> CreateVariables() => [];

    protected override void Build(IWorkflowBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AsReadonly();
        builder.AsSystemWorkflow();
        builder.WithDefinitionId(DefinitionId);
        builder.WithActivationStrategyType<SingletonStrategy>();
        builder.WorkflowOptions.IncidentStrategyType = typeof(ContinueWithIncidentsStrategy);

        foreach (Variable variable in CreateVariables())
            builder.WithVariable(variable);

        ElsaTimer timer = AzureDevOpsWorkflowDesigner.Named(ElsaTimer.FromTimeSpan(Interval), "ScheduledTrigger", $"Every {Interval:g}");
        Event manual = AzureDevOpsWorkflowDesigner.Named(new Event(ManualEventName), "ManualTrigger", "On manual poll request");
        manual.CanStartWorkflow = true;
        Activity poll = AzureDevOpsWorkflowDesigner.Named(CreatePollActivity(), "Poll", PollDisplayText);
        poll.CanStartWorkflow = true;

        // Both triggers feed the same poll, which loops back onto the timer: resuming the timer consumes its
        // bookmark, so without that loop the schedule would fire exactly once.
        AzureDevOpsWorkflowDesigner.At(timer, 80, 160);
        AzureDevOpsWorkflowDesigner.At(manual, 80, 380);
        AzureDevOpsWorkflowDesigner.At(poll, 360, 270);

        builder.Root = new Flowchart
        {
            Activities = [timer, manual, poll],
            Start = timer,
            Connections =
            [
                new Connection(timer, poll),
                new Connection(manual, poll),
                new Connection(poll, timer),
            ],
        };
    }
}
