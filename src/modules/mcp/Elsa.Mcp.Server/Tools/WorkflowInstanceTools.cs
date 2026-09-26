using System.ComponentModel;
using Elsa.Common.Models;
using Elsa.Mcp.Server.Models;
using Elsa.Mcp.Server.Services;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using ModelContextProtocol.Server;

namespace Elsa.Mcp.Server.Tools;

/// <summary>
/// Tools that let an MCP caller find workflow instances, so that it can resume them.
/// </summary>
/// <remarks>
/// The <c>get_my_*</c> tools go through <see cref="ICallerTaskSearch"/>, so a host that knows who is calling narrows
/// them to that caller; a host that does not keeps seeing everything its authentication and tenancy allow.
/// <c>search_workflow_instances</c> is scoped by its filter alone, by design — it exists to look instances up,
/// including someone else's. Leave that one out when instances of other callers must stay invisible.
/// </remarks>
[McpServerToolType]
public sealed class WorkflowInstanceTools
{
    private const int DefaultMaxResults = 50;
    private const int MaxResultsLimit = 100;

    private WorkflowInstanceTools() { }

    [McpServerTool(Name = "search_workflow_instances")]
    [Description("Searches workflow instances, for example to find a suspended instance and the bookmark to resume it with.")]
    public static async Task<WorkflowInstanceSearchResult> SearchAsync(
        [Description("Workflow definition ids to filter on.")] string[]? definitionIds,
        [Description("Exact workflow instance name to filter on.")] string? name,
        [Description("Correlation id to filter on.")] string? correlationId,
        [Description("Free search term to filter on.")] string? searchTerm,
        [Description("Workflow status to filter on: Running or Finished.")] WorkflowStatus? status,
        [Description("Workflow sub status to filter on: Pending, Executing, Suspended, Finished, Cancelled or Faulted.")] WorkflowSubStatus? subStatus,
        [Description("Number of results to skip.")] int skip,
        [Description("Maximum number of results to return.")] int maxResults,
        IWorkflowInstanceStore instanceStore,
        CancellationToken cancellationToken
    )
    {
        WorkflowInstanceFilter filter = new()
        {
            Name = name,
            CorrelationId = correlationId,
            SearchTerm = searchTerm,
            WorkflowStatus = status,
            WorkflowSubStatus = subStatus
        };

        if (definitionIds is { Length: > 0 })
            filter.DefinitionIds = [.. definitionIds.Where(definitionId => !string.IsNullOrWhiteSpace(definitionId))];

        return await FindAsync(filter, skip, maxResults, instanceStore, cancellationToken).ConfigureAwait(false);
    }

    [McpServerTool(Name = "get_my_pending_tasks")]
    [Description(
        "Lists the tasks that are waiting for an answer: workflow instances that are running and suspended on an activity. "
        + "Call the workflow's own tool with the returned workflow instance id and bookmark id to answer one. "
        + "Results are narrowed to the calling user when the host can identify one."
    )]
    public static async Task<WorkflowInstanceSearchResult> GetPendingTasksAsync(
        [Description("Free search term to filter on.")] string? searchTerm,
        [Description("Number of results to skip.")] int skip,
        [Description("Maximum number of results to return.")] int maxResults,
        ICallerTaskSearch instanceSearch,
        CancellationToken cancellationToken
    )
    {
        return await FindCallerTasksAsync(
            new CallerTaskCriteria(searchTerm, WorkflowStatus.Running, [WorkflowSubStatus.Suspended]),
            skip,
            maxResults,
            instanceSearch,
            cancellationToken).ConfigureAwait(false);
    }

    [McpServerTool(Name = "get_my_running_tasks")]
    [Description(
        "Lists the tasks that have not finished yet: every running workflow instance, whether it is executing or suspended. "
        + "Results are narrowed to the calling user when the host can identify one."
    )]
    public static async Task<WorkflowInstanceSearchResult> GetRunningTasksAsync(
        [Description("Free search term to filter on.")] string? searchTerm,
        [Description("Number of results to skip.")] int skip,
        [Description("Maximum number of results to return.")] int maxResults,
        ICallerTaskSearch instanceSearch,
        CancellationToken cancellationToken
    )
    {
        // No sub status: a running instance is either executing or suspended, and both are still open work.
        return await FindCallerTasksAsync(
            new CallerTaskCriteria(searchTerm, WorkflowStatus.Running, null),
            skip,
            maxResults,
            instanceSearch,
            cancellationToken).ConfigureAwait(false);
    }

