using System.ComponentModel;
using System.Reflection;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Extensions;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Agents.Activities.ActivityProviders;

/// <summary>
/// Provides activities for each code-first agent registered via <see cref="CodeFirstAgentOptions"/>.
/// Inputs are derived from public properties on the agent type using simple
/// reflection rules. Execution is delegated to <see cref="CodeFirstAgentActivity"/>
/// via the common <see cref="IAgent"/> abstraction.
/// </summary>
[UsedImplicitly]
public class CodeFirstAgentActivityProvider(
    IOptions<AgentsOptions> agentOptions,
    IActivityDescriber activityDescriber,
    IWellKnownTypeRegistry wellKnownTypeRegistry) : IActivityProvider
{
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var descriptors = new List<ActivityDescriptor>();

        foreach (var kvp in agentOptions.Value.AgentTypes)
        {
            var key = kvp.Key;
            var type = kvp.Value;
            var descriptor = await CreateDescriptorForAgentAsync(key, type, cancellationToken);
            descriptors.Add(descriptor);
        }

        return descriptors;
    }

    private async Task<ActivityDescriptor> CreateDescriptorForAgentAsync(string key, Type agentType, CancellationToken cancellationToken)
    {
        var descriptor = await activityDescriber.DescribeActivityAsync(typeof(CodeFirstAgentActivity), cancellationToken);
        var activityTypeName = $"Elsa.Agents.CodeFirst.{key.Pascalize()}";

        descriptor.Name = key.Pascalize();
        descriptor.TypeName = activityTypeName;
        descriptor.DisplayName = key.Humanize().Transform(To.TitleCase);
        descriptor.Category = "Agents";
        descriptor.Kind = ActivityKind.Task;
        descriptor.RunAsynchronously = true;
        descriptor.IsBrowsable = true;
        descriptor.ClrType = typeof(CodeFirstAgentActivity);

        descriptor.Constructor = context =>
        {
            var activity = context.CreateActivity<CodeFirstAgentActivity>();
            activity.Type = activityTypeName;
            activity.AgentName = key;
            activity.RunAsynchronously = true;
            return activity;
        };

        // Build inputs from public instance properties.
        descriptor.Inputs.Clear();
        foreach (var prop in agentType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!IsInputProperty(prop))
                continue;

            var inputName = prop.Name;
            var inputType = prop.PropertyType.FullName ?? "object";
            var nakedInputType = wellKnownTypeRegistry.GetTypeOrDefault(inputType);
            var description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description;

            var inputDescriptor = new InputDescriptor
            {
                Name = inputName,
                DisplayName = inputName.Humanize(),
                Description = description,
                Type = nakedInputType,
                ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(inputName),
                ValueSetter = (activity, value) => activity.SyntheticProperties[inputName] = value!,
                IsSynthetic = true,
                IsWrapped = true,
                UIHint = ActivityDescriber.GetUIHint(nakedInputType)
            };

            descriptor.Inputs.Add(inputDescriptor);
        }

        // For now, expose a single synthetic Output of type object, mirroring
        // the existing AgentActivity behavior.
        descriptor.Outputs.Clear();
        var outputName = "Output";
        var outputDescriptor = new OutputDescriptor
        {
            Name = outputName,
            Description = "The agent's output.",
            Type = typeof(object),
            IsSynthetic = true,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(outputName),
            ValueSetter = (activity, value) => activity.SyntheticProperties[outputName] = value!,
        };
        descriptor.Outputs.Add(outputDescriptor);

        return descriptor;
    }

    private static bool IsInputProperty(PropertyInfo prop)
    {
        // Simple heuristic for now:
        // - Must be readable and writable
        // - Exclude indexers
        if (!prop.CanRead || !prop.CanWrite)
            return false;

        if (prop.GetIndexParameters().Length > 0)
            return false;

        // In the future, you can add a dedicated [AgentInput] attribute and
        // check for it here. For now, treat all simple public properties as
        // potential inputs.
        return true;
    }
}

