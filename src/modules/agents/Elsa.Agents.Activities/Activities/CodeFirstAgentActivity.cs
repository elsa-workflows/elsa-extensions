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
        var functionInput = new Dictionary<string, object?>();

        foreach (var inputDescriptor in inputDescriptors)
        {
            var input = (Input?)inputDescriptor.ValueGetter(this);
            var inputValue = input != null ? context.Get(input.MemoryBlockReference()) : null;

            if (inputValue is ExpandoObject expandoObject)
                inputValue = expandoObject.ConvertTo<string>();

            functionInput[inputDescriptor.Name] = inputValue;
        }

        // Resolve the agent via the unified abstraction.
        var agentResolver = context.GetRequiredService<IAgentResolver>();
        var agent = await agentResolver.ResolveAsync(AgentName, cancellationToken);
        var agentType = agent.GetType();
        var agentPropertyLookup = agentType.GetProperties().ToDictionary(x => x.Name, x => x);

        // Copy activity input descriptor values into the agent public properties:
        foreach (var inputDescriptor in inputDescriptors)
        {
            var input = (Input?)inputDescriptor.ValueGetter(this);
            var inputValue = input != null ? context.Get(input.MemoryBlockReference()) : null;
            agentPropertyLookup[inputDescriptor.Name].SetValue(agent, inputValue);
        }

        // Invoke the specified method on the agent using reflection
        var method = agentType.GetMethod(MethodName, BindingFlags.Instance | BindingFlags.Public);
        if (method == null)
            throw new InvalidOperationException($"Method '{MethodName}' not found on agent type '{agentType.Name}'.");

        var agentExecutionContext = new AgentExecutionContext { CancellationToken = context.CancellationToken };
        var task = method.Invoke(agent, new object[] { agentExecutionContext }) as Task<Microsoft.Agents.AI.AgentRunResponse>;
        if (task == null)
            throw new InvalidOperationException($"Method '{MethodName}' did not return a Task<AgentRunResponse>.");

        var agentExecutionResponse = await task;
        var responseText = StripCodeFences(agentExecutionResponse.Text);
        var isJsonResponse = IsJsonResponse(responseText);
        var outputType = context.ActivityDescriptor.Outputs.Single().Type;

        // If the target type is object and the response is in JSON format, we want it to be deserialized into an ExpandoObject for dynamic field access. 
        if (outputType == typeof(object) && isJsonResponse)
            outputType = typeof(ExpandoObject);
        
        var outputValue = isJsonResponse ? responseText.ConvertTo(outputType) : responseText;
        var outputDescriptor = activityDescriptor.Outputs.Single();
        var output = (Output?)outputDescriptor.ValueGetter(this);
        context.Set(output, outputValue, "Output");
    }
}