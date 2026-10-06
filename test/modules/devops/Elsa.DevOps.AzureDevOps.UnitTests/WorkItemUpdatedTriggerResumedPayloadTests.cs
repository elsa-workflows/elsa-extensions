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
/// The same conversion as <see cref="WorkItemUpdatedTriggerPayloadTests"/>, but on the payload as it comes back rather
/// than as it arrived.
/// </summary>
/// <remarks>
/// <para>
/// A trigger that is already waiting is resumed from a bookmark, and the event reaches it through the instance state
/// rather than in memory: the handler puts the event in the stimulus input, Elsa writes that input into the instance
/// and reads it back before the bookmark callback runs. The event survives that trip - the record is rebuilt - but its
/// <c>Payload</c> does not: it is declared <c>object</c>, and the state serializer rebuilds a JSON object as
/// <c>Dictionary&lt;string, object&gt;</c>, never as the <see cref="JsonElement"/> that went in. See
/// <c>WorkflowStateRoundTripTests</c> in the workflow customizations for the same behaviour stated on its own.
/// </para>
/// <para>
/// Everything the conversion does is keyed off that <see cref="JsonElement"/>, so on the resume path all of it was
/// skipped: the work item was read from the update rather than from its revision, and the update was handed to a
/// serializer that cannot read it, because a <c>workitem.updated</c> resource carries <c>relations</c> as an
/// added/removed/updated object while a work item carries it as a list. That is the exception the workflow host was
/// throwing on every update delivery that changed a relation.
/// </para>
/// </remarks>
public class WorkItemUpdatedTriggerResumedPayloadTests
{
    /// <summary>
    /// Trimmed from a delivery this endpoint actually received. The <c>relations</c> block is the part that matters:
    /// it is an object describing the change, in the property a work item spells as a list.
    /// </summary>
    private const string UpdateWithRelations = """
        {
          "id": 7,
          "workItemId": 36018,
          "rev": 7,
          "revisedBy": { "displayName": "Alice", "uniqueName": "alice@contoso.com" },
          "revisedDate": "2026-08-26T11:42:55.4Z",
          "fields": {
            "System.Rev": { "oldValue": 6, "newValue": 7 },
            "System.ChangedDate": { "oldValue": "2026-08-26T11:40:00Z", "newValue": "2026-08-26T11:42:55Z" }
          },
          "relations": {
            "added": [
              {
                "rel": "AttachedFile",
                "url": "https://dev.azure.com/contoso/_apis/wit/attachments/6f1d2f2f",
                "attributes": { "name": "screenshot.png", "resourceSize": 12345 }
              }
            ]
          },
          "url": "https://dev.azure.com/contoso/_apis/wit/updates/36018/7",
          "revision": {
            "id": 36018,
            "rev": 7,
            "fields": {
              "System.TeamProject": "Contoso",
              "System.WorkItemType": "Task",
              "System.State": "Active",
              "System.Title": "Object reference not set to an instance of an object."
            }
          }
        }
        """;

    /// <summary>What arrives when the subscription sends minimal resource details: the same relations block, no revision.</summary>
    private const string UpdateWithRelationsAndNoRevision = """
        {
          "id": 7,
          "workItemId": 36018,
          "rev": 7,
          "fields": { "System.Rev": { "oldValue": 6, "newValue": 7 } },
          "relations": {
            "removed": [
              {
                "rel": "System.LinkTypes.Related",
                "url": "https://dev.azure.com/contoso/_apis/wit/workItems/36019",
                "attributes": { "id": 91 }
              }
            ]
          }
        }
        """;

    [Fact]
    public async Task ReadsTheWorkItemFromAnUpdateThatCameBackThroughTheInstanceState()
    {
        Captured captured = await RunAsync(await ResumedAsync(UpdateWithRelations));

        Assert.Equal(36018, captured.WorkItem?.Id);
        Assert.Equal("Active", captured.State);
        Assert.Equal("Task", captured.WorkItem?.Fields?["System.WorkItemType"]?.ToString());
    }

    [Fact]
    public async Task ReadsTheWorkItemIdFromAnUpdateThatCameBackWithoutARevision()
    {
        Captured captured = await RunAsync(await ResumedAsync(UpdateWithRelationsAndNoRevision));

        Assert.Equal(36018, captured.WorkItem?.Id);
    }

    [Fact]
    public async Task TheStateSerializerDoesNotHandBackTheJsonElementItWasGiven()
    {
        // The reason the two tests above exist. Stated on its own so the conversion is not "simplified" back to
        // reading the payload as a JsonElement by someone who only sees the in-memory path.
        object payload = await ResumedAsync(UpdateWithRelations);

        Assert.IsNotType<JsonElement>(payload);
    }

    /// <summary>The payload as the bookmark callback receives it: written into the instance state and read back.</summary>
    private static async Task<object> ResumedAsync(string raw)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddElsa();

        await using ServiceProvider provider = services.BuildServiceProvider();
        IWorkflowStateSerializer serializer = provider.GetRequiredService<IWorkflowStateSerializer>();

        AzureDevOpsWebhookEvent sent = new(AzureDevOpsWebhookEventTypes.WorkItemUpdated, Json(raw));
        AzureDevOpsWebhookEvent? received = serializer.Deserialize<AzureDevOpsWebhookEvent>(serializer.Serialize(sent));

        return received?.Payload ?? throw new InvalidOperationException("The event did not survive the state serializer.");
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
