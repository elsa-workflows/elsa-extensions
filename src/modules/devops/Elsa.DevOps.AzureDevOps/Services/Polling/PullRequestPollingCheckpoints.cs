namespace Elsa.DevOps.AzureDevOps.Services.Polling;

/// <summary>
/// How far pull request polling has progressed. Creation and closure are queried separately — the API filters on one
/// time range at a time — and so each carries its own checkpoint.
/// </summary>
/// <remarks>
/// A settable class rather than a record, because it is stored in a workflow variable and so has to survive a
/// serialization round-trip through whichever storage driver the workflow uses.
/// </remarks>
public class PullRequestPollingCheckpoints
{
    public DateTimeOffset LastCreated { get; set; }

    public DateTimeOffset LastClosed { get; set; }
}
