using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.Services.Polling;

/// <summary>
/// Condenses a pull request into a value that changes exactly when Azure DevOps would have sent
/// <c>git.pullrequest.updated</c>. There is no queryable "last updated" timestamp on a pull request, so comparing a
/// fingerprint between polls is the only way to notice a change.
/// </summary>
public static class PullRequestFingerprint
{
    /// <summary>
    /// Returns the iteration to fingerprint: the one with the highest ID, or <c>null</c> when there is none. Pushing
    /// to the branch of a pull request is the most common update there is and shows up nowhere else on the pull
    /// request, so picking any other iteration means those pushes go unnoticed. The API promises no order, which is
    /// why the newest is picked explicitly rather than taken from either end of the list.
    /// </summary>
    public static GitPullRequestIteration? SelectNewest(IEnumerable<GitPullRequestIteration>? iterations) =>
        iterations?.OrderBy(iteration => iteration.Id).LastOrDefault();

    public static string Compute(GitPullRequest pullRequest, GitPullRequestIteration? newestIteration)
    {
        ArgumentNullException.ThrowIfNull(pullRequest);

        // A separator no field value can contain, so that text moving from one field to the next still registers.
        // Written as an escape rather than as the raw control character, which editors do not show and encoding
        // conversions do not reliably carry across.
        const char separator = '\u001f';

        StringBuilder builder = new();
        builder.Append(pullRequest.Title).Append(separator);
        builder.Append(pullRequest.Description).Append(separator);
        builder.Append(pullRequest.Status).Append(separator);
        builder.Append(pullRequest.MergeStatus).Append(separator);
        builder.Append(pullRequest.SourceRefName).Append(separator);
        builder.Append(pullRequest.TargetRefName).Append(separator);
        builder.Append(pullRequest.IsDraft).Append(separator);
        builder.Append(newestIteration?.Id?.ToString(CultureInfo.InvariantCulture)).Append(separator);
        builder.Append(newestIteration?.UpdatedDate?.ToString("O", CultureInfo.InvariantCulture)).Append(separator);

        // Sorted, because the API promises no order and a reshuffle is not a change.
        foreach (string vote in (pullRequest.Reviewers ?? [])
            .Select(reviewer => $"{reviewer.Id}:{reviewer.Vote.ToString(CultureInfo.InvariantCulture)}")
            .OrderBy(vote => vote, StringComparer.Ordinal))
        {
            builder.Append(vote).Append(separator);
        }

        // Hashed rather than stored raw, because the value lives in a workflow variable and a description can be long.
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}
