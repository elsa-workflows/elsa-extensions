using System.Text.Json;
using Elsa.DevOps.AzureDevOps.Activities.WorkItems;
using Elsa.DevOps.AzureDevOps.Bookmarks;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.DevOps.AzureDevOps.Activities;

/// <summary>
/// The names one kind of display answers to: what it writes on the wire, and where it keeps what it is showing.
/// </summary>
/// <remarks>
/// The names are data rather than overridden constants because a viewer in another host needs the same six values, and
/// a record can be handed to a bookmark factory, asserted against in one line, and read next to its siblings. Every
/// value is a contract with whatever renders the display: renaming one without renaming its twin in that host leaves
/// the viewer looking for a bookmark that is no longer written.
/// </remarks>
/// <param name="Kind">The bookmark payload discriminator, versioned so a later shape can be told from this one.</param>
/// <param name="DisplayTarget">What kind of view this display asks for.</param>
/// <param name="DisplayIdStateKey">The activity state key carrying the handle of the display.</param>
/// <param name="SnapshotStateKey">The activity state key carrying the projection, as JSON.</param>
/// <param name="HeadingStateKey">The activity state key carrying the heading, when the workflow gave one.</param>
/// <param name="SeenEventName">The execution log event written when the display is confirmed.</param>
public sealed record DevOpsDisplayDescriptor(
    string Kind,
    string DisplayTarget,
    string DisplayIdStateKey,
    string SnapshotStateKey,
    string HeadingStateKey,
    string SeenEventName);

/// <summary>
/// What a display puts on show: the thing itself, projected, and the id a viewer names it by.
/// </summary>
/// <param name="ResourceId">Which one is on show. Travels in the bookmark payload as well as in the projection.</param>
/// <param name="Snapshot">The projection to serialize into activity state.</param>
public sealed record DevOpsDisplayProjection(string ResourceId, object Snapshot);

/// <summary>
/// Shows something from Azure DevOps to a person and waits until they have seen it.
/// </summary>
/// <remarks>
/// <para>
/// A UI interaction with a display target of its own: the payload it suspends on carries the descriptor's
/// <see cref="DevOpsDisplayDescriptor.DisplayTarget"/> where a <c>UIInteraction</c> carries a <c>UiDisplayTarget</c>.
/// The reason it is not that activity is that <c>UiDisplayTarget</c> is an enum in a binary drop from another
/// repository, so further values cannot be added here - see <see cref="DevOpsDisplayBookmark"/> for the whole argument.
/// </para>
/// <para>
/// Nothing is read from Azure DevOps: what is shown is handed in, normally by the matching <c>Get</c> activity or by
/// one of the triggers. So this deliberately does not derive from <see cref="AzureDevOpsActivity"/>, whose
/// <c>CanExecuteAsync</c> demands a token - a display refusing to run because no PAT was configured would be failing
/// over a credential it never uses. The organization URL is still taken, because the link a reader opens is derived
/// from it and the API address these objects carry is of no use to them.
/// </para>
/// <para>
/// <see cref="DisplayWorkItem"/> is this activity's twin and deliberately does not derive from it: it shipped first, on
/// a bookmark payload of its own naming the work item id outright, and instances are suspended on that shape today.
/// Rewriting a wire format that a running instance is already waiting on would strand it.
/// </para>
/// </remarks>
public abstract class DevOpsDisplayActivity : Elsa.Workflows.Activity
{
    /// <summary>Description shared by the <c>Heading</c> input of every display.</summary>
    protected const string HeadingDescription =
        "The heading shown above what is on display. Leave empty for a heading derived from the item itself.";

    private static readonly JsonSerializerOptions StateJsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The organization the link points into.</summary>
    [Input(Description = AzureDevOpsActivity.OrganizationUrlDescription)]
    public Input<string> OrganizationUrl { get; set; } = null!;

    /// <summary>What to tell the reader they are looking at.</summary>
    [Input(Description = HeadingDescription)]
    public Input<string?>? Heading { get; set; }

    /// <summary>The names this display answers to. See <see cref="DevOpsDisplayDescriptor"/>.</summary>
    protected abstract DevOpsDisplayDescriptor Descriptor { get; }

    /// <inheritdoc />
    protected sealed override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ActivityInputValidation.ThrowIfInvalid(context, ValidateResource(context));
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateUri(ResolveOrganizationUrl(context), nameof(OrganizationUrl)));

        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected sealed override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        DevOpsDisplayProjection projection = Project(context, ResolveOrganizationUrl(context)!);
        string displayId = Guid.NewGuid().ToString("N");

        context.ActivityState[Descriptor.DisplayIdStateKey] = displayId;

        // JSON rather than the record, and the same choice for the same reason as on DisplayWorkItem: Elsa writes
        // activity state through its own state serializer, and a string round-trips through it unchanged whatever that
        // serializer decides about reference handling or type discriminators. Serialized against the runtime type, so
        // the snapshot is written out in full rather than as the bare object this base declares.
        context.ActivityState[Descriptor.SnapshotStateKey] = JsonSerializer.Serialize(projection.Snapshot, projection.Snapshot.GetType(), StateJsonOptions);

        string? heading = context.Get(Heading);

        if (!string.IsNullOrWhiteSpace(heading))
            context.ActivityState[Descriptor.HeadingStateKey] = heading;

        context.CreateBookmark(new CreateBookmarkArgs
        {
            Stimulus = DevOpsDisplayBookmark.Create(Descriptor, displayId, projection.ResourceId),
            Callback = OnSeenAsync,

            // Defaults to false. With the id present, a viewer resolving activity state finds it on its first, exact
            // match rather than falling back to matching a node id.
            IncludeActivityInstanceId = true,
        });

        return default;
    }

    /// <summary>Whether what was handed in can be shown, and why not when it cannot.</summary>
    protected abstract (bool Valid, string? Error) ValidateResource(ActivityExecutionContext context);

    /// <summary>The projection to show, and the id a viewer names it by.</summary>
    protected abstract DevOpsDisplayProjection Project(ActivityExecutionContext context, string organizationUrl);

    /// <summary>What the execution log says once this display has been confirmed.</summary>
    protected abstract string SeenMessage(ActivityExecutionContext context);

    /// <summary>
    /// Resolves the organization URL the link is derived from, falling back to the configured default when the
    /// activity does not specify one.
    /// </summary>
    protected string? ResolveOrganizationUrl(ActivityExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.GetRequiredService<AzureDevOpsOrganizationUrlResolver>().Resolve(context.Get(OrganizationUrl));
    }

    /// <summary>
    /// Completes once what was shown has been seen. Nothing is read from the resume: the view is read-only, so the only
    /// thing it can report is that it was closed, and reaching here is that report.
    /// </summary>
    private ValueTask OnSeenAsync(ActivityExecutionContext context)
    {
        context.AddExecutionLogEntry(
            eventName: Descriptor.SeenEventName,
            message: SeenMessage(context),
            source: GetType().Name);

        return context.CompleteActivityAsync();
    }
}
