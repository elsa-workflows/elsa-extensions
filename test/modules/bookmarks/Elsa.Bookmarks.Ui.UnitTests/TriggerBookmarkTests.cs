using Elsa.Bookmarks.Ui.Extensions;
using Elsa.Bookmarks.Ui.Models;
using Elsa.Bookmarks.Ui.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bookmarks.Ui.UnitTests;

/// <summary>
/// Runs a real trigger to the point where it suspends, and asks the real mapper about the bookmark it left behind.
/// </summary>
/// <remarks>
/// <see cref="BookmarkUiMapperTests"/> fakes the registry, so it proves the decision but not the three facts the
/// decision rests on: that Elsa names a bookmark after the activity that created it, that running the activity is
/// enough for the registry to know it, and that a trigger's descriptor says it is one. All three belong to Elsa, and
/// any of them changing quietly would put a free-form answer box back on every webhook trigger in the host - which is
/// exactly what this was removed for.
/// </remarks>
public class TriggerBookmarkTests
{
    [Fact]
    public async Task SaysNothingAboutTheBookmarkALiveTriggerLeftBehind()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddElsa();
        services.AddBookmarkUi();

        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        IWorkflowRunner runner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        RunWorkflowResult result = await runner.RunAsync(new WaitForAnotherSystemTrigger(), new RunWorkflowOptions(), CancellationToken.None);
        WorkflowState state = result.WorkflowState;

        Bookmark bookmark = Assert.Single(state.Bookmarks);

        // The mapper's lookup key, and what it finds under it. Asserted separately from the view below so a failure
        // says which of the two assumptions gave way rather than only that the view came back non-null.
        Assert.Equal("Test.Bookmarks.WaitForAnotherSystemTrigger", bookmark.Name);

        ActivityDescriptor? descriptor = scope.ServiceProvider.GetRequiredService<IActivityRegistry>().Find(bookmark.Name);

        Assert.Equal(ActivityKind.Trigger, descriptor?.Kind);

        IBookmarkUiMapper mapper = scope.ServiceProvider.GetRequiredService<IBookmarkUiMapper>();
        BookmarkUiContext context = BookmarkActivityStateResolver.CreateContext(state, "TestWorkflow", bookmark);

        Assert.Null(await mapper.DescribeAsync(context, CancellationToken.None));
        Assert.Null(await mapper.DescribeWithProviderAsync(context, CancellationToken.None));
    }

    /// <summary>
    /// A trigger that suspends, standing in for the Azure DevOps webhook triggers.
    /// </summary>
    /// <remarks>
    /// Local to this test rather than a real one: the package deliberately references no feature, and what decides the
    /// outcome is the kind of the activity, not the payload of any particular event.
    /// </remarks>
    [Activity("Test.Bookmarks", "Test", "Waits for another system to raise an event.", Kind = ActivityKind.Trigger)]
    private sealed class WaitForAnotherSystemTrigger : Trigger<string>
    {
        protected override object GetTriggerPayload(TriggerIndexingContext context) => new TestStimulus("something-happened");

        protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            context.CreateBookmark(new TestStimulus("something-happened"), includeActivityInstanceId: false);
            return default;
        }
    }

    private sealed record TestStimulus(string EventType);
}
