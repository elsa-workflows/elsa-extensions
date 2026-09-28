using System.Collections;
using System.Text.Json;
using Elsa.Common.Models;
using Elsa.Mcp.Abstractions;
using Elsa.Mcp.Server.Configuration;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

namespace Elsa.Mcp.Server.Services;

/// <summary>
/// Turns the published workflow definitions that are opted in for MCP into tool descriptors.
/// </summary>
public class WorkflowToolCatalog(IWorkflowDefinitionStore definitionStore, IOptions<McpOptions> options)
{
    /// <summary>
    /// Lists the exposed workflows as MCP tools.
    /// </summary>
    public async Task<IReadOnlyList<Tool>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ExposedWorkflow> exposed = await FindExposedWorkflowsAsync(cancellationToken).ConfigureAwait(false);

        return
        [
            .. exposed
                .OrderBy(workflow => workflow.Definition.Name ?? workflow.Definition.DefinitionId, StringComparer.OrdinalIgnoreCase)
                .Take(options.Value.MaxTools)
                .Select(workflow => ToTool(workflow.Definition, workflow.ToolName))
        ];
    }

    /// <summary>
    /// Finds the published workflow behind a tool name, or <c>null</c> when the workflow does not exist or is not
    /// exposed. Tools are resolved on every call so that unpublishing a workflow takes effect immediately.
    /// </summary>
    public async Task<WorkflowDefinition?> FindExposedDefinitionAsync(string toolName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return null;

        // The definition id is tried first, and it costs one indexed lookup rather than a scan. It also keeps a client
        // working that learned the tool by its definition id, which is what every tool was named before names were
        // derived from the workflow.
        WorkflowDefinitionFilter filter = new()
        {
            DefinitionId = toolName,
            VersionOptions = VersionOptions.Published
        };

        WorkflowDefinition? definition = await definitionStore.FindAsync(filter, cancellationToken).ConfigureAwait(false);

        if (definition != null && IsExposed(definition))
            return definition;

        IReadOnlyList<ExposedWorkflow> exposed = await FindExposedWorkflowsAsync(cancellationToken).ConfigureAwait(false);

        foreach (ExposedWorkflow workflow in exposed)
        {
            if (string.Equals(workflow.ToolName, toolName, StringComparison.OrdinalIgnoreCase))
                return workflow.Definition;
        }

        return null;
    }

    /// <summary>
    /// Maps a workflow definition to its tool descriptor. A tool name that was resolved against the whole exposed set —
    /// which is what disambiguates workflows sharing a name — can be passed in; without one the name is derived from
    /// this definition alone.
    /// </summary>
    public Tool ToTool(WorkflowDefinition definition, string? toolName = null)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return new Tool
        {
            Name = toolName ?? GetPreferredToolName(definition),
            Title = definition.Name ?? definition.DefinitionId,
            Description = BuildDescription(definition),
            InputSchema = BuildInputSchema(definition)
        };
    }

    /// <summary>
    /// Loads the published workflows that are opted in and settles on the tool name each one answers to.
    /// </summary>
    private async Task<IReadOnlyList<ExposedWorkflow>> FindExposedWorkflowsAsync(CancellationToken cancellationToken)
    {
        WorkflowDefinitionFilter filter = new()
        {
            VersionOptions = VersionOptions.Published
        };

        IEnumerable<WorkflowDefinition> definitions = await definitionStore.FindManyAsync(filter, cancellationToken).ConfigureAwait(false);
        List<WorkflowDefinition> exposed = [.. definitions.Where(IsExposed)];

        Dictionary<string, int> occurrences = new(StringComparer.OrdinalIgnoreCase);

        foreach (WorkflowDefinition definition in exposed)
        {
            string preferred = GetPreferredToolName(definition);
            occurrences[preferred] = occurrences.GetValueOrDefault(preferred) + 1;
        }

        List<ExposedWorkflow> workflows = new(exposed.Count);

        foreach (WorkflowDefinition definition in exposed)
        {
            string preferred = GetPreferredToolName(definition);

            // Nothing stops two published workflows from carrying the same name. Letting both answer to it would send
            // a call to whichever the store happened to return first, so every workflow in the clash keeps its id and
            // none of them silently takes over another's tool.
            workflows.Add(new ExposedWorkflow(
                definition,
                occurrences[preferred] > 1 ? McpToolName.Disambiguate(preferred, definition.DefinitionId) : preferred));
        }

        return workflows;
    }

    /// <summary>
    /// The tool name a workflow asks for on its own: an explicit <c>mcp:name</c>, else its name reduced to what a
    /// client accepts, else its definition id.
    /// </summary>
    internal string GetPreferredToolName(WorkflowDefinition definition)
    {
        string? explicitName = McpCustomProperties.ReadText(definition.CustomProperties, options.Value.ToolNameCustomPropertyName);

        // An explicit name is taken as written rather than reduced, because the author picked those exact characters —
        // but only when a client would accept it, so a typo falls back to a working name instead of breaking the list.
        if (explicitName != null && McpToolName.IsAcceptable(explicitName))
            return explicitName;

        return McpToolName.FromWorkflowName(definition.Name) ?? definition.DefinitionId;
    }

    private bool IsExposed(WorkflowDefinition definition) =>
        options.Value.ExposeAllPublishedWorkflows || McpCustomProperties.ReadFlag(definition.CustomProperties, options.Value.EnabledCustomPropertyName);

    /// <summary>
    /// The description a client shows and a model reads: what the workflow says about itself, followed by the
    /// instructions the author wrote for an agent.
    /// </summary>
    private string BuildDescription(WorkflowDefinition definition)
    {
        // A description left blank in the designer is an empty string rather than null, so testing for null alone
        // sent the client an empty description and the model had nothing at all to go on.
        string description = FirstNonBlank(definition.Description, definition.Name) ?? definition.DefinitionId;
        string? instructions = McpCustomProperties.ReadText(definition.CustomProperties, options.Value.InstructionsCustomPropertyName);

        return instructions == null ? description : $"{description}\n\n{instructions}";
    }

    private JsonElement BuildInputSchema(WorkflowDefinition definition)
    {
        Dictionary<string, object?> properties = new(StringComparer.Ordinal);

        if (options.Value.DescribeWorkflowInputs)
        {
            foreach (InputDefinition input in definition.Inputs)
            {
                if (string.IsNullOrWhiteSpace(input.Name))
                    continue;

                properties[input.Name] = DescribeWorkflowInput(definition, input);
            }
        }

        // The control arguments are always accepted: they carry the resume coordinates and the escape hatch for input
        // that the definition does not declare.
        properties["additionalData"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["type"] = "object", ["additionalProperties"] = true, ["description"] = "Additional workflow input, merged with the declared inputs." };
        properties["answers"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["type"] = "object", ["additionalProperties"] = true, ["description"] = "Answers handed to the resumed activity." };
        properties["answersJson"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["type"] = "string", ["description"] = "Answers handed to the resumed activity, as a JSON string." };
        properties["bookmarkId"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["type"] = "string", ["description"] = "The bookmark to resume. Required together with workflowInstanceId to resume instead of start." };
        properties["correlationId"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["type"] = "string", ["description"] = "Correlation id for the new workflow instance." };
        properties["dispatchWorkflow"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["type"] = "boolean", ["description"] = "Runs the workflow in the background instead of awaiting its first suspension point." };
        properties["versionOptions"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["type"] = "string", ["description"] = "Workflow version to start. Defaults to the published version." };
        properties["workflowInstanceId"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["type"] = "string", ["description"] = "The workflow instance to resume. Required together with bookmarkId." };

        return JsonSerializer.SerializeToElement(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["type"] = "object",
                // Workflows accept input they do not declare, so callers are not restricted to the declared inputs.
                ["additionalProperties"] = true,
                ["properties"] = properties
            });
    }

    private Dictionary<string, object?> DescribeWorkflowInput(WorkflowDefinition definition, InputDefinition input)
    {
        Dictionary<string, object?> description = ToJsonSchema(input.Type);

        // An input carries no property bag of its own, so the instruction written for an agent is keyed by input name
        // on the definition. It wins over the description, which stays what a person reads in the designer — the two
        // say different things to different readers and neither should have to serve both.
        string? inputDescription = McpCustomProperties.ReadText(definition.CustomProperties, $"{options.Value.InputInstructionCustomPropertyPrefix}{input.Name}")
            ?? FirstNonBlank(input.Description, input.DisplayName);

        if (inputDescription != null)
            description["description"] = inputDescription;

        return description;
    }

    private static string? FirstNonBlank(string? first, string? second)
    {
        if (!string.IsNullOrWhiteSpace(first))
            return first;

        return !string.IsNullOrWhiteSpace(second) ? second : null;
    }

    private static Dictionary<string, object?> ToJsonSchema(Type? type)
    {
        string schemaType = ToJsonSchemaType(type);

        Dictionary<string, object?> schema = new(StringComparer.Ordinal)
        {
            ["type"] = schemaType
        };

        // Strict clients (VS Code among them) refuse the whole tool when an array schema carries no items, so an
        // array always gets one: the element type's schema when the type reveals it, otherwise the empty schema,
        // which honestly says "anything".
        if (schemaType == "array")
        {
            Type? elementType = GetElementType(type!);
            schema["items"] = elementType != null ? ToJsonSchema(elementType) : new Dictionary<string, object?>(StringComparer.Ordinal);
        }

        return schema;
    }

    private static Type? GetElementType(Type type)
    {
        Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType.IsArray)
            return underlyingType.GetElementType();

        return underlyingType.GetInterfaces()
            .Append(underlyingType)
            .Where(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(candidate => candidate.GetGenericArguments()[0])
            .FirstOrDefault();
    }

    private static string ToJsonSchemaType(Type? type)
    {
        if (type == null)
            return "string";

        Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(bool))
            return "boolean";

        if (underlyingType == typeof(byte) || underlyingType == typeof(short) || underlyingType == typeof(int) || underlyingType == typeof(long))
            return "integer";

        if (underlyingType == typeof(float) || underlyingType == typeof(double) || underlyingType == typeof(decimal))
            return "number";

        if (underlyingType == typeof(string) || underlyingType == typeof(Guid) || underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset) || underlyingType.IsEnum)
            return "string";

        // Dictionaries are enumerable too, but they describe a map rather than a list, so they are matched first.
        if (IsDictionary(underlyingType))
            return "object";

        if (typeof(IEnumerable).IsAssignableFrom(underlyingType))
            return "array";

        return "object";
    }

    private static bool IsDictionary(Type type)
    {
        if (typeof(IDictionary).IsAssignableFrom(type))
            return true;

        return type.GetInterfaces()
            .Append(type)
            .Any(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>));
    }

    /// <summary>
    /// A published workflow that is opted in, paired with the tool name it answers to.
    /// </summary>
    private sealed record ExposedWorkflow(WorkflowDefinition Definition, string ToolName);
}
