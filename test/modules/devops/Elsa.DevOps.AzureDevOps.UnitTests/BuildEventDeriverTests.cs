using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services.Polling;
using Microsoft.TeamFoundation.Build.WebApi;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

public class BuildEventDeriverTests
{
    private static readonly DateTimeOffset Checkpoint = new(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);

    private static BuildPollingCheckpoints Checkpoints() => new()
    {
        LastQueued = Checkpoint,
        LastStarted = Checkpoint,
        LastFinished = Checkpoint,
    };

    private static Build BuildWith(int id, DateTime? queued = null, DateTime? started = null, DateTime? finished = null) => new()
    {
        Id = id,
        QueueTime = queued,
        StartTime = started,
        FinishTime = finished,
    };

    private static DateTime At(int minutes) => Checkpoint.UtcDateTime.AddMinutes(minutes);

    [Fact]
    public void Derive_reports_a_build_queued_after_the_checkpoint()
    {
        var build = BuildWith(1, queued: At(1));

        var events = BuildEventDeriver.Derive([build], [], [], Checkpoints());

        var single = Assert.Single(events);
        Assert.Equal(AzureDevOpsWebhookEventTypes.BuildQueued, single.EventType);
        Assert.Equal(1, single.Build.Id);
    }

    [Fact]
    public void Derive_ignores_a_build_sitting_exactly_on_the_checkpoint()
    {
        // minTime is inclusive server-side, so the newest build of a poll comes back on the next one; a >= test would
        // dispatch it twice.
        var build = BuildWith(1, queued: Checkpoint.UtcDateTime);

        Assert.Empty(BuildEventDeriver.Derive([build], [], [], Checkpoints()));
    }

    [Fact]
    public void Derive_reports_all_three_events_for_a_build_that_runs_inside_one_interval()
    {
        var build = BuildWith(1, queued: At(1), started: At(2), finished: At(3));

        var events = BuildEventDeriver.Derive([build], [build], [build], Checkpoints());

        Assert.Equal(
            [AzureDevOpsWebhookEventTypes.BuildQueued, AzureDevOpsWebhookEventTypes.BuildStarted, AzureDevOpsWebhookEventTypes.BuildCompleted],
            events.Select(e => e.EventType));
    }

    [Fact]
    public void Derive_reports_only_the_completion_of_a_build_queued_before_the_checkpoint()
    {
        // A long build is queued in one interval and finishes several intervals later; its queue and start were
        // already reported then.
        var build = BuildWith(1, queued: At(-30), started: At(-29), finished: At(4));

        var events = BuildEventDeriver.Derive([build], [build], [build], Checkpoints());

        var single = Assert.Single(events);
        Assert.Equal(AzureDevOpsWebhookEventTypes.BuildCompleted, single.EventType);
    }

    [Fact]
    public void Derive_orders_events_oldest_first_across_builds()
    {
        var older = BuildWith(1, queued: At(1));
        var newer = BuildWith(2, queued: At(5));

        var events = BuildEventDeriver.Derive([newer, older], [], [], Checkpoints());

        Assert.Equal([1, 2], events.Select(e => e.Build.Id));
    }

    [Fact]
    public void Derive_ignores_a_build_without_timestamps()
    {
        Assert.Empty(BuildEventDeriver.Derive([BuildWith(1)], [BuildWith(1)], [BuildWith(1)], Checkpoints()));
    }

    [Fact]
    public void Derive_reports_a_single_queued_event_for_a_build_present_in_both_the_in_flight_and_completed_lists()
    {
        // The dispatcher merges its in-flight and completed queries into one queued candidate list, so the same
        // build can legitimately appear twice.
        var build = BuildWith(1, queued: At(1));

        var events = BuildEventDeriver.Derive([build, build], [], [], Checkpoints());

        var single = Assert.Single(events);
        Assert.Equal(AzureDevOpsWebhookEventTypes.BuildQueued, single.EventType);
    }

    [Fact]
    public void Derive_keys_each_event_to_the_list_it_came_from()
    {
        // The dispatcher feeds in-flight builds into the queued and started candidates while the finished input stays
        // completed builds only. The build below has a finish time, so only the keying can keep the completion out — a
        // deriver that read the completion off the queued or started list would report this build as done.
        var build = BuildWith(1, queued: At(1), started: At(2), finished: At(3));

        var events = BuildEventDeriver.Derive([build], [build], [], Checkpoints());

        Assert.Equal(
            [AzureDevOpsWebhookEventTypes.BuildQueued, AzureDevOpsWebhookEventTypes.BuildStarted],
            events.Select(e => e.EventType));
    }

