using Microsoft.Extensions.Logging;

namespace Elsa.DevOps.AzureDevOps.Configuration;

/// <summary>
/// Temporary diagnostics for the Service Hook endpoint, for working out why a delivery that arrived did not start or
/// resume anything. Off in the sense that matters by default: nothing is written unless <see cref="Level"/> passes the
/// host's log filter, and the delivered message is only included when <see cref="IncludePayloads"/> says so.
/// </summary>
public class AzureDevOpsWebhookDiagnosticsOptions
{
    /// <summary>
    /// The level every diagnostic line is written at.
    /// </summary>
    /// <remarks>
    /// A level rather than a boolean because the level is what decides whether the line survives the host's log
    /// filter, and a deployment can filter far above <see cref="LogLevel.Debug"/> - the workflow server exports
    /// nothing below <see cref="LogLevel.Warning"/> to Application Insights. Raising this to <c>Warning</c> for the
    /// duration of an investigation is what gets these lines out of the process, without moving the filter for
    /// everything else the host logs.
    /// </remarks>
    public LogLevel Level { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Whether the delivered message itself is written to the log, as it arrived.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="Level"/>, and off by default, because a work item payload carries whatever people put
    /// in the work item. Switch it on for as long as an investigation needs it and off again afterwards; the log keeps
    /// what it was given for as long as the log is retained.
    /// </remarks>
    public bool IncludePayloads { get; set; }

    /// <summary>
    /// How much of the delivered message is written before it is cut off. A Service Hook payload with all resource
    /// details runs to tens of kilobytes, and a log sink truncates the whole line rather than the payload in it.
    /// </summary>
    public int MaxPayloadLength { get; set; } = 8000;
}
