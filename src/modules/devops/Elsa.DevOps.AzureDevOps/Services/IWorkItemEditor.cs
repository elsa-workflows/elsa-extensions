using Elsa.DevOps.AzureDevOps.Models;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Reads and writes single work item facts, one remote call at a time.
/// </summary>
/// <remarks>
/// An interface because <c>WorkItemTrackingHttpClient</c> comes out of a <c>VssConnection</c> and cannot be faked, and
/// because its callers - the agent tools - are worth testing without Azure DevOps.
/// </remarks>
public interface IWorkItemEditor
{
    /// <summary>Reads a work item.</summary>
    ValueTask<WorkItemSnapshot> GetAsync(WorkItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a WIQL query and brings back a line per hit. Two calls, because WIQL answers with ids alone.
    /// </summary>
    ValueTask<WorkItemSearchResult> SearchAsync(WorkItemQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Writes one field.</summary>
    ValueTask<WorkItemFieldChange> SetFieldAsync(WorkItemFieldRequest request, CancellationToken cancellationToken = default);

    /// <summary>Adds a comment, in Markdown.</summary>
    ValueTask<WorkItemCommentAdded> AddCommentAsync(WorkItemCommentRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// What Azure DevOps refused, in words its caller can pass on.
/// </summary>
/// <remarks>
/// Azure DevOps rejects plenty that a caller can put right: a work item that does not exist, a state a type does not
/// have, a field rule. Those arrive as this, message intact, so an agent tool can hand them to the model instead of
/// faulting a workflow. Anything else - a broken connection, a serializer failure - travels on untouched.
/// </remarks>
public sealed class WorkItemEditorException(string message, Exception? innerException = null)
    : Exception(message, innerException);
