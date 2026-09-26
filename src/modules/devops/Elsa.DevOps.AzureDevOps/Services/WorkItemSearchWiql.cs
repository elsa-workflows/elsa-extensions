using System.Text;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Builds the WIQL behind the filtered search tool, so a model never has to write any.
/// </summary>
/// <remarks>
/// The point of building it here rather than letting the model write it: a filter cannot be a syntax error, a quote in a
/// title cannot break the query, and the tag filter can be narrowed with <c>CONTAINS</c> and then compared whole by the
/// caller - which is the part a model reliably gets wrong, because <c>CONTAINS 'problemId:19'</c> also selects
/// <c>problemId:190</c>.
/// </remarks>
public static class WorkItemSearchWiql
{
    /// <summary>
    /// The value that means "whoever this token belongs to", which is the user the workflow runs for.
    /// </summary>
    private static readonly string[] CallerAliases = ["me", "@me"];

    /// <summary>
    /// A query for the filters that are filled in. Every filter left blank is left out rather than matched against an
    /// empty string.
    /// </summary>
    public static string Build(string? searchText, string? workItemType, string? state, string? tag, string? assignedTo)
    {
        StringBuilder wiql = new();
        wiql.AppendLine("SELECT [System.Id] FROM WorkItems");
        wiql.Append("WHERE [System.TeamProject] = @project");

        Append(wiql, "[System.Title] CONTAINS", searchText, quoted: true);
        Append(wiql, "[System.WorkItemType] =", workItemType, quoted: true);
        Append(wiql, "[System.State] =", state, quoted: true);

        // CONTAINS is all WIQL offers over the semicolon-separated tag list. It narrows; the caller compares the tag
        // whole afterwards.
        Append(wiql, "[System.Tags] CONTAINS", tag, quoted: true);

        if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            bool caller = CallerAliases.Contains(assignedTo.Trim(), StringComparer.OrdinalIgnoreCase);

            // @me is a macro, not a name. Quoting it would search for somebody literally called "@me".
            Append(wiql, "[System.AssignedTo] =", assignedTo, quoted: !caller, value: caller ? "@me" : null);
        }

        wiql.AppendLine();
        wiql.Append("ORDER BY [System.ChangedDate] DESC");

        return wiql.ToString();
    }

    private static void Append(StringBuilder wiql, string clause, string? filter, bool quoted, string? value = null)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return;

        string written = value ?? (quoted ? $"'{Escape(filter.Trim())}'" : filter.Trim());

        wiql.AppendLine();
        wiql.Append("  AND ").Append(clause).Append(' ').Append(written);
    }

    /// <summary>
    /// Doubles a single quote, which is how WIQL escapes one. "Can't log in" is an ordinary work item title and would
    /// otherwise produce a query that does not parse - or one that parses into something else.
    /// </summary>
    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