    [McpServerTool(Name = "get_my_completed_tasks")]
    [Description(
        "Lists the tasks that are done: every finished workflow instance, including the cancelled and faulted ones. "
        + "Read the sub status of a result to tell them apart. Results are narrowed to the calling user when the host can identify one."
    )]
    public static async Task<WorkflowInstanceSearchResult> GetCompletedTasksAsync(
        [Description("Free search term to filter on.")] string? searchTerm,
        [Description("Number of results to skip.")] int skip,
        [Description("Maximum number of results to return.")] int maxResults,
        ICallerTaskSearch instanceSearch,
        CancellationToken cancellationToken
    )
    {
        return await FindCallerTasksAsync(
            new CallerTaskCriteria(searchTerm, WorkflowStatus.Finished, null),
            skip,
            maxResults,
            instanceSearch,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<WorkflowInstanceSearchResult> FindAsync(
        WorkflowInstanceFilter filter,
        int skip,
        int maxResults,
        IWorkflowInstanceStore instanceStore,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instanceStore);

        Page<WorkflowInstanceSummary> page = await instanceStore.SummarizeManyAsync(filter, ToPageArgs(skip, maxResults), cancellationToken).ConfigureAwait(false);

        return ToSearchResult(page);
    }

    /// <summary>
    /// Runs a caller-task lookup through <see cref="ICallerTaskSearch"/>, so that a host which scopes by owner narrows
    /// the result. Kept separate from <see cref="FindAsync"/>: the search tool takes an explicit filter and stays on
    /// the store, because narrowing a search by the caller would make it impossible to look something up on behalf of
    /// someone else — which is the whole point of that tool.
    /// </summary>
    private static async Task<WorkflowInstanceSearchResult> FindCallerTasksAsync(
        CallerTaskCriteria criteria,
        int skip,
        int maxResults,
        ICallerTaskSearch instanceSearch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instanceSearch);

        // No order: the tools offer no way to ask for one, and the store's default - oldest created first - is the
        // order these tools have always answered in.
        Page<WorkflowInstanceSummary> page = await instanceSearch
            .FindAsync(criteria, ToPageArgs(skip, maxResults), order: null, cancellationToken)
            .ConfigureAwait(false);

        return ToSearchResult(page);
    }

    /// <summary>
    /// Maps a page of instances to the tool result shape. Shared by <see cref="FindAsync"/> and
    /// <see cref="FindCallerTasksAsync"/> because the mapping itself has nothing to do with which store-access path
    /// produced the page - only the two callers' await targets differ, and those stay separate on purpose.
    /// </summary>
    private static WorkflowInstanceSearchResult ToSearchResult(Page<WorkflowInstanceSummary> page) =>
        new(page.TotalCount, [.. page.Items.Select(Describe)]);

    /// <summary>
    /// Turns the paging arguments of a tool call into page args. A tool caller pays for every result it reads, so an
    /// unbounded or absent page size is capped rather than honoured.
    /// </summary>
    private static PageArgs ToPageArgs(int skip, int maxResults)
    {
        int take = maxResults is > 0 and <= MaxResultsLimit ? maxResults : maxResults > MaxResultsLimit ? MaxResultsLimit : DefaultMaxResults;

        return new PageArgs
        {
            Offset = Math.Max(skip, 0),
            Limit = take
        };
    }

    private static WorkflowInstanceDescription Describe(WorkflowInstanceSummary summary) =>
        new(
            summary.Id,
            summary.DefinitionId,
            summary.Version,
            summary.Name,
            summary.Status.ToString(),
            summary.SubStatus.ToString(),
            summary.CorrelationId,
            summary.CreatedAt,
            summary.FinishedAt,
            summary.IncidentCount);
}
