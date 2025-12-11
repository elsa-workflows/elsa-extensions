using System.ComponentModel;
using System.Dynamic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Elsa.Agents;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Agents.Activities.ActivityProviders;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Agents.Activities;

/// <summary>
/// An activity that executes a multi-agent workflow. This is an internal activity used by <see cref="AgentActivityProvider"/>.
/// </summary>
[Browsable(false)]
public class AgentWorkflowActivity : CodeActivity
{
    private static JsonSerializerOptions? _serializerOptions;

    private static JsonSerializerOptions SerializerOptions =>
        _serializerOptions ??= new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            PropertyNameCaseInsensitive = true
        }.WithConverters(new ExpandoObjectConverterFactory());

    [JsonIgnore] internal string WorkflowName { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var activityDescriptor = context.ActivityDescriptor;
        var inputDescriptors = activityDescriptor.Inputs;
        var workflowInput = new Dictionary<string, object?>();

        foreach (var inputDescriptor in inputDescriptors)
        {
            var input = (Input?)inputDescriptor.ValueGetter(this);
            var inputValue = input != null ? context.Get(input.MemoryBlockReference()) : null;

            if (inputValue is ExpandoObject expandoObject)
            {
                inputValue = expandoObject.ConvertTo<string>();
            }
            
            workflowInput[inputDescriptor.Name] = inputValue;
        }

        var kernelConfigProvider = context.GetRequiredService<IKernelConfigProvider>();
        var workflowExecutor = context.GetRequiredService<AgentWorkflowExecutor>();
        
        var kernelConfig = await kernelConfigProvider.GetKernelConfigAsync(context.CancellationToken);
        
        if (!kernelConfig.AgentWorkflows.TryGetValue(WorkflowName, out var workflowConfig))
            throw new InvalidOperationException($"Agent workflow '{WorkflowName}' not found");

        var result = await workflowExecutor.ExecuteWorkflowAsync(WorkflowName, workflowConfig, workflowInput, context.CancellationToken);
        var json = result.Output?.Trim();

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("The workflow output is empty or null.");

        var outputs = context.ActivityDescriptor.Outputs;
        if (outputs.Count != 1)
            throw new InvalidOperationException($"Expected exactly one output, but found {outputs.Count}");

        var outputType = outputs.First().Type;

        // If the target type is object, we want the JSON to be deserialized into an ExpandoObject for dynamic field access. 
        if (outputType == typeof(object))
            outputType = typeof(ExpandoObject);

        var converterOptions = new ObjectConverterOptions(SerializerOptions);
        var outputValue = json.ConvertTo(outputType, converterOptions);
        var outputDescriptor = outputs.First();
        var output = outputDescriptor.ValueGetter(this) as Output;
        
        if (output == null)
            throw new InvalidOperationException("Output descriptor did not return a valid Output object");
            
        context.Set(output, outputValue);
    }
}
