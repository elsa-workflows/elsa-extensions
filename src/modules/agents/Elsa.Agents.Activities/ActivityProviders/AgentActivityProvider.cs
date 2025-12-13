using Elsa.Agents;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Extensions;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Humanizer;
using JetBrains.Annotations;

namespace Elsa.Agents.Activities.ActivityProviders;

/// <summary>
/// Provides activities for each function of registered agents.
/// </summary>
[UsedImplicitly]
public class AgentActivityProvider(
    IKernelConfigProvider kernelConfigProvider,
    IActivityDescriber activityDescriber,
    IWellKnownTypeRegistry wellKnownTypeRegistry
) : IActivityProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var kernelConfig = await kernelConfigProvider.GetKernelConfigAsync(cancellationToken);
        var activityDescriptors = new List<ActivityDescriptor>();

        // Add descriptors for individual agents
        foreach (var kvp in kernelConfig.Agents)
        {
            var agentConfig = kvp.Value;
            var descriptor = await CreateAgentActivityDescriptor(agentConfig, cancellationToken);
            activityDescriptors.Add(descriptor);
        }

        // Add descriptors for agent workflows
        foreach (var kvp in kernelConfig.AgentWorkflows)
        {
            var workflowConfig = kvp.Value;
            var descriptor = await CreateAgentWorkflowActivityDescriptor(workflowConfig, cancellationToken);
            activityDescriptors.Add(descriptor);
        }

        return activityDescriptors;
    }

    private async Task<ActivityDescriptor> CreateAgentActivityDescriptor(AgentConfig agentConfig, CancellationToken cancellationToken)
    {
        var activityDescriptor = await activityDescriber.DescribeActivityAsync(typeof(AgentActivity), cancellationToken);
        var activityTypeName = $"Elsa.Agents.{agentConfig.Name.Pascalize()}";
        activityDescriptor.Name = agentConfig.Name.Pascalize();
        activityDescriptor.TypeName = activityTypeName;
        activityDescriptor.Description = agentConfig.Description;
        activityDescriptor.DisplayName = agentConfig.Name.Humanize().Transform(To.TitleCase);
        activityDescriptor.IsBrowsable = true;
        activityDescriptor.Category = "Agents";
        activityDescriptor.Kind = ActivityKind.Task;
        activityDescriptor.ClrType = typeof(AgentActivity);

        activityDescriptor.Constructor = context =>
        {
            var activity = context.CreateActivity<AgentActivity>();
            activity.Type = activityTypeName;
            activity.AgentName = agentConfig.Name;
            return activity;
        };

        activityDescriptor.Inputs.Clear();

        foreach (var inputVariable in agentConfig.InputVariables)
        {
            var inputName = inputVariable.Name;
            var inputType = inputVariable.Type == null! ? "object" : inputVariable.Type;
            var nakedInputType = wellKnownTypeRegistry.GetTypeOrDefault(inputType);
            var inputDescriptor = new InputDescriptor
            {
                Name = inputVariable.Name,
                DisplayName = inputVariable.Name.Humanize(),
                Description = inputVariable.Description,
                Type = nakedInputType,
                ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(inputName),
                ValueSetter = (activity, value) => activity.SyntheticProperties[inputName] = value!,
                IsSynthetic = true,
                IsWrapped = true,
                UIHint = ActivityDescriber.GetUIHint(nakedInputType)
            };
            activityDescriptor.Inputs.Add(inputDescriptor);
        }

        activityDescriptor.Outputs.Clear();
        var outputVariable = agentConfig.OutputVariable;
        var outputType = outputVariable.Type == null! ? "object" : outputVariable.Type;
        var nakedOutputType = wellKnownTypeRegistry.GetTypeOrDefault(outputType);
        var outputName = "Output";
        var outputDescriptor = new OutputDescriptor
        {
            Name = outputName,
            Description = agentConfig.OutputVariable.Description,
            Type = nakedOutputType,
            IsSynthetic = true,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(outputName),
            ValueSetter = (activity, value) => activity.SyntheticProperties[outputName] = value!,
        };
        activityDescriptor.Outputs.Add(outputDescriptor);

        return activityDescriptor;
    }

    private async Task<ActivityDescriptor> CreateAgentWorkflowActivityDescriptor(AgentWorkflowConfig workflowConfig, CancellationToken cancellationToken)
    {
        var activityDescriptor = await activityDescriber.DescribeActivityAsync(typeof(AgentActivity), cancellationToken);
        var activityTypeName = $"Elsa.Agents.Workflows.{workflowConfig.Name.Pascalize()}";
        activityDescriptor.Name = workflowConfig.Name.Pascalize();
        activityDescriptor.TypeName = activityTypeName;
        activityDescriptor.Description = workflowConfig.Description;
        activityDescriptor.DisplayName = workflowConfig.Name.Humanize().Transform(To.TitleCase);
        activityDescriptor.IsBrowsable = true;
        activityDescriptor.Category = "Agent Workflows";
        activityDescriptor.Kind = ActivityKind.Task;
        activityDescriptor.CustomProperties["RootType"] = nameof(AgentActivity);

        activityDescriptor.Constructor = context =>
        {
            var activity = context.CreateActivity<AgentActivity>();
            activity.Type = activityTypeName;
            // Workflows will be resolved by name via IAgentResolver.
            activity.AgentName = workflowConfig.Name;
            return activity;
        };

        activityDescriptor.Inputs.Clear();

        foreach (var inputVariable in workflowConfig.InputVariables)
        {
            var inputName = inputVariable.Name;
            var inputType = inputVariable.Type == null! ? "object" : inputVariable.Type;
            var nakedInputType = wellKnownTypeRegistry.GetTypeOrDefault(inputType);
            var inputDescriptor = new InputDescriptor
            {
                Name = inputVariable.Name,
                DisplayName = inputVariable.Name.Humanize(),
                Description = inputVariable.Description,
                Type = nakedInputType,
                ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(inputName),
                ValueSetter = (activity, value) => activity.SyntheticProperties[inputName] = value!,
                IsSynthetic = true,
                IsWrapped = true,
                UIHint = ActivityDescriber.GetUIHint(nakedInputType)
            };
            activityDescriptor.Inputs.Add(inputDescriptor);
        }

        activityDescriptor.Outputs.Clear();
        var outputVariable = workflowConfig.OutputVariable;
        var outputType = outputVariable.Type == null! ? "object" : outputVariable.Type;
        var nakedOutputType = wellKnownTypeRegistry.GetTypeOrDefault(outputType);
        var outputName = "Output";
        var outputDescriptor = new OutputDescriptor
        {
            Name = outputName,
            Description = workflowConfig.OutputVariable.Description,
            Type = nakedOutputType,
            IsSynthetic = true,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(outputName),
            ValueSetter = (activity, value) => activity.SyntheticProperties[outputName] = value!,
        };
        activityDescriptor.Outputs.Add(outputDescriptor);

        return activityDescriptor;
    }
}