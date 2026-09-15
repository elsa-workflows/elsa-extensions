using Elsa.DevOps.AzureDevOps.Activities.PullRequests;
using Elsa.DevOps.AzureDevOps.Configuration;
using ElsaTimer = Elsa.Scheduling.Activities.Timer;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Runtime.ActivationValidators;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.RuntimeWorkflows;

/// <summary>
/// System workflow that watches one pull request for updates. There is one instance per pull request, correlated on
/// the pull request ID, because Azure DevOps offers no way to query for pull request updates.
/// </summary>
public class AzureDevOpsPullRequestWatcherWorkflow(IOptions<AzureDevOpsPollingOptions> options) : WorkflowBase
{
    private TimeSpan Interval =>
        options.Value.PullRequests.WatchInterval is { } watchInterval && watchInterval > TimeSpan.Zero
            ? watchInterval
            : options.Value.ResolveInterval(options.Value.PullRequests);

    protected override void Build(IWorkflowBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AsReadonly();
        builder.AsSystemWorkflow();
        builder.WithDefinitionId(AzureDevOpsPollingWorkflowNames.PullRequestWatcher);
        // Correlated rather than plain singleton: one watcher per pull request, not one for all of them.
        builder.WithActivationStrategyType<CorrelatedSingletonStrategy>();
        // As on every family workflow: without it a single fault ends this watcher for good, and since the poller
        // only ever starts a watcher from a creation event, that pull request would never be watched again.
        builder.WorkflowOptions.IncidentStrategyType = typeof(ContinueWithIncidentsStrategy);

        foreach (Variable variable in CreateVariables())
            builder.WithVariable(variable);

        InitializePullRequestWatch initialize = AzureDevOpsWorkflowDesigner.Named(new InitializePullRequestWatch(), "Initialize", "Record the pull request");
        ElsaTimer timer = AzureDevOpsWorkflowDesigner.Named(ElsaTimer.FromTimeSpan(Interval), "ScheduledTrigger", $"Every {Interval:g}");
        WatchAzureDevOpsPullRequest watch = AzureDevOpsWorkflowDesigner.Named(new WatchAzureDevOpsPullRequest(), "Watch", "Check for updates");
        Finish finish = AzureDevOpsWorkflowDesigner.Named(new Finish(), "Done", "Pull request closed");

        AzureDevOpsWorkflowDesigner.At(initialize, 80, 260);
        AzureDevOpsWorkflowDesigner.At(timer, 320, 260);
        AzureDevOpsWorkflowDesigner.At(watch, 560, 260);
        AzureDevOpsWorkflowDesigner.At(finish, 800, 260);

        builder.Root = new Flowchart
        {
            Activities = [initialize, timer, watch, finish],
            Start = initialize,
            Connections =
            [
                // Initializing reads the pull request to seed the baseline, so it already knows whether the pull
                // request is still open. A closed one goes straight to Done: starting the timer would only buy an
                // interval of waiting before the first check finds the same thing.
                new Connection(new Endpoint(initialize, InitializePullRequestWatch.ActiveOutcome), new Endpoint(timer)),
                new Connection(new Endpoint(initialize, InitializePullRequestWatch.ClosedOutcome), new Endpoint(finish)),
                new Connection(timer, watch),
                // Resuming the timer consumes its bookmark, so the loop back is what keeps the schedule going.
                new Connection(new Endpoint(watch, WatchAzureDevOpsPullRequest.ActiveOutcome), new Endpoint(timer)),
                new Connection(new Endpoint(watch, WatchAzureDevOpsPullRequest.ClosedOutcome), new Endpoint(finish)),
            ],
        };
    }

    private static IEnumerable<Variable> CreateVariables() =>
    [
        new Variable<string?>(InitializePullRequestWatch.ProjectVariableName, null) { StorageDriverType = typeof(WorkflowInstanceStorageDriver) },
        new Variable<string?>(InitializePullRequestWatch.RepositoryIdVariableName, null) { StorageDriverType = typeof(WorkflowInstanceStorageDriver) },
        new Variable<int>(InitializePullRequestWatch.PullRequestIdVariableName, 0) { StorageDriverType = typeof(WorkflowInstanceStorageDriver) },
        new Variable<string?>(InitializePullRequestWatch.FingerprintVariableName, null) { StorageDriverType = typeof(WorkflowInstanceStorageDriver) },
        // The connection the poll found the pull request over. Null for the configured organization, which is also
        // what a watcher started before these variables existed finds when it resumes.
        new Variable<string?>(InitializePullRequestWatch.OrganizationUrlVariableName, null) { StorageDriverType = typeof(WorkflowInstanceStorageDriver) },
        new Variable<string?>(InitializePullRequestWatch.TokenSecretNameVariableName, null) { StorageDriverType = typeof(WorkflowInstanceStorageDriver) },
    ];
}
