using Elsa.Bookmarks.Ui.Models;

namespace Elsa.Bookmarks.Ui.Services;

/// <summary>A view together with the provider that produced it.</summary>
/// <param name="View">How the bookmark should be shown and answered.</param>
/// <param name="Provider">
/// Null only when no registered provider claimed the bookmark and the mapper answered from its own built-in fallback.
/// A consumer needs this to ask the same provider how the answers travel - see <see cref="IBookmarkResumeWriter"/>.
/// </param>
public sealed record BookmarkUiDescription(BookmarkUiView View, IBookmarkUiProvider? Provider);

/// <summary>Describes a bookmark through whichever registered provider recognises it.</summary>
public interface IBookmarkUiMapper
{
    /// <summary>
    /// The view for this bookmark, or null when there is deliberately nothing to show.
    /// </summary>
    /// <remarks>
    /// Null means the mapper had something to say and chose not to, not that it failed. A bookmark no provider
    /// recognised gets the fallback view; a bookmark waiting in a <em>trigger</em> gets nothing, because a workflow
    /// waiting for another system to act is not a task anyone can look at or answer. A consumer shows such a bookmark
    /// under its own name, as it did before this package existed, or leaves it out.
    /// </remarks>
    ValueTask<BookmarkUiView?> DescribeAsync(BookmarkUiContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// The same view, with the provider that produced it.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="DescribeAsync"/> rather than replacing it: only a caller that has to resume the
    /// bookmark needs the provider, and putting a live service reference on <see cref="BookmarkUiView"/> would make a
    /// value that is serialised to clients carry something that cannot be.
    /// </remarks>
    ValueTask<BookmarkUiDescription?> DescribeWithProviderAsync(BookmarkUiContext context, CancellationToken cancellationToken = default);
}
