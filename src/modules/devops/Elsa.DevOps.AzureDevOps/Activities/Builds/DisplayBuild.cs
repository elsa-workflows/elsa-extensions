using System.Globalization;
using Elsa.DevOps.AzureDevOps.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using AzureBuild = Microsoft.TeamFoundation.Build.WebApi.Build;

namespace Elsa.DevOps.AzureDevOps.Activities.Builds;

/// <summary>
/// Shows a build to a person and waits until they have seen it.
/// </summary>
/// <remarks>
/// <see cref="DevOpsDisplayActivity"/> carries the design: a UI interaction with a display target this package owns,
/// nothing read from Azure DevOps, and therefore no PAT demanded. The build is handed in, normally by
/// <see cref="GetBuild"/> or by one of the build triggers.
/// </remarks>
[Activity(
    "Elsa.AzureDevOps.Builds",
    "Azure DevOps Builds",
    "Shows a build read-only, with a link to Azure DevOps, and waits until it has been seen.",
    DisplayName = "Display Build")]
[UsedImplicitly]
public class DisplayBuild : DevOpsDisplayActivity
{
    /// <summary>The activity state key carrying the handle of this display.</summary>
    /// <remarks>
    /// Public because it is a contract, not an implementation detail: a viewer in another host joins the bookmark to
    /// the state on this value. Named <c>BuildDisplay*</c> rather than <c>Display*</c> because Elsa writes every
    /// evaluated input into <c>ActivityState</c> under its own property name, and a shorter name risks colliding with
    /// one - as would <c>Build</c> itself, which is this activity's own input.
    /// </remarks>
    public const string DisplayIdStateKey = "BuildDisplayId";

    /// <summary>The activity state key carrying the build projection, as JSON.</summary>
    public const string BuildStateKey = "BuildDisplay";

    /// <summary>The activity state key carrying the heading, when the workflow gave one.</summary>
    public const string HeadingStateKey = "BuildDisplayHeading";

    /// <summary>The bookmark payload discriminator this activity writes.</summary>
    public const string BookmarkKind = "azuredevops-build/v1";

    /// <summary>The custom UI display target this interaction asks for.</summary>
    public const string DisplayTargetName = "Build";

    private static readonly DevOpsDisplayDescriptor Names = new(
        BookmarkKind,
        DisplayTargetName,
        DisplayIdStateKey,
        BuildStateKey,
        HeadingStateKey,
        SeenEventName: "BuildSeen");

    /// <summary>The build to show.</summary>
    [Input(Description = "The build to show. Bind the output of Get Build, or the build a trigger handed over.")]
    public Input<AzureBuild> Build { get; set; } = null!;

    /// <inheritdoc />
    protected override DevOpsDisplayDescriptor Descriptor => Names;

    /// <inheritdoc />
    protected override (bool Valid, string? Error) ValidateResource(ActivityExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        AzureBuild? build = context.Get(Build);

        if (build == null)
            return (false, $"'{nameof(Build)}' must be specified.");

        // A build without an id cannot be linked to, which is half of what this view is for. It is also the shape a
        // Service Hook payload arrives in when its casing did not bind, so an id of zero is refused rather than
        // rendered as a link to build 0 - the same rule, for the same reason, as on DisplayWorkItem.
        return build.Id == 0
            ? (false, $"'{nameof(Build)}' must carry an id.")
            : (true, null);
    }

    /// <inheritdoc />
    protected override DevOpsDisplayProjection Project(ActivityExecutionContext context, string organizationUrl)
    {
        ArgumentNullException.ThrowIfNull(context);

        AzureBuild build = context.Get(Build)!;

        return new DevOpsDisplayProjection(
            build.Id.ToString(CultureInfo.InvariantCulture),
            BuildSnapshot.From(build, organizationUrl));
    }

    /// <inheritdoc />
    protected override string SeenMessage(ActivityExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        int buildId = context.Get(Build)?.Id ?? 0;

        return $"Build {buildId.ToString(CultureInfo.InvariantCulture)} was confirmed as seen.";
    }
}
