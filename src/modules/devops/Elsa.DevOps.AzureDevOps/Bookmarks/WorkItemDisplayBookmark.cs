namespace Elsa.DevOps.AzureDevOps.Bookmarks;

/// <summary>
/// The stimulus a <c>Display Work Item</c> interaction waits on.
/// </summary>
/// <remarks>
/// <para>
/// Shaped like the payload of a UI interaction - a display target plus the data it is about - but with a target this
/// package owns. The target travels as a string, <see cref="DisplayTarget"/>, rather than as a value in an enum every
/// viewer in the host shares, so a host can add a work item viewer without the other viewers having to learn the
/// value: only the viewers that recognise it act on it.
/// </para>
/// <para>
/// Deliberately free of the work item itself. A bookmark payload is the stimulus, not the state: the projection lives
/// in the activity's own <c>ActivityState</c>, which is where a viewer reads it from. Putting a work item - title,
/// description, tags - in here would copy all of it into the bookmark record as well, for no reader that could not
/// find it in the state.
/// </para>
/// <para>
/// <see cref="Kind"/> exists to be guarded on. <c>System.Text.Json</c> fills absent members with defaults, so reading
/// any JSON object as this type succeeds; a viewer that recognises its own bookmark by "it deserialised" claims every
/// other feature's bookmark in the host.
/// </para>
/// </remarks>
/// <param name="Kind">Always <see cref="CurrentKind"/>. The discriminator a viewer guards on.</param>
/// <param name="UiDisplayTarget">Always <see cref="DisplayTarget"/>. What kind of view this interaction asks for.</param>
/// <param name="DisplayId">The handle joining this bookmark to the activity state holding the work item.</param>
/// <param name="WorkItemId">The work item on show, so a viewer can say which one without reading the state.</param>
public sealed record WorkItemDisplayBookmark(string Kind, string UiDisplayTarget, string DisplayId, int WorkItemId)
{
    /// <summary>The only kind this version writes.</summary>
    public const string CurrentKind = "azuredevops-workitem/v1";

    /// <summary>The custom UI display target this interaction asks for.</summary>
    public const string DisplayTarget = "WorkItem";

    /// <summary>A payload for one work item on show, with the constants filled in.</summary>
    public static WorkItemDisplayBookmark Create(string displayId, int workItemId) =>
        new(CurrentKind, DisplayTarget, displayId, workItemId);
}
