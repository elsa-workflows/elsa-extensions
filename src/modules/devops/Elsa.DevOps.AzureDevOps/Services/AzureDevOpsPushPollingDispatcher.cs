using System.Globalization;
using System.Text.Json;
using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services.Polling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// The events one push poll dispatched, and the per-repository checkpoints it advanced to.
/// </summary>
public sealed record AzureDevOpsPushPollingResult(
    IReadOnlyList<AzureDevOpsPollingEvent> Events,
    IReadOnlyDictionary<string, DateTimeOffset> Checkpoints);

/// <summary>
/// Polls Azure Repos pushes and dispatches matching Elsa trigger stimuli.
/// </summary>
public class AzureDevOpsPushPollingDispatcher(
    AzureDevOpsConnectionFactory connectionFactory,
    AzureDevOpsPollingCredentialResolver credentialResolver,
    AzureDevOpsWebhookEventHandler eventHandler,
    AzureDevOpsOrganizationUrlResolver organizationUrlResolver,
    AzureDevOpsProjectResolver projectResolver,
    ILogger<AzureDevOpsPushPollingDispatcher> logger,
    IOptions<AzureDevOpsPollingOptions> options)
{
    /// <summary>
    /// Polls every watched repository once, dispatching the pushes newer than its checkpoint.
    /// </summary>
    /// <param name="pollingOptions">
    /// What this poll runs with: the configured family with the poll activity's inputs laid over it. The master switch
    /// is read from configuration instead, because switching polling off altogether is the operator's call, not a
    /// workflow's. The family's own switch is deliberately not consulted: it decides whether this family's polling
    /// workflow is started at host start, not whether a poll that something asked for anyway - the manual event, or a
    /// workflow carrying the poll activity - may run.
    /// </param>
    /// <param name="checkpoints">The date of the newest push already dispatched, per repository ID.</param>
    public async Task<AzureDevOpsPushPollingResult> DispatchAsync(
        PushPollingOptions pollingOptions,
        IReadOnlyDictionary<string, DateTimeOffset>? checkpoints,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pollingOptions);

        List<AzureDevOpsPollingEvent> triggeredEvents = [];
        Dictionary<string, DateTimeOffset> newCheckpoints = checkpoints == null
            ? new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, DateTimeOffset>(checkpoints, StringComparer.OrdinalIgnoreCase);

        if (!options.Value.Enabled)
            return new(triggeredEvents, newCheckpoints);

        string? project = projectResolver.Resolve(pollingOptions.Project);
        string? organizationUrl = organizationUrlResolver.Resolve(pollingOptions.OrganizationUrl);

        if (organizationUrl == null || project == null)
            return new(triggeredEvents, newCheckpoints);

        string? token = await credentialResolver.GetTokenAsync(pollingOptions, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("Azure DevOps push polling is enabled but no token or token secret is configured.");
            return new(triggeredEvents, newCheckpoints);
        }

        VssConnection connection = connectionFactory.GetConnection(organizationUrl, token);
        GitHttpClient client = connection.GetClient<GitHttpClient>();
        DateTimeOffset horizon = DateTimeOffset.UtcNow.Subtract(pollingOptions.LookbackWindow);
        List<GitRepository> repositories;

        try
        {
            repositories = await client.GetRepositoriesAsync(project, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Listing Azure DevOps repositories of project {Project} failed; leaving every checkpoint unchanged.", project);
            return new(triggeredEvents, newCheckpoints);
        }

        IReadOnlyCollection<string> configured = [.. pollingOptions.Repositories];

        foreach (GitRepository repository in repositories.Where(repository => PushPayloadFactory.IsWatched(repository, configured)))
        {
            string key = repository.Id.ToString();
            DateTimeOffset checkpoint = newCheckpoints.TryGetValue(key, out DateTimeOffset value) ? value : horizon;
            IReadOnlyList<GitPush> pushes;

            try
            {
                // Paged, because the endpoint answers newest-first: reading one page of Top and then advancing the
                // checkpoint to the newest push would bury every older push of a burst below the checkpoint.
                PolledPage<GitPush> page = await PolledPages.ReadAsync<GitPush>(
                    (skip, top) => client.GetPushesAsync(
                        project,
                        repository.Id,
                        skip: skip,
                        top: top,
                        searchCriteria: new GitPushSearchCriteria { FromDate = checkpoint.UtcDateTime, IncludeRefUpdates = true },
                        cancellationToken: cancellationToken),
                    pollingOptions.Top).ConfigureAwait(false);
                pushes = page.Items;

                if (page.Truncated)
                {
                    logger.LogWarning(
                        "Reading pushes of Azure DevOps repository {Repository} stopped at the cap of {MaxPages} pages of {PageSize}; only the newest {Count} pushes since {Checkpoint:o} are dispatched and older ones are skipped, because the checkpoint advances past them.",
                        repository.Name,
                        PolledPages.MaxPages,
                        pollingOptions.Top,
                        pushes.Count,
                        checkpoint);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The repository's checkpoint stays put: nothing was seen, so nothing should be skipped next time,
                // and one unreadable repository must not stop the others.
                logger.LogWarning(ex, "Reading pushes of Azure DevOps repository {Repository} failed; leaving its checkpoint unchanged.", repository.Name);
                continue;
            }

            DateTimeOffset lastSeen = checkpoint;

            try
            {
                // FromDate is inclusive, so the newest push of the previous poll comes back; strictly greater keeps
                // it from being dispatched twice.
                foreach (GitPush push in pushes
                    .Select(push => (Push: push, Date: ToUtc(push.Date)))
                    .Where(entry => entry.Date > checkpoint)
                    .OrderBy(entry => entry.Date)
                    .Select(entry => entry.Push))
                {
                    JsonElement payload = PushPayloadFactory.ToPayload(push);
                    AzureDevOpsWebhookEvent message = new(
                        AzureDevOpsWebhookEventTypes.CodePushed,
                        payload,
                        repository.ProjectReference?.Id.ToString(),
                        repository.ProjectReference?.Name ?? project);
                    await eventHandler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
                    triggeredEvents.Add(new AzureDevOpsPollingEvent(
                        AzureDevOpsWebhookEventTypes.CodePushed,
                        project,
                        push.PushId.ToString(CultureInfo.InvariantCulture),
                        $"push {push.PushId} to {repository.Name}"));

                    // Only reached once the push above is fully handled, so a failure here leaves lastSeen at the
                    // last push that was actually dispatched.
                    lastSeen = ToUtc(push.Date);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Dispatching a push of Azure DevOps repository {Repository} failed; continuing with the next repository.", repository.Name);
            }
            finally
            {
                // Written even when the loop above threw, so progress made before a mid-pass failure is not lost.
                newCheckpoints[key] = lastSeen;
            }
        }

        return new(triggeredEvents, newCheckpoints);
    }

    private static DateTimeOffset ToUtc(DateTime value) =>
        value == default ? DateTimeOffset.MinValue : new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
