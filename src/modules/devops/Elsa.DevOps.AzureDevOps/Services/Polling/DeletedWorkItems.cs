using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.Services.Polling;

/// <summary>
/// Turns the Azure DevOps recycle bin into deletion events. Deleted work items are invisible to WIQL, so the only way
/// to see them is to watch the bin's contents change.
/// </summary>
public static class DeletedWorkItems
{
    /// <summary>
    /// Returns the IDs present in the recycle bin now but not on the previous poll. A <c>null</c>
    /// <paramref name="seen"/> means no poll has run yet and yields nothing: the bin holds every deletion ever made,
    /// and reporting it would replay all of them the moment the feature is switched on.
    /// </summary>
    public static IReadOnlyList<int> GetNewlyDeleted(IReadOnlyCollection<int>? seen, IEnumerable<int> current)
    {
        ArgumentNullException.ThrowIfNull(current);

        if (seen == null)
            return [];

        HashSet<int> known = [.. seen];
        return [.. current.Where(id => !known.Contains(id)).Distinct()];
    }

    /// <summary>
    /// Presents a recycle bin entry as a work item. The Work Item Deleted trigger filters on work item ID and type,
    /// and the event handler reads both off a <see cref="WorkItem"/> payload, so the deletion has to arrive in that
    /// shape or those filters quietly stop matching.
    /// </summary>
    public static WorkItem ToWorkItem(WorkItemDeleteReference reference, string project)
    {
        ArgumentNullException.ThrowIfNull(reference);

        WorkItem workItem = new()
        {
            Id = reference.Id,
            Fields = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["System.TeamProject"] = project
            }
        };

        if (!string.IsNullOrWhiteSpace(reference.Type))
            workItem.Fields["System.WorkItemType"] = reference.Type;

        if (!string.IsNullOrWhiteSpace(reference.Name))
            workItem.Fields["System.Title"] = reference.Name;

        return workItem;
    }
}
