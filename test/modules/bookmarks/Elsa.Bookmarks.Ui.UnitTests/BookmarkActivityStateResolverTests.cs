using Elsa.Bookmarks.Ui.Models;
using Elsa.Bookmarks.Ui.Services;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;

namespace Elsa.Bookmarks.Ui.UnitTests;

public class BookmarkActivityStateResolverTests
{
    [Fact]
    public void FindsTheActivityStateByActivityInstanceId()
    {
        WorkflowState state = CreateState(contextId: "context-1", scheduledActivityNodeId: "node-1", stateValue: "found");
        Bookmark bookmark = CreateBookmark(activityInstanceId: "context-1", activityNodeId: "other-node");

        IDictionary<string, object>? activityState = BookmarkActivityStateResolver.Resolve(state, bookmark);

        // The instance id is the precise match: it names one execution of the activity, where the node id names the
        // activity in the definition and can be scheduled more than once.
        Assert.Equal("found", activityState?["Payload"]);
    }

    [Fact]
    public void FindsTheActivityStateByNodeIdWhenTheBookmarkCarriesNoInstanceId()
    {
        WorkflowState state = CreateState(contextId: "context-1", scheduledActivityNodeId: "node-1", stateValue: "found");
        Bookmark bookmark = CreateBookmark(activityInstanceId: null, activityNodeId: "node-1");

        IDictionary<string, object>? activityState = BookmarkActivityStateResolver.Resolve(state, bookmark);

        // This is the case the whole feature stands on: CreateTaskBookmark - the activity behind every UIInteraction -
        // creates its bookmark with includeActivityInstanceId false, so matching on the instance id alone finds nothing
        // and every UI payload would be invisible.
        Assert.Equal("found", activityState?["Payload"]);
    }

    [Fact]
    public void ReturnsNullWhenNeitherIdMatches()
    {
        WorkflowState state = CreateState(contextId: "context-1", scheduledActivityNodeId: "node-1", stateValue: "found");
        Bookmark bookmark = CreateBookmark(activityInstanceId: "context-2", activityNodeId: "node-2");

        IDictionary<string, object>? activityState = BookmarkActivityStateResolver.Resolve(state, bookmark);

        // A provider must see null rather than another activity's state: describing a bookmark from the wrong
        // activity's inputs would be worse than not describing it at all.
        Assert.Null(activityState);
    }

    private static Bookmark CreateBookmark(string? activityInstanceId, string activityNodeId) =>
        new()
        {
            Id = "bookmark-1",
            Name = "TestStimulus",
            ActivityId = "activity-1",
            ActivityNodeId = activityNodeId,
            ActivityInstanceId = activityInstanceId,
            CreatedAt = DateTimeOffset.UnixEpoch
        };

    private static WorkflowState CreateState(string contextId, string scheduledActivityNodeId, string stateValue) =>
        new()
        {
            Id = "instance-1",
            DefinitionId = "TestWorkflow",
            ActivityExecutionContexts =
            [
                new ActivityExecutionContextState
                {
                    Id = contextId,
                    ScheduledActivityNodeId = scheduledActivityNodeId,
                    ActivityState = new Dictionary<string, object> { ["Payload"] = stateValue }
                }
            ]
        };
}
