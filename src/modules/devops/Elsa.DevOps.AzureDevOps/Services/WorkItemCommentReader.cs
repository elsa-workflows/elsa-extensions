using System.Globalization;
using System.Text.Json;
using Elsa.DevOps.AzureDevOps.Events;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Recovers the comment behind a <c>workitem.commentedOn</c> event from whichever source produced it.
/// </summary>
/// <remarks>
/// A Service Hook does not send the comment as a comment. It sends the work item, with the text in the
/// <c>System.History</c> field, and the author and timestamp in whichever of several shapes that particular resource
/// version happens to use. Everything shape-dependent lives here so the trigger stays readable and so the shapes can
/// be tested without a webhook.
/// </remarks>
public static class WorkItemCommentReader
{
    private const string HistoryField = "System.History";
    private const string ChangedByField = "System.ChangedBy";
    private const string ChangedDateField = "System.ChangedDate";

    /// <summary>
    /// Maps a comment the API returned. Everything is known on this path.
    /// </summary>
    public static PostedComment? FromApi(Comment? comment)
    {
        if (string.IsNullOrWhiteSpace(comment?.Text))
            return null;

        return new PostedComment(
            comment.Text,
            comment.Id == 0 ? null : comment.Id,
            comment.CreatedBy?.DisplayName,
            comment.CreatedBy?.UniqueName,
            new DateTimeOffset(DateTime.SpecifyKind(comment.CreatedDate, DateTimeKind.Utc)));
    }

    /// <summary>
    /// Recovers the comment from the payload of a <c>workitem.commentedOn</c> event. Returns <c>null</c> when the
    /// payload carries no comment text, which is the honest answer for an event that turned out not to be about a
    /// comment after all.
    /// </summary>
    public static PostedComment? FromPayload(object? payload) => payload switch
    {
        JsonElement json => FromJson(json),
        WorkItem workItem => FromWorkItem(workItem),
        _ => null,
    };

    private static PostedComment? FromWorkItem(WorkItem workItem)
    {
        string? text = Normalize(GetField(workItem, HistoryField));

        if (text == null)
            return null;

        // Split the same way the JSON path splits it: a field value is a field value, and which of the two shapes it
        // travelled in should not change what the workflow is handed.
        (string? author, string? uniqueName) = SplitIdentityString(GetField(workItem, ChangedByField));

        return new PostedComment(
            text,
            Id: null,
            author,
            uniqueName,
            ParseDate(GetField(workItem, ChangedDateField)));
    }

    private static PostedComment? FromJson(JsonElement resource)
    {
        if (resource.ValueKind != JsonValueKind.Object)
            return null;

        string? text = Normalize(GetJsonField(resource, HistoryField));

        if (text == null)
            return null;

        // revisedBy sits on the resource on some resource versions and is the better source when it is there: it is
        // an identity object, so it carries the sign-in name as well as the display name.
        (string? author, string? uniqueName) = GetIdentity(resource, "revisedBy");

        if (author == null)
            (author, uniqueName) = GetIdentityFromField(resource, ChangedByField);

        return new PostedComment(
            text,
            Id: null,
            author,
            uniqueName,
            ParseDate(GetJsonValue(resource, "revisedDate") ?? GetJsonField(resource, ChangedDateField)));
    }

    private static string? GetField(WorkItem workItem, string fieldName) =>
        workItem.Fields != null && workItem.Fields.TryGetValue(fieldName, out object? value) ? value?.ToString() : null;

    /// <summary>
    /// Reads a work item field out of the resource, allowing for the two shapes a field takes: a plain value, and the
    /// <c>oldValue</c>/<c>newValue</c> object a changed field becomes.
    /// </summary>
    private static string? GetJsonField(JsonElement resource, string fieldName)
    {
        if (!resource.TryGetProperty("fields", out JsonElement fields) || fields.ValueKind != JsonValueKind.Object)
            return null;

        if (!fields.TryGetProperty(fieldName, out JsonElement field))
            return null;

        if (field.ValueKind != JsonValueKind.Object)
            return ToStringValue(field);

        return field.TryGetProperty("newValue", out JsonElement newValue) ? ToStringValue(newValue) : null;
    }

    /// <summary>
    /// An identity field is a display name, an object with <c>displayName</c>, or the
    /// <c>Display Name &lt;sign-in name&gt;</c> string Azure DevOps uses in field values.
    /// </summary>
    private static (string? Author, string? UniqueName) GetIdentity(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement identity))
            return (null, null);

        return identity.ValueKind == JsonValueKind.Object
            ? (Normalize(GetJsonValue(identity, "displayName")), Normalize(GetJsonValue(identity, "uniqueName")))
            : SplitIdentityString(ToStringValue(identity));
    }

    private static (string? Author, string? UniqueName) GetIdentityFromField(JsonElement resource, string fieldName)
    {
        if (!resource.TryGetProperty("fields", out JsonElement fields) || fields.ValueKind != JsonValueKind.Object)
            return (null, null);

        if (!fields.TryGetProperty(fieldName, out JsonElement field))
            return (null, null);

        if (field.ValueKind == JsonValueKind.Object && field.TryGetProperty("newValue", out JsonElement newValue))
            return newValue.ValueKind == JsonValueKind.Object
                ? (Normalize(GetJsonValue(newValue, "displayName")), Normalize(GetJsonValue(newValue, "uniqueName")))
                : SplitIdentityString(ToStringValue(newValue));

        return field.ValueKind == JsonValueKind.Object
            ? (Normalize(GetJsonValue(field, "displayName")), Normalize(GetJsonValue(field, "uniqueName")))
            : SplitIdentityString(ToStringValue(field));
    }

    private static (string? Author, string? UniqueName) SplitIdentityString(string? value)
    {
        string? identity = Normalize(value);

        if (identity == null)
            return (null, null);

        int open = identity.LastIndexOf('<');
        int close = identity.LastIndexOf('>');

        if (open < 0 || close < open)
            return (identity, null);

        return (Normalize(identity[..open]), Normalize(identity[(open + 1)..close]));
    }

    private static string? GetJsonValue(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out JsonElement property)
            ? ToStringValue(property)
            : null;

    private static string? ToStringValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetRawText(),
        _ => null,
    };

    private static DateTimeOffset? ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset parsed)
            ? parsed
            : null;

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
