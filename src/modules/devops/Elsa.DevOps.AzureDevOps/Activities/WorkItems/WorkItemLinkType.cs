namespace Elsa.DevOps.AzureDevOps.Activities.WorkItems;

/// <summary>
/// The link types <see cref="AddWorkItemRelation"/> can create between two work items.
/// </summary>
/// <remarks>
/// Each value names the role the <em>target</em> plays, which is how Azure DevOps labels the link in the work item
/// form: adding <see cref="Parent"/> on work item 1234 pointing at 5678 makes 5678 the parent of 1234. The underlying
/// reference names say <c>Forward</c> or <c>Reverse</c> instead, and read as the opposite of the label for hierarchy
/// and dependency links, which is exactly the mistake this enum exists to prevent.
/// </remarks>
public enum WorkItemLinkType
{
    /// <summary>The target is related to the work item. The default.</summary>
    Related,

    /// <summary>The target is the parent of the work item.</summary>
    Parent,

    /// <summary>The target is a child of the work item.</summary>
    Child,

    /// <summary>The target must be finished before the work item can start.</summary>
    Predecessor,

    /// <summary>The target can only start once the work item is finished.</summary>
    Successor,

    /// <summary>The target is a duplicate of the work item.</summary>
    Duplicate,

    /// <summary>The work item is a duplicate of the target.</summary>
    DuplicateOf,

    /// <summary>The target tests the work item.</summary>
    TestedBy,

    /// <summary>The work item tests the target.</summary>
    Tests,

    /// <summary>The work item affects the target. CMMI processes only.</summary>
    Affects,

    /// <summary>The work item is affected by the target. CMMI processes only.</summary>
    AffectedBy
}
