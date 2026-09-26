using System.Text.Json;
using Elsa.Mcp.Server.Models;
using Elsa.Mcp.Server.Services;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;

namespace Elsa.Mcp.Server.UnitTests;

public class WorkflowRunResultMapperTests
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BuildPayload_ReturnsTheOutputOfAFinishedWorkflow()
    {
        RunWorkflowInstanceResponse response = new()
        {
            WorkflowInstanceId = "instance-1",
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Finished
        };
        WorkflowState state = new()
        {
            Id = "instance-1",
            Output = new Dictionary<string, object> { ["total"] = 42 }
        };

        Dictionary<string, object?> payload = WorkflowRunResultMapper.BuildPayload("order-intake", response, state);

        Dictionary<string, object> output = Assert.IsType<Dictionary<string, object>>(payload["output"]);
        Assert.Equal(42, output["total"]);
        Assert.Equal("Finished", payload["status"]);
        Assert.Equal("instance-1", payload["workflowInstanceId"]);
        Assert.Equal("order-intake", payload["definitionId"]);
    }

    [Fact]
    public void BuildPayload_OmitsOutputWhenTheWorkflowProducedNone()
    {
        RunWorkflowInstanceResponse response = new()
        {
            WorkflowInstanceId = "instance-1",
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Finished
        };

        Dictionary<string, object?> payload = WorkflowRunResultMapper.BuildPayload("order-intake", response, new WorkflowState());

        Assert.False(payload.ContainsKey("output"));
    }

    [Fact]
    public void BuildPayload_ReturnsOpenBookmarksWithTheStateOfTheWaitingActivity()
    {
        RunWorkflowInstanceResponse response = new()
        {
            WorkflowInstanceId = "instance-1",
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Suspended
        };
        WorkflowState state = new()
        {
            Bookmarks =
            [
                new Bookmark("bookmark-1", "RunTask", "hash-1", new { TaskName = "Approve" }, "activity-1", "node-1", "context-1", Moment)
            ],
            ActivityExecutionContexts =
            [
                new ActivityExecutionContextState
                {
                    Id = "context-1",
                    ActivityState = new Dictionary<string, object> { ["TaskName"] = "Approve" }
                }
            ]
        };

        Dictionary<string, object?> payload = WorkflowRunResultMapper.BuildPayload("order-intake", response, state);

        WorkflowBookmarkDescription bookmark = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<WorkflowBookmarkDescription>>(payload["bookmarks"]));
        Assert.Equal("bookmark-1", bookmark.Id);
        Assert.Equal("RunTask", bookmark.Name);
        Assert.Equal("activity-1", bookmark.ActivityId);
        Assert.Equal("Approve", bookmark.ActivityState!["TaskName"]);

        // The single bookmark id stays on the payload, so callers that already resume with it keep working.
        Assert.Equal("bookmark-1", payload["bookmarkId"]);
        Assert.Equal(new[] { "bookmark-1" }, payload["bookmarkIds"]);
    }

    [Fact]
    public void BuildPayload_ReturnsBookmarksEvenWhenTheRunResponseCarriesNone()
    {
        // The local runtime leaves the response bookmarks empty, so the state is the only source for them.
        RunWorkflowInstanceResponse response = new()
        {
            WorkflowInstanceId = "instance-1",
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Suspended
        };
        WorkflowState state = new()
        {
            Bookmarks = [new Bookmark("bookmark-1", "RunTask", "hash-1", null, "activity-1", "node-1", "context-1", Moment)]
        };

        Dictionary<string, object?> payload = WorkflowRunResultMapper.BuildPayload("order-intake", response, state);

        Assert.Equal("bookmark-1", payload["bookmarkId"]);
    }

    [Fact]
    public void BuildPayload_OmitsBookmarksOfAFinishedWorkflow()
    {
        RunWorkflowInstanceResponse response = new()
        {
            WorkflowInstanceId = "instance-1",
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Finished
        };
        WorkflowState state = new()
        {
            Bookmarks = [new Bookmark("bookmark-1", "RunTask", "hash-1", null, "activity-1", "node-1", "context-1", Moment)]
        };

        Dictionary<string, object?> payload = WorkflowRunResultMapper.BuildPayload("order-intake", response, state);

        Assert.False(payload.ContainsKey("bookmarks"));
        Assert.False(payload.ContainsKey("bookmarkId"));
    }

    [Fact]
    public void BuildPayload_ReturnsTheIncidentBehindAFaultedRun()
    {
        RunWorkflowInstanceResponse response = new()
        {
            WorkflowInstanceId = "instance-1",
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Faulted
        };
        WorkflowState state = new()
        {
            Incidents =
            [
                new ActivityIncident(
                    "activity-1",
                    "node-1",
                    "HttpRequest",
                    "The remote server returned 500.",
                    ExceptionState.FromException(new InvalidOperationException("Boom", new TimeoutException("Timed out"))),
                    Moment)
            ]
        };

        Dictionary<string, object?> payload = WorkflowRunResultMapper.BuildPayload("order-intake", response, state);

        WorkflowIncidentDescription incident = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<WorkflowIncidentDescription>>(payload["incidents"]));
        Assert.Equal("activity-1", incident.ActivityId);
        Assert.Equal("HttpRequest", incident.ActivityType);
        Assert.Equal("The remote server returned 500.", incident.Message);
        Assert.Equal(Moment, incident.Timestamp);
        Assert.Equal("System.InvalidOperationException", incident.Exception!.Type);
        Assert.Equal("Boom", incident.Exception.Message);
        Assert.Equal("Timed out", incident.Exception.InnerException!.Message);
    }

    [Fact]
    public void BuildPayload_ReportsAnIncidentOnceWhenBothTheRunAndTheStateCarryIt()
    {
        ActivityIncident incident = new("activity-1", "node-1", "HttpRequest", "The remote server returned 500.", null, Moment);
        RunWorkflowInstanceResponse response = new()
        {
            WorkflowInstanceId = "instance-1",
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Faulted,
            Incidents = [incident]
        };
        WorkflowState state = new() { Incidents = [incident] };

        Dictionary<string, object?> payload = WorkflowRunResultMapper.BuildPayload("order-intake", response, state);

        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<WorkflowIncidentDescription>>(payload["incidents"]));
    }

    [Fact]
    public void BuildPayload_StillDescribesTheRunWithoutState()
    {
        RunWorkflowInstanceResponse response = new()
        {
            WorkflowInstanceId = "instance-1",
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Finished
        };

        Dictionary<string, object?> payload = WorkflowRunResultMapper.BuildPayload("order-intake", response, state: null);

        Assert.Equal("instance-1", payload["workflowInstanceId"]);
        Assert.Equal("Finished", payload["subStatus"]);
        Assert.False(payload.ContainsKey("output"));
    }

    [Fact]
    public void BuildPayload_SerializesToTheShapeAnMcpCallerReads()
    {
        RunWorkflowInstanceResponse response = new()
        {
            WorkflowInstanceId = "instance-1",
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Suspended
        };
        WorkflowState state = new()
        {
            Output = new Dictionary<string, object> { ["total"] = 42 },
            Bookmarks = [new Bookmark("bookmark-1", "RunTask", "hash-1", null, "activity-1", "node-1", "context-1", Moment)],
            ActivityExecutionContexts =
            [
                new ActivityExecutionContextState
                {
                    Id = "context-1",
                    ActivityState = new Dictionary<string, object> { ["TaskName"] = "Approve" }
                }
            ]
        };

        JsonElement json = JsonSerializer.SerializeToElement(
            WorkflowRunResultMapper.BuildPayload("order-intake", response, state),
            WorkflowRunResultMapper.SerializerOptions);

        Assert.Equal(42, json.GetProperty("output").GetProperty("total").GetInt32());
        Assert.Equal("Suspended", json.GetProperty("subStatus").GetString());

        JsonElement bookmark = json.GetProperty("bookmarks")[0];
        Assert.Equal("bookmark-1", bookmark.GetProperty("id").GetString());
        Assert.Equal("Approve", bookmark.GetProperty("activityState").GetProperty("TaskName").GetString());

        // A bookmark without a payload leaves the key out rather than writing a null the caller has to skip.
        Assert.False(bookmark.TryGetProperty("payload", out _));
    }

    [Fact]
    public void BuildPayload_SerializesOutputThatRefersToItself()
    {
        // Workflow output is whatever the workflow put there, including a graph that points back at itself.
        Dictionary<string, object> node = [];
        node["self"] = node;

        RunWorkflowInstanceResponse response = new()
        {
            WorkflowInstanceId = "instance-1",
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Finished
        };
        WorkflowState state = new() { Output = new Dictionary<string, object> { ["node"] = node } };

        JsonElement json = JsonSerializer.SerializeToElement(
            WorkflowRunResultMapper.BuildPayload("order-intake", response, state),
            WorkflowRunResultMapper.SerializerOptions);

        Assert.True(json.TryGetProperty("output", out _));
    }

    [Fact]
    public void IsError_IsTrueForAFaultedRunWithoutIncidents()
    {
        RunWorkflowInstanceResponse response = new()
        {
            WorkflowInstanceId = "instance-1",
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Faulted
        };

        Assert.True(WorkflowRunResultMapper.IsError(response, new WorkflowState()));
    }

    [Fact]
    public void IsError_IsFalseForASuspendedRun()
    {
        RunWorkflowInstanceResponse response = new()
        {
            WorkflowInstanceId = "instance-1",
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Suspended
        };

        Assert.False(WorkflowRunResultMapper.IsError(response, new WorkflowState()));
    }
}
