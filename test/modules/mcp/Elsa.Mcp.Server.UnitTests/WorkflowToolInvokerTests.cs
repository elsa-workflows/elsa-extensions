using System.Text.Json;
using Elsa.Bookmarks.Ui.Models;
using Elsa.Bookmarks.Ui.Services;
using Elsa.Mcp.Server.Configuration;
using Elsa.Mcp.Server.Services;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

namespace Elsa.Mcp.Server.UnitTests;

public class WorkflowToolInvokerTests
{
    private static readonly DateTimeOffset Moment = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ResumeAsync_RefusesABookmarkWhoseViewOffersNoSchemaAndNeverReachesTheRuntime()
    {
        Bookmark bookmark = CreateBookmark();
        WorkflowState state = CreateState(bookmark);

        IWorkflowClient client = CreateClient(state);
        IWorkflowRuntime runtime = CreateRuntime(client);
        IWorkflowInstanceStore instanceStore = CreateInstanceStore();
        IBookmarkUiMapper mapper = Substitute.For<IBookmarkUiMapper>();

        // No Resume schema means nobody described how to answer this bookmark - a delay, most of the time. The
        // invoker must refuse rather than guess, and must not have let the call reach the runtime on the way there.
        mapper.DescribeWithProviderAsync(Arg.Any<BookmarkUiContext>(), Arg.Any<CancellationToken>())
            .Returns(new BookmarkUiDescription(
                new BookmarkUiView { Kind = BookmarkUiKinds.Wait, Title = "Approve", Text = "Wacht op goedkeuring." },
                Substitute.For<IBookmarkUiProvider>()));

        WorkflowToolInvoker invoker = CreateInvoker(runtime, instanceStore, mapper);
        CallToolRequestParams request = CreateResumeRequest();

        CallToolResult result = await invoker.InvokeAsync(request, "TestWorkflow", null, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("cannot be answered", GetMessage(result), StringComparison.Ordinal);

        // The whole point of validating before running: a bookmark nobody can answer must never be resumed.
        client.DidNotReceive().RunInstanceAsync(Arg.Any<RunWorkflowInstanceRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResumeAsync_RefusesAnswersThatFailValidationAndNeverReachesTheRuntime()
    {
        Bookmark bookmark = CreateBookmark();
        WorkflowState state = CreateState(bookmark);

        IWorkflowClient client = CreateClient(state);
        IWorkflowRuntime runtime = CreateRuntime(client);
        IWorkflowInstanceStore instanceStore = CreateInstanceStore();
        IBookmarkUiMapper mapper = Substitute.For<IBookmarkUiMapper>();

        BookmarkResumeSchema schema = new(
            "Ask for a reason.",
            [new BookmarkResumeField("reason", BookmarkFieldType.Text, "Reason", Required: true)]);
        mapper.DescribeWithProviderAsync(Arg.Any<BookmarkUiContext>(), Arg.Any<CancellationToken>())
            .Returns(new BookmarkUiDescription(
                new BookmarkUiView { Kind = BookmarkUiKinds.Form, Title = "Approve", Text = "Ask for a reason.", Resume = schema },
                Substitute.For<IBookmarkUiProvider>()));

        WorkflowToolInvoker invoker = CreateInvoker(runtime, instanceStore, mapper);

        // No answersJson at all, so the required 'reason' field is missing - the validator must reject this before
        // anything reaches the workflow, with its own sentence surfaced to the caller rather than a generic failure.
        CallToolRequestParams request = CreateResumeRequest();

        CallToolResult result = await invoker.InvokeAsync(request, "TestWorkflow", null, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("'reason'", GetMessage(result), StringComparison.Ordinal);
        Assert.Contains("is required", GetMessage(result), StringComparison.Ordinal);

        client.DidNotReceive().RunInstanceAsync(Arg.Any<RunWorkflowInstanceRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResumeAsync_PassesTheRevalidatedValuesRatherThanTheCallersRawAnswers()
    {
        Bookmark bookmark = CreateBookmark();
        WorkflowState preRunState = CreateState(bookmark);
        WorkflowState postRunState = new() { Id = "instance-1", DefinitionId = "TestWorkflow" };

        IWorkflowClient client = Substitute.For<IWorkflowClient>();
        client.WorkflowInstanceId.Returns("instance-1");

        // The pre-run export is what the resume check reads the open bookmark from; the post-run export is what
        // ToCallToolResultAsync reports back, by which point the bookmark this test resumes is gone.
        client.ExportStateAsync(Arg.Any<CancellationToken>()).Returns(preRunState, postRunState);

        RunWorkflowInstanceRequest? capturedRequest = null;
        client.RunInstanceAsync(Arg.Any<RunWorkflowInstanceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new RunWorkflowInstanceResponse { WorkflowInstanceId = "instance-1", Status = WorkflowStatus.Running, SubStatus = WorkflowSubStatus.Suspended })
            .AndDoes(call => capturedRequest = call.Arg<RunWorkflowInstanceRequest>());

        IWorkflowRuntime runtime = CreateRuntime(client);
        IWorkflowInstanceStore instanceStore = CreateInstanceStore();
        IBookmarkUiMapper mapper = Substitute.For<IBookmarkUiMapper>();

        BookmarkResumeSchema schema = new(
            "Ask for a count.",
            [new BookmarkResumeField("count", BookmarkFieldType.Number, "Count", Required: true)]);
        mapper.DescribeWithProviderAsync(Arg.Any<BookmarkUiContext>(), Arg.Any<CancellationToken>())
            .Returns(new BookmarkUiDescription(
                new BookmarkUiView { Kind = BookmarkUiKinds.Form, Title = "Approve", Text = "Ask for a number.", Resume = schema },
                Substitute.For<IBookmarkUiProvider>()));

        WorkflowToolInvoker invoker = CreateInvoker(runtime, instanceStore, mapper);

        // The caller sends '5' as a JSON string, the shape a language model tends to send. The schema declares
        // 'count' as a Number, so the validator converts it - what reaches the runtime must be that converted value,
        // not the caller's original string, or the workflow would see a different type than its input declares.
        const string rawAnswersJson = "{\"count\":\"5\"}";
        CallToolRequestParams request = CreateResumeRequest(rawAnswersJson);

        await invoker.InvokeAsync(request, "TestWorkflow", null, CancellationToken.None);

        Assert.NotNull(capturedRequest);
        string sentAnswersJson = Assert.IsType<string>(capturedRequest!.Input![RunTask.InputKey]);

        Assert.NotEqual(rawAnswersJson, sentAnswersJson);

        JsonElement sentCount = JsonDocument.Parse(sentAnswersJson).RootElement.GetProperty("count");
        Assert.Equal(JsonValueKind.Number, sentCount.ValueKind);
        Assert.Equal(5, sentCount.GetDouble());
    }

    [Fact]
    public async Task ResumeAsync_LetsTheProvidersOwnWriterDecideTheShapeThatReachesTheActivity()
    {
        const string writtenJson = "[{\"Name\":\"count\",\"Value\":5}]";

        IWorkflowClient client = CreateCapturingClient(out Func<RunWorkflowInstanceRequest?> captured);
        IBookmarkUiMapper mapper = Substitute.For<IBookmarkUiMapper>();

        // A provider that implements IBookmarkResumeWriter knows something the schema does not: the waiting activity
        // reads an array of questions, not a map of field names. Serialising the validated values as a flat object
        // faults DataEntry-backed activities with an unhandled JsonException, so the writer has to win here.
        IBookmarkUiProvider provider = Substitute.For<IBookmarkUiProvider, IBookmarkResumeWriter>();
        ((IBookmarkResumeWriter)provider).WriteAnswers(Arg.Any<BookmarkUiContext>(), Arg.Any<IReadOnlyDictionary<string, object?>>())
            .Returns(writtenJson);

        BookmarkResumeSchema schema = new(
            "Ask for a count.",
            [new BookmarkResumeField("count", BookmarkFieldType.Number, "Count", Required: true)]);
        mapper.DescribeWithProviderAsync(Arg.Any<BookmarkUiContext>(), Arg.Any<CancellationToken>())
            .Returns(new BookmarkUiDescription(
                new BookmarkUiView { Kind = BookmarkUiKinds.Form, Title = "Approve", Text = "Ask for a number.", Resume = schema },
                provider));

        WorkflowToolInvoker invoker = CreateInvoker(CreateRuntime(client), CreateInstanceStore(), mapper);

        await invoker.InvokeAsync(CreateResumeRequest("{\"count\":5}"), "TestWorkflow", null, CancellationToken.None);

        Assert.Equal(writtenJson, captured()!.Input![RunTask.InputKey]);
    }

    [Fact]
    public async Task ResumeAsync_ReportsTheValidatorsNonFatalMessagesOnASuccessfulResume()
    {
        IWorkflowClient client = CreateCapturingClient(out Func<RunWorkflowInstanceRequest?> _);
        IBookmarkUiMapper mapper = Substitute.For<IBookmarkUiMapper>();

        BookmarkResumeSchema schema = new(
            "Ask for a reason.",
            [new BookmarkResumeField("reason", BookmarkFieldType.Text, "Reason")]);
        mapper.DescribeWithProviderAsync(Arg.Any<BookmarkUiContext>(), Arg.Any<CancellationToken>())
            .Returns(new BookmarkUiDescription(
                new BookmarkUiView { Kind = BookmarkUiKinds.Form, Title = "Approve", Text = "Ask for a reason.", Resume = schema },
                Substitute.For<IBookmarkUiProvider>()));

        WorkflowToolInvoker invoker = CreateInvoker(CreateRuntime(client), CreateInstanceStore(), mapper);

        // 'note' is not a field of this task, so the validator drops it and says so without failing the resume. A
        // caller that never hears that sentence believes it answered something it did not - which is the whole reason
        // the design chose "drop it with a sentence" over silently passing it through.
        CallToolResult result = await invoker.InvokeAsync(
            CreateResumeRequest("{\"reason\":\"because\",\"note\":\"something extra\"}"),
            "TestWorkflow",
            null,
            CancellationToken.None);

        Assert.False(result.IsError);

        JsonElement notes = result.StructuredContent!.Value.GetProperty("notes");

        Assert.Contains(notes.EnumerateArray(), note => note.GetString()!.Contains("'note'", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InvokeAsync_SkipsDescribingBookmarksForAFinishedRunEvenWhenTheStateStillCarriesOne()
    {
        // A finished run's bookmarks are burnt; the state reflecting one anyway (this test forces that edge case)
        // must not make the invoker ask a provider about it - the caller has nothing left to resume.
        WorkflowState state = new()
        {
            Id = "instance-1",
            Bookmarks = [CreateBookmark()]
        };

        IWorkflowClient client = Substitute.For<IWorkflowClient>();
        client.WorkflowInstanceId.Returns("instance-1");
        client.ExportStateAsync(Arg.Any<CancellationToken>()).Returns(state);
        client.CreateInstanceAsync(Arg.Any<CreateWorkflowInstanceRequest>(), Arg.Any<CancellationToken>()).Returns(new CreateWorkflowInstanceResponse());
        client.RunInstanceAsync(Arg.Any<RunWorkflowInstanceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new RunWorkflowInstanceResponse { WorkflowInstanceId = "instance-1", Status = WorkflowStatus.Finished, SubStatus = WorkflowSubStatus.Finished });

        IWorkflowRuntime runtime = Substitute.For<IWorkflowRuntime>();
        runtime.CreateClientAsync(Arg.Any<CancellationToken>()).Returns(client);

        IBookmarkUiMapper mapper = Substitute.For<IBookmarkUiMapper>();
        WorkflowToolInvoker invoker = CreateInvoker(runtime, Substitute.For<IWorkflowInstanceStore>(), mapper);

        CallToolRequestParams request = new() { Name = "TestWorkflow", Arguments = new Dictionary<string, JsonElement>() };

        await invoker.InvokeAsync(request, "TestWorkflow", null, CancellationToken.None);

        mapper.DidNotReceive().DescribeAsync(Arg.Any<BookmarkUiContext>(), Arg.Any<CancellationToken>());
    }

    private static WorkflowToolInvoker CreateInvoker(IWorkflowRuntime runtime, IWorkflowInstanceStore instanceStore, IBookmarkUiMapper mapper) =>
        new(
            runtime,
            Substitute.For<IWorkflowDispatcher>(),
            instanceStore,
            Substitute.For<IHttpContextAccessor>(),
            Options.Create(new McpOptions()),
            Substitute.For<ILogger<WorkflowToolInvoker>>(),
            new BookmarkDescriptionFactory(mapper),
            mapper);

    /// <summary>
    /// A client whose pre-run export carries the open bookmark and whose post-run export no longer does, with the
    /// resume request it was called with reachable through <paramref name="captured"/>.
    /// </summary>
    private static IWorkflowClient CreateCapturingClient(out Func<RunWorkflowInstanceRequest?> captured)
    {
        WorkflowState preRunState = CreateState(CreateBookmark());
        WorkflowState postRunState = new() { Id = "instance-1", DefinitionId = "TestWorkflow" };

        IWorkflowClient client = Substitute.For<IWorkflowClient>();
        client.WorkflowInstanceId.Returns("instance-1");
        client.ExportStateAsync(Arg.Any<CancellationToken>()).Returns(preRunState, postRunState);

        RunWorkflowInstanceRequest? capturedRequest = null;
        client.RunInstanceAsync(Arg.Any<RunWorkflowInstanceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new RunWorkflowInstanceResponse { WorkflowInstanceId = "instance-1", Status = WorkflowStatus.Running, SubStatus = WorkflowSubStatus.Suspended })
            .AndDoes(call => capturedRequest = call.Arg<RunWorkflowInstanceRequest>());

        captured = () => capturedRequest;

        return client;
    }

    private static IWorkflowClient CreateClient(WorkflowState state)
    {
        IWorkflowClient client = Substitute.For<IWorkflowClient>();
        client.WorkflowInstanceId.Returns("instance-1");
        client.ExportStateAsync(Arg.Any<CancellationToken>()).Returns(state);

        return client;
    }

    private static IWorkflowRuntime CreateRuntime(IWorkflowClient client)
    {
        IWorkflowRuntime runtime = Substitute.For<IWorkflowRuntime>();
        runtime.CreateClientAsync("instance-1", Arg.Any<CancellationToken>()).Returns(client);

        return runtime;
    }

    private static IWorkflowInstanceStore CreateInstanceStore()
    {
        IWorkflowInstanceStore instanceStore = Substitute.For<IWorkflowInstanceStore>();
        instanceStore.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new WorkflowInstanceSummary { Id = "instance-1", DefinitionId = "TestWorkflow" } });

        return instanceStore;
    }

    private static Bookmark CreateBookmark() =>
        new()
        {
            Id = "bookmark-1",
            Name = "Approve",
            ActivityId = "activity-1",
            ActivityNodeId = "node-1",
            ActivityInstanceId = "context-1",
            CreatedAt = Moment
        };

    private static WorkflowState CreateState(Bookmark bookmark) =>
        new()
        {
            Id = "instance-1",
            DefinitionId = "TestWorkflow",
            Bookmarks = [bookmark]
        };

    private static CallToolRequestParams CreateResumeRequest(string? answersJson = null)
    {
        Dictionary<string, JsonElement> arguments = new(StringComparer.OrdinalIgnoreCase)
        {
            ["workflowInstanceId"] = JsonSerializer.SerializeToElement("instance-1"),
            ["bookmarkId"] = JsonSerializer.SerializeToElement("bookmark-1")
        };

        if (answersJson != null)
            arguments["answersJson"] = JsonSerializer.SerializeToElement(answersJson);

        return new CallToolRequestParams { Name = "TestWorkflow", Arguments = arguments };
    }

    private static string GetMessage(CallToolResult result) => result.StructuredContent!.Value.GetProperty("message").GetString()!;
}
