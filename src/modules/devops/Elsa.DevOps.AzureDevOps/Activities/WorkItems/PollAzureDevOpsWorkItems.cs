using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.Activities.WorkItems;

/// <summary>
/// Polls Azure DevOps work items and dispatches matching trigger stimuli.
/// </summary>
[Activity(
    "Elsa.AzureDevOps.WorkItems",
    "Azure DevOps Work Items",
    "Polls Azure DevOps work item changes and dispatches matching triggers.",
    DisplayName = "Poll Work Item Changes")]
[UsedImplicitly]
public class PollAzureDevOpsWorkItems : AzureDevOpsPollActivity
{
    public const string LastSeenVariableName = "AzureDevOpsPollingLastSeen";
    public const string DeletedIdsVariableName = "AzureDevOpsDeletedWorkItemIds";

    /// <inheritdoc cref="WorkItemPollingOptions.IncludeDeleted"/>
    [Input(Description = "Whether this poll also reads the recycle bin to report deleted work items. Falls back to the configured setting (AzureDevOps:Polling:WorkItems:IncludeDeleted) when left empty. Switch it off where the polling token may not read the recycle bin; the other three work item events keep working.")]
    public Input<bool?> IncludeDeleted { get; set; } = null!;

    protected override async ValueTask<IReadOnlyList<AzureDevOpsPollingEvent>> PollAsync(ActivityExecutionContext context)
    {
        Services.AzureDevOpsWorkItemPollingDispatcher dispatcher = context.GetRequiredService<Services.AzureDevOpsWorkItemPollingDispatcher>();
        WorkItemPollingOptions pollingOptions = context.GetRequiredService<IOptions<AzureDevOpsPollingOptions>>().Value.WorkItems
            .With(new WorkItemPollingOverrides(GetSharedOverrides(context))
            {
                IncludeDeleted = context.Get(IncludeDeleted),
            });
        DateTimeOffset checkpoint = context.GetVariable<DateTimeOffset?>(LastSeenVariableName)
            ?? DateTimeOffset.UtcNow.Subtract(pollingOptions.LookbackWindow);
        AzureDevOpsPollingResult result = await dispatcher.DispatchAsync(pollingOptions, checkpoint, context.CancellationToken).ConfigureAwait(false);
        context.SetVariable(LastSeenVariableName, result.LastSeen);

        // A separate call with a separate baseline: deleted work items are invisible to the WIQL query above, and
        // their checkpoint is a set of IDs rather than a timestamp.
        List<int>? seenIds = context.GetVariable<List<int>>(DeletedIdsVariableName);
        AzureDevOpsDeletedPollingResult deleted = await dispatcher.DispatchDeletedAsync(pollingOptions, seenIds, context.CancellationToken).ConfigureAwait(false);

        // Null means this poll never read the bin, so the next poll must still treat itself as the first one rather
        // than being handed an empty baseline that would make it replay the bin's entire contents.
        context.SetVariable(DeletedIdsVariableName, deleted.SeenIds?.ToList());

        return [.. result.Events, .. deleted.Events];
    }
}
