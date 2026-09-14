using System.Text;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Reads an Azure DevOps organization URL as an identity rather than as a string: whether two URLs name the same
/// organization, and what to call that organization inside a key.
/// </summary>
public static class AzureDevOpsOrganizationUrl
{
    private static readonly string[] Schemes = ["https://", "http://"];

    /// <summary>
    /// Returns the organization as <c>host/path</c> in lower case, or <c>null</c> when nothing was supplied. The
    /// scheme and a trailing slash are dropped: they distinguish two spellings of one organization, never two
    /// organizations.
    /// </summary>
    public static string? Normalize(string? organizationUrl)
    {
        if (string.IsNullOrWhiteSpace(organizationUrl))
            return null;

        string normalized = organizationUrl.Trim().ToLowerInvariant();

        foreach (string scheme in Schemes)
        {
            if (normalized.StartsWith(scheme, StringComparison.Ordinal))
            {
                normalized = normalized[scheme.Length..];
                break;
            }
        }

        normalized = normalized.TrimEnd('/');

        return normalized.Length == 0 ? null : normalized;
    }

    /// <summary>
    /// Whether both URLs name the same organization. Two empty values count as the same: both mean "whatever is
    /// configured".
    /// </summary>
    public static bool AreSame(string? left, string? right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);

    /// <summary>
    /// Returns a name for the organization that is safe to put in a workflow instance ID, or <c>null</c> when nothing
    /// was supplied.
    /// </summary>
    /// <remarks>
    /// Readable rather than hashed, because an instance ID is what identifies a watcher in Studio, in the logs and in
    /// the database. The host is kept: two organizations of the same name on different hosts are different
    /// organizations, and dropping it would give both the same ID.
    /// </remarks>
    public static string? ToKeySegment(string? organizationUrl)
    {
        string? normalized = Normalize(organizationUrl);

        if (normalized == null)
            return null;

        // Instance IDs travel in URLs, so everything that would need escaping there becomes a hyphen.
        StringBuilder segment = new(normalized.Length);

        foreach (char character in normalized)
            segment.Append(char.IsAsciiLetterOrDigit(character) || character is '.' or '-' ? character : '-');

        return segment.ToString();
    }
}
