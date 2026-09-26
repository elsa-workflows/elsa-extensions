using System.Text.Json;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Triggers;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// The comment travels alongside the work item rather than replacing it as the result, so that a workflow already
/// bound to the work item keeps working. These run the real trigger to check both halves arrive.
/// </summary>
public class WorkItemCommentedTriggerTests
{
    [Fact]
    public async Task HandsOverTheWorkItemAndTheCommentTogether()
    {
        WorkItem workItem = new()
        {
            Id = 41290,
            Fields = new Dictionary<string, object>
            {
                ["System.WorkItemType"] = "Bug",
                ["System.History"] = "Kun je hier even naar kijken @studio?",
                ["System.ChangedBy"] = "Alice Anderson <alice@contoso.com>",
            },
        };

        Captured captured = await RunAsync(new AzureDevOpsWebhookEvent(AzureDevOpsWebhookEventTypes.WorkItemCommented, workItem));

        // Both names carry the work item. Result comes from the base activity and cannot be renamed without orphaning
        // every workflow definition already bound to it, so it keeps being set; Work Item is the one to bind to.
        Assert.Equal(41290, captured.WorkItem?.Id);
        Assert.Equal(41290, captured.Result?.Id);
        Assert.Same(captured.Result, captured.WorkItem);

        Assert.Equal("Kun je hier even naar kijken @studio?", captured.CommentText);
        Assert.Equal("Alice Anderson", captured.Comment?.Author);
    }

    [Fact]
    public async Task ReadsTheCommentOutOfAServiceHookPayload()
    {
        Captured captured = await RunAsync(new AzureDevOpsWebhookEvent(
            AzureDevOpsWebhookEventTypes.WorkItemCommented,
            Json("""
                {
                  "id": 41290,
                  "fields": {
                    "System.WorkItemType": "Bug",
                    "System.History": "Kun je hier even naar kijken @studio?",
                    "System.ChangedBy": "Alice Anderson <alice@contoso.com>"
                  }
                }
                """)));

        Assert.Equal("Kun je hier even naar kijken @studio?", captured.CommentText);
        Assert.Equal("Alice Anderson", captured.Comment?.Author);
        Assert.Equal("alice@contoso.com", captured.Comment?.AuthorUniqueName);
    }

    [Fact]
    public async Task AWorkItemFromAServiceHookPayloadArrivesWithItsFieldsBound()
    {
        // This was pinned the other way round, as a shape that was broken and known to be: the base trigger read the
        // payload with System.Text.Json's case-sensitive defaults, so the camelCase properties of a Service Hook
        // resource bound to nothing and the workflow was handed a work item with no id and no fields. Nothing failed
        // at the time - the first sign was an activity downstream refusing a work item id of zero, once a delivery
        // finally reached a workflow. The options are case-insensitive now.
        Captured captured = await RunAsync(new AzureDevOpsWebhookEvent(
            AzureDevOpsWebhookEventTypes.WorkItemCommented,
            Json("""{ "id": 41290, "fields": { "System.History": "hoi", "System.State": "Active" } }""")));

        Assert.NotNull(captured.WorkItem);
        Assert.Equal(41290, captured.WorkItem.Id);
        Assert.Equal("Active", captured.WorkItem.Fields?["System.State"]?.ToString());

        // The comment is still read from the raw payload rather than from the bound work item, because that is the
        // only place a Service Hook carries the author alongside the text.
        Assert.Equal("hoi", captured.CommentText);
    }

    [Fact]
    public async Task PrefersTheCommentTheSourceAlreadyReadOverTheOneInThePayload()
    {
        // The poller has read the real comment through the API, so it knows the id and the sign-in name that the
        // payload does not carry.
        WorkItem workItem = new() { Id = 41290, Fields = new Dictionary<string, object> { ["System.History"] = "uit de payload" } };
        PostedComment carried = new("uit de API", 7, "Zara", "zara@contoso.nl", DateTimeOffset.UnixEpoch);

        Captured captured = await RunAsync(new AzureDevOpsWebhookEvent(
            AzureDevOpsWebhookEventTypes.WorkItemCommented, workItem, Comment: carried));

        Assert.Equal("uit de API", captured.CommentText);
        Assert.Equal(7, captured.Comment?.Id);
        Assert.Equal("zara@contoso.nl", captured.Comment?.AuthorUniqueName);
    }

    [Fact]
    public async Task RecoversTheCommentFromThePayloadWhenTheSourceCarriedNone()
    {
        WorkItem workItem = new() { Id = 41290, Fields = new Dictionary<string, object> { ["System.History"] = "uit de payload" } };

        Captured captured = await RunAsync(new AzureDevOpsWebhookEvent(AzureDevOpsWebhookEventTypes.WorkItemCommented, workItem));

        Assert.Equal("uit de payload", captured.CommentText);
    }

    [Fact]
    public async Task HandsOverAnEmptyCommentTextWhenTheEventCarriesNoComment()
    {
        // An empty string rather than nothing, so a flow decision matching on the text does not have to guard for
        // null before it can ask whether it contains anything.
        WorkItem workItem = new() { Id = 41290, Fields = new Dictionary<string, object> { ["System.Title"] = "Iets" } };

        Captured captured = await RunAsync(new AzureDevOpsWebhookEvent(AzureDevOpsWebhookEventTypes.WorkItemCommented, workItem));

        Assert.Equal(string.Empty, captured.CommentText);
        Assert.Null(captured.Comment);
        Assert.Equal(41290, captured.WorkItem?.Id);
    }

    private static async Task<Captured> RunAsync(AzureDevOpsWebhookEvent message)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddElsa();

        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        Variable<WorkItem> result = new();
        Variable<WorkItem> workItem = new();
        Variable<string> commentText = new();
        Variable<PostedComment> comment = new();
        Captured captured = new();

        WorkItemCommentedTrigger trigger = new()
        {
            ProjectId = new Input<string>("Contoso"),
            Result = new Output<WorkItem>(result),
            WorkItem = new Output<WorkItem>(workItem),
            CommentText = new Output<string>(commentText),
            Comment = new Output<PostedComment?>(comment!),
        };

        Sequence sequence = new()
        {
            Variables = { result, workItem, commentText, comment },
            Activities =
            {
                trigger,
                new Inline(context =>
                {
                    captured.Result = result.Get(context);
                    captured.WorkItem = workItem.Get(context);
                    captured.CommentText = commentText.Get(context);
                    captured.Comment = comment.Get(context);
                }),
            },
        };

        IWorkflowRunner runner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();

        // The trigger completes straight away when the run carries the event as input; without it, it would create a
        // bookmark and wait.
        await runner.RunAsync(sequence, new RunWorkflowOptions
        {
            Input = new Dictionary<string, object> { ["Message"] = message },
        });

        return captured;
    }

    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

    private sealed class Captured
    {
        public WorkItem? Result { get; set; }

        public WorkItem? WorkItem { get; set; }

        public string? CommentText { get; set; }

        public PostedComment? Comment { get; set; }
    }
}
