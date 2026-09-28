using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Mcp.Server.Services;

/// <summary>
/// The column a page of caller tasks is ordered on.
/// </summary>
/// <remarks>
/// A closed set rather than the column name as text. The value ends up in an <c>ORDER BY</c> over the instance table,
/// and a caller-supplied name reaching a query is the one thing a seam like this must not allow; an enum makes the
/// answer to "which columns may be ordered on" reviewable in one place. Every member is a column the instance table
/// carries an index for, so ordering on it costs a seek rather than a sort of the whole table.
///
/// Deliberately smaller than the set of columns <see cref="WorkflowInstanceSummary"/> has. What is missing is what
/// cannot be ordered honestly: the workflow's <em>name</em> lives on the definition rather than on the instance, so
/// ordering a task list by the definition column it shows would order by the definition id behind it and read as if
/// the column had been sorted at random.
/// </remarks>
public enum CallerTaskOrderField
{
    /// <summary>When the instance was created.</summary>
    CreatedAt,

    /// <summary>
    /// When the instance was last written to, which is what a task list shows as the moment it last ran.
    /// </summary>
    UpdatedAt,

    /// <summary>When the instance finished, or nothing while it has not.</summary>
    FinishedAt,

    /// <summary>The instance name.</summary>
    Name,

    /// <summary>The workflow status.</summary>
    Status,

    /// <summary>The workflow sub status.</summary>
    SubStatus
}

/// <summary>
/// The order a page of caller tasks is read in.
/// </summary>
/// <param name="Field">The column to order on.</param>
/// <param name="Direction">Which way round, ascending unless said otherwise.</param>
/// <remarks>
/// Separate from <see cref="CallerTaskCriteria"/> rather than a field on it: the criteria say which tasks the answer
/// holds, and the order says nothing about that. Keeping them apart is what lets a caller that has no opinion about
/// the order leave it out entirely.
///
/// Ordering on a column whose values repeat - every column here but <see cref="CallerTaskOrderField.CreatedAt"/> in
/// practice - leaves the order of the rows that tie up to the database, and skip-based paging over such an order can
/// repeat or miss a row between two pages. Elsa's own instance order has the same property and no way to name a
/// tie-breaker, so this does not either; it is worth knowing about before reading a page boundary as gospel.
/// </remarks>
public sealed record CallerTaskOrder(CallerTaskOrderField Field, OrderDirection Direction = OrderDirection.Ascending);

/// <summary>
/// Reads a page of instance summaries in a given order.
/// </summary>
/// <remarks>
/// Exists because a generic method cannot travel as a delegate: <see cref="CallerTaskOrdering"/> knows the column
/// behind every <see cref="CallerTaskOrderField"/> and has to hand a <see cref="WorkflowInstanceOrder{TProp}"/> of the
/// matching type to a store it knows nothing about. An implementation is a few lines around one store call - see
/// <see cref="UnscopedCallerTaskSearch"/> - and in return the field-to-column mapping lives in one place instead of
/// being repeated by every host that scopes the search to its callers.
/// </remarks>
public interface ICallerTaskSummarizer
{
    /// <summary>Summarizes the instances this summarizer was built for, ordered by <paramref name="order"/>.</summary>
    ValueTask<Page<WorkflowInstanceSummary>> SummarizeAsync<TOrderBy>(WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken);
}

/// <summary>
/// Turns a <see cref="CallerTaskOrder"/> into the typed order an instance store takes.
/// </summary>
public static class CallerTaskOrdering
{
    /// <summary>
    /// Reads the page <paramref name="summarizer"/> stands for in the order <paramref name="order"/> asks for.
    /// </summary>
    /// <remarks>
    /// The switch is the whole point of this class: it is the single place that decides which instance column each
    /// order field means. An unrecognised field - one added to the enum without a case here - orders on
    /// <see cref="CallerTaskOrderField.CreatedAt"/>, the same column both instance stores fall back to when no order
    /// is given at all, so a forgotten case degrades to the default rather than throwing on a read path.
    /// </remarks>
    public static ValueTask<Page<WorkflowInstanceSummary>> SummarizeAsync(
        CallerTaskOrder order,
        ICallerTaskSummarizer summarizer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(summarizer);

        OrderDirection direction = order.Direction;

        return order.Field switch
        {
            CallerTaskOrderField.UpdatedAt => summarizer.SummarizeAsync(new WorkflowInstanceOrder<DateTimeOffset>(instance => instance.UpdatedAt, direction), cancellationToken),
            CallerTaskOrderField.FinishedAt => summarizer.SummarizeAsync(new WorkflowInstanceOrder<DateTimeOffset?>(instance => instance.FinishedAt, direction), cancellationToken),
            CallerTaskOrderField.Name => summarizer.SummarizeAsync(new WorkflowInstanceOrder<string?>(instance => instance.Name, direction), cancellationToken),
            // Ordered on the stored value, and Elsa's persistence stores both statuses as their name rather than as a
            // number. So this reads alphabetically by the word the list shows, not by the position of the value in the
            // enum - which is the more useful of the two, since nothing about that position is visible on screen.
            CallerTaskOrderField.Status => summarizer.SummarizeAsync(new WorkflowInstanceOrder<WorkflowStatus>(instance => instance.Status, direction), cancellationToken),
            CallerTaskOrderField.SubStatus => summarizer.SummarizeAsync(new WorkflowInstanceOrder<WorkflowSubStatus>(instance => instance.SubStatus, direction), cancellationToken),
            _ => summarizer.SummarizeAsync(new WorkflowInstanceOrder<DateTimeOffset>(instance => instance.CreatedAt, direction), cancellationToken)
        };
    }
}
