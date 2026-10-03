using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.Activities;

/// <summary>
/// The shape every Azure DevOps poll activity has: run the family's poll, then journal and log what it dispatched.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not generic over a checkpoint type. Each family carries different checkpoint state — work items hold
/// a timestamp and a set of recycle bin IDs, pushes hold one timestamp per repository — and a shared checkpoint type
/// would have forced the work item family onto a new variable shape, resetting the checkpoints of instances already
/// running in production.
/// </para>
/// <para>
/// The inputs exist so a workflow of your own can poll something the configured family does not: another project,
/// another organization, another set of repositories. Every one of them is optional and falls back to configuration,
/// which is what keeps the built-in polling workflows — they fill in nothing — behaving exactly as before.
/// </para>
/// </remarks>
public abstract class AzureDevOpsPollActivity : CodeActivity
{
    /// <summary>
    /// Description shared by the <c>Project</c> input of every poll activity.
    /// </summary>
    public const string ProjectDescription = "The project to poll. Falls back to the project configured for this family (AzureDevOps:Polling:<family>:Project) and then to the default project (AzureDevOps:DefaultProject) when left empty.";

    /// <summary>
    /// Description shared by the <c>OrganizationUrl</c> input of every poll activity.
    /// </summary>
    public const string OrganizationUrlDescription = "The Azure DevOps organization URL to poll (e.g. https://dev.azure.com/myorg). Falls back to the organization configured for this family (AzureDevOps:Polling:<family>:OrganizationUrl) and then to the default organization URL (AzureDevOps:DefaultOrganizationUrl) when left empty.";

    /// <summary>
    /// Description shared by the <c>Token</c> input of every poll activity.
    /// </summary>
    public const string TokenDescription = "The personal access token (PAT) to poll with. Prefer TokenSecretName, which keeps the token out of the workflow definition. Falls back to the credential configured for this family (AzureDevOps:Polling:<family>:Token or TokenSecretName), then to the one every family shares (AzureDevOps:Polling:Token or TokenSecretName), and then to the default token when left empty.";

    /// <summary>
    /// Description shared by the <c>TokenSecretName</c> input of every poll activity.
    /// </summary>
    public const string TokenSecretNameDescription = "The name of the Elsa Secret holding the personal access token to poll with. Takes the place of the credential configured for this family, so the family's own token is not used beside it; a secret that holds no value still falls back to the credential every family shares (AzureDevOps:Polling:Token or TokenSecretName) and then to the default token.";

    /// <summary>
    /// Description shared by the <c>Top</c> input of every poll activity.
    /// </summary>
    public const string TopDescription = "How many items one poll reads per page. Falls back to the page size configured for this family when left empty or not positive.";

    /// <summary>
    /// Description shared by the <c>LookbackWindow</c> input of every poll activity.
    /// </summary>
    public const string LookbackWindowDescription = "How far back the first poll of this workflow instance looks; later polls continue from their checkpoint. Falls back to the window configured for this family when left empty or not positive.";

    /// <inheritdoc cref="ProjectDescription"/>
    [Input(Description = ProjectDescription)]
    public Input<string?> Project { get; set; } = null!;

    /// <inheritdoc cref="OrganizationUrlDescription"/>
    [Input(Description = OrganizationUrlDescription)]
    public Input<string?> OrganizationUrl { get; set; } = null!;

    /// <inheritdoc cref="TokenDescription"/>
    [Input(Description = TokenDescription)]
    public Input<string?> Token { get; set; } = null!;

    /// <inheritdoc cref="TokenSecretNameDescription"/>
    [Input(Description = TokenSecretNameDescription)]
    public Input<string?> TokenSecretName { get; set; } = null!;

    /// <inheritdoc cref="TopDescription"/>
    [Input(Description = TopDescription)]
    public Input<int?> Top { get; set; } = null!;

    /// <inheritdoc cref="LookbackWindowDescription"/>
    [Input(Description = LookbackWindowDescription)]
    public Input<TimeSpan?> LookbackWindow { get; set; } = null!;

    /// <summary>
    /// Reads the inputs every family shares. A family hands the result to the copy constructor of its own overrides
    /// record, so it only has to name the inputs that are its own.
    /// </summary>
    protected PollingOverrides GetSharedOverrides(ActivityExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new PollingOverrides
        {
            Project = context.Get(Project),
            OrganizationUrl = context.Get(OrganizationUrl),
            Token = context.Get(Token),
            TokenSecretName = context.Get(TokenSecretName),
            Top = context.Get(Top),
            LookbackWindow = context.Get(LookbackWindow),
        };
    }

    /// <summary>
    /// Runs one poll, reading and writing this family's checkpoint variables on the context.
    /// </summary>
    /// <returns>The events the poll dispatched, for the journal and the execution log.</returns>
    protected abstract ValueTask<IReadOnlyList<AzureDevOpsPollingEvent>> PollAsync(ActivityExecutionContext context);

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        IReadOnlyList<AzureDevOpsPollingEvent> events = await PollAsync(context).ConfigureAwait(false);

        // Copied into a plain list rather than journalled as it arrives: the journal records this value's runtime type
        // and builds that type again when the entry is read back, so it cannot be handed whichever implementation of
        // the return type a poll happens to produce. A collection expression - what a poll combining two sources
        // naturally returns - lowers to a synthesised read-only array that has no parameterless constructor, and
        // reading the entry back then fails.
        List<AzureDevOpsPollingEvent> journalled = [.. events];
        context.JournalData["Events"] = journalled;

        ReportThatThePollRan(context, events.Count);

        foreach (AzureDevOpsPollingEvent @event in events)
        {
            context.AddExecutionLogEntry(
                eventName: "AzureDevOpsPollingEvent",
                message: $"Triggered {@event.EventType} for {@event.Description} in project {@event.Project}.",
                source: GetType().Name,
                payload: @event);
        }

        await context.CompleteActivityAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Reports that this poll ran, whether or not it found anything.
    /// </summary>
    /// <remarks>
    /// A poll that dispatched nothing is otherwise indistinguishable from one that never happened: the master switch,
    /// the family switch and a polling workflow that was never started all end in the same silence, and none of them
    /// logs a thing on the way there. The level comes from configuration because the workflow server exports nothing
    /// below <see cref="LogLevel.Warning"/> to Application Insights, so the level a line has to be written at to leave
    /// the process is a deployment's decision rather than this activity's.
    /// </remarks>
    private void ReportThatThePollRan(ActivityExecutionContext context, int dispatched)
    {
        LogLevel level = context.GetRequiredService<IOptions<AzureDevOpsPollingOptions>>().Value.Diagnostics.Level;

        if (level == LogLevel.None)
            return;

        string poll = GetType().Name;

        // The journal carries no severity of its own, so the level travels as the entry's name - the way the rest of
        // this codebase writes its "Warning" and "Info" entries, and what makes the line findable in Studio.
        context.AddExecutionLogEntry(
            eventName: level.ToString(),
            message: $"{poll} ran and dispatched {dispatched} event(s).",
            source: poll);

        context.GetRequiredService<ILoggerFactory>()
            .CreateLogger(GetType())
            .Log(level, "Azure DevOps poll {Poll} ran and dispatched {DispatchedEvents} event(s).", poll, dispatched);
    }
}
