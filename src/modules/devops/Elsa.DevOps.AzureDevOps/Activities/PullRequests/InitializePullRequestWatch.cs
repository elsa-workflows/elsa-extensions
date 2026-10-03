using System.Globalization;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using JetBrains.Annotations;

namespace Elsa.DevOps.AzureDevOps.Activities.PullRequests;

/// <summary>
/// Records which pull request this watcher follows, and its state at the moment watching started. Leaves through
/// <c>Closed</c> when that first read already finds the pull request closed, which ends the watcher before its timer
/// ever starts.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.PullRequests",
    "Azure DevOps Pull Requests",
    "Records the pull request a watcher follows and its starting state.",
    DisplayName = "Initialize Pull Request Watch")]
[FlowNode(ActiveOutcome, ClosedOutcome)]
[UsedImplicitly]
public class InitializePullRequestWatch : CodeActivity
{
    // The same two outcomes WatchAzureDevOpsPullRequest reports, so both ends of the watcher speak one vocabulary.
    public const string ActiveOutcome = WatchAzureDevOpsPullRequest.ActiveOutcome;
    public const string ClosedOutcome = WatchAzureDevOpsPullRequest.ClosedOutcome;

    public const string ProjectVariableName = "AzureDevOpsWatchedProject";
    public const string RepositoryIdVariableName = "AzureDevOpsWatchedRepositoryId";
    public const string PullRequestIdVariableName = "AzureDevOpsWatchedPullRequestId";
    public const string FingerprintVariableName = "AzureDevOpsWatchedFingerprint";
    public const string OrganizationUrlVariableName = "AzureDevOpsWatchedOrganizationUrl";
    public const string TokenSecretNameVariableName = "AzureDevOpsWatchedTokenSecretName";

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string project = GetRequiredString(context, "Project");
        string repositoryId = GetRequiredString(context, "RepositoryId");
        int pullRequestId = GetRequiredInt32(context, "PullRequestId");
        // Optional, unlike the three above: a poll that used the configured connection sends neither, and so does a
        // watcher started before these inputs existed - its instance simply has no such input to rehydrate.
        string? organizationUrl = GetOptionalString(context, "OrganizationUrl");
        string? tokenSecretName = GetOptionalString(context, "TokenSecretName");

        context.SetVariable(ProjectVariableName, project);
        context.SetVariable(RepositoryIdVariableName, repositoryId);
        context.SetVariable(PullRequestIdVariableName, pullRequestId);
        context.SetVariable(OrganizationUrlVariableName, organizationUrl);
        context.SetVariable(TokenSecretNameVariableName, tokenSecretName);

        // Seeded by reading the pull request the same way the watch activity does, so the first tick cannot report a
        // difference that is only an artefact of how the baseline was built.
        AzureDevOpsPullRequestWatcher watcher = context.GetRequiredService<AzureDevOpsPullRequestWatcher>();
        PullRequestWatchResult result = await watcher
            .CheckAsync(
                new PullRequestWatchTarget(project, repositoryId, pullRequestId, organizationUrl, tokenSecretName),
                knownFingerprint: null,
                context.CancellationToken)
            .ConfigureAwait(false);
        context.SetVariable(FingerprintVariableName, result.Fingerprint);

        // A pull request that is already closed when watching starts needs no watcher at all: leaving through Closed
        // ends the instance here rather than parking it on a timer whose first tick can only find the same thing.
        // The poll starts a watcher from a creation event, so this is the reopened checkpoint, the long-running poll
        // that reads a pull request closed since, and the switched-off polling the watcher service reports as
        // inactive - each of which would otherwise cost an interval and one Azure DevOps call before winding down.
        if (!result.Active)
        {
            context.AddExecutionLogEntry(
                eventName: "AzureDevOpsPullRequestWatchSkipped",
                message: $"Pull request {pullRequestId} in project {project} is no longer open; no watcher was started.",
                source: nameof(InitializePullRequestWatch));

            await context.CompleteActivityWithOutcomesAsync(ClosedOutcome).ConfigureAwait(false);
            return;
        }

        context.AddExecutionLogEntry(
            eventName: "AzureDevOpsPullRequestWatchStarted",
            message: $"Watching pull request {pullRequestId} in project {project} for updates.",
            source: nameof(InitializePullRequestWatch));

        await context.CompleteActivityWithOutcomesAsync(ActiveOutcome).ConfigureAwait(false);
    }

    // The inputs come from AzureDevOpsPullRequestWatchStarter, never from the designer, so anything missing or of the
    // wrong shape is a programming error. Naming the key beats the KeyNotFoundException or InvalidCastException an
    // indexer-plus-cast would raise, which says nothing about which input went wrong.
    private static object GetRequiredInput(ActivityExecutionContext context, string key)
    {
        if (!context.WorkflowInput.TryGetValue(key, out object? value) || value == null)
            throw new InvalidOperationException($"The Azure DevOps pull request watcher was started without the workflow input '{key}'.");

        return value;
    }

    /// <summary>
    /// Reads an input the starter only sends when it has something to say. Anything that is not a non-empty string -
    /// absent, null, or the empty string a poll on the configured connection sends - reads as "nothing was said".
    /// </summary>
    private static string? GetOptionalString(ActivityExecutionContext context, string key) =>
        context.WorkflowInput.TryGetValue(key, out object? value) && value is string text && !string.IsNullOrWhiteSpace(text)
            ? text.Trim()
            : null;

    private static string GetRequiredString(ActivityExecutionContext context, string key)
    {
        object value = GetRequiredInput(context, key);

        return value as string
            ?? throw new InvalidOperationException($"The workflow input '{key}' of the Azure DevOps pull request watcher must be a string, but is a {value.GetType().Name}.");
    }

    private static int GetRequiredInt32(ActivityExecutionContext context, string key)
    {
        object value = GetRequiredInput(context, key);

        // Converted rather than cast: an instance whose input was rehydrated from storage can hand the same number
        // back as a long or as text.
        try
        {
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            throw new InvalidOperationException($"The workflow input '{key}' of the Azure DevOps pull request watcher must be a number, but is '{value}'.", ex);
        }
    }
}
