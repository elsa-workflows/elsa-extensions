using System.Globalization;
using Elsa.DevOps.AzureDevOps.RuntimeWorkflows;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Starts the watcher workflow of a newly polled pull request. Starting it here rather than from a trigger keeps the
/// shared event handler out of it: the watcher exists only to compensate for polling, so a pull request announced
/// over a Service Hook — which delivers real update events — needs none.
/// </summary>
public class AzureDevOpsPullRequestWatchStarter(
    IWorkflowRuntime workflowRuntime,
    IWorkflowInstanceStore instanceStore,
    IWorkflowInstanceManager instanceManager,
    AzureDevOpsOrganizationUrlResolver organizationUrlResolver,
    ILogger<AzureDevOpsPullRequestWatchStarter> logger)
{
    private const string InstanceIdPrefix = "AzureDevOpsPullRequestWatcher:";

    /// <summary>
    /// The instance ID of the watcher of a pull request. Azure DevOps allocates pull request IDs organization-wide, so
    /// the number alone identifies a pull request within one organization; <paramref name="organizationSegment"/> is
    /// what keeps two organizations apart, and is <c>null</c> for the host's own.
    /// </summary>
    /// <remarks>
    /// The host's own organization keeps the ID this method has always produced. Watchers created before the
    /// organization entered the key are running right now, pinned to that exact ID: giving them a new one would leave
    /// the old instances orphaned - still timer-driven, still reporting updates - beside a fresh watcher reporting the
    /// same ones a second time.
    /// </remarks>
    public static string GetInstanceId(int pullRequestId, string? organizationSegment) =>
        InstanceIdPrefix + GetCorrelationId(pullRequestId, organizationSegment);

    /// <summary>
    /// The correlation ID of the watcher of a pull request. The watcher workflow is a correlated singleton, so this
    /// carries the organization for the same reason the instance ID does: without it the activation strategy refuses
    /// the second organization's watcher.
    /// </summary>
    public static string GetCorrelationId(int pullRequestId, string? organizationSegment)
    {
        string number = pullRequestId.ToString(CultureInfo.InvariantCulture);

        return string.IsNullOrEmpty(organizationSegment) ? number : $"{organizationSegment}:{number}";
    }

    /// <summary>
    /// Whether a poll that ran under the given connection can have its pull requests watched.
    /// </summary>
    /// <remarks>
    /// A watcher re-reads its pull request long after the poll that started it, under a credential it resolves itself,
    /// so it needs one it can name: a literal token would have to be stored on its instance, which is no place for a
    /// personal access token. Without a secret name a watcher falls back to the configured polling credential - right
    /// for the host's own organization, wrong for any other, and wrong every interval for as long as the pull request
    /// stays open. Reporting created and merged without watching updates is the better half of that trade.
    /// </remarks>
    public bool CanWatch(string? organizationUrl, string? tokenSecretName) =>
        organizationUrlResolver.IsDefault(organizationUrl) || !string.IsNullOrWhiteSpace(tokenSecretName);

    /// <summary>
    /// Starts the watcher of a newly polled pull request.
    /// </summary>
    /// <param name="organizationUrl">The organization the poll read the pull request from.</param>
    /// <param name="tokenSecretName">
    /// The Elsa Secret the watcher authenticates with, or <c>null</c> to leave it to the configured polling
    /// credential. The secret's name travels rather than the token itself: workflow input is stored on the instance
    /// and shown in Studio, which is no place for a personal access token.
    /// </param>
    public async Task StartAsync(
        GitPullRequest pullRequest,
        string project,
        string organizationUrl,
        string? tokenSecretName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pullRequest);

        string? organizationSegment = organizationUrlResolver.IsDefault(organizationUrl)
            ? null
            : AzureDevOpsOrganizationUrl.ToKeySegment(organizationUrl);
        string instanceId = GetInstanceId(pullRequest.PullRequestId, organizationSegment);

        try
        {
            WorkflowInstanceSummary[] existing =
            [
                .. await instanceStore
                    .SummarizeManyAsync(new WorkflowInstanceFilter { Id = instanceId }, cancellationToken)
                    .ConfigureAwait(false)
            ];

            if (existing.Any(instance => instance.Status == WorkflowStatus.Running))
            {
                // A checkpoint reset re-reports pull requests that are already watched; adopting the running watcher
                // keeps one instance per pull request.
                logger.LogDebug("Azure DevOps pull request {PullRequestId} is already being watched.", pullRequest.PullRequestId);
                return;
            }

            foreach (WorkflowInstanceSummary instance in existing)
            {
                // A finished watcher will never watch again but still occupies the pinned instance ID, so creating
                // over it collides and the watcher silently never starts. Logged before deleting, because deleting
                // takes the execution log with it.
                logger.LogInformation(
                    "Deleting {SubStatus} watcher instance {InstanceId} of Azure DevOps pull request {PullRequestId}, created at {CreatedAt}, to make way for a new watcher.",
                    instance.SubStatus,
                    instance.Id,
                    pullRequest.PullRequestId,
                    instance.CreatedAt);
            }

            if (existing.Length > 0)
            {
                await instanceManager
                    .BulkDeleteAsync(new WorkflowInstanceFilter { Id = instanceId }, cancellationToken)
                    .ConfigureAwait(false);
            }

            IWorkflowClient workflowClient = await workflowRuntime.CreateClientAsync(instanceId, cancellationToken).ConfigureAwait(false);
            await workflowClient.CreateAndRunInstanceAsync(
                new CreateAndRunWorkflowInstanceRequest
                {
                    WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(AzureDevOpsPollingWorkflowNames.PullRequestWatcher),
                    CorrelationId = GetCorrelationId(pullRequest.PullRequestId, organizationSegment),
                    Input = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        // The ID rather than the pull request itself: it is all the watcher reads, and a whole
                        // GitPullRequest would have to survive serialisation into the instance's input for nothing.
                        ["PullRequestId"] = pullRequest.PullRequestId,
                        ["Project"] = project,
                        ["RepositoryId"] = pullRequest.Repository?.Id.ToString() ?? string.Empty,
                        // The connection the poll ran under, so the watcher re-reads the pull request where it was
                        // found. Both are empty for a poll that used the configured connection, which is what a
                        // watcher started before these inputs existed also finds when it resumes.
                        ["OrganizationUrl"] = organizationUrl,
                        ["TokenSecretName"] = tokenSecretName ?? string.Empty,
                    },
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A watcher that will not start must not fail the poll that found the pull request: the creation event
            // itself was already dispatched.
            logger.LogWarning(ex, "Failed to start the watcher for Azure DevOps pull request {PullRequestId}.", pullRequest.PullRequestId);
        }
    }
}
