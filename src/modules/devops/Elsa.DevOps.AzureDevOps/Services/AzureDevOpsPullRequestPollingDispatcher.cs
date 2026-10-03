using System.Globalization;
using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services.Polling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// The events one pull request poll dispatched, and the checkpoints it advanced to.
/// </summary>
public sealed record AzureDevOpsPullRequestPollingResult(
    IReadOnlyList<AzureDevOpsPollingEvent> Events,
    PullRequestPollingCheckpoints Checkpoints);

/// <summary>
/// Polls Azure DevOps pull requests and dispatches matching Elsa trigger stimuli.
/// </summary>
public class AzureDevOpsPullRequestPollingDispatcher(
    AzureDevOpsConnectionFactory connectionFactory,
    AzureDevOpsPollingCredentialResolver credentialResolver,
    AzureDevOpsWebhookEventHandler eventHandler,
    AzureDevOpsOrganizationUrlResolver organizationUrlResolver,
    AzureDevOpsProjectResolver projectResolver,
    AzureDevOpsPullRequestWatchStarter watchStarter,
    ILogger<AzureDevOpsPullRequestPollingDispatcher> logger,
    IOptions<AzureDevOpsPollingOptions> options)
{
    /// <param name="pollingOptions">
    /// What this poll runs with: the configured family with the poll activity's inputs laid over it. The master switch
    /// is read from configuration instead, because switching polling off altogether is the operator's call, not a
    /// workflow's. The family's own switch is deliberately not consulted: it decides whether this family's polling
    /// workflow is started at host start, not whether a poll that something asked for anyway - the manual event, or a
    /// workflow carrying the poll activity - may run.
    /// </param>
    public async Task<AzureDevOpsPullRequestPollingResult> DispatchAsync(
        PullRequestPollingOptions pollingOptions,
        PullRequestPollingCheckpoints? checkpoints,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pollingOptions);

        List<AzureDevOpsPollingEvent> triggeredEvents = [];
        PullRequestPollingCheckpoints current = checkpoints ?? Seed(pollingOptions.LookbackWindow);

        if (!options.Value.Enabled)
            return new(triggeredEvents, current);

        string? project = projectResolver.Resolve(pollingOptions.Project);
        string? organizationUrl = organizationUrlResolver.Resolve(pollingOptions.OrganizationUrl);

        if (organizationUrl == null || project == null)
            return new(triggeredEvents, current);

        string? token = await credentialResolver.GetTokenAsync(pollingOptions, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("Azure DevOps pull request polling is enabled but no token or token secret is configured.");
            return new(triggeredEvents, current);
        }

        VssConnection connection = connectionFactory.GetConnection(organizationUrl, token);
        GitHttpClient client = connection.GetClient<GitHttpClient>();
        IReadOnlyList<GitPullRequest> created;
        IReadOnlyList<GitPullRequest> closed;

        try
        {
            // Both reads are paged, because the endpoint answers newest-first: reading one page of Top and then
            // advancing the checkpoint to the newest pull request would bury every older one of a burst below the
            // checkpoint.
            // Status must be set explicitly: the API defaults to Active, which would hide a pull request opened and
            // completed inside one interval.
            PolledPage<GitPullRequest> createdPage = await PolledPages.ReadAsync<GitPullRequest>(
                (skip, top) => client.GetPullRequestsByProjectAsync(
                    project,
                    new GitPullRequestSearchCriteria
                    {
                        Status = PullRequestStatus.All,
                        QueryTimeRangeType = PullRequestTimeRangeType.Created,
                        MinTime = current.LastCreated.UtcDateTime,
                    },
                    skip: skip,
                    top: top,
                    cancellationToken: cancellationToken),
                pollingOptions.Top).ConfigureAwait(false);
            PolledPage<GitPullRequest> closedPage = await PolledPages.ReadAsync<GitPullRequest>(
                (skip, top) => client.GetPullRequestsByProjectAsync(
                    project,
                    new GitPullRequestSearchCriteria
                    {
                        Status = PullRequestStatus.Completed,
                        QueryTimeRangeType = PullRequestTimeRangeType.Closed,
                        MinTime = current.LastClosed.UtcDateTime,
                    },
                    skip: skip,
                    top: top,
                    cancellationToken: cancellationToken),
                pollingOptions.Top).ConfigureAwait(false);
            created = createdPage.Items;
            closed = closedPage.Items;
            WarnWhenTruncated(createdPage, "created", pollingOptions.Top, current.LastCreated, project);
            WarnWhenTruncated(closedPage, "closed", pollingOptions.Top, current.LastClosed, project);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Reading Azure DevOps pull requests of project {Project} failed; leaving its checkpoints unchanged.", project);
            return new(triggeredEvents, current);
        }

        bool canWatch = watchStarter.CanWatch(organizationUrl, pollingOptions.TokenSecretName);

        if (pollingOptions.WatchUpdates && !canWatch)
        {
            logger.LogWarning(
                "Azure DevOps pull request polling of organization {OrganizationUrl} reports created and merged pull requests but starts no update watchers, because a watcher of another organization needs a credential it can resolve itself. Set TokenSecretName instead of Token on the poll activity to have updates watched as well.",
                organizationUrl);
        }

        foreach (PullRequestPollingEvent @event in PullRequestEventDeriver.Derive(created, closed, current))
        {
            try
            {
                await DispatchAsync(@event, project, organizationUrl, pollingOptions, canWatch, cancellationToken).ConfigureAwait(false);
                triggeredEvents.Add(new AzureDevOpsPollingEvent(
                    @event.EventType,
                    project,
                    @event.PullRequest.PullRequestId.ToString(CultureInfo.InvariantCulture),
                    $"pull request {@event.PullRequest.PullRequestId}"));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Stop before advancing: the failed event must be retried on the next poll, and everything after it
                // is newer, so leaving the checkpoint here loses nothing.
                logger.LogWarning(ex, "Dispatching Azure DevOps {EventType} for pull request {PullRequestId} failed; retrying on the next poll.", @event.EventType, @event.PullRequest.PullRequestId);
                break;
            }

            PullRequestEventDeriver.Advance(current, @event);
        }

        return new(triggeredEvents, current);
    }

    private void WarnWhenTruncated(
        PolledPage<GitPullRequest> page,
        string query,
        int pageSize,
        DateTimeOffset checkpoint,
        string project)
    {
        if (!page.Truncated)
            return;

        logger.LogWarning(
            "Reading the {Query} pull requests of Azure DevOps project {Project} stopped at the cap of {MaxPages} pages of {PageSize}; only the newest {Count} since {Checkpoint:o} are dispatched and older ones are skipped, because the checkpoint advances past them.",
            query,
            project,
            PolledPages.MaxPages,
            pageSize,
            page.Items.Count,
            checkpoint);
    }

    private async Task DispatchAsync(
        PullRequestPollingEvent @event,
        string project,
        string organizationUrl,
        PullRequestPollingOptions pollingOptions,
        bool canWatch,
        CancellationToken cancellationToken)
    {
        // Reuse the webhook handler so polled events produce the exact same bookmark variants as Service Hook events;
        // sending a stimulus without a bookmark payload never matches an indexed trigger.
        AzureDevOpsWebhookEvent message = new(
            @event.EventType,
            @event.PullRequest,
            @event.PullRequest.Repository?.ProjectReference?.Id.ToString(),
            @event.PullRequest.Repository?.ProjectReference?.Name ?? project);
        await eventHandler.HandleAsync(message, cancellationToken).ConfigureAwait(false);

        // A new pull request also gets a watcher, which is the only route to git.pullrequest.updated: no query
        // filters on when a pull request last changed. The watcher is handed the connection this poll ran under, so it
        // re-reads the pull request where it was found rather than where configuration points.
        if (@event.EventType == AzureDevOpsWebhookEventTypes.PullRequestCreated && pollingOptions.WatchUpdates && canWatch)
        {
            await watchStarter
                .StartAsync(@event.PullRequest, project, organizationUrl, pollingOptions.TokenSecretName, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static PullRequestPollingCheckpoints Seed(TimeSpan lookbackWindow)
    {
        DateTimeOffset horizon = DateTimeOffset.UtcNow.Subtract(lookbackWindow);
        return new PullRequestPollingCheckpoints { LastCreated = horizon, LastClosed = horizon };
    }
}
