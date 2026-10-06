using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services;

using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.Activities.Repositories;

[Activity(
    "Elsa.AzureDevOps.Repositories",
    "Azure DevOps Repositories",
    "Polls Azure Repos pushes and dispatches matching triggers.",
    DisplayName = "Poll Pushes")]
[UsedImplicitly]
public class PollAzureDevOpsPushes : AzureDevOpsPollActivity
{
    public const string CheckpointsVariableName = "AzureDevOpsPushPollingCheckpoints";

    /// <inheritdoc cref="PushPollingOptions.Repositories"/>
    [Input(Description = "The repositories to poll, by name or ID. Falls back to the configured list (AzureDevOps:Polling:Pushes:Repositories) when left empty; an empty configured list means every repository in the project.")]
    public Input<ICollection<string>?> Repositories { get; set; } = null!;

    protected override async ValueTask<IReadOnlyList<AzureDevOpsPollingEvent>> PollAsync(ActivityExecutionContext context)
    {
        AzureDevOpsPushPollingDispatcher dispatcher = context.GetRequiredService<AzureDevOpsPushPollingDispatcher>();
        PushPollingOptions pollingOptions = context.GetRequiredService<IOptions<AzureDevOpsPollingOptions>>().Value.Pushes
            .With(new PushPollingOverrides(GetSharedOverrides(context))
            {
                Repositories = context.Get(Repositories) is { } repositories ? [.. repositories] : null,
            });
        Dictionary<string, DateTimeOffset>? checkpoints = context.GetVariable<Dictionary<string, DateTimeOffset>>(CheckpointsVariableName);
        AzureDevOpsPushPollingResult result = await dispatcher.DispatchAsync(pollingOptions, checkpoints, context.CancellationToken).ConfigureAwait(false);
        context.SetVariable(CheckpointsVariableName, new Dictionary<string, DateTimeOffset>(result.Checkpoints, StringComparer.OrdinalIgnoreCase));

        return result.Events;
    }
}
