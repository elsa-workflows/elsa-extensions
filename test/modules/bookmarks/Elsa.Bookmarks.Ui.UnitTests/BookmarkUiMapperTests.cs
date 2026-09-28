using Elsa.Bookmarks.Ui.Models;
using Elsa.Bookmarks.Ui.Services;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Bookmarks.Ui.UnitTests;

public class BookmarkUiMapperTests
{
    [Fact]
    public async Task TakesTheViewOfTheLowestOrderedProviderThatRecognisesTheBookmark()
    {
        StubProvider first = new(order: 10, view: CreateView("first"));
        StubProvider second = new(order: 20, view: CreateView("second"));
        BookmarkUiMapper mapper = CreateMapper([second, first]);

        BookmarkUiView? view = await mapper.DescribeAsync(CreateContext(), CancellationToken.None);

        // Order is what lets a host put a specific provider in front of a general one, so it has to beat registration
        // order rather than merely tie-break it.
        Assert.Equal("first", view!.Title);
    }

    [Fact]
    public async Task SkipsAProviderThatDeclines()
    {
        StubProvider declining = new(order: 10, view: null);
        StubProvider answering = new(order: 20, view: CreateView("answered"));
        BookmarkUiMapper mapper = CreateMapper([declining, answering]);

        BookmarkUiView? view = await mapper.DescribeAsync(CreateContext(), CancellationToken.None);

        Assert.Equal("answered", view!.Title);
    }

    [Fact]
    public async Task TreatsAThrowingProviderAsADecline()
    {
        ThrowingProvider throwing = new();
        StubProvider answering = new(order: 20, view: CreateView("answered"));
        BookmarkUiMapper mapper = CreateMapper([throwing, answering]);

        BookmarkUiView? view = await mapper.DescribeAsync(CreateContext(), CancellationToken.None);

        // One feature's broken provider must not be able to fail the read of every other bookmark on the instance.
        Assert.Equal("answered", view!.Title);
    }

    [Fact]
    public async Task FallsBackWhenEveryProviderDeclines()
    {
        BookmarkUiMapper mapper = CreateMapper([new StubProvider(order: 10, view: null)]);

        BookmarkUiView? view = await mapper.DescribeAsync(CreateContext(), CancellationToken.None);

        // An undescribed bookmark still has to be answerable, or adding this feature would take away an ability the
        // caller has today.
        Assert.NotNull(view);
        Assert.Equal(BookmarkUiKinds.Wait, view.Kind);
        Assert.NotNull(view.Resume);
        Assert.Equal("answers", view.Resume!.Fields.Single().Name);
    }

    [Fact]
    public async Task NamesTheProviderThatProducedTheView()
    {
        StubProvider declining = new(order: 10, view: null);
        StubProvider answering = new(order: 20, view: CreateView("answered"));
        BookmarkUiMapper mapper = CreateMapper([declining, answering]);

        BookmarkUiDescription? description = await mapper.DescribeWithProviderAsync(CreateContext(), CancellationToken.None);

        // The consumer asks this provider how the answers travel back to its activity, so naming the wrong one - or
        // none - would send a shape the waiting activity cannot read.
        Assert.Same(answering, description!.Provider);
        Assert.Equal("answered", description.View.Title);
    }

    [Fact]
    public async Task NamesNoProviderWhenTheMappersOwnFallbackAnswers()
    {
        BookmarkUiMapper mapper = CreateMapper([new StubProvider(order: 10, view: null)]);

        BookmarkUiDescription? description = await mapper.DescribeWithProviderAsync(CreateContext(), CancellationToken.None);

        // The fallback is the mapper's own last resort rather than a registered provider, so there is nothing to ask
        // how the answers travel - and a consumer reading Provider must see that rather than a provider it can call.
        Assert.NotNull(description);
        Assert.Null(description.Provider);
        Assert.True(description.View.IsFallback);
    }

