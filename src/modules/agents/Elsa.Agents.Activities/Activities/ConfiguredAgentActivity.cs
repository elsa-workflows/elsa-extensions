using System.ComponentModel;
using System.Dynamic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Agents.Activities.ActivityProviders;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Agents.Activities;

/// <summary>
/// An activity that executes a function of a skilled agent. This is an internal activity that is used by <see cref="AgentActivityProvider"/>.
/// </summary>
[Browsable(false)]
public class ConfiguredAgentActivity : CodeActivity
{
    private static JsonSerializerOptions? _serializerOptions;

    private static JsonSerializerOptions SerializerOptions =>
        _serializerOptions ??= new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            PropertyNameCaseInsensitive = true
        }.WithConverters(new ExpandoObjectConverterFactory());

    [JsonIgnore] internal string AgentName { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
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

        var agentInvoker = context.GetRequiredService<AgentInvoker>();
        var agentExecutionResponse = await agentInvoker.InvokeAgentAsync(AgentName, functionInput, context.CancellationToken);
        var responseText = StripCodeFences(agentExecutionResponse.ChatMessageContent.Content!);
        var isJsonResponse = IsJsonResponse(responseText);
        var outputType = context.ActivityDescriptor.Outputs.Single().Type;

        // If the target type is object and the response is in JSON format, we want it to be deserialized into an ExpandoObject for dynamic field access. 
        if (outputType == typeof(object) && isJsonResponse)
            outputType = typeof(ExpandoObject);

        var converterOptions = new ObjectConverterOptions(SerializerOptions);
        var outputValue = isJsonResponse ? responseText.ConvertTo(outputType, converterOptions) : responseText;
        var outputDescriptor = activityDescriptor.Outputs.Single();
        var output = (Output?)outputDescriptor.ValueGetter(this);
        context.Set(output, outputValue, "Output");
    }

    private static bool IsJsonResponse(string text)
    {
        return text.StartsWith("{", StringComparison.OrdinalIgnoreCase) || text.StartsWith("[", StringComparison.OrdinalIgnoreCase);
    }
    
    private static string StripCodeFences(string content)
    {
        var trimmed = content.Trim();

        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            return trimmed;

        var lines = trimmed.Split('\n');
        return lines.Length < 2 ? trimmed : string.Join('\n', lines.Skip(1).Take(lines.Length - 2)).Trim();
    }
}