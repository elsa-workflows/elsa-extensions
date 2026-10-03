using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services.Polling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// What one check of a watched pull request found.
/// </summary>
/// <param name="Changed">Whether an update was dispatched.</param>
/// <param name="Active">Whether the pull request is still open, and so still worth watching.</param>
/// <param name="Fingerprint">
/// The fingerprint to compare against on the next check, or <c>null</c> when no successful read has ever
/// established one. It must stay <c>null</c> rather than collapse to an empty string: the seeding call from
/// <see cref="Elsa.DevOps.AzureDevOps.Activities.PullRequests.InitializePullRequestWatch"/> passes
/// <c>knownFingerprint: null</c>, and an empty string is a value like any other — the next successful check would
/// compare a real fingerprint against it, find them different, and report an update that never happened.
/// </param>
public sealed record PullRequestWatchResult(bool Changed, bool Active, string? Fingerprint);

/// <summary>
/// Which pull request a watcher follows, and the connection the poll found it over.
/// </summary>
/// <param name="OrganizationUrl">
/// The organization the pull request lives in, or <c>null</c> to use the configured one. Set for a poll that read
/// another organization: its repository and pull request IDs mean nothing in the configured one.
/// </param>
/// <param name="TokenSecretName">
/// The Elsa Secret the watcher authenticates with, or <c>null</c> to use the configured polling credential. A secret's
/// name rather than a token, because a watcher outlives the poll that started it and would otherwise have to carry the
/// token in its stored instance.
/// </param>
public sealed record PullRequestWatchTarget(
    string Project,
    string RepositoryId,
    int PullRequestId,
    string? OrganizationUrl = null,
    string? TokenSecretName = null);

/// <summary>
/// Re-reads one pull request and dispatches <c>git.pullrequest.updated</c> when it changed. Polling cannot query for
/// updates — the API filters only on creation and closure — so each open pull request is watched individually.
/// </summary>
public class AzureDevOpsPullRequestWatcher(
    AzureDevOpsConnectionFactory connectionFactory,
    AzureDevOpsPollingCredentialResolver credentialResolver,
    AzureDevOpsWebhookEventHandler eventHandler,
    AzureDevOpsOrganizationUrlResolver organizationUrlResolver,
    ILogger<AzureDevOpsPullRequestWatcher> logger,
    IOptions<AzureDevOpsPollingOptions> options)
{
    /// <summary>
    /// Reads the pull request and compares it with <paramref name="knownFingerprint"/>. A <c>null</c> fingerprint
    /// means this is the first look, which only records the baseline and dispatches nothing.
    /// </summary>
    public async Task<PullRequestWatchResult> CheckAsync(
        PullRequestWatchTarget target,
        string? knownFingerprint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);

        string project = target.Project;
        string repositoryId = target.RepositoryId;
        int pullRequestId = target.PullRequestId;
        // The connection the poll ran under wins over configuration, exactly as it does on the poll activity itself.
        // WatchUpdates is not part of that: it stays the configured switch, which is what lets a running watcher wind
        // itself down below.
        PullRequestPollingOptions pollingOptions = options.Value.PullRequests.With(new PullRequestPollingOverrides
        {
            OrganizationUrl = target.OrganizationUrl,
            TokenSecretName = target.TokenSecretName,
        });

        // A watcher outlives the switches that created it: its instance is timer-driven, so the scheduler keeps
        // resuming it however the configuration changed since. Reporting the pull request as no longer active is what
        // sends the watch activity down its Closed outcome, which is the only way a running watcher winds itself
        // down; without this, switching polling off would leave one instance per open pull request calling Azure
        // DevOps every interval.
        //
        // The family switch is not one of these two, by the same rule the dispatchers follow: it governs what starts
        // at host start, not what a running instance may do. Winding existing watchers down is therefore the master
        // switch or WatchUpdates - PullRequests:Enabled on its own leaves them running until their pull request closes.
        if (!options.Value.Enabled || !pollingOptions.WatchUpdates)
        {
            logger.LogInformation(
                "Ending the watcher of Azure DevOps pull request {PullRequestId}: polling or update watching has been switched off.",
                pullRequestId);
            return new(false, false, knownFingerprint);
        }

        string? organizationUrl = organizationUrlResolver.Resolve(pollingOptions.OrganizationUrl);
        string? token = await credentialResolver.GetTokenAsync(pollingOptions, cancellationToken).ConfigureAwait(false);

        if (organizationUrl == null || string.IsNullOrWhiteSpace(token))
            return new(false, true, knownFingerprint);

        VssConnection connection = connectionFactory.GetConnection(organizationUrl, token);
        GitHttpClient client = connection.GetClient<GitHttpClient>();
        GitPullRequest pullRequest;
        GitPullRequestIteration? newestIteration = null;

        try
        {
            pullRequest = await client
                .GetPullRequestAsync(project, repositoryId, pullRequestId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            List<GitPullRequestIteration> iterations = await client
                .GetPullRequestIterationsAsync(project, repositoryId, pullRequestId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            newestIteration = PullRequestFingerprint.SelectNewest(iterations);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Keep watching rather than guess: a transient failure must not end a watcher, and the fingerprint stays
            // as it was so the next check still notices whatever changed meanwhile. Passing knownFingerprint through
            // unchanged also matters when it is null: a failed seeding read must stay "unknown", not become an
            // empty-string baseline that the next successful check would then treat as a real, different value.
            logger.LogWarning(ex, "Reading watched Azure DevOps pull request {PullRequestId} failed; watching continues.", pullRequestId);
            return new(false, true, knownFingerprint);
        }

        string fingerprint = PullRequestFingerprint.Compute(pullRequest, newestIteration);
        bool active = pullRequest.Status == PullRequestStatus.Active;
        bool changed = knownFingerprint != null && !string.Equals(knownFingerprint, fingerprint, StringComparison.Ordinal);

        if (changed && active)
        {
            // Reuse the webhook handler so the watched update produces the same bookmark variants as a Service Hook
            // event would.
            AzureDevOpsWebhookEvent message = new(
                AzureDevOpsWebhookEventTypes.PullRequestUpdated,
                pullRequest,
                pullRequest.Repository?.ProjectReference?.Id.ToString(),
                pullRequest.Repository?.ProjectReference?.Name ?? project);
            await eventHandler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
        }

        return new(changed && active, active, fingerprint);
    }
}
