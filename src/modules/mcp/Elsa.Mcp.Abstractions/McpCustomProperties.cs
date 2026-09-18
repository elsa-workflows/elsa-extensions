using System.Text.Json;

namespace Elsa.Mcp.Abstractions;

/// <summary>
/// The custom properties a workflow definition carries to describe itself as an MCP tool, and the reading of their
/// values.
/// </summary>
public static class McpCustomProperties
{
    /// <summary>The property that opts a workflow in as a tool.</summary>
    public const string Enabled = "mcp:enabled";

    /// <summary>The property that names the tool explicitly, instead of deriving the name from the workflow.</summary>
    public const string Name = "mcp:name";

    /// <summary>The property holding instructions for an agent, appended to the tool description.</summary>
    public const string Instructions = "mcp:instructions";

    /// <summary>The prefix of the property holding one input's instruction, completed with the input's name.</summary>
    public const string InputInstructionPrefix = "mcp:input:";

    /// <summary>
    /// The property name holding the instruction for the input called <paramref name="inputName"/>. An input carries
    /// no property bag of its own — Elsa's <c>ArgumentDefinition</c> has only a name, display name, description,
    /// category and type — so the instruction is keyed by input name on the definition instead.
    /// </summary>
    public static string InputInstruction(string inputName)
    {
        ArgumentNullException.ThrowIfNull(inputName);

        return InputInstructionPrefix + inputName;
    }

    /// <summary>
    /// Reads a flag. The value survives code, the designer, the API and JSON persistence in different shapes, so
    /// every shape that means "true" is accepted.
    /// </summary>
    public static bool ReadFlag(IDictionary<string, object> customProperties, string key)
    {
        object? value = Find(customProperties, key);

        return value switch
        {
            bool booleanValue => booleanValue,
            string stringValue => IsTrue(stringValue),
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.String } element => IsTrue(element.GetString()),
            _ => false
        };
    }

    /// <summary>
    /// Reads a text value, trimmed, or <c>null</c> when it is absent, blank or not text at all.
    /// </summary>
    public static string? ReadText(IDictionary<string, object> customProperties, string key)
    {
        object? value = Find(customProperties, key);

        string? text = value switch
        {
            string stringValue => stringValue,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => null
        };

        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    /// <summary>
    /// Finds a value by key, ignoring casing: these keys are typed by hand in the designer, and an input name like
    /// <c>PRId</c> is easy to get subtly wrong.
    /// </summary>
    private static object? Find(IDictionary<string, object> customProperties, string key)
    {
        ArgumentNullException.ThrowIfNull(customProperties);
        ArgumentNullException.ThrowIfNull(key);

        foreach (KeyValuePair<string, object> entry in customProperties)
        {
            if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
                return entry.Value;
        }

        return null;
    }

    private static bool IsTrue(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "1", StringComparison.Ordinal);
}
