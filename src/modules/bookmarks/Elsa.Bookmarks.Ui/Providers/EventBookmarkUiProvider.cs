using Elsa.Bookmarks.Ui.Models;
using Elsa.Bookmarks.Ui.Services;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Bookmarks.Ui.Providers;

/// <summary>Describes a workflow that is waiting for a named event.</summary>
public sealed class EventBookmarkUiProvider : IBookmarkUiProvider
{
    /// <inheritdoc />
    public int Order => 110;

    /// <inheritdoc />
    public ValueTask<BookmarkUiView?> DescribeAsync(BookmarkUiContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Same reason as the delay provider: a payload that deserialises into an empty stimulus is not an event
        // bookmark, it is any other bookmark read through the wrong type.
        if (!BookmarkPayloadReader.TryRead(context.Bookmark.Payload, out EventStimulus? stimulus)
            || string.IsNullOrWhiteSpace(stimulus!.EventName))
        {
            return ValueTask.FromResult<BookmarkUiView?>(null);
        }

        return ValueTask.FromResult<BookmarkUiView?>(new BookmarkUiView
        {
            Kind = BookmarkUiKinds.Wait,
            Title = stimulus!.EventName,
            Text = $"This workflow is waiting for the event '{stimulus.EventName}'.",
            Resume = new BookmarkResumeSchema(
                $"Only send the event '{stimulus.EventName}' when the user asked for it. Anything supplied travels to the workflow as the event's payload.",
                [new BookmarkResumeField("answers", BookmarkFieldType.Text, "Payload")])
        });
    }
}
