using Elsa.Bookmarks.Ui.Models;

namespace Elsa.Mcp.Server.Models;

/// <summary>
/// A bookmark that is still open on a workflow instance, described for an MCP caller. Elsa's own bookmark is not
/// returned as-is: it carries types that do not survive serialization, and the caller only needs what it takes to
/// resume and to see what the workflow is waiting for.
/// </summary>
/// <param name="Id">The bookmark id to resume the instance with.</param>
/// <param name="Name">The name of the bookmark, usually the stimulus the activity waits for.</param>
/// <param name="ActivityId">The id of the waiting activity within the workflow definition.</param>
/// <param name="ActivityNodeId">The node id of the waiting activity.</param>
/// <param name="ActivityInstanceId">The id of the waiting activity's execution context.</param>
/// <param name="CreatedAt">When the bookmark was created.</param>
/// <param name="Payload">The data the activity stored on the bookmark.</param>
/// <param name="ActivityState">The evaluated properties of the waiting activity, taken from its execution context.</param>
/// <param name="Ui">How this bookmark should be shown and answered, as its provider described it.</param>
public sealed record WorkflowBookmarkDescription(
    string Id,
    string Name,
    string ActivityId,
    string ActivityNodeId,
    string? ActivityInstanceId,
    DateTimeOffset CreatedAt,
    object? Payload,
    IDictionary<string, object>? ActivityState,
    BookmarkUiView? Ui = null);

/// <summary>
/// Something that went wrong while the workflow ran, described for an MCP caller.
/// </summary>
/// <param name="ActivityId">The id of the activity that faulted.</param>
/// <param name="ActivityNodeId">The node id of the activity that faulted.</param>
/// <param name="ActivityType">The type of the activity that faulted.</param>
/// <param name="Message">What went wrong.</param>
/// <param name="Timestamp">When it went wrong.</param>
/// <param name="Exception">The exception behind the incident, if the activity recorded one.</param>
public sealed record WorkflowIncidentDescription(
    string ActivityId,
    string ActivityNodeId,
    string ActivityType,
    string Message,
    DateTimeOffset Timestamp,
    WorkflowExceptionDescription? Exception);

/// <summary>
/// An exception behind an incident. The stack trace is deliberately left out: it maps the server's internals for a
/// caller that has no business seeing them.
/// </summary>
/// <param name="Type">The full name of the exception type.</param>
/// <param name="Message">The exception message.</param>
/// <param name="InnerException">The exception this one wraps, if any.</param>
public sealed record WorkflowExceptionDescription(
    string Type,
    string Message,
    WorkflowExceptionDescription? InnerException);
