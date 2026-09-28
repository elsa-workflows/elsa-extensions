using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Mcp.Server.Models;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;

namespace Elsa.Mcp.Server.Services;

/// <summary>
/// Describes the result of a workflow run to an MCP caller.
/// </summary>
/// <remarks>
/// The run response only carries the instance id, its status and whatever incidents the run itself produced. Everything
/// the caller actually asked the workflow for - its output, and what it is waiting for when it did not finish - lives on
/// the workflow state, which is why both are mapped here rather than the response alone.
/// </remarks>
internal static class WorkflowRunResultMapper
{
    /// <summary>
    /// The options the payload is written with. Workflow output and bookmark payloads are shaped by the workflow, not
    /// by this server, so a self-referencing graph must not take the tool call down.
    /// </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Builds the payload returned for a workflow tool call. The state is optional: without it the caller still learns
    /// how the run ended, only without output, bookmarks and incidents recorded on the instance. When
    /// <paramref name="describedBookmarks"/> is supplied, it is used instead of the raw bookmarks on the response and
    /// state, so a caller that has already had the bookmarks described by their providers does not have that work redone.
    /// </summary>
    public static Dictionary<string, object?> BuildPayload(
        string definitionId,
        RunWorkflowInstanceResponse response,
        WorkflowState? state,
        IReadOnlyList<WorkflowBookmarkDescription>? describedBookmarks = null)
    {
        ArgumentNullException.ThrowIfNull(response);

        Dictionary<string, object?> payload = new(StringComparer.Ordinal)
        {
            ["definitionId"] = definitionId,
            ["workflowInstanceId"] = response.WorkflowInstanceId,
            ["status"] = response.Status.ToString(),
            ["subStatus"] = response.SubStatus.ToString()
        };

        if (state is { Output.Count: > 0 })
            payload["output"] = state.Output;

        // A finished workflow has nothing left to resume, so its burnt bookmarks are not worth the caller's tokens.
        if (response.Status != WorkflowStatus.Finished)
        {
            IReadOnlyList<WorkflowBookmarkDescription> bookmarks = describedBookmarks ?? DescribeBookmarks(response, state);

            if (bookmarks.Count > 0)
            {
                // The first bookmark is the one a caller resumes with, so it is surfaced next to the full list.
                payload["bookmarkId"] = bookmarks[0].Id;
                payload["bookmarkIds"] = bookmarks.Select(bookmark => bookmark.Id).ToArray();
                payload["bookmarks"] = bookmarks;
            }
        }

        IReadOnlyList<WorkflowIncidentDescription> incidents = DescribeIncidents(response, state);

        if (incidents.Count > 0)
            payload["incidents"] = incidents;

        return payload;
    }

    /// <summary>
    /// Whether the run is to be reported as a failed tool call.
    /// </summary>
    public static bool IsError(RunWorkflowInstanceResponse response, WorkflowState? state)
    {
        ArgumentNullException.ThrowIfNull(response);

        // A faulted workflow that recorded no incident is still a failed call, so the status is checked as well.
        return response.SubStatus == WorkflowSubStatus.Faulted
            || response.Incidents.Count > 0
            || state is { Incidents.Count: > 0 };
    }

    private static IReadOnlyList<WorkflowBookmarkDescription> DescribeBookmarks(RunWorkflowInstanceResponse response, WorkflowState? state)
    {
        // The state holds every bookmark that is still open; the response only holds the ones the run produced, and the
        // local runtime does not even fill those. The state therefore wins whenever it has any.
        ICollection<Bookmark> bookmarks = state is { Bookmarks.Count: > 0 } ? state.Bookmarks : response.Bookmarks;

        if (bookmarks.Count == 0)
            return [];

        Dictionary<string, ActivityExecutionContextState> contexts = new(StringComparer.Ordinal);

        foreach (ActivityExecutionContextState context in state?.ActivityExecutionContexts ?? [])
            contexts[context.Id] = context;

        return
        [
            .. bookmarks.Select(bookmark => new WorkflowBookmarkDescription(
                bookmark.Id,
                bookmark.Name,
                bookmark.ActivityId,
                bookmark.ActivityNodeId,
                bookmark.ActivityInstanceId,
                bookmark.CreatedAt,
                bookmark.Payload,
                FindActivityState(contexts, bookmark.ActivityInstanceId)))
        ];
    }

    private static IDictionary<string, object>? FindActivityState(Dictionary<string, ActivityExecutionContextState> contexts, string? activityInstanceId) =>
        activityInstanceId != null && contexts.TryGetValue(activityInstanceId, out ActivityExecutionContextState? context)
            ? context.ActivityState
            : null;

    private static IReadOnlyList<WorkflowIncidentDescription> DescribeIncidents(RunWorkflowInstanceResponse response, WorkflowState? state)
    {
        // The same incident is reported by both the run and the instance it was recorded on, hence the de-duplication.
        IEnumerable<ActivityIncident> incidents = state == null
            ? response.Incidents
            : response.Incidents.Concat(state.Incidents);

        return
        [
            .. incidents
                .DistinctBy(incident => (incident.ActivityNodeId, incident.Message, incident.Timestamp))
                .Select(incident => new WorkflowIncidentDescription(
                    incident.ActivityId,
                    incident.ActivityNodeId,
                    incident.ActivityType,
                    incident.Message,
                    incident.Timestamp,
                    DescribeException(incident.Exception)))
        ];
    }

    private static WorkflowExceptionDescription? DescribeException(ExceptionState? exception) =>
        exception == null
            ? null
            : new WorkflowExceptionDescription(
                exception.Type?.FullName ?? exception.Type?.Name ?? nameof(Exception),
                exception.Message,
                DescribeException(exception.InnerException));
}
