using Elsa.Bookmarks.Ui.Models;

namespace Elsa.Bookmarks.Ui.Services;

/// <summary>
/// Describes the bookmarks of one feature.
/// </summary>
/// <remarks>
/// One method rather than a <c>CanHandle</c> plus a <c>Describe</c>: returning null <em>is</em> "not mine", and a
/// split would make every provider parse the same payload twice - once to recognise it, once to read it.
/// </remarks>
public interface IBookmarkUiProvider
{
    /// <summary>Lower runs first. Every provider runs before the mapper's own fallback, whatever its order.</summary>
    int Order { get; }

    /// <summary>The view for this bookmark, or null when this provider does not recognise it.</summary>
    ValueTask<BookmarkUiView?> DescribeAsync(BookmarkUiContext context, CancellationToken cancellationToken = default);
}
