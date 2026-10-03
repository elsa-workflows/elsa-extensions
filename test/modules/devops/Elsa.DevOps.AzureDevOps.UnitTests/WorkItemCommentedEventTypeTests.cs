using Elsa.DevOps.AzureDevOps.Bookmarks;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.DevOps.AzureDevOps.Triggers;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// Azure DevOps labels the subscription "Work item commented on" and then delivers <c>workitem.commented</c>. This
/// package indexes the event as <c>workitem.commentedOn</c>, so every real comment delivery arrived under a name
/// nothing here recognised: the handler threw, the endpoint answered 500, and Azure DevOps counted a failed delivery
/// towards disabling the subscription. Polling never showed it, because there the same constant writes both the
/// bookmark and the trigger and the two agree by construction.
/// </summary>
public class WorkItemCommentedEventTypeTests
{
    private const string Project = "Contoso";

    private static WorkItem CommentedWorkItem() => new()
    {
        Id = 41290,
        Fields = new Dictionary<string, object> { ["System.WorkItemType"] = "Bug" },
    };

    [Fact]
    public void NormalizesTheNameAzureDevOpsActuallySends()
    {
        Assert.Equal(AzureDevOpsWebhookEventTypes.WorkItemCommented, AzureDevOpsWebhookEventTypes.Normalize(AzureDevOpsWebhookEventTypes.WorkItemCommentedDelivered));

        // Everything else passes through untouched; an alias table that starts rewriting the names that were already
        // right would break the three work item events that never had this problem.
        Assert.Equal(AzureDevOpsWebhookEventTypes.WorkItemUpdated, AzureDevOpsWebhookEventTypes.Normalize(AzureDevOpsWebhookEventTypes.WorkItemUpdated));
        Assert.Equal("build.complete", AzureDevOpsWebhookEventTypes.Normalize("build.complete"));
    }

    [Fact]
    public async Task DispatchesACommentDeliveryUnderTheNameTheTriggerIsIndexedWith()
    {
        IStimulusSender stimulusSender = Substitute.For<IStimulusSender>();
        AzureDevOpsWebhookEventHandler handler = new(stimulusSender, NullLogger<AzureDevOpsWebhookEventHandler>.Instance);

        await handler.HandleAsync(
            new AzureDevOpsWebhookEvent(AzureDevOpsWebhookEventTypes.WorkItemCommentedDelivered, CommentedWorkItem(), Project),
            CancellationToken.None);

        await stimulusSender.Received(1).SendAsync(
            ActivityTypeNameHelper.GenerateTypeName(typeof(WorkItemCommentedTrigger)),
            new AzureDevOpsWebhookBookmark(AzureDevOpsWebhookEventTypes.WorkItemCommented, Project),
            Arg.Any<StimulusMetadata>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandsTheWorkflowTheNameItsTriggerComparesAgainst()
    {
        IStimulusSender stimulusSender = Substitute.For<IStimulusSender>();
        AzureDevOpsWebhookEventHandler handler = new(stimulusSender, NullLogger<AzureDevOpsWebhookEventHandler>.Instance);

        await handler.HandleAsync(
            new AzureDevOpsWebhookEvent(AzureDevOpsWebhookEventTypes.WorkItemCommentedDelivered, CommentedWorkItem(), Project),
            CancellationToken.None);

        // Normalizing the lookup but not the event itself would be the worse bug of the two: the stimulus reaches the
        // trigger, the trigger compares the event to its own name, does not recognise it, and suspends the instance
        // that the stimulus had just resumed - no error anywhere, and a workflow that never continues.
        await stimulusSender.Received().SendAsync(
            Arg.Any<string>(),
            Arg.Any<object>(),
            Arg.Is<StimulusMetadata>(metadata => CarriesTheCanonicalEventType(metadata)),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A method rather than an inline lambda, because the body needs an <c>is</c> pattern and reads better named than
    /// spelled out inside the matcher.
    /// </summary>
    private static bool CarriesTheCanonicalEventType(StimulusMetadata metadata) =>
        metadata.Input != null
        && metadata.Input.TryGetValue("Message", out object? value)
        && value is AzureDevOpsWebhookEvent message
        && message.EventType == AzureDevOpsWebhookEventTypes.WorkItemCommented;

    [Fact]
    public async Task StillRefusesAnEventTypeThatIsGenuinelyUnknown()
    {
        IStimulusSender stimulusSender = Substitute.For<IStimulusSender>();
        AzureDevOpsWebhookEventHandler handler = new(stimulusSender, NullLogger<AzureDevOpsWebhookEventHandler>.Instance);

        // The alias table must not turn into a shrug. An event nothing here handles - workitem.restored, say - should
        // still be loud, because a subscription for it is a configuration mistake worth seeing.
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(
                new AzureDevOpsWebhookEvent("workitem.restored", CommentedWorkItem(), Project),
                CancellationToken.None));

        Assert.Contains("workitem.restored", exception.Message, StringComparison.Ordinal);
    }
}
