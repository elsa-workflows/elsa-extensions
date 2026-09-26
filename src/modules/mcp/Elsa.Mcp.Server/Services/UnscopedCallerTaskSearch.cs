using Elsa.Common.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Mcp.Server.Services;

/// <summary>
/// Finds instances by criteria alone, without narrowing to the caller.
/// </summary>
/// <remarks>
/// The default for a host that has no owner concept. It is deliberately not a no-op or a throw: a generic Elsa host
/// with the instance tools enabled should keep working, and its callers see whatever the host's authentication and
/// tenancy already allow.
/// </remarks>
public sealed class UnscopedCallerTaskSearch(IWorkflowInstanceStore instanceStore) : ICallerTaskSearch
{
    /// <inheritdoc />
    public async Task<Page<WorkflowInstanceSummary>> FindAsync(
        CallerTaskCriteria criteria,
        PageArgs pageArgs,
        CallerTaskOrder? order = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        WorkflowInstanceFilter filter = new()
        {
            SearchTerm = criteria.SearchTerm,
            WorkflowStatus = criteria.Status,
            WorkflowSubStatuses = criteria.SubStatusFilter,
            IsSystem = criteria.IsSystemFilter,
            DefinitionIds = criteria.DefinitionFilter
        };

        // Without an order, the store's own overload rather than this store's default spelled out here. The two say the
        // same thing today - both order by CreatedAt ascending - and letting the store answer for itself keeps that a
        // fact about the store rather than a copy of it that can go stale.
        if (order is null)
        {
            return await instanceStore.SummarizeManyAsync(filter, pageArgs, cancellationToken).ConfigureAwait(false);
        }

        return await CallerTaskOrdering
            .SummarizeAsync(order, new StoreSummarizer(instanceStore, filter, pageArgs), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Hands this store's ordered summarize call to <see cref="CallerTaskOrdering"/>.
    /// </summary>
    /// <remarks>
    /// A type rather than a lambda because the call it makes is generic in the order's key type; see
    /// <see cref="ICallerTaskSummarizer"/> for why that mapping is worth centralising.
    /// </remarks>
    private sealed class StoreSummarizer(IWorkflowInstanceStore instanceStore, WorkflowInstanceFilter filter, PageArgs pageArgs) : ICallerTaskSummarizer
    {
        /// <inheritdoc />
        public ValueTask<Page<WorkflowInstanceSummary>> SummarizeAsync<TOrderBy>(WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken) =>
            instanceStore.SummarizeManyAsync(filter, pageArgs, order, cancellationToken);
    }
}
