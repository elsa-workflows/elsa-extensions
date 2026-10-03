using System.Text.Json;

namespace Elsa.DevOps.AzureDevOps.Activities.WorkItems;

/// <summary>
/// Helpers shared by the activities that accept a free-form dictionary of work item fields.
/// </summary>
internal static class WorkItemFieldValues
{
    /// <summary>
    /// Turns a value coming out of the dictionary editor into something the REST API accepts. Evaluated entries arrive
    /// as plain CLR values and pass through untouched; anything still in its raw JSON form is unwrapped here, including
    /// the <c>{ "type": ..., "value": ... }</c> envelope the editor stores per entry.
    /// </summary>
    public static object? Normalize(object? value)
    {
        if (value is not JsonElement element)
            return value;

        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty("type", out var expressionType)
            && element.TryGetProperty("value", out var expressionValue))
        {
            if (!string.Equals(expressionType.GetString(), "Literal", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"The value of field expression type '{expressionType.GetString()}' was not evaluated. Only literal values can be used here.");
            return Normalize(expressionValue);
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var integer) ? integer : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }
}
