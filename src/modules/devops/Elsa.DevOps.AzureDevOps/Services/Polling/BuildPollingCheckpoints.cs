namespace Elsa.DevOps.AzureDevOps.Services.Polling;

/// <summary>
/// How far build polling has progressed. Each build event has its own checkpoint: a build that queues in one interval
/// and finishes several later would otherwise drag a single checkpoint past builds that were never reported.
/// </summary>
/// <remarks>
/// A settable class rather than a record, because it is stored in a workflow variable and so has to survive a
/// serialization round-trip through whichever storage driver the workflow uses.
/// </remarks>
public class BuildPollingCheckpoints
{
    public DateTimeOffset LastQueued { get; set; }

    public DateTimeOffset LastStarted { get; set; }

    public DateTimeOffset LastFinished { get; set; }
}
