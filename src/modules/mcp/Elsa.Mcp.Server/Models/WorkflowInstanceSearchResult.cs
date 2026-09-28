namespace Elsa.Mcp.Server.Models;

/// <summary>
/// The result of a workflow instance search performed through MCP.
/// </summary>
/// <param name="TotalCount">The number of instances matching the search, ignoring paging.</param>
/// <param name="Items">The matching instances of the requested page.</param>
public sealed record WorkflowInstanceSearchResult(long TotalCount, IReadOnlyList<WorkflowInstanceDescription> Items);

/// <summary>
/// A workflow instance as described to an MCP caller.
/// </summary>
public sealed record WorkflowInstanceDescription(
    string Id,
    string DefinitionId,
    int Version,
    string? Name,
    string Status,
    string SubStatus,
    string? CorrelationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FinishedAt,
    int IncidentCount);
