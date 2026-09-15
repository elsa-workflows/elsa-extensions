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
/// A <c>workitem.updated</c> delivery describes the update, not the work item: its <c>id</c> is the revision number,
/// its <c>fields</c> hold only what changed and hold it as oldValue/newValue pairs, and the work item is one level down
/// under <c>revision</c>. Read as-is it yields work item 7 instead of 36018 with an unusable field dictionary, which a
/// workflow only discovers when an activity downstream refuses a work item id of zero.
/// </summary>
public class WorkItemUpdatedTriggerPayloadTests
{
    /// <summary>
    /// Trimmed from a delivery this endpoint actually received, keeping every part the conversion depends on.
    /// </summary>
    private const string UpdatePayload = """
        {
          "id": 7,
          "workItemId": 36018,
          "rev": 7,
          "fields": {
            "System.Rev": { "oldValue": 6, "newValue": 7 },
            "System.History": { "newValue": "@Zara welke tools heb je" }
          },
          "revision": {
            "id": 36018,
            "rev": 7,
            "fields": {
              "System.TeamProject": "Contoso",
              "System.WorkItemType": "Issue",
              "System.State": "New",
              "System.Title": "Object reference not set to an instance of an object."
            }
          }
        }
        """;

    [Fact]
    public async Task TakesTheWorkItemFromTheRevisionRatherThanFromTheUpdate()
    {
        Captured captured = await RunAsync(Json(UpdatePayload));

        // 36018 and not 7. The revision number is the trap: it is a small plausible integer in the field the work item
        // id would occupy, so it fails downstream rather than here.
        Assert.Equal(36018, captured.WorkItem?.Id);
        Assert.Equal("New", captured.State);
        Assert.Equal("Issue", captured.WorkItem?.Fields?["System.WorkItemType"]?.ToString());
        Assert.Same(captured.Result, captured.WorkItem);
    }

    [Fact]
    public async Task TakesTheWorkItemIdFromTheUpdateWhenNoRevisionWasDelivered()
    {
        // What arrives when the subscription sends minimal resource details: no revision to read, and an id that is
        // still the revision number. workItemId is then the only place the work item's own number appears, so it has
        // to overwrite rather than fill in.
        Captured captured = await RunAsync(Json("""{ "id": 7, "workItemId": 36018, "rev": 7 }"""));

        Assert.Equal(36018, captured.WorkItem?.Id);
    }

    [Fact]
    public async Task LeavesAWorkItemThePollerAlreadyReadAlone()
    {
        // The poller hands over a real WorkItem read through the API, so none of the unwrapping above applies to it.
        WorkItem polled = new()
        {
            Id = 36018,
            Fields = new Dictionary<string, object> { ["System.State"] = "Active" },
        };

        Captured captured = await RunAsync(polled);

        Assert.Equal(36018, captured.WorkItem?.Id);
        Assert.Equal("Active", captured.State);
    }

    private static async Task<Captured> RunAsync(object payload)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddElsa();

        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        Variable<WorkItem> result = new();
        Variable<WorkItem> workItem = new();
        Variable<string> state = new();
        Captured captured = new();

        WorkItemUpdatedTrigger trigger = new()
        {
            ProjectId = new Input<string>("Contoso"),
            Result = new Output<WorkItem>(result),
            WorkItem = new Output<WorkItem>(workItem),
            State = new Output<string>(state),
        };

        Sequence sequence = new()
        {
            Variables = { result, workItem, state },
            Activities =
            {
                trigger,
                new Inline(context =>
                {
                    captured.Result = result.Get(context);
                    captured.WorkItem = workItem.Get(context);
                    captured.State = state.Get(context);
                }),
            },
        };

        IWorkflowRunner runner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();

        await runner.RunAsync(sequence, new RunWorkflowOptions
        {
            Input = new Dictionary<string, object>
            {
                ["Message"] = new AzureDevOpsWebhookEvent(AzureDevOpsWebhookEventTypes.WorkItemUpdated, payload),
            },
        });

        return captured;
    }

    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

    private sealed class Captured
    {
        public WorkItem? Result { get; set; }

        public WorkItem? WorkItem { get; set; }

        public string? State { get; set; }
    }
}
