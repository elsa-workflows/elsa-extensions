using Elsa.Bookmarks.Ui.Models;
using Elsa.Bookmarks.Ui.Services;
using Elsa.Mcp.Server.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;

namespace Elsa.Mcp.Server.Services;

/// <summary>
/// Describes the bookmarks of a run to an MCP caller, with the view its provider produced.
/// </summary>
/// <remarks>
/// Separate from <see cref="WorkflowRunResultMapper"/>, which is static and stays that way: describing a bookmark now
/// needs an injected mapper and an await, and the rest of that mapper needs neither.
/// </remarks>
public sealed class BookmarkDescriptionFactory(IBookmarkUiMapper mapper)
{
    /// <summary>
    /// Describes each bookmark, dropping both the raw payload and the activity state of the ones a provider
    /// recognised. Those two are the unbounded fields in the description and the view already says what they held; a
    /// bookmark only the fallback described keeps both, so nothing is lost where nothing was gained.
    /// </summary>
    /// <remarks>
    /// A bookmark the mapper deliberately describes as nothing - a trigger, waiting for another system - is reported
    /// with a null <c>ui</c> and keeps its payload for the same reason: the caller still needs to see that the
    /// instance is suspended there, and the payload is all that is left to say what on.
    /// </remarks>
    public async ValueTask<IReadOnlyList<WorkflowBookmarkDescription>> DescribeAsync(
        WorkflowState? state,
        ICollection<Bookmark> bookmarks,
        string definitionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bookmarks);

        if (bookmarks.Count == 0)
            return [];

        List<WorkflowBookmarkDescription> descriptions = new(bookmarks.Count);

        foreach (Bookmark bookmark in bookmarks)
        {
            BookmarkUiView? view = null;

            if (state != null)
            {
                BookmarkUiContext context = BookmarkActivityStateResolver.CreateContext(state, definitionId, bookmark);
                view = await mapper.DescribeAsync(context, cancellationToken).ConfigureAwait(false);
            }

            bool described = view is { IsFallback: false };

            descriptions.Add(new WorkflowBookmarkDescription(
                bookmark.Id,
                bookmark.Name,
                bookmark.ActivityId,
                bookmark.ActivityNodeId,
                bookmark.ActivityInstanceId,
                bookmark.CreatedAt,
                described ? null : bookmark.Payload,
                described ? null : BookmarkActivityStateResolver.Resolve(state, bookmark),
                view));
        }

        return descriptions;
    }
}
