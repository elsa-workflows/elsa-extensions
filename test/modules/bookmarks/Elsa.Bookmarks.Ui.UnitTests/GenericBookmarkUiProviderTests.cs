using System.Text.Json;
using Elsa.Bookmarks.Ui.Models;
using Elsa.Bookmarks.Ui.Providers;
using Elsa.Scheduling.Bookmarks;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Bookmarks.Ui.UnitTests;

public class GenericBookmarkUiProviderTests
{
    [Fact]
    public async Task TheDelayProviderReportsWhenTheWaitEnds()
    {
        DateTimeOffset resumeAt = DateTimeOffset.UtcNow.AddMinutes(7);
        DelayBookmarkUiProvider provider = new();

        BookmarkUiView? view = await provider.DescribeAsync(
            CreateContext("DelayPayload", new DelayPayload(resumeAt)),
            CancellationToken.None);

        // RefreshAt is the whole point: without it the caller has nothing to decide "come back later" on.
        Assert.NotNull(view);
        Assert.Equal(BookmarkUiKinds.Wait, view!.Kind);
        Assert.Equal(resumeAt, view.RefreshAt);
    }

    [Fact]
    public async Task TheDelayProviderOffersNoWayToAnswerADelay()
    {
        DelayBookmarkUiProvider provider = new();

        BookmarkUiView? view = await provider.DescribeAsync(
            CreateContext("DelayPayload", new DelayPayload(DateTimeOffset.UtcNow.AddHours(1))),
            CancellationToken.None);

        // Elsa would let a caller resume a delay early. An agent doing that has mistaken waiting for asking, and the
        // damage - a workflow running off its schedule - is worse than the wait.
        Assert.Null(view!.Resume);
    }

    [Fact]
    public async Task TheDelayProviderReadsAPayloadThatCameBackAsJson()
    {
        DateTimeOffset resumeAt = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
        JsonElement payload = JsonSerializer.SerializeToElement(new DelayPayload(resumeAt));
        DelayBookmarkUiProvider provider = new();

        BookmarkUiView? view = await provider.DescribeAsync(
            CreateContext("DelayPayload", payload),
            CancellationToken.None);

        // A payload read back from the instance store is a JsonElement, not the type that was written. A provider that
        // only handles the typed case works on a fresh run and silently stops working after a restart.
        Assert.Equal(resumeAt, view!.RefreshAt);
    }

    [Fact]
    public async Task TheDelayProviderDeclinesABookmarkThatIsNotADelay()
    {
        DelayBookmarkUiProvider provider = new();

        BookmarkUiView? view = await provider.DescribeAsync(
            CreateContext("SomethingElse", payload: null),
            CancellationToken.None);

        // If this provider claimed unrelated bookmarks - through a name collision or an overly loose deserialisation -
        // every unmatched bookmark in the host would render as a delay ending at the start of time.
        Assert.Null(view);
    }

    [Fact]
    public async Task TheEventProviderReportsTheEventItIsWaitingFor()
    {
        EventBookmarkUiProvider provider = new();

        BookmarkUiView? view = await provider.DescribeAsync(
            CreateContext("EventStimulus", new EventStimulus("OrderApproved")),
            CancellationToken.None);

        // The event name is the whole point: without it in Title/Text and the Resume prompt, a caller has nothing to
        // tell the user it is waiting for, and nothing to send back to resume it correctly.
        Assert.NotNull(view);
        Assert.Equal(BookmarkUiKinds.Wait, view!.Kind);
        Assert.Equal("OrderApproved", view.Title);
        Assert.Contains("OrderApproved", view.Text);
        Assert.NotNull(view.Resume);
        Assert.Equal("answers", view.Resume!.Fields.Single().Name);
    }

    [Fact]
    public async Task TheEventProviderReadsAPayloadThatCameBackAsJson()
    {
        JsonElement payload = JsonSerializer.SerializeToElement(new EventStimulus("OrderApproved"));
        EventBookmarkUiProvider provider = new();

        BookmarkUiView? view = await provider.DescribeAsync(
            CreateContext("EventStimulus", payload),
            CancellationToken.None);

        // Same restart hazard as the delay provider: a stimulus read back from the instance store arrives as a
        // JsonElement, not an EventStimulus, and the event name must still come through.
        Assert.NotNull(view);
        Assert.Equal("OrderApproved", view!.Title);
    }

