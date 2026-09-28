using System.Globalization;
using Elsa.Bookmarks.Ui.Models;
using Elsa.Bookmarks.Ui.Services;
using Elsa.Scheduling.Bookmarks;

namespace Elsa.Bookmarks.Ui.Providers;

/// <summary>
/// Describes a workflow that is waiting for a moment to arrive.
/// </summary>
/// <remarks>
/// One provider covers Delay, Timer, Cron and StartAt: <see cref="DelayPayload"/> is the only bookmark payload
/// <c>Elsa.Scheduling</c> declares, and the timer activities all derive from one base. It matches on the payload it
/// can read rather than on the activity, so an activity that turns out to suspend on something else falls through to
/// the fallback instead of being described wrongly.
/// </remarks>
public sealed class DelayBookmarkUiProvider : IBookmarkUiProvider
{
    /// <inheritdoc />
    public int Order => 100;

    /// <inheritdoc />
    public ValueTask<BookmarkUiView?> DescribeAsync(BookmarkUiContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // The default check is not belt-and-braces: System.Text.Json fills what a payload does not carry, so reading
        // succeeds on any JSON object at all. Without it every unrecognised bookmark would be announced as a delay
        // that ends at the start of time.
        if (!BookmarkPayloadReader.TryRead(context.Bookmark.Payload, out DelayPayload? payload) || payload!.ResumeAt == default)
            return ValueTask.FromResult<BookmarkUiView?>(null);

        DateTimeOffset resumeAt = payload.ResumeAt;
        TimeSpan remaining = resumeAt - DateTimeOffset.UtcNow;

        string text = remaining > TimeSpan.Zero
            ? $"Waiting another {Describe(remaining)}, until {resumeAt.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture)}."
            : "This workflow's wait has passed; it continues on its own.";

        return ValueTask.FromResult<BookmarkUiView?>(new BookmarkUiView
        {
            Kind = BookmarkUiKinds.Wait,
            Title = "Waiting",
            Text = text,
            RefreshAt = resumeAt

            // No Resume: Elsa would allow resuming this bookmark early, but a caller that answers a delay has mistaken
            // waiting for asking, and a workflow that ran off its schedule is not something an apology puts right.
        });
    }

    private static string Describe(TimeSpan remaining) =>
        remaining.TotalMinutes < 1
            ? $"{Math.Ceiling(remaining.TotalSeconds).ToString("0", CultureInfo.InvariantCulture)} seconds"
            : remaining.TotalHours < 1
                ? $"{Math.Ceiling(remaining.TotalMinutes).ToString("0", CultureInfo.InvariantCulture)} minutes"
                : $"{Math.Round(remaining.TotalHours, 1).ToString("0.#", CultureInfo.InvariantCulture)} hours";
}
