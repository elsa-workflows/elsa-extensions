using System.Text.Json;

namespace Elsa.Bookmarks.Ui.Services;

/// <summary>
/// Reads a bookmark payload as the type it was written as.
/// </summary>
/// <remarks>
/// A payload is the typed object while the run that created it is still in memory, and a <see cref="JsonElement"/>
/// once it has been through the instance store. A provider that handles only the first works on a fresh run and stops
/// working after a restart, which is why every provider goes through here.
/// </remarks>
public static class BookmarkPayloadReader
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Reads <paramref name="payload"/> as <typeparamref name="TPayload"/>, or returns false.</summary>
    public static bool TryRead<TPayload>(object? payload, out TPayload? value)
        where TPayload : class
    {
        value = null;

        switch (payload)
        {
            case null:
                return false;

            case TPayload typed:
                value = typed;
                return true;

            case JsonElement { ValueKind: JsonValueKind.Object } element:
                return TryDeserialize(element.GetRawText(), out value);

            case string text:
                return TryDeserialize(text, out value);

            default:
                return TryDeserialize(JsonSerializer.Serialize(payload, SerializerOptions), out value);
        }
    }

    private static bool TryDeserialize<TPayload>(string json, out TPayload? value)
        where TPayload : class
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            value = JsonSerializer.Deserialize<TPayload>(json, SerializerOptions);
            return value != null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
