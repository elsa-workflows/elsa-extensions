using Elsa.Bookmarks.Ui.Models;
using Elsa.Bookmarks.Ui.Services;

namespace Elsa.Bookmarks.Ui.Providers;

/// <summary>
/// Describes Elsa's own <c>RunTask</c>: a task that has a name and says nothing about what it wants.
/// </summary>
/// <remarks>
/// Ordered behind every feature provider, because a feature's own task bookmark is a RunTask too and knows far more
/// about itself than this can.
/// </remarks>
public sealed class RunTaskBookmarkUiProvider : IBookmarkUiProvider
{
    /// <summary>
    /// The bookmark name Elsa gives a <c>RunTask</c>: the activity type, from its <c>[Activity]</c> namespace and
    /// class name.
    /// </summary>
    private const string BookmarkName = "Elsa.RunTask";

    /// <inheritdoc />
    public int Order => 1000;

    /// <inheritdoc />
    public ValueTask<BookmarkUiView?> DescribeAsync(BookmarkUiContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // An exact match rather than a substring: this provider sits at Order 1000, ahead of the fallback, so a loose
        // match claims any feature bookmark whose name merely contains "RunTask" and describes it as a task about which
        // nothing is known. The cost of being too strict is the fallback's blander sentence; the cost of being too
        // loose is a wrong description that looks authoritative.
        if (!string.Equals(context.Bookmark.Name, BookmarkName, StringComparison.Ordinal))
            return ValueTask.FromResult<BookmarkUiView?>(null);

        return ValueTask.FromResult<BookmarkUiView?>(new BookmarkUiView
        {
            Kind = BookmarkUiKinds.Form,
            Title = context.Bookmark.Name,
            Text = "This workflow is waiting for a task to be completed.",
            Resume = new BookmarkResumeSchema(
                "Ask the user what to answer. Nothing is known about the fields this task expects, so send what the user says as it is.",
                [new BookmarkResumeField("answers", BookmarkFieldType.Text, "Answer")])
        });
    }
}
