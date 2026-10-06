using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services.Polling;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

public class PullRequestEventDeriverTests
{
    private static readonly DateTimeOffset Checkpoint = new(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);

    private static PullRequestPollingCheckpoints Checkpoints() => new() { LastCreated = Checkpoint, LastClosed = Checkpoint };

    private static DateTime At(int minutes) => Checkpoint.UtcDateTime.AddMinutes(minutes);

    private static GitPullRequest PullRequest(int id, DateTime created, PullRequestStatus status = PullRequestStatus.Active, DateTime? closed = null) => new()
    {
        PullRequestId = id,
        CreationDate = created,
        Status = status,
        ClosedDate = closed ?? default,
    };

    [Fact]
    public void Derive_reports_a_pull_request_created_after_the_checkpoint()
    {
        var events = PullRequestEventDeriver.Derive([PullRequest(1, At(1))], [], Checkpoints());

        var single = Assert.Single(events);
        Assert.Equal(AzureDevOpsWebhookEventTypes.PullRequestCreated, single.EventType);
    }

    [Fact]
    public void Derive_ignores_a_pull_request_created_exactly_on_the_checkpoint()
    {
        Assert.Empty(PullRequestEventDeriver.Derive([PullRequest(1, Checkpoint.UtcDateTime)], [], Checkpoints()));
    }

    [Fact]
    public void Derive_reports_a_completed_pull_request_as_merged()
    {
        var merged = PullRequest(1, At(-60), PullRequestStatus.Completed, At(2));

        var events = PullRequestEventDeriver.Derive([], [merged], Checkpoints());

        var single = Assert.Single(events);
        Assert.Equal(AzureDevOpsWebhookEventTypes.PullRequestMerged, single.EventType);
    }

    [Fact]
    public void Derive_reports_nothing_for_an_abandoned_pull_request()
    {
        // Abandoning closes a pull request as well, and reporting that as a merge would run merge workflows on code
        // that was never merged.
        var abandoned = PullRequest(1, At(-60), PullRequestStatus.Abandoned, At(2));

        Assert.Empty(PullRequestEventDeriver.Derive([], [abandoned], Checkpoints()));
    }

    [Fact]
    public void Derive_reports_creation_and_merge_of_a_pull_request_completed_inside_one_interval()
    {
        var pullRequest = PullRequest(1, At(1), PullRequestStatus.Completed, At(3));

        var events = PullRequestEventDeriver.Derive([pullRequest], [pullRequest], Checkpoints());

        Assert.Equal(
            [AzureDevOpsWebhookEventTypes.PullRequestCreated, AzureDevOpsWebhookEventTypes.PullRequestMerged],
            events.Select(e => e.EventType));
    }

    [Fact]
    public void Advance_moves_only_the_checkpoint_of_the_event_that_was_dispatched()
    {
        var checkpoints = Checkpoints();
        var events = PullRequestEventDeriver.Derive([], [PullRequest(1, At(-60), PullRequestStatus.Completed, At(4))], checkpoints);

        PullRequestEventDeriver.Advance(checkpoints, events[0]);

        Assert.Equal(Checkpoint, checkpoints.LastCreated);
        Assert.Equal(new DateTimeOffset(At(4), TimeSpan.Zero), checkpoints.LastClosed);
    }
}
