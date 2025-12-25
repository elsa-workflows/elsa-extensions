using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Agents.Activities.ActivityProviders;

/// <summary>
/// Provides activities for each code-first agent registered via <see cref="CodeFirstAgentOptions"/>.
/// Inputs are derived from public properties on the agent type using simple
/// reflection rules. Execution is delegated to <see cref="CodeFirstAgentActivity"/>
/// </summary>
[UsedImplicitly]
public class CodeFirstAgentActivityProvider(IOptions<AgentsOptions> agentOptions, IActivityDescriber activityDescriber) : IActivityProvider
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

        var methods = agentType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
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
        var activityAttribute = agentType.GetCustomAttribute<ActivityAttribute>() ?? method.GetCustomAttribute<ActivityAttribute>();

        var methodName = method.Name;
        var activityTypeName = BuildActivityTypeName(agentKey, method, agentType, activityAttribute);

        var displayAttribute = method.GetCustomAttribute<DisplayAttribute>();
        var typeDisplayName = activityAttribute?.DisplayName ?? agentType.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
        var methodDisplayName = displayAttribute?.Name ?? methodName.Humanize().Transform(To.TitleCase);
        var displayName = !string.IsNullOrWhiteSpace(typeDisplayName) ? typeDisplayName : methodDisplayName;
        if (!string.IsNullOrWhiteSpace(activityAttribute?.DisplayName))
            displayName = activityAttribute.DisplayName!;

        descriptor.Name = methodName;
        descriptor.TypeName = activityTypeName;
        descriptor.DisplayName = displayName;
        descriptor.Description = activityAttribute?.Description ?? method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? agentType.GetCustomAttribute<DescriptionAttribute>()?.Description;
        descriptor.Category = activityAttribute?.Category ?? "Agents";
        descriptor.Kind = activityAttribute?.Kind ?? ActivityKind.Task;
        descriptor.RunAsynchronously = activityAttribute?.RunAsynchronously ?? true;
        descriptor.IsBrowsable = true;
        descriptor.ClrType = typeof(CodeFirstAgentActivity);

        descriptor.Constructor = context =>
        {
            var activity = context.CreateActivity<CodeFirstAgentActivity>();
            activity.Type = activityTypeName;
            activity.AgentName = agentKey;
            activity.MethodName = methodName;
            activity.RunAsynchronously = descriptor.RunAsynchronously;
            return activity;
        };

        descriptor.Inputs.Clear();
        foreach (var prop in agentType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!IsInputProperty(prop))
                continue;

            var inputDescriptor = CreatePropertyInputDescriptor(prop);
            descriptor.Inputs.Add(inputDescriptor);
        }

        foreach (var parameter in method.GetParameters())
        {
            if (parameter.ParameterType == typeof(AgentExecutionContext) || parameter.ParameterType == typeof(CancellationToken))
                continue;

            var inputDescriptor = CreateParameterInputDescriptor(parameter);
            descriptor.Inputs.Add(inputDescriptor);
        }

        descriptor.Outputs.Clear();
        var outputDescriptor = CreateOutputDescriptor(method);
        if (outputDescriptor != null)
            descriptor.Outputs.Add(outputDescriptor);

        return descriptor;
    }

    private string BuildActivityTypeName(string agentKey, MethodInfo method, Type agentType, ActivityAttribute? activityAttribute)
    {
        var methodName = method.Name.EndsWith("Async", StringComparison.Ordinal)
            ? method.Name[..^5]
            : method.Name;

        if (activityAttribute != null && !string.IsNullOrWhiteSpace(activityAttribute.Namespace))
        {
            var typeSegment = activityAttribute.Type ?? methodName;
            return $"{activityAttribute.Namespace}.{typeSegment}";
        }

        return $"Elsa.Agents.CodeFirst.{agentKey.Pascalize()}.{methodName}";
    }

    private InputDescriptor CreatePropertyInputDescriptor(PropertyInfo prop)
    {
        var inputAttribute = prop.GetCustomAttribute<InputAttribute>();
        var displayNameAttribute = prop.GetCustomAttribute<DisplayNameAttribute>();
        var descriptionAttribute = prop.GetCustomAttribute<DescriptionAttribute>();

        var inputName = inputAttribute?.Name ?? prop.Name;
        var displayName = inputAttribute?.DisplayName ?? displayNameAttribute?.DisplayName ?? prop.Name.Humanize();
        var description = inputAttribute?.Description ?? descriptionAttribute?.Description;
        var nakedInputType = prop.PropertyType;

        return new InputDescriptor
        {
            Name = inputName,
            DisplayName = displayName,
            Description = description,
            Type = nakedInputType,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(inputName),
            ValueSetter = (activity, value) => activity.SyntheticProperties[inputName] = value!,
            IsSynthetic = true,
            IsWrapped = true,
            UIHint = inputAttribute?.UIHint ?? ActivityDescriber.GetUIHint(nakedInputType),
            Category = inputAttribute?.Category,
            DefaultValue = inputAttribute?.DefaultValue,
            Order = inputAttribute?.Order ?? 0,
            IsBrowsable = inputAttribute?.IsBrowsable ?? true,
            AutoEvaluate = inputAttribute?.AutoEvaluate ?? true
        };
    }

    private InputDescriptor CreateParameterInputDescriptor(ParameterInfo parameter)
    {
        var inputAttribute = parameter.GetCustomAttribute<InputAttribute>();
        var displayNameAttribute = parameter.GetCustomAttribute<DisplayNameAttribute>();

        var inputName = inputAttribute?.Name ?? parameter.Name ?? "input";
        var displayName = inputAttribute?.DisplayName ?? displayNameAttribute?.DisplayName ?? inputName.Humanize();
        var description = inputAttribute?.Description;
        var nakedInputType = parameter.ParameterType;

        return new InputDescriptor
        {
            Name = inputName,
            DisplayName = displayName,
            Description = description,
            Type = nakedInputType,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(inputName),
            ValueSetter = (activity, value) => activity.SyntheticProperties[inputName] = value!,
            IsSynthetic = true,
            IsWrapped = true,
            UIHint = inputAttribute?.UIHint ?? ActivityDescriber.GetUIHint(nakedInputType),
            Category = inputAttribute?.Category,
            DefaultValue = inputAttribute?.DefaultValue,
            Order = inputAttribute?.Order ?? 0,
            IsBrowsable = inputAttribute?.IsBrowsable ?? true,
            AutoEvaluate = inputAttribute?.AutoEvaluate ?? true
        };
    }

    private OutputDescriptor? CreateOutputDescriptor(MethodInfo method)
    {
        var returnType = method.ReturnType;
        if (returnType == typeof(Task))
            return null;

        Type actualReturnType;
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            actualReturnType = returnType.GetGenericArguments()[0];
        else
            return null;

        if (actualReturnType != typeof(string) && actualReturnType != typeof(object) && actualReturnType != typeof(Microsoft.Agents.AI.AgentRunResponse))
            return null;

        var outputAttribute = method.ReturnParameter.GetCustomAttribute<OutputAttribute>() ??
                              method.GetCustomAttribute<OutputAttribute>() ??
                              method.DeclaringType?.GetCustomAttribute<OutputAttribute>();

        var displayNameAttribute = method.ReturnParameter.GetCustomAttribute<DisplayNameAttribute>();
        var outputName = outputAttribute?.Name ?? "Output";
        var displayName = outputAttribute?.DisplayName ?? displayNameAttribute?.DisplayName ?? outputName.Humanize();
        var description = outputAttribute?.Description ?? "The agent's output.";
        var nakedOutputType = actualReturnType;

        return new OutputDescriptor
        {
            Name = outputName,
            DisplayName = displayName,
            Description = description,
            Type = nakedOutputType,
            IsSynthetic = true,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(outputName),
            ValueSetter = (activity, value) => activity.SyntheticProperties[outputName] = value!,
            IsBrowsable = outputAttribute?.IsBrowsable ?? true,
            IsSerializable = outputAttribute?.IsSerializable ?? true
        };
    }

    private static bool IsAgentActionMethod(MethodInfo method)
    {
        if (!typeof(Task).IsAssignableFrom(method.ReturnType))
            return false;

        if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = method.ReturnType.GetGenericArguments()[0];
            if (resultType != typeof(string) && resultType != typeof(object) && resultType != typeof(Microsoft.Agents.AI.AgentRunResponse))
                return false;
        }

        return true;
    }

    private static bool IsInputProperty(PropertyInfo prop)
    {
        if (!prop.CanRead || !prop.CanWrite)
            return false;

        if (prop.GetIndexParameters().Length > 0)
            return false;

        return true;
    }
}

