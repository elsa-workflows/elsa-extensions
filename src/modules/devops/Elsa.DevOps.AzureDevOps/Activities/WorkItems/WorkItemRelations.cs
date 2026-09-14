using System.Globalization;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.Activities.WorkItems;

/// <summary>
/// Helpers shared by the activities that add a relation to a work item. Kept apart from the activities because none of
/// this needs an Azure DevOps connection, and the direction of a link type is worth pinning down in tests.
/// </summary>
internal static class WorkItemRelations
{
    /// <summary>
    /// The reference name Azure DevOps uses for a plain URL attached to a work item.
    /// </summary>
    public const string HyperlinkReferenceName = "Hyperlink";

    /// <summary>
    /// The path a relation is appended to in the patch document that adds it.
    /// </summary>
    public const string AddRelationPath = "/relations/-";

    /// <summary>
    /// Translates a relation type into the reference name the REST API expects.
    /// </summary>
    public static string ToReferenceName(WorkItemLinkType type) => type switch
    {
        WorkItemLinkType.Related => "System.LinkTypes.Related",
        WorkItemLinkType.Parent => "System.LinkTypes.Hierarchy-Reverse",
        WorkItemLinkType.Child => "System.LinkTypes.Hierarchy-Forward",
        WorkItemLinkType.Predecessor => "System.LinkTypes.Dependency-Reverse",
        WorkItemLinkType.Successor => "System.LinkTypes.Dependency-Forward",
        WorkItemLinkType.Duplicate => "System.LinkTypes.Duplicate-Forward",
        WorkItemLinkType.DuplicateOf => "System.LinkTypes.Duplicate-Reverse",
        WorkItemLinkType.TestedBy => "Microsoft.VSTS.Common.TestedBy-Forward",
        WorkItemLinkType.Tests => "Microsoft.VSTS.Common.TestedBy-Reverse",
        WorkItemLinkType.Affects => "Microsoft.VSTS.Common.Affects-Forward",
        WorkItemLinkType.AffectedBy => "Microsoft.VSTS.Common.Affects-Reverse",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown work item relation type.")
    };

    /// <summary>
    /// Reads the work item ID off a relation URL, or returns <c>null</c> when the URL does not address a work item.
    /// </summary>
    /// <remarks>
    /// Azure DevOps does not store relation URLs in one shape — the project segment is present on some and absent on
    /// others — so comparing the ID is the only reliable way to tell whether two relations point at the same work item.
    /// </remarks>
    public static int? TryGetWorkItemId(string? relationUrl)
    {
        if (string.IsNullOrWhiteSpace(relationUrl))
            return null;

        var path = relationUrl.Trim();
        var separator = path.IndexOfAny(['?', '#']);
        if (separator >= 0)
            path = path[..separator];
        path = path.TrimEnd('/');

        var lastSegment = path[(path.LastIndexOf('/') + 1)..];
        return int.TryParse(lastSegment, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : null;
    }

    /// <summary>
    /// Reports whether the work item already carries the given relation to the given work item.
    /// </summary>
    public static bool HasWorkItemRelation(WorkItem workItem, string referenceName, int targetWorkItemId) =>
        // Relations is null rather than empty when the work item has no links at all.
        workItem.Relations?.Any(relation =>
            string.Equals(relation.Rel, referenceName, StringComparison.OrdinalIgnoreCase)
            && TryGetWorkItemId(relation.Url) == targetWorkItemId) == true;

    /// <summary>
    /// Reports whether the work item already carries a hyperlink to the given URL.
    /// </summary>
    /// <remarks>
    /// The comparison ignores case and surrounding whitespace, which is looser than the equality Azure DevOps applies
    /// itself. Skipping a link Azure DevOps would have accepted is the safer of the two mistakes available here.
    /// </remarks>
    public static bool HasHyperlink(WorkItem workItem, string url) =>
        workItem.Relations?.Any(relation =>
            string.Equals(relation.Rel, HyperlinkReferenceName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(relation.Url?.Trim(), url.Trim(), StringComparison.OrdinalIgnoreCase)) == true;

    /// <summary>
    /// Builds the value of the patch operation that adds a relation.
    /// </summary>
    public static object CreateRelationValue(string referenceName, string url, string? comment)
    {
        // The attributes object is left out entirely when there is no comment, rather than sent with a null in it.
        if (string.IsNullOrWhiteSpace(comment))
            return new { rel = referenceName, url };
        return new { rel = referenceName, url, attributes = new { comment } };
    }
}
