using Elsa.Bookmarks.Ui.Models;
using Elsa.Bookmarks.Ui.Services;
using Elsa.Mcp.Server.Models;
using Elsa.Mcp.Server.Services;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;

namespace Elsa.Mcp.Server.UnitTests;

public class BookmarkDescriptionFactoryTests
{
    [Fact]
    public async Task CarriesTheViewAndDropsTheRawPayloadWhenAProviderDescribedTheBookmark()
    {
        IBookmarkUiMapper mapper = Substitute.For<IBookmarkUiMapper>();
        mapper.DescribeAsync(Arg.Any<BookmarkUiContext>(), Arg.Any<CancellationToken>())
            .Returns(new BookmarkUiView { Kind = BookmarkUiKinds.Link, Title = "Openen", Text = "[Openen](https://example.test)" });

        BookmarkDescriptionFactory factory = new(mapper);

        IReadOnlyList<WorkflowBookmarkDescription> descriptions =
            await factory.DescribeAsync(CreateState(), [CreateBookmark()], "TestWorkflow", CancellationToken.None);

        // The view says everything the raw payload said, and the payload and activity state are the two unbounded
        // fields in the result - keeping both would spend the caller's tokens twice on the same thing.
        Assert.Equal("Openen", descriptions.Single().Ui?.Title);
        Assert.Null(descriptions.Single().Payload);
        Assert.Null(descriptions.Single().ActivityState);
    }

    [Fact]
    public async Task KeepsTheRawPayloadWhenOnlyTheFallbackDescribedTheBookmark()
    {
        IBookmarkUiMapper mapper = Substitute.For<IBookmarkUiMapper>();
        mapper.DescribeAsync(Arg.Any<BookmarkUiContext>(), Arg.Any<CancellationToken>())
            .Returns(new BookmarkUiView { Kind = BookmarkUiKinds.Wait, Title = "TestStimulus", Text = "This workflow is waiting for TestStimulus.", IsFallback = true });

        BookmarkDescriptionFactory factory = new(mapper);

        IReadOnlyList<WorkflowBookmarkDescription> descriptions =
            await factory.DescribeAsync(CreateState(), [CreateBookmark()], "TestWorkflow", CancellationToken.None);

        // An undescribed bookmark must not come back with less than it does today: the raw payload and the waiting
        // activity's state are both all the caller has to reason with, so the fallback case must keep both.
        Assert.NotNull(descriptions.Single().Payload);
        Assert.NotNull(descriptions.Single().ActivityState);
    }

    private static Bookmark CreateBookmark() =>
        new()
        {
            Id = "bookmark-1",
            Name = "TestStimulus",
            ActivityId = "activity-1",
            ActivityNodeId = "node-1",
            ActivityInstanceId = "context-1",
            CreatedAt = DateTimeOffset.UnixEpoch,
            Payload = new Dictionary<string, object> { ["taskId"] = "task-1" }
        };

    private static WorkflowState CreateState() =>
        new()
        {
            Id = "instance-1",
            DefinitionId = "TestWorkflow",
            ActivityExecutionContexts =
            [
                new ActivityExecutionContextState
                {
                    Id = "context-1",
                    ScheduledActivityNodeId = "node-1",
                    ActivityState = new Dictionary<string, object> { ["Payload"] = "something" }
                }
            ]
        };
}
