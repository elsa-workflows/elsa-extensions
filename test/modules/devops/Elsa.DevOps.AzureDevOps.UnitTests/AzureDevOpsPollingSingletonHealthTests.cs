using Elsa.DevOps.AzureDevOps.Services;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// The rule that tells a polling singleton which will never poll again from one that is merely waiting.
/// </summary>
/// <remarks>
/// Written after the pull request and push pollers of the test environment sat at <c>Running/Suspended</c> for a day
/// without running (2026-08-31): a host recycle dropped their flowchart and timer contexts from the persisted state,
/// leaving a timer bookmark that points at a context that is no longer there. Nothing resumes such a bookmark, and
/// nothing creates a new one, so the instance is dead while every summary field still says it is healthy.
/// </remarks>
public class AzureDevOpsPollingSingletonHealthTests
{
    [Fact]
    public void A_bookmark_whose_activity_execution_context_is_gone_makes_the_instance_unresumable()
    {
        WorkflowState state = Suspended(
            contextIds: ["root"],
            bookmarkContextId: "timer-context-that-no-longer-exists");

        Assert.True(AzureDevOpsPollingWorkflowStarter.IsUnresumable(state));
    }

    [Fact]
    public void A_suspended_instance_whose_bookmark_still_has_its_context_is_left_alone()
    {
        WorkflowState state = Suspended(
            contextIds: ["root", "flowchart", "timer"],
            bookmarkContextId: "timer");

        Assert.False(AzureDevOpsPollingWorkflowStarter.IsUnresumable(state));
    }

    [Fact]
    public void A_finished_instance_is_left_to_the_rule_that_already_removes_it()
    {
        WorkflowState state = Suspended(contextIds: ["root"], bookmarkContextId: "gone");
        state.Status = WorkflowStatus.Finished;

        Assert.False(AzureDevOpsPollingWorkflowStarter.IsUnresumable(state));
    }

    [Fact]
    public void An_instance_holding_no_bookmarks_is_left_alone()
    {
        // It may be running right now on another host of the same app service; only an orphaned bookmark is proof.
        WorkflowState state = Suspended(contextIds: ["root"], bookmarkContextId: null);

        Assert.False(AzureDevOpsPollingWorkflowStarter.IsUnresumable(state));
    }

    [Fact]
    public void A_bookmark_without_an_activity_instance_id_is_not_read_as_orphaned()
    {
        WorkflowState state = Suspended(contextIds: ["root"], bookmarkContextId: "");

        Assert.False(AzureDevOpsPollingWorkflowStarter.IsUnresumable(state));
    }

    private static WorkflowState Suspended(IEnumerable<string> contextIds, string? bookmarkContextId)
    {
        WorkflowState state = new()
        {
            Id = "AzureDevOpsPullRequestPollingWorkflow",
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Suspended,
        };

        foreach (string contextId in contextIds)
            state.ActivityExecutionContexts.Add(new ActivityExecutionContextState { Id = contextId });

        if (bookmarkContextId != null)
        {
            state.Bookmarks.Add(new Bookmark
            {
                Id = "62a5023c18ec1670",
                Name = "Elsa.Timer",
                ActivityInstanceId = bookmarkContextId,
            });
        }

        return state;
    }
}
