using System.Globalization;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.Models;

/// <summary>
/// The part of a Git repository worth handing to a reader.
/// </summary>
/// <remarks>
/// The same choice, for the same reason, as <see cref="WorkItemSnapshot"/>. A <see cref="GitRepository"/> also nests a
/// second <see cref="GitRepository"/> when it is a fork, which is a cycle waiting to be serialized into activity state.
/// </remarks>
/// <param name="Id">The repository's GUID, as a string. A repository has no integer id, which is why the bookmark
/// payload carries its resource id as text.</param>
/// <param name="Name">Its name.</param>
/// <param name="Project">The project it lives in. Also what the link is built from.</param>
/// <param name="DefaultBranch">Its default branch, without the <c>refs/heads/</c> prefix.</param>
/// <param name="Size">Its size in bytes, as the API reports it.</param>
/// <param name="IsDisabled">Whether the repository is disabled.</param>
/// <param name="IsFork">Whether it is a fork of another repository.</param>
/// <param name="RemoteUrl">The HTTPS address to clone from.</param>
/// <param name="Url">The repository page a person opens.</param>
public sealed record RepositorySnapshot(
    string Id,
    string? Name,
    string? Project,
    string? DefaultBranch,
    long? Size,
    bool IsDisabled,
    bool IsFork,
    string? RemoteUrl,
    string? Url)
{
    /// <summary>
    /// Projects a repository read from Azure DevOps.
    /// </summary>
    /// <remarks>
    /// The web address is the one the API reports when it reports one, and derived from
    /// <paramref name="organizationUrl"/> otherwise. That order is the other way around from the build and pull request
    /// snapshots on purpose: a repository read is the one call that answers with a browser URL of its own, and it is
    /// right by construction where a derived one is right by assumption.
    /// </remarks>
    public static RepositorySnapshot From(GitRepository repository, string organizationUrl)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationUrl);

        string? name = DevOpsDisplayText.Trimmed(repository.Name);
        string? project = DevOpsDisplayText.Trimmed(repository.ProjectReference?.Name);

        return new RepositorySnapshot(
            repository.Id.ToString(),
            name,
            project,
            DevOpsDisplayText.ShortRef(repository.DefaultBranch),
            repository.Size,
            repository.IsDisabled ?? false,
            repository.IsFork,
            DevOpsDisplayText.Trimmed(repository.RemoteUrl),
            DevOpsDisplayText.Trimmed(repository.WebUrl) ?? DevOpsDisplayText.RepositoryUrl(organizationUrl, project, name));
    }

    /// <summary>The size in whole megabytes, or null when the API did not report one.</summary>
    public string? SizeText =>
        Size == null ? null : (Size.Value / 1024d / 1024d).ToString("0.# MB", CultureInfo.InvariantCulture);
}
