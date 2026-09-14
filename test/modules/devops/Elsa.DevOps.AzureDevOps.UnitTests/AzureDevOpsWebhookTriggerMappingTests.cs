using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// The handler dispatches from a table of event type to trigger, and that table also supplies the trigger names the
/// diagnostics ask the store about. An event type the package advertises but the table does not hold is an event that
/// arrives, is refused, and answers 500 to Azure DevOps - which is how the comment event went unnoticed for as long as
/// it did.
/// </summary>
public class AzureDevOpsWebhookTriggerMappingTests
{
    private const string Project = "Contoso";

    [Fact]
    public async Task DispatchesEveryEventTypeThePackageAdvertises()
    {
        foreach (string eventType in AzureDevOpsWebhookEventTypes.All)
        {
            IStimulusSender stimulusSender = Substitute.For<IStimulusSender>();
            AzureDevOpsWebhookEventHandler handler = new(stimulusSender, NullLogger<AzureDevOpsWebhookEventHandler>.Instance);

            // Throws InvalidOperationException naming the event type when the table has no entry for it, which is the
            // failure this covers; the assertion below then catches an entry that maps to nothing being sent.
            await handler.HandleAsync(
                new AzureDevOpsWebhookEvent(eventType, new WorkItem { Id = 1 }, Project),
                CancellationToken.None);

            Assert.NotEmpty(stimulusSender.ReceivedCalls());
        }
    }

    [Fact]
    public async Task DispatchesTheDeliveredCommentNameThatIsNotOnThatList()
    {
        // AzureDevOpsWebhookEventTypes.All carries the names this package indexes by, so the name Azure DevOps
        // actually delivers a comment under is deliberately absent from it and has to be covered separately.
        IStimulusSender stimulusSender = Substitute.For<IStimulusSender>();
        AzureDevOpsWebhookEventHandler handler = new(stimulusSender, NullLogger<AzureDevOpsWebhookEventHandler>.Instance);

        await handler.HandleAsync(
            new AzureDevOpsWebhookEvent(AzureDevOpsWebhookEventTypes.WorkItemCommentedDelivered, new WorkItem { Id = 1 }, Project),
            CancellationToken.None);

        Assert.NotEmpty(stimulusSender.ReceivedCalls());
    }
}
