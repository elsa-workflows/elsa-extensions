using Elsa.DevOps.AzureDevOps.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.Activities.Repositories;

/// <summary>
/// Shows a Git repository to a person and waits until they have seen it.
/// </summary>
/// <remarks>
/// <see cref="DevOpsDisplayActivity"/> carries the design: a UI interaction with a display target this package owns,
/// nothing read from Azure DevOps, and therefore no PAT demanded. The repository is handed in, normally by
/// <see cref="GetRepository"/> or by the push trigger.
/// </remarks>
[Activity(
    "Elsa.AzureDevOps.Repositories",
    "Azure DevOps Repositories",
    "Shows a Git repository read-only, with a link to Azure DevOps, and waits until it has been seen.",
    DisplayName = "Display Repository")]
[UsedImplicitly]
public class DisplayRepository : DevOpsDisplayActivity
{
    /// <summary>The activity state key carrying the handle of this display.</summary>
    /// <remarks>
    /// Public because it is a contract, not an implementation detail: a viewer in another host joins the bookmark to
    /// the state on this value. Named <c>RepositoryDisplay*</c> because Elsa writes every evaluated input into
    /// <c>ActivityState</c> under its own property name, and <c>Repository</c> is this activity's own input.
    /// </remarks>
    public const string DisplayIdStateKey = "RepositoryDisplayId";

    /// <summary>The activity state key carrying the repository projection, as JSON.</summary>
    public const string RepositoryStateKey = "RepositoryDisplay";

    /// <summary>The activity state key carrying the heading, when the workflow gave one.</summary>
    public const string HeadingStateKey = "RepositoryDisplayHeading";

    /// <summary>The bookmark payload discriminator this activity writes.</summary>
    public const string BookmarkKind = "azuredevops-repository/v1";

    /// <summary>The custom UI display target this interaction asks for.</summary>
    public const string DisplayTargetName = "Repository";

    private static readonly DevOpsDisplayDescriptor Names = new(
        BookmarkKind,
        DisplayTargetName,
        DisplayIdStateKey,
        RepositoryStateKey,
        HeadingStateKey,
        SeenEventName: "RepositorySeen");

    /// <summary>The repository to show.</summary>
    [Input(Description = "The repository to show. Bind the output of Get Repository, or the repository a trigger handed over.")]
    public Input<GitRepository> Repository { get; set; } = null!;

    /// <inheritdoc />
    protected override DevOpsDisplayDescriptor Descriptor => Names;

    /// <inheritdoc />
    protected override (bool Valid, string? Error) ValidateResource(ActivityExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        GitRepository? repository = context.Get(Repository);

        if (repository == null)
            return (false, $"'{nameof(Repository)}' must be specified.");

        // A repository is identified by a GUID rather than a number, so the empty GUID is what "no id" looks like here.
        // Refused for the same reason a build without an id is: it cannot be linked to, and a card with a name and no
        // way to open it looks like it worked.
        return repository.Id == Guid.Empty
            ? (false, $"'{nameof(Repository)}' must carry an id.")
            : (true, null);
    }

    /// <inheritdoc />
    protected override DevOpsDisplayProjection Project(ActivityExecutionContext context, string organizationUrl)
    {
        ArgumentNullException.ThrowIfNull(context);

        GitRepository repository = context.Get(Repository)!;

        return new DevOpsDisplayProjection(
            repository.Id.ToString(),
            RepositorySnapshot.From(repository, organizationUrl));
    }

    /// <inheritdoc />
    protected override string SeenMessage(ActivityExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        GitRepository? repository = context.Get(Repository);

        // By name, not by GUID: this is what a person reads in the execution log, and "cc-studio was confirmed as seen"
        // says something where a GUID does not.
        return $"Repository {repository?.Name ?? repository?.Id.ToString() ?? "?"} was confirmed as seen.";
    }
}
