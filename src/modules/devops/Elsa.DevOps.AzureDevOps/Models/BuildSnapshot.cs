using Microsoft.TeamFoundation.Build.WebApi;

namespace Elsa.DevOps.AzureDevOps.Models;

/// <summary>
/// The part of a build worth handing to a reader.
/// </summary>
/// <remarks>
/// A <see cref="Build"/> carries its definition, its queue, its repository, its triggering artifacts and its links -
/// far more than a card on a workflow instance page shows, and all of it stored in activity state and re-read on every
/// render. The same choice, for the same reason, as <see cref="WorkItemSnapshot"/>.
/// </remarks>
/// <param name="Id">The build id. What the results page is addressed by.</param>
/// <param name="BuildNumber">The name a person calls the build: <c>20260827.3</c>.</param>
/// <param name="Definition">The pipeline that ran.</param>
/// <param name="Project">The project it ran in. Also what the link is built from.</param>
/// <param name="Status">Where the build is: <c>InProgress</c>, <c>Completed</c>, and so on.</param>
/// <param name="Result">How it ended, or null while it is still running.</param>
/// <param name="Reason">Why it ran: a push, a pull request, a schedule, a person.</param>
/// <param name="SourceBranch">The branch it built, without its <c>refs/heads/</c> prefix.</param>
/// <param name="SourceVersion">The commit it built.</param>
/// <param name="RequestedFor">Who it ran for.</param>
/// <param name="QueuedAt">When it was queued.</param>
/// <param name="StartedAt">When it started, or null while it is still queued.</param>
/// <param name="FinishedAt">When it finished, or null while it is still running.</param>
/// <param name="Url">The results page a person opens, or null when the build does not say which project it ran in.</param>
public sealed record BuildSnapshot(
    int Id,
    string? BuildNumber,
    string? Definition,
    string? Project,
    string? Status,
    string? Result,
    string? Reason,
    string? SourceBranch,
    string? SourceVersion,
    string? RequestedFor,
    DateTimeOffset? QueuedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    string? Url)
{
    /// <summary>
    /// Projects a build read from Azure DevOps, deriving the URL a person would open from
    /// <paramref name="organizationUrl"/> rather than reporting the API address, which is of no use to a reader.
    /// </summary>
    public static BuildSnapshot From(Build build, string organizationUrl)
    {
        ArgumentNullException.ThrowIfNull(build);
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationUrl);

        string? project = DevOpsDisplayText.Trimmed(build.Project?.Name);

        // Null while the build is still running, rather than reported as "None": the enum's zero value means "no
        // result yet", and a card saying a running build resulted in None reads as a failure. The reason is treated
        // the same way, for the same reason.
        string? result = build.Result is null or BuildResult.None ? null : build.Result.ToString();
        string? reason = build.Reason == BuildReason.None ? null : build.Reason.ToString();

        return new BuildSnapshot(
            build.Id,
            DevOpsDisplayText.Trimmed(build.BuildNumber),
            DevOpsDisplayText.Trimmed(build.Definition?.Name),
            project,
            build.Status?.ToString(),
            result,
            reason,
            DevOpsDisplayText.ShortRef(build.SourceBranch),
            DevOpsDisplayText.Trimmed(build.SourceVersion),
            DevOpsDisplayText.Trimmed(build.RequestedFor?.DisplayName),
            Moment(build.QueueTime),
            Moment(build.StartTime),
            Moment(build.FinishTime),
            DevOpsDisplayText.BuildUrl(organizationUrl, project, build.Id));
    }

    /// <summary>
    /// A build timestamp as an offset. The API answers in UTC but hands back an unspecified <see cref="DateTime"/>, so
    /// the kind is stated rather than left to the local machine - the same trap BuildEventDeriver documents.
    /// </summary>
    private static DateTimeOffset? Moment(DateTime? value) =>
        value == null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
}
