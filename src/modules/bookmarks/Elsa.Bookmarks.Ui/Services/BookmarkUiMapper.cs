using Elsa.Bookmarks.Ui.Models;
using Elsa.Bookmarks.Ui.Providers;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Bookmarks.Ui.Services;

/// <inheritdoc />
public sealed class BookmarkUiMapper(
    IEnumerable<IBookmarkUiProvider> providers,
    IActivityRegistry activityRegistry,
    ILogger<BookmarkUiMapper> logger) : IBookmarkUiMapper
{
    private readonly IReadOnlyList<IBookmarkUiProvider> orderedProviders = [.. providers.OrderBy(provider => provider.Order)];

    /// <inheritdoc />
    public async ValueTask<BookmarkUiView?> DescribeAsync(BookmarkUiContext context, CancellationToken cancellationToken = default) =>
        (await DescribeWithProviderAsync(context, cancellationToken).ConfigureAwait(false))?.View;

    /// <inheritdoc />
    public async ValueTask<BookmarkUiDescription?> DescribeWithProviderAsync(BookmarkUiContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (IBookmarkUiProvider provider in orderedProviders)
        {
            BookmarkUiView? view;

            try
            {
                view = await provider.DescribeAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // A provider reads payloads a workflow wrote, so it can meet a shape it did not expect. That is worth
                // a log line and the next provider's turn - not the failure of a read covering every other bookmark.
                logger.LogWarning(
                    exception,
                    "Bookmark UI provider {Provider} failed on bookmark {BookmarkId}.",
                    provider.GetType().Name,
                    context.Bookmark.Id);
                continue;
            }

            if (view != null)
                return new BookmarkUiDescription(view, provider);
        }

        if (IsWaitingOnATrigger(context))
            return null;

        // The last resort, and not a registered provider: a fallback that took its turn in the ordered list would
        // claim every trigger bookmark before the check below could see it.
        return new BookmarkUiDescription(FallbackBookmarkUiProvider.Describe(context), null);
    }

    /// <summary>
    /// Whether the activity this bookmark is waiting in is a trigger.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read from the activity registry, because nothing in the payload says it: an
    /// <c>AzureDevOpsWebhookBookmark</c> is a record like any other. The lookup key is the bookmark's name, which is
    /// the activity type name - Elsa names a bookmark after the activity that created it unless that activity chose a
    /// name of its own, and a trigger does not.
    /// </para>
    /// <para>
    /// A name the registry does not know is treated as not-a-trigger. A host whose registry was never populated
    /// should go on describing its bookmarks rather than fall silent about all of them at once.
    /// </para>
    /// </remarks>
    private bool IsWaitingOnATrigger(BookmarkUiContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Bookmark.Name))
            return false;

        ActivityDescriptor? descriptor = activityRegistry.Find(context.Bookmark.Name);

        return descriptor?.Kind == ActivityKind.Trigger;
    }
}
