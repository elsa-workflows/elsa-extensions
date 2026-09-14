using System.Globalization;
using System.Text.Json;
using Elsa.DevOps.AzureDevOps.Bookmarks;
using Elsa.DevOps.AzureDevOps.Models;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.Activities.WorkItems;

/// <summary>
/// Shows a work item to a person and waits until they have seen it.
/// </summary>
/// <remarks>
/// <para>
/// A UI interaction with a display target of its own: the payload it suspends on carries
/// <see cref="WorkItemDisplayBookmark.DisplayTarget"/> where a <c>UIInteraction</c> carries a <c>UiDisplayTarget</c>.
/// The reason it is not that activity is that <c>UiDisplayTarget</c> is an enum in a binary drop from another
/// repository, so a fifth value cannot be added here - see <see cref="WorkItemDisplayBookmark"/> for the whole
/// argument.
/// </para>
/// <para>
/// Nothing is read from Azure DevOps: the work item is handed in, normally by <see cref="GetWorkItem"/> or by one of
/// the work item triggers. So this deliberately does not derive from <see cref="AzureDevOpsActivity"/>, whose
/// <c>CanExecuteAsync</c> demands a token - a display refusing to run because no PAT was configured would be failing
/// over a credential it never uses. The organization URL is still taken, because the link a reader opens is derived
/// from it and the API address the work item carries is of no use to them.
/// </para>
/// </remarks>
[Activity(
    "Elsa.AzureDevOps.WorkItems",
    "Azure DevOps Work Items",
    "Shows a work item read-only, with a link to Azure DevOps, and waits until it has been seen.",
    DisplayName = "Display Work Item")]
[UsedImplicitly]
public class DisplayWorkItem : Elsa.Workflows.Activity
{
    /// <summary>The activity state key carrying the handle of this display.</summary>
    /// <remarks>
    /// Public because it is a contract, not an implementation detail: a viewer in another host joins the bookmark to
    /// the state on this value. Named <c>WorkItemDisplay*</c> rather than <c>Display*</c> because Elsa writes every
    /// evaluated input into <c>ActivityState</c> under its own property name, and a shorter name risks colliding with
    /// one.
    /// </remarks>
    public const string DisplayIdStateKey = "WorkItemDisplayId";

    /// <summary>The activity state key carrying the work item projection, as JSON.</summary>
    /// <remarks>
    /// A projection rather than the work item, and JSON rather than the record: Elsa writes activity state through its
    /// own state serializer, and a string round-trips through it unchanged whatever that serializer decides about
    /// reference handling or type discriminators. The same choice, for the same reason, as the conversation on
    /// <c>AgentChatSession</c>. The projection is <see cref="WorkItemSnapshot"/>, which is already the answer to "the
    /// part of a work item worth handing to a reader" and already derives the browser URL.
    /// </remarks>
    public const string WorkItemStateKey = "WorkItemDisplay";

    /// <summary>The activity state key carrying the heading, when the workflow gave one.</summary>
    public const string HeadingStateKey = "WorkItemDisplayHeading";

    private static readonly JsonSerializerOptions StateJsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The work item to show.</summary>
    [Input(Description = "The work item to show. Bind the output of Get Work Item, or the work item a trigger handed over.")]
    public Input<WorkItem> WorkItem { get; set; } = null!;

    /// <summary>The organization the link points into.</summary>
    [Input(Description = AzureDevOpsActivity.OrganizationUrlDescription)]
    public Input<string> OrganizationUrl { get; set; } = null!;

    /// <summary>What to tell the reader they are looking at.</summary>
    [Input(Description = "The heading shown above the work item. Leave empty for a heading derived from the work item's own type and id.")]
    public Input<string?>? Heading { get; set; }

    /// <inheritdoc />
    protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ActivityInputValidation.ThrowIfInvalid(context, ValidateWorkItem(context.Get(WorkItem)));
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateUri(ResolveOrganizationUrl(context), nameof(OrganizationUrl)));

        return base.CanExecuteAsync(context);
    }

    /// <inheritdoc />
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        WorkItemSnapshot snapshot = WorkItemSnapshot.From(context.Get(WorkItem)!, ResolveOrganizationUrl(context)!);
        string displayId = Guid.NewGuid().ToString("N");

        context.ActivityState[DisplayIdStateKey] = displayId;
        context.ActivityState[WorkItemStateKey] = JsonSerializer.Serialize(snapshot, StateJsonOptions);

        string? heading = context.Get(Heading);

        if (!string.IsNullOrWhiteSpace(heading))
            context.ActivityState[HeadingStateKey] = heading;

        context.CreateBookmark(new CreateBookmarkArgs
        {
            Stimulus = WorkItemDisplayBookmark.Create(displayId, snapshot.Id),
            Callback = OnSeenAsync,

            // Defaults to false. With the id present, a viewer resolving activity state finds it on its first, exact
            // match rather than falling back to matching a node id.
            IncludeActivityInstanceId = true,
        });

        return default;
    }

    /// <summary>
    /// Completes once the work item has been seen. Nothing is read from the resume: the view is read-only, so the only
    /// thing it can report is that it was closed, and reaching here is that report.
    /// </summary>
    private ValueTask OnSeenAsync(ActivityExecutionContext context)
    {
        int workItemId = context.Get(WorkItem)?.Id ?? 0;

        context.AddExecutionLogEntry(
            eventName: "WorkItemSeen",
            message: $"Work item {workItemId.ToString(CultureInfo.InvariantCulture)} was confirmed as seen.",
            source: nameof(DisplayWorkItem));

        return context.CompleteActivityAsync();
    }

    private static (bool Valid, string? Error) ValidateWorkItem(WorkItem? workItem)
    {
        if (workItem == null)
            return (false, $"'{nameof(WorkItem)}' must be specified.");

        // A work item without an id cannot be linked to, which is half of what this view is for. It is also the shape a
        // Service Hook payload arrives in when its casing did not bind - the failure WorkItemCommentedTriggerTests
        // pins down - so an id of zero is refused rather than rendered as a link to work item 0.
        return workItem.Id is null or 0
            ? (false, $"'{nameof(WorkItem)}' must carry an id.")
            : (true, null);
    }

    private string? ResolveOrganizationUrl(ActivityExecutionContext context) =>
        context.GetRequiredService<AzureDevOpsOrganizationUrlResolver>().Resolve(context.Get(OrganizationUrl));
}
