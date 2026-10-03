namespace Elsa.DevOps.AzureDevOps.Services.Polling;

/// <summary>
/// What one paged read collected.
/// </summary>
/// <param name="Items">Every item the pages returned, in the order the server returned them.</param>
/// <param name="Truncated">
/// Whether the read stopped at the page cap with a full page still coming back, meaning items were left unread.
/// </param>
public sealed record PolledPage<T>(IReadOnlyList<T> Items, bool Truncated);

/// <summary>
/// Reads an Azure DevOps list endpoint page by page.
/// </summary>
/// <remarks>
/// The pushes and pull requests endpoints return newest-first and cap a response at <c>top</c> items, so a single
/// read only ever yields the newest page. Dispatching that page and advancing the checkpoint to its newest item
/// buries everything older below the checkpoint, where no later poll looks again — which is exactly what a burst, or
/// the backlog after any outage longer than one polling interval, produces.
/// </remarks>
public static class PolledPages
{
    /// <summary>
    /// How many pages one read may fetch. A bound is needed because the loop is driven by the server's answers: a
    /// checkpoint reset behind a very large history would otherwise keep one poll reading indefinitely.
    /// </summary>
    public const int MaxPages = 10;

    /// <summary>
    /// Requests pages of <paramref name="pageSize"/> with an increasing skip until one comes back short, and returns
    /// everything read.
    /// </summary>
    /// <param name="readPage">Reads one page, given the number of items to skip and the number to return.</param>
    /// <param name="pageSize">How many items to request per page.</param>
    public static async Task<PolledPage<T>> ReadAsync<T>(Func<int, int, Task<List<T>>> readPage, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(readPage);

        // A non-positive page size would ask the server for nothing and read that same nothing MaxPages times.
        int size = pageSize > 0 ? pageSize : 1;
        List<T> items = [];

        for (int page = 0; page < MaxPages; page++)
        {
            List<T> current = await readPage(page * size, size).ConfigureAwait(false);
            items.AddRange(current);

            // A short page is the only end marker these endpoints offer: neither reports a total or a continuation
            // token on the overloads the client exposes.
            if (current.Count < size)
                return new PolledPage<T>(items, false);
        }

        return new PolledPage<T>(items, true);
    }
}
