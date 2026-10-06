using System.Globalization;
using Elsa.DevOps.AzureDevOps.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.Activities.PullRequests;

/// <summary>
/// Shows a pull request to a person and waits until they have seen it.
/// </summary>
/// <remarks>
/// <see cref="DevOpsDisplayActivity"/> carries the design: a UI interaction with a display target this package owns,
/// nothing read from Azure DevOps, and therefore no PAT demanded. The pull request is handed in, normally by
/// <see cref="GetPullRequest"/>, by <see cref="WatchAzureDevOpsPullRequest"/> or by one of the pull request triggers.
/// </remarks>
[Activity(
    "Elsa.AzureDevOps.PullRequests",
    "Azure DevOps Pull Requests",
    "Shows a pull request read-only, with a link to Azure DevOps, and waits until it has been seen.",
    DisplayName = "Display Pull Request")]
[UsedImplicitly]
public class DisplayPullRequest : DevOpsDisplayActivity
{
    /// <summary>The activity state key carrying the handle of this display.</summary>
    /// <remarks>
    /// Public because it is a contract, not an implementation detail: a viewer in another host joins the bookmark to
    /// the state on this value. Named <c>PullRequestDisplay*</c> because Elsa writes every evaluated input into
    /// <c>ActivityState</c> under its own property name, and <c>PullRequest</c> is this activity's own input.
    /// </remarks>
    public const string DisplayIdStateKey = "PullRequestDisplayId";

    /// <summary>The activity state key carrying the pull request projection, as JSON.</summary>
    public const string PullRequestStateKey = "PullRequestDisplay";

    /// <summary>The activity state key carrying the heading, when the workflow gave one.</summary>
    public const string HeadingStateKey = "PullRequestDisplayHeading";

    /// <summary>The bookmark payload discriminator this activity writes.</summary>
    public const string BookmarkKind = "azuredevops-pullrequest/v1";

    /// <summary>The custom UI display target this interaction asks for.</summary>
    public const string DisplayTargetName = "PullRequest";

    private static readonly DevOpsDisplayDescriptor Names = new(
        BookmarkKind,
        DisplayTargetName,
        DisplayIdStateKey,
        PullRequestStateKey,
        HeadingStateKey,
        SeenEventName: "PullRequestSeen");

    /// <summary>The pull request to show.</summary>
    [Input(Description = "The pull request to show. Bind the output of Get Pull Request, or the pull request a trigger or watch handed over.")]
    public Input<GitPullRequest> PullRequest { get; set; } = null!;

    /// <inheritdoc />
    protected override DevOpsDisplayDescriptor Descriptor => Names;

    /// <inheritdoc />
    protected override (bool Valid, string? Error) ValidateResource(ActivityExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        GitPullRequest? pullRequest = context.Get(PullRequest);

        if (pullRequest == null)
            return (false, $"'{nameof(PullRequest)}' must be specified.");

        // A pull request without an id cannot be linked to, which is half of what this view is for - the same rule,
        // for the same reason, as on DisplayWorkItem and DisplayBuild.
        return pullRequest.PullRequestId == 0
            ? (false, $"'{nameof(PullRequest)}' must carry an id.")
            : (true, null);
    }

    /// <inheritdoc />
    protected override DevOpsDisplayProjection Project(ActivityExecutionContext context, string organizationUrl)
    {
        ArgumentNullException.ThrowIfNull(context);

        GitPullRequest pullRequest = context.Get(PullRequest)!;

        return new DevOpsDisplayProjection(
            pullRequest.PullRequestId.ToString(CultureInfo.InvariantCulture),
            PullRequestSnapshot.From(pullRequest, organizationUrl));
    }

    /// <inheritdoc />
    protected override string SeenMessage(ActivityExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        int pullRequestId = context.Get(PullRequest)?.PullRequestId ?? 0;

        return $"Pull request {pullRequestId.ToString(CultureInfo.InvariantCulture)} was confirmed as seen.";
    }
}
