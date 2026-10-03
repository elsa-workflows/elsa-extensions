namespace Elsa.DevOps.AzureDevOps.Events;

/// <summary>
/// Describes an Azure DevOps event dispatched by a polling activity. Kept generic over the families, because it only
/// ever feeds the workflow's execution log and journal.
/// </summary>
/// <param name="EventType">The Azure DevOps event type that was dispatched.</param>
/// <param name="Project">The project the resource belongs to.</param>
/// <param name="ResourceId">The identifier of the resource the event is about, when it has one.</param>
/// <param name="Description">What is written to the execution log, e.g. "work item 42 (Bug)".</param>
public sealed record AzureDevOpsPollingEvent(
    string EventType,
    string Project,
    string? ResourceId,
    string Description);
