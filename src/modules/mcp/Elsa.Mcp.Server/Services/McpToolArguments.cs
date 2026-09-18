using System.Text.Json;

namespace Elsa.Mcp.Server.Services;

/// <summary>
/// Reads the loosely typed arguments of an MCP <c>tools/call</c> request. Argument names are matched
/// case-insensitively, because tool callers are language models rather than compilers.
/// </summary>
internal static class McpToolArguments
{
    /// <summary>
    /// The argument names that steer the call itself instead of describing workflow input.
    /// </summary>
    public static readonly string[] Reserved =
    [
        "additionalData",
        "answers",
        "answersJson",
        "bookmarkId",
        "bookMarkId",
        "correlationId",
        "dispatchWorkflow",
        "versionOptions",
        "workflowInstanceId"
    ];

    public static bool IsReserved(string name) => Reserved.Contains(name, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether the call resumes a suspended workflow instead of starting a new instance.
    /// </summary>
    public static bool IsResumeRequest(IDictionary<string, JsonElement>? arguments) =>
        !string.IsNullOrWhiteSpace(GetString(arguments, "workflowInstanceId")) && !string.IsNullOrWhiteSpace(GetBookmarkId(arguments));

    /// <summary>
    /// Reads the bookmark to resume. Both casings are accepted, because callers copy the name from either the schema
    /// or from an earlier tool result.
    /// </summary>
    public static string? GetBookmarkId(IDictionary<string, JsonElement>? arguments) =>
        GetString(arguments, "bookmarkId") ?? GetString(arguments, "bookMarkId");

    /// <summary>
    /// Reads the answers to hand to the resumed activity, either from <c>answersJson</c>, from <c>answers</c>, or -
    /// when neither is given - from every remaining argument.
    /// </summary>
    public static string? GetAnswersJson(IDictionary<string, JsonElement>? arguments)
    {
        if (TryGet(arguments, "answersJson", out JsonElement answersJson))
            return answersJson.ValueKind == JsonValueKind.String ? answersJson.GetString() : answersJson.GetRawText();

        if (TryGet(arguments, "answers", out JsonElement answers))
            return answers.GetRawText();

        if (arguments == null)
            return null;

        Dictionary<string, object?> fallbackAnswers = new(StringComparer.Ordinal);

        foreach ((string key, JsonElement value) in arguments)
        {
            if (IsResumeArgument(key))
                continue;

            fallbackAnswers[key] = ConvertJsonValue(value);
        }

        return fallbackAnswers.Count > 0 ? JsonSerializer.Serialize(fallbackAnswers) : null;
    }

    /// <summary>
    /// Flattens the arguments into workflow input: the contents of <c>additionalData</c> plus every argument that is
    /// not reserved for the call itself.
    /// </summary>
    public static Dictionary<string, object> BuildInput(IDictionary<string, JsonElement>? arguments)
    {
        Dictionary<string, object> input = new(StringComparer.OrdinalIgnoreCase);

        if (arguments == null)
            return input;

        foreach ((string key, JsonElement value) in arguments)
        {
            if (string.Equals(key, "additionalData", StringComparison.OrdinalIgnoreCase) && value.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in value.EnumerateObject())
                {
                    object? propertyValue = ConvertJsonValue(property.Value);

                    if (propertyValue != null)
                        input[property.Name] = propertyValue;
                }

                continue;
            }

            if (IsReserved(key))
                continue;

            object? argumentValue = ConvertJsonValue(value);

            if (argumentValue != null)
                input[key] = argumentValue;
        }

        return input;
    }

    public static string? GetString(IDictionary<string, JsonElement>? arguments, string key)
    {
        if (!TryGet(arguments, key, out JsonElement value))
            return null;

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText().Trim('"');
    }

    public static bool GetBoolean(IDictionary<string, JsonElement>? arguments, string key)
    {
        if (!TryGet(arguments, key, out JsonElement value))
            return false;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out bool parsed) => parsed,
            _ => false
        };
    }

    public static bool TryGet(IDictionary<string, JsonElement>? arguments, string key, out JsonElement value)
    {
        if (arguments != null)
        {
            foreach ((string candidateKey, JsonElement candidateValue) in arguments)
            {
                if (string.Equals(candidateKey, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = candidateValue;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Converts a JSON argument to the plain CLR values Elsa stores as workflow input.
    /// </summary>
    public static object? ConvertJsonValue(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                Dictionary<string, object?> result = new(StringComparer.Ordinal);

                foreach (JsonProperty property in value.EnumerateObject())
                    result[property.Name] = ConvertJsonValue(property.Value);

                return result;
            case JsonValueKind.Array:
                List<object?> items = [];

                foreach (JsonElement item in value.EnumerateArray())
                    items.Add(ConvertJsonValue(item));

                return items.ToArray();
            case JsonValueKind.String:
                return value.GetString();
            case JsonValueKind.Number when value.TryGetInt64(out long longValue):
                return longValue;
            case JsonValueKind.Number when value.TryGetDecimal(out decimal decimalValue):
                return decimalValue;
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null;
            default:
                return value.GetRawText();
        }
    }

    private static bool IsResumeArgument(string key) =>
        string.Equals(key, "workflowInstanceId", StringComparison.OrdinalIgnoreCase)
        || string.Equals(key, "bookmarkId", StringComparison.OrdinalIgnoreCase)
        || string.Equals(key, "bookMarkId", StringComparison.OrdinalIgnoreCase);
}
