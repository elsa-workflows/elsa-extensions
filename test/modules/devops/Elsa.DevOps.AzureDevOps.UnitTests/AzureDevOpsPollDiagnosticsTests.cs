using Elsa.DevOps.AzureDevOps.Activities;
using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// Covers the line every poll writes to the journal saying that it ran. A poll that finds nothing is
/// indistinguishable from a poll that never happened - the family switch, the master switch and a workflow that was
/// never started all end in the same silence - and that is the question this line exists to answer.
/// </summary>
public class AzureDevOpsPollDiagnosticsTests
{
    [Fact]
    public async Task A_poll_that_dispatched_nothing_still_says_it_ran()
    {
        SilentPoll activity = await RunPollAsync();

        WorkflowExecutionLogEntry entry = Assert.Single(activity.Diagnostics);

        Assert.Contains("SilentPoll", entry.Message);
    }

    [Fact]
    public async Task The_journal_entry_carries_how_many_events_the_poll_dispatched()
    {
        SilentPoll activity = await RunPollAsync(events: [new("build.queued", "Contoso Web Platform", "1", "build 1")]);

        WorkflowExecutionLogEntry entry = Assert.Single(activity.Diagnostics);

        Assert.Contains("1 event", entry.Message);
    }

    [Fact]
    public async Task The_configured_level_names_the_journal_entry()
    {
        // The journal has no severity of its own, so the level travels as the event name - the same way the rest of
        // this codebase writes "Warning" and "Info" entries. Naming it is what makes the line stand out in Studio.
        SilentPoll activity = await RunPollAsync(level: LogLevel.Error);

        WorkflowExecutionLogEntry entry = Assert.Single(activity.Diagnostics);

        Assert.Equal("Error", entry.EventName);
    }

    [Fact]
    public async Task Nothing_is_written_when_the_diagnostics_are_switched_off()
    {
        // A deployment that wants its journal back has to be able to have it without a code change.
        SilentPoll activity = await RunPollAsync(level: LogLevel.None);

        Assert.Empty(activity.Diagnostics);
    }

    /// <summary>
    /// A poll that reaches the journal without calling Azure DevOps. It dispatches whatever it is handed, so that the
    /// line can be read for a poll that found nothing as well as for one that found something.
    /// </summary>
    private sealed class SilentPoll(IReadOnlyList<AzureDevOpsPollingEvent> events) : AzureDevOpsPollActivity
    {
        public IReadOnlyList<WorkflowExecutionLogEntry> Diagnostics { get; private set; } = [];

        protected override ValueTask<IReadOnlyList<AzureDevOpsPollingEvent>> PollAsync(ActivityExecutionContext context) =>
            ValueTask.FromResult(events);

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            await base.ExecuteAsync(context).ConfigureAwait(false);

            // The per-event entries are a different line with a different name; this reads only the one that reports
            // the poll itself.
            Diagnostics =
            [
                .. context.WorkflowExecutionContext.ExecutionLog
                    .Where(entry => entry.Source == nameof(SilentPoll) && entry.EventName != "AzureDevOpsPollingEvent")
            ];
        }
    }

    private static async Task<SilentPoll> RunPollAsync(
        LogLevel? level = null,
        IReadOnlyList<AzureDevOpsPollingEvent>? events = null)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddElsa(_ => { });

        if (level is { } configured)
            services.Configure<AzureDevOpsPollingOptions>(options => options.Diagnostics.Level = configured);

        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        IWorkflowRunner runner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        SilentPoll activity = new(events ?? []);

        await runner.RunAsync(activity);

        return activity;
    }
}
