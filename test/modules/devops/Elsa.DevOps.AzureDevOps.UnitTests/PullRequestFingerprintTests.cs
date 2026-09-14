using Elsa.DevOps.AzureDevOps.Services.Polling;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

public class PullRequestFingerprintTests
{
    private static GitPullRequest PullRequest() => new()
    {
        PullRequestId = 1,
        Title = "Add polling",
        Description = "Adds the remaining families.",
        Status = PullRequestStatus.Active,
        MergeStatus = PullRequestAsyncStatus.Succeeded,
        SourceRefName = "refs/heads/feature",
        TargetRefName = "refs/heads/main",
        IsDraft = false,
        Reviewers = [new IdentityRefWithVote { Id = "reviewer-1", Vote = 0 }],
    };

    private static GitPullRequestIteration Iteration(int id) => new()
    {
        Id = id,
        UpdatedDate = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public void Compute_is_stable_for_an_unchanged_pull_request()
    {
        // Every interval recomputes this; an unstable fingerprint would report an update on every tick.
        Assert.Equal(
            PullRequestFingerprint.Compute(PullRequest(), Iteration(1)),
            PullRequestFingerprint.Compute(PullRequest(), Iteration(1)));
    }

    [Fact]
    public void Compute_changes_when_the_title_changes()
    {
        var renamed = PullRequest();
        renamed.Title = "Add polling for the remaining events";

        Assert.NotEqual(PullRequestFingerprint.Compute(PullRequest(), Iteration(1)), PullRequestFingerprint.Compute(renamed, Iteration(1)));
    }

    [Fact]
    public void Compute_changes_when_a_reviewer_votes()
    {
        var approved = PullRequest();
        approved.Reviewers = [new IdentityRefWithVote { Id = "reviewer-1", Vote = 10 }];

        Assert.NotEqual(PullRequestFingerprint.Compute(PullRequest(), Iteration(1)), PullRequestFingerprint.Compute(approved, Iteration(1)));
    }

    [Fact]
    public void Compute_changes_when_a_new_iteration_is_pushed()
    {
        // Pushing to the branch of a pull request is the most common update, and it changes nothing on the pull
        // request itself.
        Assert.NotEqual(PullRequestFingerprint.Compute(PullRequest(), Iteration(1)), PullRequestFingerprint.Compute(PullRequest(), Iteration(2)));
    }

    [Fact]
    public void Compute_changes_when_the_pull_request_becomes_a_draft()
    {
        var draft = PullRequest();
        draft.IsDraft = true;

        Assert.NotEqual(PullRequestFingerprint.Compute(PullRequest(), Iteration(1)), PullRequestFingerprint.Compute(draft, Iteration(1)));
    }

    [Fact]
    public void SelectNewest_picks_the_highest_iteration_id_whatever_order_they_arrive_in()
    {
        // The API promises no order, and this single choice decides whether a push to the pull request branch - the
        // most common update there is - is noticed at all.
        var newest = PullRequestFingerprint.SelectNewest([Iteration(2), Iteration(5), Iteration(1), Iteration(4)]);

        Assert.Equal(5, newest?.Id);
    }

    [Fact]
    public void SelectNewest_yields_null_without_iterations()
    {
        // A pull request always has at least one iteration in practice, but an empty read must leave the iteration
        // part of the fingerprint empty rather than fault the watcher.
        Assert.Null(PullRequestFingerprint.SelectNewest([]));
        Assert.Null(PullRequestFingerprint.SelectNewest(null));
    }

    [Fact]
    public void Compute_changes_when_the_newest_iteration_changes()
    {
        // The pair that matters: selecting the newest iteration and fingerprinting it have to add up to "a push is an
        // update", which is what the watcher relies on.
        var before = PullRequestFingerprint.SelectNewest([Iteration(1), Iteration(2)]);
        var after = PullRequestFingerprint.SelectNewest([Iteration(1), Iteration(2), Iteration(3)]);

        Assert.NotEqual(PullRequestFingerprint.Compute(PullRequest(), before), PullRequestFingerprint.Compute(PullRequest(), after));
    }

    [Fact]
    public void Compute_ignores_the_order_reviewers_come_back_in()
    {
        // The API does not promise an order, and a reshuffle is not an update.
        var one = PullRequest();
        one.Reviewers = [new IdentityRefWithVote { Id = "a", Vote = 0 }, new IdentityRefWithVote { Id = "b", Vote = 10 }];
        var other = PullRequest();
        other.Reviewers = [new IdentityRefWithVote { Id = "b", Vote = 10 }, new IdentityRefWithVote { Id = "a", Vote = 0 }];

        Assert.Equal(PullRequestFingerprint.Compute(one, Iteration(1)), PullRequestFingerprint.Compute(other, Iteration(1)));
    }

    [Fact]
    public void Compute_handles_a_pull_request_without_reviewers_or_iterations()
    {
        var bare = new GitPullRequest { PullRequestId = 1, Status = PullRequestStatus.Active };

        Assert.False(string.IsNullOrWhiteSpace(PullRequestFingerprint.Compute(bare, null)));
    }
}
