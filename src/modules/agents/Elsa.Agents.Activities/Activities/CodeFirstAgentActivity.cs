using System.ComponentModel;
using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Helpers;
using Elsa.Agents.Activities.ActivityProviders;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using static Elsa.Agents.Activities.Extensions.ResponseHelpers;

namespace Elsa.Agents.Activities;

/// <summary>
/// An activity that executes a function of a skilled agent. This is an internal activity that is used by <see cref="CodeFirstAgentActivityProvider"/>.
/// </summary>
[Browsable(false)]
public class CodeFirstAgentActivity : CodeActivity
{
    private static JsonSerializerOptions? _serializerOptions;

    [JsonIgnore] internal string AgentName { get; set; } = null!;
    [JsonIgnore] internal string MethodName { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var activityDescriptor = context.ActivityDescriptor;
        var inputDescriptors = activityDescriptor.Inputs;

        var agentResolver = context.GetRequiredService<IAgentResolver>();
        var agent = await agentResolver.ResolveAsync(AgentName, cancellationToken);
        var agentType = agent.GetType();

        var method = agentType.GetMethod(MethodName, BindingFlags.Instance | BindingFlags.Public);
        if (method == null)
            throw new InvalidOperationException($"Method '{MethodName}' not found on agent type '{agentType.Name}'.");

        // Build argument list from parameters and inputs.
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (parameter.ParameterType == typeof(AgentExecutionContext))
            {
                args[i] = new AgentExecutionContext { CancellationToken = cancellationToken };
                continue;
            }

            if (parameter.ParameterType == typeof(CancellationToken))
            {
                args[i] = cancellationToken;
                continue;
            }

            // Find matching input descriptor by name
            var inputDescriptor = inputDescriptors.FirstOrDefault(x => string.Equals(x.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
            if (inputDescriptor == null)
            {
                args[i] = parameter.HasDefaultValue ? parameter.DefaultValue : null;
                continue;
            }

            var input = (Input?)inputDescriptor.ValueGetter(this);
            var inputValue = input != null ? context.Get(input.MemoryBlockReference()) : null;

            if (inputValue is ExpandoObject expandoObject)
                inputValue = expandoObject.ConvertTo(parameter.ParameterType);

            args[i] = inputValue;
        }

        // Copy property-based inputs to the agent instance when the property exists.
        var agentPropertyLookup = agentType.GetProperties().ToDictionary(x => x.Name, x => x);
        foreach (var inputDescriptor in inputDescriptors)
        {
            if (!agentPropertyLookup.TryGetValue(inputDescriptor.Name, out var prop) || !prop.CanWrite)
                continue;

            var input = (Input?)inputDescriptor.ValueGetter(this);
            var inputValue = input != null ? context.Get(input.MemoryBlockReference()) : null;
            if (inputValue is ExpandoObject expandoObject)
                inputValue = expandoObject.ConvertTo(prop.PropertyType);

            prop.SetValue(agent, inputValue);
        }

        var invocationResult = method.Invoke(agent, args);
        if (invocationResult is not Task task)
            throw new InvalidOperationException($"Method '{MethodName}' did not return a Task.");

        await task.ConfigureAwait(false);

        object? resultValue = null;
        var returnType = method.ReturnType;

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultProperty = task.GetType().GetProperty("Result");
            resultValue = resultProperty?.GetValue(task);
        }

        // Map result to output if descriptor exists.
        var outputDescriptor = activityDescriptor.Outputs.SingleOrDefault();
        if (outputDescriptor != null)
        {
            var output = (Output?)outputDescriptor.ValueGetter(this);

            if (resultValue is Microsoft.Agents.AI.AgentRunResponse agentResponse)
            {
                var responseText = StripCodeFences(agentResponse.Text);
                var isJsonResponse = IsJsonResponse(responseText);
                var targetType = outputDescriptor.Type;

                if (targetType == typeof(object) && isJsonResponse)
                    targetType = typeof(ExpandoObject);

                var outputValue = isJsonResponse ? responseText.ConvertTo(targetType) : responseText;
                context.Set(output, outputValue, outputDescriptor.Name);
            }
            else
            {
                // string/object passthrough
                context.Set(output, resultValue, outputDescriptor.Name);
            }
        }
    }
}