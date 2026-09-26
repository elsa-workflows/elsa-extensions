namespace Elsa.DevOps.AzureDevOps.Events;

/// <summary>
/// Represents an event received from an Azure DevOps Service Hook.
/// </summary>
/// <param name="Comment">
/// The comment behind a <c>workitem.commentedOn</c> event, when the source knew it. The poller fills this in because
/// it has already read the comment through the API; a Service Hook does not, and the comment is recovered from the
/// payload instead. Always <c>null</c> for every other event type.
/// </param>
public sealed record AzureDevOpsWebhookEvent(
    string EventType,
    object? Payload,
    string? ProjectId = null,
    string? ProjectName = null,
    string? ResourceVersion = null,
    string? PublisherId = null,
    PostedComment? Comment = null);
