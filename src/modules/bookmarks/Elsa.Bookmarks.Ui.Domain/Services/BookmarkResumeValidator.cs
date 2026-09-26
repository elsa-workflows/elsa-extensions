using System.Globalization;
using System.Text.Json;
using Elsa.Bookmarks.Ui.Models;

namespace Elsa.Bookmarks.Ui.Services;

/// <summary>What validating an answer produced.</summary>
/// <param name="IsValid">Whether the answer may be used to resume the bookmark.</param>
/// <param name="Values">The normalised values, keyed by field name. Empty when the answer was rejected.</param>
/// <param name="Messages">
/// Sentences for the caller. Present on a rejection, and possibly present on an accepted answer - a dropped unknown
/// field is worth saying out loud without failing the resume over it.
/// </param>
public sealed record BookmarkResumeValidationResult(
    bool IsValid,
    IReadOnlyDictionary<string, object?> Values,
    IReadOnlyList<string> Messages);

/// <summary>
/// Checks an answer against the schema the provider declared.
/// </summary>
/// <remarks>
/// Everything it rejects is something the caller can put right, so it returns sentences rather than throwing: an
/// agent runs with detailed errors off, and an exception reaches its model as something it cannot act on.
/// </remarks>
public static class BookmarkResumeValidator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Validates <paramref name="answersJson"/> against <paramref name="schema"/>.</summary>
    public static BookmarkResumeValidationResult Validate(BookmarkResumeSchema? schema, string? answersJson)
    {
        // A bookmark no provider described carries no schema, and is answered as freely as it was before this existed.
        if (schema == null)
            return new BookmarkResumeValidationResult(true, new Dictionary<string, object?>(StringComparer.Ordinal), []);

        // Rejections and warnings are counted apart rather than recognised by their wording later: a dropped unknown
        // field is worth saying out loud without failing the answer over it, and telling the two apart by matching on
        // sentence text would make every reworded message a behaviour change.
        List<string> messages = [];
        int failures = 0;
        Dictionary<string, JsonElement> supplied;

        if (string.IsNullOrWhiteSpace(answersJson))
        {
            supplied = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            try
            {
                supplied = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(answersJson, SerializerOptions)
                    ?? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                return new BookmarkResumeValidationResult(
                    false,
                    new Dictionary<string, object?>(StringComparer.Ordinal),
                    ["The answers must be a JSON object whose keys are the field names."]);
            }
        }

        Dictionary<string, object?> values = new(StringComparer.Ordinal);

        foreach (BookmarkResumeField field in schema.Fields)
        {
            if (!TryFind(supplied, field.Name, out JsonElement element))
            {
                if (field.Required)
                {
                    messages.Add($"'{field.Name}' ({field.Label}) is required.");
                    failures++;
                }
                else if (field.DefaultValue != null)
                {
                    // Routed through the same TryConvert path a supplied value takes, so a default reaches the
                    // workflow in the same shape a caller's answer would - a boxed int default for a Number field
                    // would otherwise sit next to a double every supplied answer produces.
                    JsonElement defaultElement = JsonSerializer.SerializeToElement(field.DefaultValue, SerializerOptions);

                    if (TryConvert(defaultElement, field, out object? defaultValue, out _))
                        values[field.Name] = defaultValue;
                    else
                        // The caller answering this bookmark did not supply the field and cannot fix the provider's
                        // own default, so this is left out of Values and reported without counting as a failure.
                        messages.Add($"'{field.Name}' ({field.Label}) has a default value that could not be used.");
                }

                continue;
            }

            if (!TryConvert(element, field, out object? value, out string? failure))
            {
                messages.Add(failure!);
                failures++;
                continue;
            }

            values[field.Name] = value;
        }

        foreach (string key in supplied.Keys)
        {
            if (!schema.Fields.Any(field => string.Equals(field.Name, key, StringComparison.OrdinalIgnoreCase)))
                messages.Add($"'{key}' is not a field of this task and was left out.");
        }

        bool isValid = failures == 0;

        return new BookmarkResumeValidationResult(
            isValid,
            isValid ? values : new Dictionary<string, object?>(StringComparer.Ordinal),
            messages);
    }

    private static bool TryFind(Dictionary<string, JsonElement> supplied, string name, out JsonElement element)
    {
        foreach ((string key, JsonElement value) in supplied)
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
            {
                element = value;
                return value.ValueKind != JsonValueKind.Null;
            }
        }

        element = default;
        return false;
    }

    private static bool TryConvert(JsonElement element, BookmarkResumeField field, out object? value, out string? failure)
    {
        value = null;
        failure = null;

        switch (field.Type)
        {
            case BookmarkFieldType.Boolean:
                if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    value = element.GetBoolean();
                    return true;
                }

                if (element.ValueKind == JsonValueKind.String && bool.TryParse(element.GetString(), out bool parsedBoolean))
                {
                    value = parsedBoolean;
                    return true;
                }

                failure = $"'{field.Name}' ({field.Label}) could not be read as yes or no.";
                return false;

            case BookmarkFieldType.Number:
                if (element.ValueKind == JsonValueKind.Number)
                {
                    value = element.GetDouble();
                    return true;
                }

                if (element.ValueKind == JsonValueKind.String
                    && double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedNumber))
                {
                    value = parsedNumber;
                    return true;
                }

                failure = $"'{field.Name}' ({field.Label}) could not be read as a number.";
                return false;

            case BookmarkFieldType.Date:
            case BookmarkFieldType.DateTime:
                string? text = element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();

                if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset parsedDate))
                {
                    // Round-tripped rather than handed on as a DateTimeOffset: the value travels to the workflow as
                    // JSON, and one shape everywhere is what keeps a workflow from having to guess which it got.
                    value = parsedDate.ToString("O", CultureInfo.InvariantCulture);
                    return true;
                }

                failure = $"'{field.Name}' ({field.Label}) could not be read as a date.";
                return false;

            case BookmarkFieldType.Choice:
                string? choice = element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();

                if (field.Options == null || field.Options.Any(option => string.Equals(option.Value, choice, StringComparison.OrdinalIgnoreCase)))
                {
                    value = choice;
                    return true;
                }

                failure = $"'{choice}' is not one of the options for '{field.Name}': {string.Join(", ", field.Options.Select(option => option.Value))}.";
                return false;

            case BookmarkFieldType.MultiChoice:
                if (element.ValueKind != JsonValueKind.Array)
                {
                    failure = $"'{field.Name}' ({field.Label}) could not be read as a list of choices.";
                    return false;
                }

                List<string> chosen = [];

                foreach (JsonElement item in element.EnumerateArray())
                {
                    string? single = item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText();

                    if (field.Options != null && !field.Options.Any(option => string.Equals(option.Value, single, StringComparison.OrdinalIgnoreCase)))
                    {
                        failure = $"'{single}' is not one of the options for '{field.Name}': {string.Join(", ", field.Options.Select(option => option.Value))}.";
                        return false;
                    }

                    if (single != null)
                        chosen.Add(single);
                }

                value = chosen;
                return true;

            default:
                value = element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();
                return true;
        }
    }
}
