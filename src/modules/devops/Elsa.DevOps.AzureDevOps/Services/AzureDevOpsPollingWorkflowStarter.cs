using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.RuntimeWorkflows;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Ensures the singleton polling workflow of every enabled family is started when the host starts.
/// </summary>
public sealed class AzureDevOpsPollingWorkflowStarter(
    IServiceScopeFactory scopeFactory,
    ILogger<AzureDevOpsPollingWorkflowStarter> logger) : IHostedService
{
    /// <summary>
    /// Returns the definition IDs of the polling workflows that must run. Each workflow is a singleton whose
    /// instance carries the definition ID as its instance ID, which keeps it recognisable in Studio, in the logs and
    /// in the database.
    /// </summary>
    /// <remarks>
    /// This is the only place a family's own switch is read, and it only decides what to start here: an instance that
    /// is already running is left alone and keeps polling on its timer, so switching a family off means stopping its
    /// instance as well. Only <see cref="AzureDevOpsPollingOptions.Enabled"/> stops a running poll.
    /// </remarks>
    public static IReadOnlyList<string> GetEnabledDefinitionIds(AzureDevOpsPollingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
            return [];

        List<string> definitionIds = [];

        if (options.WorkItems.Enabled)
            definitionIds.Add(AzureDevOpsPollingWorkflowNames.WorkItems);

        if (options.Builds.Enabled)
            definitionIds.Add(AzureDevOpsPollingWorkflowNames.Builds);

        if (options.PullRequests.Enabled)
            definitionIds.Add(AzureDevOpsPollingWorkflowNames.PullRequests);

        if (options.Pushes.Enabled)
            definitionIds.Add(AzureDevOpsPollingWorkflowNames.Pushes);

        return definitionIds;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        AzureDevOpsPollingOptions pollingOptions = scope.ServiceProvider
            .GetRequiredService<IOptions<AzureDevOpsPollingOptions>>()
            .Value;

        foreach (string definitionId in GetEnabledDefinitionIds(pollingOptions))
            await StartWorkflowAsync(scope.ServiceProvider, definitionId, cancellationToken).ConfigureAwait(false);
    }

    private async Task StartWorkflowAsync(IServiceProvider services, string definitionId, CancellationToken cancellationToken)
    {
        try
        {
            IWorkflowRuntime workflowRuntime = services.GetRequiredService<IWorkflowRuntime>();

            if (!await ClearWayForSingletonAsync(services, definitionId, cancellationToken).ConfigureAwait(false))
            {
                logger.LogInformation(
                    "Azure DevOps polling workflow instance {InstanceId} is already running; leaving it in place.",
                    definitionId);
                return;
            }

            // This argument is the workflow instance ID, not the definition ID; the definition travels in the request
            // below. Pinning it is only safe because the step above guarantees the instance is about to be created:
            // when creation is skipped, the runtime runs the pinned ID and fails if no such instance exists.
            IWorkflowClient workflowClient = await workflowRuntime
                .CreateClientAsync(definitionId, cancellationToken)
                .ConfigureAwait(false);
            await workflowClient.CreateAndRunInstanceAsync(
                new CreateAndRunWorkflowInstanceRequest
                {
                    WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(definitionId)
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // One family that will not start must not keep the others from starting.
            logger.LogError(ex, "Failed to start Azure DevOps polling workflow {DefinitionId} at host startup.", definitionId);
        }
    }

    /// <summary>
    /// Clears out every instance of the polling workflow that stands in the way of the singleton instance, and
    /// reports whether that instance still has to be created.
    /// </summary>
    /// <remarks>
    /// Two kinds of instance are removed. Instances carrying another ID hold the singleton slot hostage: the
    /// activation strategy then refuses to create the singleton while the runtime still tries to run it, which fails
    /// because it does not exist. Finished instances — completed, faulted or cancelled — will never poll again and
    /// only keep the ID occupied.
    /// </remarks>
    /// <returns><c>true</c> when the singleton instance must be created, <c>false</c> when it is already running.</returns>
    private async Task<bool> ClearWayForSingletonAsync(IServiceProvider services, string definitionId, CancellationToken cancellationToken)
    {
        IWorkflowInstanceStore instanceStore = services.GetRequiredService<IWorkflowInstanceStore>();
        WorkflowInstanceSummary[] instances =
        [
            .. await instanceStore
                .SummarizeManyAsync(new WorkflowInstanceFilter { DefinitionId = definitionId }, cancellationToken)
                .ConfigureAwait(false)
        ];
        List<WorkflowInstanceSummary> stale =
        [
            .. instances.Where(instance => instance.Id != definitionId || instance.Status == WorkflowStatus.Finished)
        ];

        foreach (WorkflowInstanceSummary instance in stale)
        {
            // Logged before deleting, because deleting takes the execution log with it.
            logger.LogWarning(
                "Deleting {SubStatus} Azure DevOps polling workflow instance {InstanceId} with {IncidentCount} incident(s), created at {CreatedAt}, to make way for singleton instance {SingletonInstanceId}.",
                instance.SubStatus,
                instance.Id,
                instance.IncidentCount,
                instance.CreatedAt,
                definitionId);
        }

        WorkflowInstanceSummary? singleton = instances
            .FirstOrDefault(instance => instance.Id == definitionId && instance.Status != WorkflowStatus.Finished);
        bool singletonIsUnresumable = singleton != null
            && await IsUnresumableAsync(instanceStore, definitionId, cancellationToken).ConfigureAwait(false);

        if (singletonIsUnresumable)
            stale.Add(singleton!);

        if (stale.Count > 0)
        {
            IWorkflowInstanceManager instanceManager = services.GetRequiredService<IWorkflowInstanceManager>();
            WorkflowInstanceFilter filter = new() { Ids = [.. stale.Select(instance => instance.Id)] };
            await instanceManager.BulkDeleteAsync(filter, cancellationToken).ConfigureAwait(false);
        }

        return singletonIsUnresumable
            || !instances.Any(instance => instance.Id == definitionId && instance.Status == WorkflowStatus.Running);
    }

    /// <summary>
    /// Reads the singleton instance in full and reports whether it can still be woken.
    /// </summary>
    /// <remarks>
    /// Only reached for an instance the summary calls alive, so this is one extra read per family per host start.
    /// </remarks>
    private async Task<bool> IsUnresumableAsync(
        IWorkflowInstanceStore instanceStore,
        string definitionId,
        CancellationToken cancellationToken)
    {
        WorkflowInstance? instance = await instanceStore
            .FindAsync(new WorkflowInstanceFilter { Id = definitionId }, cancellationToken)
            .ConfigureAwait(false);

        if (instance == null || !IsUnresumable(instance.WorkflowState))
            return false;

        logger.LogWarning(
            "Deleting Azure DevOps polling workflow instance {InstanceId}: it is {SubStatus} and holds bookmark(s) {BookmarkIds} whose activity execution context is no longer in its state, so nothing can ever resume it.",
            definitionId,
            instance.WorkflowState.SubStatus,
            string.Join(", ", OrphanedBookmarks(instance.WorkflowState).Select(bookmark => bookmark.Id)));

        return true;
    }

    /// <summary>
    /// Reports whether an instance can never run again because a bookmark of its outlived the activity execution
    /// context it belongs to.
    /// </summary>
    /// <remarks>
    /// The shape this catches was found on 2026-08-31: a host recycle while the pull request and push pollers were
    /// suspended dropped their flowchart and timer contexts from the persisted state. Elsa then logs "Could not find
    /// activity execution context ... for bookmark ..." on every resume and does nothing further - no incident, no
    /// new bookmark, and a status that still reads <c>Running/Suspended</c>. Only the orphaned bookmark distinguishes
    /// such an instance from one that is simply waiting for its next tick, which is why nothing here reasons about
    /// how long ago a timer was due: a long outage leaves a healthy instance overdue too.
    /// </remarks>
    public static bool IsUnresumable(WorkflowState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        // A finished instance is already removed by the rule above, and one holding no bookmarks at all may be
        // executing right now on another host of the same app service.
        return state.Status != WorkflowStatus.Finished && OrphanedBookmarks(state).Any();
    }

    private static IEnumerable<Bookmark> OrphanedBookmarks(WorkflowState state)
    {
        HashSet<string> contextIds = [.. state.ActivityExecutionContexts.Select(context => context.Id)];

        return state.Bookmarks.Where(bookmark =>
            !string.IsNullOrWhiteSpace(bookmark.ActivityInstanceId)
            && !contextIds.Contains(bookmark.ActivityInstanceId));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
