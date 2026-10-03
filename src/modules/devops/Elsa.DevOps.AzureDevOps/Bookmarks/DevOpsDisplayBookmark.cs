namespace Elsa.DevOps.AzureDevOps.Bookmarks;

/// <summary>
/// The stimulus a <c>Display Build</c>, <c>Display Pull Request</c> or <c>Display Repository</c> interaction waits on.
/// </summary>
/// <remarks>
/// <para>
/// Shaped like the payload of a UI interaction - a display target plus which thing it is about - but with a target
/// this package owns. The target travels as a string rather than as a value in an enum every viewer in the host
/// shares, so a host can add a viewer for a build or a pull request without the other viewers having to learn the
/// value: only the viewers that recognise it act on it. <see cref="WorkItemDisplayBookmark"/> makes the same argument
/// and stays on its own type, because its payload names the work item id outright and rewriting a wire format that
/// instances are already suspended on would strand every one of them.
/// </para>
/// <para>
/// One record for the three of them rather than three near-identical ones, because <see cref="Kind"/> already tells
/// them apart and the fields do not differ. What does differ - the build's number, the pull request's reviewers - is
/// not here at all: a bookmark payload is the stimulus, not the state. The projection lives in the activity's own
/// <c>ActivityState</c>, which is where a viewer reads it from.
/// </para>
/// <para>
/// <see cref="Kind"/> exists to be guarded on. <c>System.Text.Json</c> fills absent members with defaults, so reading
/// any JSON object as this type succeeds; a viewer that recognises its own bookmark by "it deserialised" claims every
/// other feature's bookmark in the host - and, here, every other display's as well.
/// </para>
/// </remarks>
/// <param name="Kind">Which display wrote this payload. The discriminator a viewer guards on.</param>
/// <param name="UiDisplayTarget">What kind of view this interaction asks for.</param>
/// <param name="DisplayId">The handle joining this bookmark to the activity state holding the projection.</param>
/// <param name="ResourceId">
/// What is on show, so a viewer can say which one without reading the state. A string rather than the int a build and
/// a pull request both have, because a repository is identified by a GUID.
/// </param>
public sealed record DevOpsDisplayBookmark(string Kind, string UiDisplayTarget, string DisplayId, string ResourceId)
{
    /// <summary>A payload for one thing on show, for the display described by <paramref name="descriptor"/>.</summary>
    public static DevOpsDisplayBookmark Create(Activities.DevOpsDisplayDescriptor descriptor, string displayId, string resourceId)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return new DevOpsDisplayBookmark(descriptor.Kind, descriptor.DisplayTarget, displayId, resourceId);
    }
}