    [Fact]
    public async Task TheEventProviderDeclinesABookmarkThatIsNotAnEvent()
    {
        EventBookmarkUiProvider provider = new();

        BookmarkUiView? view = await provider.DescribeAsync(
            CreateContext("SomethingElse", payload: null),
            CancellationToken.None);

        // Guards against the same failure mode as the delay provider: System.Text.Json fills EventName with an empty
        // string for any payload that does not carry one, so without this guard every unmatched bookmark in the host
        // would be announced as waiting for an event named nothing.
        Assert.Null(view);
    }

    [Fact]
    public async Task TheRunTaskProviderOffersAFreeFormAnswer()
    {
        RunTaskBookmarkUiProvider provider = new();

        BookmarkUiView? view = await provider.DescribeAsync(
            CreateContext("Elsa.RunTask", payload: null),
            CancellationToken.None);

        // Elsa's own RunTask says what it is called and nothing about what it wants, so a free-form answer is the
        // honest offer - a schema invented here would be a guess presented as fact. The bookmark name is the activity
        // type, built from the [Activity] namespace and the class name.
        Assert.NotNull(view);
        Assert.Equal(BookmarkUiKinds.Form, view!.Kind);
        Assert.Equal("answers", view.Resume!.Fields.Single().Name);
    }

    [Fact]
    public async Task TheRunTaskProviderDeclinesABookmarkThatMerelyMentionsRunTask()
    {
        RunTaskBookmarkUiProvider provider = new();

        BookmarkUiView? view = await provider.DescribeAsync(
            CreateContext("MyRunTaskThing", payload: null),
            CancellationToken.None);

        // This provider sits at Order 1000, ahead of the fallback, so a substring match would let it claim any feature
        // bookmark whose name happens to contain "RunTask" and describe it as a task nothing is known about.
        Assert.Null(view);
    }

    [Fact]
    public async Task TheGenericProvidersSpeakEnglish()
    {
        DelayBookmarkUiProvider delay = new();
        EventBookmarkUiProvider @event = new();
        RunTaskBookmarkUiProvider runTask = new();

        BookmarkUiView delayView = (await delay.DescribeAsync(
            CreateContext("DelayPayload", new DelayPayload(DateTimeOffset.UtcNow.AddMinutes(7))),
            CancellationToken.None))!;
        BookmarkUiView eventView = (await @event.DescribeAsync(
            CreateContext("EventStimulus", new EventStimulus("OrderApproved")),
            CancellationToken.None))!;
        BookmarkUiView runTaskView = (await runTask.DescribeAsync(
            CreateContext("Elsa.RunTask", payload: null),
            CancellationToken.None))!;
        BookmarkUiView fallbackView = FallbackBookmarkUiProvider.Describe(CreateContext("SomethingElse", payload: null));

        // The built-in providers speak English throughout. Their Resume prompts already did, and a view whose title
        // is in another language above an English prompt reads as half-translated. A host that wants its own
        // language registers its own provider; these are the fallbacks.
        Assert.Equal("Waiting", delayView.Title);
        Assert.Contains("Waiting another 7 minutes", delayView.Text, StringComparison.Ordinal);
        Assert.Contains("is waiting for the event", eventView.Text, StringComparison.Ordinal);
        Assert.Contains("waiting for a task", runTaskView.Text, StringComparison.Ordinal);
        Assert.Equal("Answer", runTaskView.Resume!.Fields.Single().Label);
        Assert.Contains("is waiting for", fallbackView.Text, StringComparison.Ordinal);
        Assert.Equal("Answer", fallbackView.Resume!.Fields.Single().Label);
    }

    private static BookmarkUiContext CreateContext(string bookmarkName, object? payload) =>
        new("instance-1", "TestWorkflow",
            new BookmarkDescriptor("bookmark-1", bookmarkName, "activity-1", "node-1", null, DateTimeOffset.UnixEpoch, payload, null),
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase));
}
