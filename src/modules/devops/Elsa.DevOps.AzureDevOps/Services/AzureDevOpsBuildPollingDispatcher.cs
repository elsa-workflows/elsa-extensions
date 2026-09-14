using System.Globalization;
using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services.Polling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// The events one build poll dispatched, and the checkpoints it advanced to.
/// </summary>
public sealed record AzureDevOpsBuildPollingResult(
    IReadOnlyList<AzureDevOpsPollingEvent> Events,
    BuildPollingCheckpoints Checkpoints);

/// <summary>
/// Polls Azure DevOps builds and dispatches matching Elsa trigger stimuli.
/// </summary>
public class AzureDevOpsBuildPollingDispatcher(
    AzureDevOpsConnectionFactory connectionFactory,
    AzureDevOpsPollingCredentialResolver credentialResolver,
    AzureDevOpsWebhookEventHandler eventHandler,
    AzureDevOpsOrganizationUrlResolver organizationUrlResolver,
    AzureDevOpsProjectResolver projectResolver,
    ILogger<AzureDevOpsBuildPollingDispatcher> logger,
    IOptions<AzureDevOpsPollingOptions> options)
{
    /// <param name="pollingOptions">
    /// What this poll runs with: the configured family with the poll activity's inputs laid over it. The master switch
    /// is read from configuration instead, because switching polling off altogether is the operator's call, not a
    /// workflow's. The family's own switch is deliberately not consulted: it decides whether this family's polling
    /// workflow is started at host start, not whether a poll that something asked for anyway - the manual event, or a
    /// workflow carrying the poll activity - may run.
    /// </param>
    public async Task<AzureDevOpsBuildPollingResult> DispatchAsync(
        BuildPollingOptions pollingOptions,
        BuildPollingCheckpoints? checkpoints,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pollingOptions);

        List<AzureDevOpsPollingEvent> triggeredEvents = [];
        BuildPollingCheckpoints current = checkpoints ?? Seed(pollingOptions.LookbackWindow);

        if (!options.Value.Enabled)
            return new(triggeredEvents, current);

        string? project = projectResolver.Resolve(pollingOptions.Project);
        string? organizationUrl = organizationUrlResolver.Resolve(pollingOptions.OrganizationUrl);

        if (organizationUrl == null || project == null)
            return new(triggeredEvents, current);

        string? token = await credentialResolver.GetTokenAsync(pollingOptions, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("Azure DevOps build polling is enabled but no token or token secret is configured.");
            return new(triggeredEvents, current);
        }

        VssConnection connection = connectionFactory.GetConnection(organizationUrl, token);
        BuildHttpClient client = connection.GetClient<BuildHttpClient>();
        List<Build> notStarted;
        List<Build> inProgress;
        List<Build> completed;

        try
        {
            // The only GetBuildsAsync overload BuildHttpClient exposes filters on finish time, so it can never find
            // an in-flight build - one with no finish time yet - however the query is ordered. In-flight builds are
            // therefore read by status instead, with no time filter at all; there are few of them at any moment, so
            // reading all of them every poll is cheap. BuildStatus is not a [Flags] enum even though its members are
            // bit values, so NotStarted and InProgress are read as two separate queries rather than relying on `|`
            // to mean "either status".
            notStarted = await client.GetBuildsAsync(
                project,
                statusFilter: BuildStatus.NotStarted,
                top: pollingOptions.Top,
                queryOrder: BuildQueryOrder.QueueTimeAscending,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            inProgress = await client.GetBuildsAsync(
                project,
                statusFilter: BuildStatus.InProgress,
                top: pollingOptions.Top,
                queryOrder: BuildQueryOrder.QueueTimeAscending,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            completed = await client.GetBuildsAsync(
                project,
                statusFilter: BuildStatus.Completed,
                minFinishTime: current.LastFinished.UtcDateTime,
                top: pollingOptions.Top,
                queryOrder: BuildQueryOrder.FinishTimeAscending,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The checkpoints stay put: nothing was seen, so nothing should be skipped next time.
            logger.LogWarning(ex, "Reading Azure DevOps builds of project {Project} failed; leaving its checkpoints unchanged.", project);
            return new(triggeredEvents, current);
        }

        WarnWhenInFlightReadIsFull(notStarted, "not started", pollingOptions.Top, project);
        WarnWhenInFlightReadIsFull(inProgress, "in progress", pollingOptions.Top, project);

        // The completed query also feeds the queued and started candidates: a build that queues, starts and
        // finishes inside one interval has already left the in-flight query by the time it is read, and would
        // otherwise never report its queued and started events.
        List<Build> inFlight = [.. notStarted, .. inProgress];
        List<Build> queuedOrStartedCandidates = [.. inFlight, .. completed];

        foreach (BuildPollingEvent @event in BuildEventDeriver.Derive(queuedOrStartedCandidates, queuedOrStartedCandidates, completed, current))
        {
            try
            {
                // Reuse the webhook handler so polled events produce the exact same bookmark variants as Service Hook
                // events; sending a stimulus without a bookmark payload never matches an indexed trigger.
                AzureDevOpsWebhookEvent message = new(
                    @event.EventType,
                    @event.Build,
                    @event.Build.Project?.Id.ToString(),
                    @event.Build.Project?.Name ?? project);
                await eventHandler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
                triggeredEvents.Add(new AzureDevOpsPollingEvent(
                    @event.EventType,
                    project,
                    @event.Build.Id.ToString(CultureInfo.InvariantCulture),
                    $"build {@event.Build.BuildNumber ?? @event.Build.Id.ToString(CultureInfo.InvariantCulture)}"));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Stop before advancing: the failed event must be retried on the next poll, and everything after it
                // is newer, so leaving the checkpoint here loses nothing.
                logger.LogWarning(ex, "Dispatching Azure DevOps {EventType} for build {BuildId} failed; retrying on the next poll.", @event.EventType, @event.Build.Id);
                break;
            }

            BuildEventDeriver.Advance(current, @event);
        }

        return new(triggeredEvents, current);
    }

    // The in-flight reads carry no time filter and ask for the oldest builds first, so once a project holds more than
    // Top concurrent in-flight builds the same oldest page comes back every poll while newer in-flight builds stay
    // invisible until they complete. They cannot be paged past: the GetBuildsAsync overload used here exposes no skip
    // and discards the continuation token, and GetBuildsAsync2, which returns it, takes the project as a Guid where
    // the configured project may just as well be a name. Making the condition visible in the log is what is left.
    private void WarnWhenInFlightReadIsFull(List<Build> builds, string status, int top, string project)
    {
        if (top <= 0 || builds.Count < top)
            return;

        logger.LogWarning(
            "The {Status} build read of Azure DevOps project {Project} returned its full page of {Top}; builds beyond it are not seen while they are in flight. Raise Builds:Top above the number of concurrent builds.",
            status,
            project,
            top);
    }

    private static BuildPollingCheckpoints Seed(TimeSpan lookbackWindow)
    {
        DateTimeOffset horizon = DateTimeOffset.UtcNow.Subtract(lookbackWindow);
        return new BuildPollingCheckpoints { LastQueued = horizon, LastStarted = horizon, LastFinished = horizon };
    }
}
