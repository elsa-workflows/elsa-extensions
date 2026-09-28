using Elsa.Bookmarks.Ui.Models;

namespace Elsa.Bookmarks.Ui.Providers;

/// <summary>
/// Describes a bookmark nobody claimed.
/// </summary>
/// <remarks>
/// <para>
/// It offers one free-form field rather than nothing, so an undescribed bookmark stays exactly as answerable as it was
/// before any of this existed - the feature adds descriptions, it does not take away the ability to resume.
/// </para>
/// <para>
/// Not an <see cref="Services.IBookmarkUiProvider"/>, and deliberately so: this is the mapper's last resort rather
/// than a competitor in the ordered chain. Registered at <c>int.MaxValue</c> it still ran before the mapper could ask
/// whether the bookmark belongs to a trigger, and so claimed every trigger bookmark with an answer box that nobody
/// can answer.
/// </para>
/// </remarks>
public static class FallbackBookmarkUiProvider
{
    /// <summary>The view for a bookmark no other provider recognised.</summary>
    public static BookmarkUiView Describe(BookmarkUiContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string name = string.IsNullOrWhiteSpace(context.Bookmark.Name) ? "an unknown event" : context.Bookmark.Name;

        return new BookmarkUiView
        {
            Kind = BookmarkUiKinds.Wait,
            Title = context.Bookmark.Name,
            Text = $"This workflow is waiting for {name}.",
            IsFallback = true,
            Resume = new BookmarkResumeSchema(
                "Only resume this when the user asked for it; nothing is known about what it expects.",
                [new BookmarkResumeField("answers", BookmarkFieldType.Text, "Answer")])
        };
    }
}
