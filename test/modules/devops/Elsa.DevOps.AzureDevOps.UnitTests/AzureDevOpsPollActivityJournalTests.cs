using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.Extensions;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// The journal records the runtime type of everything a poll activity writes to it and reconstructs that type when the
/// entry is read back, so a poll may only journal a collection the journal can build again.
/// </summary>
public class AzureDevOpsPollActivityJournalTests
{
    /// <summary>
    /// A poll returning the shape <see cref="Activities.WorkItems.PollAzureDevOpsWorkItems"/> returns: a collection
    /// expression over two sources. The compiler lowers that to a synthesised read-only array type, which has no
    /// parameterless constructor, and reading the journal entry back then fails with "Cannot dynamically create an
    /// instance of type '&lt;&gt;z__ReadOnlyArray`1[...AzureDevOpsPollingEvent]'".
    /// </summary>
    private sealed class SpreadingPoll : AzureDevOpsPollActivity
    {
        public object? Journalled { get; private set; }

        protected override ValueTask<IReadOnlyList<AzureDevOpsPollingEvent>> PollAsync(ActivityExecutionContext context)
        {
            List<AzureDevOpsPollingEvent> polled = [new("workitem.created", "Project", "1", "work item 1 (Bug)")];
            List<AzureDevOpsPollingEvent> deleted = [new("workitem.deleted", "Project", "2", "work item 2 (Task)")];

            IReadOnlyList<AzureDevOpsPollingEvent> events = [.. polled, .. deleted];

            return ValueTask.FromResult(events);
        }

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            await base.ExecuteAsync(context).ConfigureAwait(false);

            Journalled = context.JournalData["Events"];
        }
    }

    private static async Task<SpreadingPoll> RunPollAsync()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddElsa(_ => { });

        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        IWorkflowRunner runner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        SpreadingPoll activity = new();

        await runner.RunAsync(activity);

        return activity;
    }

    [Fact]
    public async Task JournalledEventsCanBeReconstructedByTheJournal()
    {
        SpreadingPoll activity = await RunPollAsync();

        Type journalled = Assert.IsAssignableFrom<object>(activity.Journalled).GetType();

        Assert.NotNull(Activator.CreateInstance(journalled));
    }

    [Fact]
    public async Task JournalledEventsKeepEveryPolledEvent()
    {
        SpreadingPoll activity = await RunPollAsync();

        IEnumerable<AzureDevOpsPollingEvent> journalled = Assert.IsAssignableFrom<IEnumerable<AzureDevOpsPollingEvent>>(activity.Journalled);

        Assert.Equal(
            ["workitem.created", "workitem.deleted"],
            journalled.Select(@event => @event.EventType));
    }
}
