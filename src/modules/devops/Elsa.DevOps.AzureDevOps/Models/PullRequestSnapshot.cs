using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.Models;

/// <summary>
/// One reviewer of a pull request, and where they stand.
/// </summary>
/// <param name="Name">The reviewer, as Azure DevOps displays them.</param>
/// <param name="Vote">Their vote in words. See <see cref="PullRequestSnapshot"/> for why not the number.</param>
/// <param name="IsRequired">Whether this reviewer is required rather than optional.</param>
public sealed record PullRequestReviewer(string? Name, string Vote, bool IsRequired);

/// <summary>
/// The part of a pull request worth handing to a reader.
/// </summary>
/// <remarks>
/// A <see cref="GitPullRequest"/> carries both full repository objects, every commit, the merge job status and the
/// completion options. The same choice, for the same reason, as <see cref="WorkItemSnapshot"/>: what is stored here is
/// re-read on every render of the instance page, so it holds what a card shows and nothing else.
/// </remarks>
/// <param name="Id">The pull request id. What the pull request page is addressed by.</param>
/// <param name="Title">Its title.</param>
/// <param name="Description">Its description, shortened. See <see cref="DevOpsDisplayText.MaxProseCharacters"/>.</param>
/// <param name="Status">Active, abandoned or completed.</param>
/// <param name="IsDraft">Whether it is still a draft.</param>
/// <param name="MergeStatus">What the merge job made of it: succeeded, conflicts, queued.</param>
/// <param name="Repository">The repository it targets. Also what the link is built from.</param>
/// <param name="Project">The project that repository lives in. Also what the link is built from.</param>
/// <param name="SourceBranch">The branch being merged, without its <c>refs/heads/</c> prefix.</param>
/// <param name="TargetBranch">The branch being merged into, without its <c>refs/heads/</c> prefix.</param>
/// <param name="CreatedBy">Who opened it.</param>
/// <param name="CreatedAt">When it was opened.</param>
/// <param name="ClosedAt">When it was completed or abandoned, or null while it is open.</param>
/// <param name="Reviewers">Who is reviewing it, and where each of them stands.</param>
/// <param name="Url">The pull request page a person opens, or null when the project or repository is unknown.</param>
public sealed record PullRequestSnapshot(
    int Id,
    string? Title,
    string? Description,
    string? Status,
    bool IsDraft,
    string? MergeStatus,
    string? Repository,
    string? Project,
    string? SourceBranch,
    string? TargetBranch,
    string? CreatedBy,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<PullRequestReviewer> Reviewers,
    string? Url)
{
    /// <summary>
    /// Projects a pull request read from Azure DevOps, deriving the URL a person would open from
    /// <paramref name="organizationUrl"/> rather than reporting the API address, which is of no use to a reader.
    /// </summary>
    public static PullRequestSnapshot From(GitPullRequest pullRequest, string organizationUrl)
    {
        ArgumentNullException.ThrowIfNull(pullRequest);
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationUrl);

        string? repository = DevOpsDisplayText.Trimmed(pullRequest.Repository?.Name);

        // The repository's own project when it carries one, because a pull request read by id alone comes back with the
        // repository filled in and nothing else that names the project.
        string? project = DevOpsDisplayText.Trimmed(pullRequest.Repository?.ProjectReference?.Name);

        // NotSet covers both "not asked yet" and a pull request that is already closed, so both enums are reported as
        // nothing rather than as a status of their own.
        string? status = pullRequest.Status == PullRequestStatus.NotSet ? null : pullRequest.Status.ToString();
        string? mergeStatus = pullRequest.MergeStatus == PullRequestAsyncStatus.NotSet ? null : pullRequest.MergeStatus.ToString();

        return new PullRequestSnapshot(
            pullRequest.PullRequestId,
            DevOpsDisplayText.Trimmed(pullRequest.Title),
            DevOpsDisplayText.Shorten(DevOpsDisplayText.Trimmed(pullRequest.Description)),
            status,
            pullRequest.IsDraft ?? false,
            mergeStatus,
            repository,
            project,
            DevOpsDisplayText.ShortRef(pullRequest.SourceRefName),
            DevOpsDisplayText.ShortRef(pullRequest.TargetRefName),
            DevOpsDisplayText.Trimmed(pullRequest.CreatedBy?.DisplayName),
            Moment(pullRequest.CreationDate),
            Moment(pullRequest.ClosedDate),
            ReadReviewers(pullRequest),
            DevOpsDisplayText.PullRequestUrl(organizationUrl, project, repository, pullRequest.PullRequestId));
    }

    private static IReadOnlyList<PullRequestReviewer> ReadReviewers(GitPullRequest pullRequest) =>
        pullRequest.Reviewers == null
            ? []
            : [.. pullRequest.Reviewers.Select(reviewer => new PullRequestReviewer(
                DevOpsDisplayText.Trimmed(reviewer.DisplayName),
                VoteText(reviewer.Vote),
                reviewer.IsRequired))];

    /// <summary>
    /// A reviewer's vote in words.
    /// </summary>
    /// <remarks>
    /// Translated here rather than in the viewer because the numbers are an Azure DevOps wire detail - 10 approved,
    /// -5 waiting for the author - and a card rendering "-5" says nothing to the person reading it. Anything outside
    /// the five documented values falls back to no vote rather than to a guess.
    /// </remarks>
    private static string VoteText(short vote) => vote switch
    {
        10 => "Approved",
        5 => "Approved with suggestions",
        -5 => "Waiting for author",
        -10 => "Rejected",
        _ => "No vote",
    };

    /// <summary>
    /// A pull request timestamp as an offset, stating the UTC kind the API answers in but does not mark - the same
    /// trap BuildEventDeriver documents.
    /// </summary>
    private static DateTimeOffset? Moment(DateTime? value) =>
        value == null || value == default(DateTime) ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
}