    [Fact]
    public async Task SaysNothingAboutABookmarkWaitingInATrigger()
    {
        BookmarkUiMapper mapper = CreateMapper(
            [new StubProvider(order: 10, view: null)],
            "Elsa.AzureDevOps.WorkItems.WorkItemDeletedTrigger",
            ActivityKind.Trigger);

        BookmarkUiContext context = CreateContext("Elsa.AzureDevOps.WorkItems.WorkItemDeletedTrigger");

        // The fallback would offer a free-form answer field here, and a deleted work item is not something anybody can
        // answer: the workflow is waiting for Azure DevOps, not for a person. No view means no component and no box to
        // type in, which is the whole point.
        Assert.Null(await mapper.DescribeAsync(context, CancellationToken.None));
        Assert.Null(await mapper.DescribeWithProviderAsync(context, CancellationToken.None));
    }

    [Fact]
    public async Task StillFallsBackForABookmarkWaitingInAnActivityThatIsNotATrigger()
    {
        BookmarkUiMapper mapper = CreateMapper([new StubProvider(order: 10, view: null)], "Some.Task", ActivityKind.Task);

        BookmarkUiView? view = await mapper.DescribeAsync(CreateContext("Some.Task"), CancellationToken.None);

        Assert.NotNull(view);
        Assert.True(view.IsFallback);
    }

    [Fact]
    public async Task LetsAProviderDescribeATriggerBookmarkItRecognises()
    {
        BookmarkUiMapper mapper = CreateMapper(
            [new StubProvider(order: 10, view: CreateView("recognised"))],
            "Elsa.Event",
            ActivityKind.Trigger);

        BookmarkUiView? view = await mapper.DescribeAsync(CreateContext("Elsa.Event"), CancellationToken.None);

        // The check exists to replace the fallback, not to override a provider. An Event is a trigger too, and a
        // workflow waiting on one mid-flow is exactly the case EventBookmarkUiProvider is for.
        Assert.Equal("recognised", view!.Title);
    }

    [Fact]
    public async Task TreatsAnActivityTheRegistryDoesNotKnowAsNotATrigger()
    {
        BookmarkUiMapper mapper = CreateMapper([new StubProvider(order: 10, view: null)]);

        BookmarkUiView? view = await mapper.DescribeAsync(CreateContext("Unregistered"), CancellationToken.None);

        // A host whose activity registry was never populated must keep describing its bookmarks rather than fall
        // silent about every one of them at once.
        Assert.NotNull(view);
        Assert.True(view.IsFallback);
    }

    private static BookmarkUiMapper CreateMapper(
        IEnumerable<IBookmarkUiProvider> providers,
        string? activityTypeName = null,
        ActivityKind kind = ActivityKind.Action)
    {
        IActivityRegistry registry = Substitute.For<IActivityRegistry>();

        if (activityTypeName != null)
        {
            registry.Find(activityTypeName)
                .Returns(new ActivityDescriptor { TypeName = activityTypeName, Kind = kind });
        }

        return new BookmarkUiMapper(providers, registry, NullLogger<BookmarkUiMapper>.Instance);
    }

    private static BookmarkUiView CreateView(string title) =>
        new() { Kind = BookmarkUiKinds.Form, Title = title, Text = title };

    private static BookmarkUiContext CreateContext(string bookmarkName = "TestStimulus") =>
        new("instance-1", "TestWorkflow",
            new BookmarkDescriptor("bookmark-1", bookmarkName, "activity-1", "node-1", null, DateTimeOffset.UnixEpoch, null, null),
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase));

    private sealed class StubProvider(int order, BookmarkUiView? view) : IBookmarkUiProvider
    {
        public int Order => order;

        public ValueTask<BookmarkUiView?> DescribeAsync(BookmarkUiContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(view);
    }

    private sealed class ThrowingProvider : IBookmarkUiProvider
    {
        public int Order => 10;

        public ValueTask<BookmarkUiView?> DescribeAsync(BookmarkUiContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("This provider is broken.");
    }
}
