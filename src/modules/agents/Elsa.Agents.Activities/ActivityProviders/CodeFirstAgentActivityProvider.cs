using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
            var agentDescriptors = await CreateDescriptorsForAgentAsync(key, type, cancellationToken);
            descriptors.AddRange(agentDescriptors);
        }

        return descriptors;
    }

    private async Task<IEnumerable<ActivityDescriptor>> CreateDescriptorsForAgentAsync(string key, Type agentType, CancellationToken cancellationToken)
    {
        var descriptors = new List<ActivityDescriptor>();

        // Discover all public methods that match the signature: async Task MethodName(AgentExecutionContext)
        var methods = agentType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(IsAgentActionMethod)
            .ToList();

        foreach (var method in methods)
        {
            var descriptor = await CreateDescriptorForAgentMethodAsync(key, agentType, method, cancellationToken);
            descriptors.Add(descriptor);
        }

        return descriptors;
    }

    private async Task<ActivityDescriptor> CreateDescriptorForAgentMethodAsync(string agentKey, Type agentType, MethodInfo method, CancellationToken cancellationToken)
    {
        var descriptor = await activityDescriber.DescribeActivityAsync(typeof(CodeFirstAgentActivity), cancellationToken);
        var methodName = method.Name;
        var activityTypeName = $"Elsa.Agents.CodeFirst.{agentKey.Pascalize()}.{methodName}";

        // Strip "Async" suffix for display purposes
        var displayMethodName = methodName.EndsWith("Async", StringComparison.Ordinal)
            ? methodName[..^5]
            : methodName;

        // Check for DisplayAttribute
        var displayAttribute = method.GetCustomAttribute<DisplayAttribute>();
        var displayName = displayAttribute?.Name ?? displayMethodName.Humanize().Transform(To.TitleCase);

        descriptor.Name = methodName;
        descriptor.TypeName = activityTypeName;
        descriptor.DisplayName = displayName;
        descriptor.Description = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
        descriptor.Category = "Agents";
        descriptor.Kind = ActivityKind.Task;
        descriptor.RunAsynchronously = true;
        descriptor.IsBrowsable = true;
        descriptor.ClrType = typeof(CodeFirstAgentActivity);

        descriptor.Constructor = context =>
        {
            var activity = context.CreateActivity<CodeFirstAgentActivity>();
            activity.Type = activityTypeName;
            activity.AgentName = agentKey;
            activity.MethodName = methodName;
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

    private static bool IsAgentActionMethod(MethodInfo method)
    {
        // Must return Task<AgentRunResponse>
        if (method.ReturnType != typeof(Task<Microsoft.Agents.AI.AgentRunResponse>))
            return false;

        // Must have exactly one parameter of type AgentExecutionContext
        var parameters = method.GetParameters();
        if (parameters.Length != 1)
            return false;

        if (parameters[0].ParameterType != typeof(AgentExecutionContext))
            return false;

        return true;
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

