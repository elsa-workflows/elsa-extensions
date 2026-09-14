namespace Elsa.DevOps.AzureDevOps.Events;

/// <summary>
/// The comment that caused a <c>workitem.commentedOn</c> event.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not <see cref="Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.Comment"/>. The two paths that
/// produce this event know different amounts: the poller has read the real comment through the API and knows all of
/// it, while a Service Hook payload carries the text and the author but no comment id. Handing out a
/// <c>Comment</c> with a zero id from the webhook path would look like a comment that could be fetched or replied to,
/// and it cannot be. Everything optional here is genuinely optional.
/// </para>
/// </remarks>
/// <param name="Text">
/// What was written. HTML, as Azure DevOps stores it - a comment typed as "check <b>this</b>" arrives with the tags.
/// </param>
/// <param name="Id">The comment's id, when it is known. Absent on the Service Hook path, which does not carry one.</param>
/// <param name="Author">The display name of whoever wrote it.</param>
/// <param name="AuthorUniqueName">The sign-in name of whoever wrote it, when the source carried one.</param>
/// <param name="CreatedOn">When it was written.</param>
public sealed record PostedComment(
    string Text,
    int? Id = null,
    string? Author = null,
    string? AuthorUniqueName = null,
    DateTimeOffset? CreatedOn = null);
