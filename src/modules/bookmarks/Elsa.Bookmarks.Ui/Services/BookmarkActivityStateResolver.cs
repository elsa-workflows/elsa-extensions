using Elsa.Bookmarks.Ui.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;

namespace Elsa.Bookmarks.Ui.Services;

/// <summary>
/// Finds the state of the activity a bookmark is waiting in, and builds the context a provider reads.
/// </summary>
/// <remarks>
/// Two matches, in order, because one is not enough. An activity execution context is normally found by the
/// bookmark's <c>ActivityInstanceId</c>. A bookmark created with <c>includeActivityInstanceId: false</c> - which is
/// how <c>CreateTaskBookmark</c>, the activity behind every UIInteraction, creates its own - carries no instance id
/// at all, and is found only by matching its activity node id against the context's scheduled node id.
/// </remarks>
public static class BookmarkActivityStateResolver
{
    /// <summary>The waiting activity's evaluated inputs, or null when no context matches.</summary>
    public static IDictionary<string, object>? Resolve(WorkflowState? state, Bookmark bookmark)
    {
        ArgumentNullException.ThrowIfNull(bookmark);

        if (state == null)
            return null;

        foreach (ActivityExecutionContextState context in state.ActivityExecutionContexts)
        {
            if (bookmark.ActivityInstanceId != null && string.Equals(context.Id, bookmark.ActivityInstanceId, StringComparison.Ordinal))
                return context.ActivityState;
        }

        foreach (ActivityExecutionContextState context in state.ActivityExecutionContexts)
        {
            if (string.Equals(context.ScheduledActivityNodeId, bookmark.ActivityNodeId, StringComparison.Ordinal))
                return context.ActivityState;
        }

        return null;
    }

    /// <summary>The context a provider is given for one bookmark of one instance.</summary>
    public static BookmarkUiContext CreateContext(WorkflowState state, string definitionId, Bookmark bookmark)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(bookmark);

        BookmarkDescriptor descriptor = new(
            bookmark.Id,
            bookmark.Name,
            bookmark.ActivityId,
            bookmark.ActivityNodeId,
            bookmark.ActivityInstanceId,
            bookmark.CreatedAt,
            bookmark.Payload,
            Resolve(state, bookmark));

        IReadOnlyDictionary<string, object?> properties = state.Properties.ToDictionary(
            entry => entry.Key,
            entry => (object?)entry.Value,
            StringComparer.OrdinalIgnoreCase);

        return new BookmarkUiContext(state.Id, definitionId, descriptor, properties);
    }
}
