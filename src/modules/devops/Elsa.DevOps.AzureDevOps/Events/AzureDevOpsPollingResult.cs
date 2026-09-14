namespace Elsa.DevOps.AzureDevOps.Events;

public sealed record AzureDevOpsPollingResult(
    IReadOnlyList<AzureDevOpsPollingEvent> Events,
    DateTimeOffset LastSeen);
