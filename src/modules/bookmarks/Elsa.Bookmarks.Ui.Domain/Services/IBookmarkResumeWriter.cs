using Elsa.Bookmarks.Ui.Models;

namespace Elsa.Bookmarks.Ui.Services;

/// <summary>
/// Converts the answers a caller supplied into the payload the waiting activity actually reads.
/// </summary>
/// <remarks>
/// Implemented by a provider whose activity expects something other than a map of the schema's own field names.
/// The schema says what to ask; this says how the answer travels, which is the waiting activity's business and
/// therefore the provider's knowledge rather than the caller's.
/// </remarks>
public interface IBookmarkResumeWriter
{
    /// <summary>The JSON the activity reads, built from the validated values.</summary>
    string WriteAnswers(BookmarkUiContext context, IReadOnlyDictionary<string, object?> values);
}
