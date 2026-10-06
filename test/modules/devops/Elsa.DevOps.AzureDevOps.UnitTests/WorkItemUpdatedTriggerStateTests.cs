using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Triggers;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// The work item triggers offer the state as a plain string next to the work item. These run the real trigger to check
/// it arrives.
/// </summary>
public class WorkItemUpdatedTriggerStateTests
{
    [Fact]
    public async Task HandsOverTheStateAsAPlainString()
    {
        WorkItem workItem = new()
        {
            Id = 41290,
            Fields = new Dictionary<string, object>
            {
                ["System.WorkItemType"] = "Bug",
                ["System.State"] = "Closed",
            },
        };

        Captured captured = await RunAsync(new AzureDevOpsWebhookEvent(AzureDevOpsWebhookEventTypes.WorkItemUpdated, workItem));

        // The work item trigger fires on every edit, a tag included. A workflow that waits for the work item to be
        // finished has to be able to read the state as a plain string - navigating into the work item from a workflow
        // binding is exactly what the code-defined workflow rules forbid.
        Assert.Equal("Closed", captured.State);
        Assert.Equal(41290, captured.WorkItem?.Id);
    }

    [Fact]
    public async Task HandsOverAnEmptyStateWhenTheEventCarriesNone()
    {
        // An empty string rather than nothing, so a flow decision matching on the state does not have to guard for
        // null before it can compare.
        WorkItem workItem = new() { Id = 41290, Fields = new Dictionary<string, object> { ["System.Title"] = "Iets" } };

        Captured captured = await RunAsync(new AzureDevOpsWebhookEvent(AzureDevOpsWebhookEventTypes.WorkItemUpdated, workItem));

        Assert.Equal(string.Empty, captured.State);
    }

    private static async Task<Captured> RunAsync(AzureDevOpsWebhookEvent message)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddElsa();

        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        Variable<WorkItem> workItem = new();
        Variable<string> state = new();
        Captured captured = new();

        WorkItemUpdatedTrigger trigger = new()
        {
            ProjectId = new Input<string>("Contoso"),
            WorkItem = new Output<WorkItem>(workItem),
            State = new Output<string>(state),
        };

        Sequence sequence = new()
        {
            Variables = { workItem, state },
            Activities =
            {
                trigger,
                new Inline(context =>
                {
                    captured.WorkItem = workItem.Get(context);
                    captured.State = state.Get(context);
                }),
            },
        };

        IWorkflowRunner runner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();

        // The trigger completes straight away when the run carries the event as input; without it, it would create a
        // bookmark and wait.
        await runner.RunAsync(sequence, new RunWorkflowOptions
        {
            Input = new Dictionary<string, object> { ["Message"] = message },
        });

        return captured;
    }

    private sealed class Captured
    {
        public WorkItem? WorkItem { get; set; }

        public string? State { get; set; }
    }
}
