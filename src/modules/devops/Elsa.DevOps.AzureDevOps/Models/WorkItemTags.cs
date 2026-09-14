namespace Elsa.DevOps.AzureDevOps.Models;

/// <summary>
/// Adds and removes one tag in the semicolon-separated list Azure DevOps keeps tags in.
/// </summary>
/// <remarks>
/// There is no endpoint for a single tag: <c>System.Tags</c> is one field, so changing one tag means writing them all
/// back - and therefore reading them first, or the ones the caller never saw are lost. Both methods return
/// <c>null</c> when the field already says what the caller wanted, which spares the work item a revision that changes
/// nothing.
/// </remarks>
public static class WorkItemTags
{
    private const string Separator = "; ";

    /// <summary>
    /// The field value with <paramref name="tag"/> added, or <c>null</c> when it is already there.
    /// </summary>
    public static string? Add(string? tags, string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        List<string> existing = Split(tags);
        string trimmed = tag.Trim();

        if (existing.Any(candidate => string.Equals(candidate, trimmed, StringComparison.OrdinalIgnoreCase)))
            return null;

        existing.Add(trimmed);

        return string.Join(Separator, existing);
    }

    /// <summary>
    /// The field value with <paramref name="tag"/> removed, or <c>null</c> when it was not there. Removing the only tag
    /// yields an empty string, which is a write: it clears the field.
    /// </summary>
    public static string? Remove(string? tags, string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        List<string> existing = Split(tags);
        string trimmed = tag.Trim();

        List<string> kept = [.. existing.Where(candidate => !string.Equals(candidate, trimmed, StringComparison.OrdinalIgnoreCase))];

        return kept.Count == existing.Count ? null : string.Join(Separator, kept);
    }

    private static List<string> Split(string? tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? []
            : [.. tags.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
}
