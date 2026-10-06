using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.DevOps.AzureDevOps.RuntimeWorkflows;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// Covers the shape of <see cref="AzureDevOpsPullRequestWatcherWorkflow"/>: a pull request that is already closed
/// when watching starts must end the instance at once, and an open one must still park on the timer.
/// </summary>
public class AzureDevOpsPullRequestWatcherWorkflowTests
{
    [Fact]
    public async Task Watcher_finishes_without_starting_the_timer_when_the_pull_request_is_already_closed()
    {
        // The check that seeds the fingerprint already knows whether the pull request is open, so a closed one has no
        // reason to wait out an interval first. Polling switched off is the deterministic way to reach that: the
        // watcher service reports "not active" from its own switch guard, without an Azure DevOps call.
        WorkflowState state = await RunAsync(new AzureDevOpsPollingOptions { Enabled = false });

        Assert.Equal(WorkflowStatus.Finished, state.Status);
        // The timer never ran, so there is nothing left for the scheduler to resume.
        Assert.Empty(state.Bookmarks);
    }

    [Fact]
    public async Task Watcher_waits_on_the_timer_when_the_pull_request_is_still_open()
    {
        // The other half of the same decision, and the one that would break silently: with polling on and nothing to
        // read the pull request with, the seeding check reports the pull request as still active and the watcher must
        // suspend on its timer exactly as it always did.
        WorkflowState state = await RunAsync(new AzureDevOpsPollingOptions { Enabled = true });

        Assert.Equal(WorkflowStatus.Running, state.Status);
        Assert.NotEmpty(state.Bookmarks);
    }

    private static async Task<WorkflowState> RunAsync(AzureDevOpsPollingOptions pollingOptions)
    {
        ServiceCollection services = new();
        services.AddLogging();
        // Secrets are a feature the host opts into, so the extension never registers one itself; without it the
        // watcher service cannot even be constructed and every run ends as an incident instead of a decision.
        services.AddSingleton(Substitute.For<IAzureDevOpsSecretReader>());
        services.AddElsa(elsa => elsa.UseAzureDevOps(feature =>
            feature.ConfigurePollingOptions = options =>
            {
                options.Enabled = pollingOptions.Enabled;
                options.PullRequests.WatchUpdates = pollingOptions.PullRequests.WatchUpdates;
            }));

        await using ServiceProvider provider = services.BuildServiceProvider();
        IWorkflowBuilder builder = provider.GetRequiredService<IWorkflowBuilderFactory>().CreateBuilder();
        Workflow workflow = await builder.BuildWorkflowAsync(
            new AzureDevOpsPullRequestWatcherWorkflow(provider.GetRequiredService<IOptions<AzureDevOpsPollingOptions>>()));

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IWorkflowRunner runner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        RunWorkflowResult result = await runner.RunAsync(
            workflow,
            new RunWorkflowOptions
            {
                Input = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["PullRequestId"] = 1,
                    ["Project"] = "MyProject",
                    ["RepositoryId"] = "MyRepo",
                },
            },
            CancellationToken.None);

        return result.WorkflowState;
    }
}