    [Fact]
    public void Derive_orders_the_events_of_a_build_whose_timestamps_are_identical_the_way_they_happened()
    {
        // A build that queues, starts and finishes inside the same second reports all three with one timestamp, and
        // the timestamp sort alone leaves their order to chance. Dispatching the completion before the queue would
        // reach a workflow that reacts to build.queued after the build is already done.
        var build = BuildWith(1, queued: At(1), started: At(1), finished: At(1));

        var events = BuildEventDeriver.Derive([build], [build], [build], Checkpoints());

        Assert.Equal(
            [AzureDevOpsWebhookEventTypes.BuildQueued, AzureDevOpsWebhookEventTypes.BuildStarted, AzureDevOpsWebhookEventTypes.BuildCompleted],
            events.Select(e => e.EventType));
    }

    [Fact]
    public void Derive_converts_timestamps_without_shifting_them_by_the_host_offset()
    {
        // The deriver labels the SDK's unspecified-kind timestamps as UTC with DateTime.SpecifyKind. Converting them
        // instead - by treating them as local time - would move every checkpoint by the host's offset, which is
        // invisible on a UTC build agent and wrong everywhere else.
        var queued = new DateTime(2026, 8, 12, 10, 1, 0, DateTimeKind.Utc);

        var events = BuildEventDeriver.Derive([BuildWith(1, queued: queued)], [], [], Checkpoints());

        Assert.Equal(TimeSpan.Zero, events[0].Timestamp.Offset);
        Assert.Equal(queued, events[0].Timestamp.UtcDateTime);
    }

    [Fact]
    public void Advance_moves_only_the_checkpoint_of_the_event_that_was_dispatched()
    {
        // Each event kind keeps its own checkpoint, so a late completion cannot drag the queue checkpoint past
        // builds that were never reported.
        var checkpoints = Checkpoints();
        var events = BuildEventDeriver.Derive([], [], [BuildWith(1, finished: At(9))], checkpoints);

        BuildEventDeriver.Advance(checkpoints, events[0]);

        Assert.Equal(Checkpoint, checkpoints.LastQueued);
        Assert.Equal(Checkpoint, checkpoints.LastStarted);
        Assert.Equal(new DateTimeOffset(At(9), TimeSpan.Zero), checkpoints.LastFinished);
    }

    [Fact]
    public void Advance_moves_the_queue_checkpoint_for_a_queued_event()
    {
        var checkpoints = Checkpoints();
        var events = BuildEventDeriver.Derive([BuildWith(1, queued: At(3))], [], [], checkpoints);

        BuildEventDeriver.Advance(checkpoints, events[0]);

        Assert.Equal(new DateTimeOffset(At(3), TimeSpan.Zero), checkpoints.LastQueued);
        Assert.Equal(Checkpoint, checkpoints.LastStarted);
        Assert.Equal(Checkpoint, checkpoints.LastFinished);
    }

    [Fact]
    public void Advance_moves_the_start_checkpoint_for_a_started_event()
    {
        var checkpoints = Checkpoints();
        var events = BuildEventDeriver.Derive([], [BuildWith(1, started: At(4))], [], checkpoints);

        BuildEventDeriver.Advance(checkpoints, events[0]);

        Assert.Equal(Checkpoint, checkpoints.LastQueued);
        Assert.Equal(new DateTimeOffset(At(4), TimeSpan.Zero), checkpoints.LastStarted);
        Assert.Equal(Checkpoint, checkpoints.LastFinished);
    }

    [Fact]
    public void Advance_leaves_a_checkpoint_that_is_already_further_along_alone()
    {
        // Events are dispatched oldest first, but the same build can be handed in from two lists; re-advancing to an
        // older timestamp would make the next poll replay everything in between.
        var checkpoints = Checkpoints();
        var events = BuildEventDeriver.Derive([BuildWith(1, queued: At(2)), BuildWith(2, queued: At(6))], [], [], checkpoints);

        BuildEventDeriver.Advance(checkpoints, events[1]);
        BuildEventDeriver.Advance(checkpoints, events[0]);

        Assert.Equal(new DateTimeOffset(At(6), TimeSpan.Zero), checkpoints.LastQueued);
    }
}
