using Microsoft.Extensions.Logging;

namespace Elsa.DevOps.AzureDevOps.Configuration;

/// <summary>
/// Diagnostics for the polling workflows, for working out whether a poll ran at all. A poll that found nothing leaves
/// exactly as little behind as a poll that never happened: the master switch, the family switch and a polling workflow
/// that was never started all end in the same silence, and none of the three logs a thing on the way there.
/// </summary>
public class AzureDevOpsPollingDiagnosticsOptions
{
    /// <summary>
    /// The level the line reporting a poll is written at, and the name it carries in the journal.
    /// </summary>
    /// <remarks>
    /// A level rather than a boolean for the same reason the Service Hook diagnostics take one: the level is what
    /// decides whether the line survives the host's log filter, and the workflow server exports nothing below
    /// <see cref="LogLevel.Warning"/> to Application Insights. <see cref="LogLevel.None"/> writes nothing at all, so a
    /// deployment can have its journal back without a code change.
    /// </remarks>
    public LogLevel Level { get; set; } = LogLevel.Debug;
}
