using Elsa.Common.Models;
using Elsa.Workflows;
using Elsa.Workflows.Management.Models;

namespace Elsa.Mcp.Server.Services;

/// <summary>
/// The criteria the caller-task tools filter on.
/// </summary>
/// <param name="SearchTerm">A free search term, or <c>null</c> for no term.</param>
/// <param name="Status">The workflow status to filter on, or <c>null</c> for any.</param>
/// <param name="SubStatuses">The workflow sub statuses to filter on, or <c>null</c> for any. A set rather than a single
/// value because the useful filters are sets: "still open" is pending, suspended and executing, and "done" is finished,
/// faulted and cancelled. An empty set means the same as <c>null</c> - see <see cref="SubStatusFilter"/>.</param>
/// <param name="IncludeSystem">Whether the instances of system workflows count as tasks. <c>false</c> by default: a
/// system workflow is the host's own housekeeping rather than work someone has to do, so a caller that wants to see it
/// has to ask.</param>
/// <param name="DefinitionIds">The workflow definitions to filter on, or <c>null</c> for any. Last and optional so the
/// existing positional callers keep compiling. A set for the same reason <paramref name="SubStatuses"/> is one: the
/// task list's definition picker is single-select today, but the sibling system-workflow list already filters on
/// several and the wire shape should not have to change when this one follows. An empty set means the same as
/// <c>null</c> - see <see cref="DefinitionFilter"/>.</param>
public sealed record CallerTaskCriteria(
    string? SearchTerm,
    WorkflowStatus? Status,
    ICollection<WorkflowSubStatus>? SubStatuses,
    bool IncludeSystem = false,
    ICollection<string>? DefinitionIds = null)
{
    /// <summary>
    /// The sub statuses as a filter value: the set itself, or <c>null</c> when it is empty.
    /// </summary>
    /// <remarks>
    /// Elsa's <c>WorkflowInstanceFilter.Apply</c> tests <c>WorkflowSubStatuses != null</c>, not its count, so handing it
    /// an empty collection filters every instance away instead of filtering on nothing. Every caller that copies this
    /// onto a filter goes through here so that mistake can only be made once.
    /// </remarks>
    public ICollection<WorkflowSubStatus>? SubStatusFilter => SubStatuses is { Count: > 0 } ? SubStatuses : null;

    /// <summary>
    /// The <c>IsSystem</c> value to filter on: <c>false</c> to leave the system instances out, or <c>null</c> to filter
    /// on nothing once they have been asked for.
    /// </summary>
    /// <remarks>
    /// <c>null</c> rather than <c>true</c> on the including side: asking for the system instances widens the list to
    /// everything, it does not narrow it to the system ones. Every caller that copies this onto a filter goes through
    /// here, the same reason <see cref="SubStatusFilter"/> exists.
    /// </remarks>
    public bool? IsSystemFilter => IncludeSystem ? null : false;

    /// <summary>
    /// The definitions as a filter value: the set itself, or <c>null</c> when it is empty.
    /// </summary>
    /// <remarks>
    /// The same trap as <see cref="SubStatusFilter"/>, and worth spelling out because it inverts the intent rather than
    /// merely widening it: <c>Apply</c> tests <c>DefinitionIds != null</c>, so an empty collection compiles to
    /// <c>[].Contains(x.DefinitionId)</c> and filters every instance away. A cleared definition picker means "any
    /// definition", so it has to arrive here as <c>null</c>. Every caller that copies this onto a filter goes through
    /// this property so the mistake can only be made once.
    /// </remarks>
    public ICollection<string>? DefinitionFilter => DefinitionIds is { Count: > 0 } ? DefinitionIds : null;
}

/// <summary>
/// Finds the workflow instances that count as the calling user's tasks.
/// </summary>
/// <remarks>
/// This is the seam that lets a host narrow what its callers see. It takes criteria rather than a
/// <c>WorkflowInstanceFilter</c> on purpose: a host that scopes by owner has to build a filter type of its own, and
/// copying an incoming filter field by field would silently drop whatever field Elsa adds next. Criteria are the three
/// things the tools actually offer, so there is nothing to lose track of.
///
/// The default implementation, <see cref="UnscopedCallerTaskSearch"/>, scopes by the criteria alone — right for a
/// generic package, wrong for a host that knows who is calling. Such a host replaces this service.
/// </remarks>
public interface ICallerTaskSearch
{
    /// <summary>
    /// Returns the page of instances matching <paramref name="criteria"/> that the caller may see.
    /// </summary>
    /// <param name="criteria">Which tasks the answer holds.</param>
    /// <param name="pageArgs">Which page of them.</param>
    /// <param name="order">
    /// The order to read them in, or <c>null</c> to leave it to the store. Optional because it is skip-based paging's
    /// only real dependency: a caller that pages has to name an order or its page boundaries mean nothing, while a
    /// caller that reads one page of the caller's open work does not care. Both instance stores fall back to oldest
    /// created first, so <c>null</c> is a stable order rather than an arbitrary one - just not one this seam promises.
    /// </param>
    /// <param name="cancellationToken">Cancels the search.</param>
    Task<Page<WorkflowInstanceSummary>> FindAsync(
        CallerTaskCriteria criteria,
        PageArgs pageArgs,
        CallerTaskOrder? order = null,
        CancellationToken cancellationToken = default);
}
