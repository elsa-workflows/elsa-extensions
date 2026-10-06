using System.Globalization;

namespace Elsa.DevOps.AzureDevOps.Models;

/// <summary>
/// The small conversions every display projection needs: readable branch names, shortened prose, and the addresses a
/// person opens rather than the ones the API answers on.
/// </summary>
/// <remarks>
/// Shared by <see cref="BuildSnapshot"/>, <see cref="PullRequestSnapshot"/> and <see cref="RepositorySnapshot"/> rather
/// than copied into each: three copies of "strip refs/heads/" is three chances to strip it differently.
/// </remarks>
public static class DevOpsDisplayText
{
    /// <summary>
    /// How much prose travels into a projection. A pull request description is written by whoever opened it and has no
    /// practical ceiling; the activity state it ends up in is read back on every render of the instance page.
    /// </summary>
    public const int MaxProseCharacters = 4_000;

    private const string BranchPrefix = "refs/heads/";
    private const string TagPrefix = "refs/tags/";

    /// <summary>
    /// A branch as a person writes it: <c>main</c> rather than <c>refs/heads/main</c>.
    /// </summary>
    /// <remarks>
    /// Anything that is not a branch or a tag ref is returned untouched rather than guessed at - a pull request against
    /// a ref this does not recognise is better shown in full than shown wrong.
    /// </remarks>
    public static string? ShortRef(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        if (reference.StartsWith(BranchPrefix, StringComparison.OrdinalIgnoreCase))
            return reference[BranchPrefix.Length..];

        return reference.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase)
            ? reference[TagPrefix.Length..]
            : reference;
    }

    /// <summary>Shortens prose to <see cref="MaxProseCharacters"/>, saying so where it cut.</summary>
    public static string? Shorten(string? text) =>
        text == null || text.Length <= MaxProseCharacters
            ? text
            : text[..MaxProseCharacters] + " […] [shortened]";

    /// <summary>Null when the text is absent or blank, so a viewer can tell "not set" from "set to nothing".</summary>
    public static string? Trimmed(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    /// <summary>
    /// The address of a build's results page, or null when the build does not say which project it ran in.
    /// </summary>
    /// <remarks>
    /// Null rather than a best guess: a build results URL is scoped to a project, and an organization-level one leads
    /// to a 404. A viewer offers no link at all rather than one that does not open.
    /// </remarks>
    public static string? BuildUrl(string organizationUrl, string? project, int buildId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationUrl);

        return string.IsNullOrWhiteSpace(project)
            ? null
            : $"{Organization(organizationUrl)}/{Escape(project)}/_build/results?buildId={buildId.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>The address of a pull request, or null when the project or repository is unknown.</summary>
    public static string? PullRequestUrl(string organizationUrl, string? project, string? repository, int pullRequestId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationUrl);

        return string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(repository)
            ? null
            : $"{Organization(organizationUrl)}/{Escape(project)}/_git/{Escape(repository)}/pullrequest/{pullRequestId.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>The address of a repository, or null when the project or name is unknown.</summary>
    public static string? RepositoryUrl(string organizationUrl, string? project, string? repository)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationUrl);

        return string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(repository)
            ? null
            : $"{Organization(organizationUrl)}/{Escape(project)}/_git/{Escape(repository)}";
    }

    private static string Organization(string organizationUrl) => organizationUrl.TrimEnd('/');

    /// <summary>
    /// Escapes a path segment. Project and repository names carry spaces often enough - "Contoso Web Platform" is one
    /// - that an unescaped link would be broken for a good part of this organization.
    /// </summary>
    private static string Escape(string segment) => Uri.EscapeDataString(segment);
}
