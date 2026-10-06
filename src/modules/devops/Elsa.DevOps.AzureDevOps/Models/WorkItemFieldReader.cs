using System.Globalization;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace Elsa.DevOps.AzureDevOps.Models;

/// <summary>
/// Reads work item fields out of the untyped dictionary Azure DevOps returns them in.
/// </summary>
/// <remarks>
/// Shared by <see cref="WorkItemSnapshot"/> and <see cref="WorkItemRow"/>, which project the same fields to different
/// depths. The awkward cases are the reason this is one place: an assignment arrives as an <see cref="IdentityRef"/> on
/// one route and as plain text on another, and a date as a <see cref="DateTime"/>, a <see cref="DateTimeOffset"/> or a
/// string depending on how the value was deserialized.
/// </remarks>
internal static class WorkItemFieldReader
{
    public static object? Field(WorkItem workItem, string referenceName) =>
        workItem.Fields != null && workItem.Fields.TryGetValue(referenceName, out object? value) ? value : null;

    public static string? Text(WorkItem workItem, string referenceName) => Field(workItem, referenceName)?.ToString();

    /// <summary>The display name of an assignment, which is what a person - or a model - can act on.</summary>
    public static string? Identity(object? value) =>
        value switch
        {
            null => null,
            IdentityRef identity => identity.DisplayName ?? identity.UniqueName,
            _ => value.ToString(),
        };

    public static DateTimeOffset? Moment(object? value) =>
        value switch
        {
            DateTimeOffset moment => moment,
            DateTime moment => new DateTimeOffset(moment.ToUniversalTime(), TimeSpan.Zero),
            string text when DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsed) => parsed,
            _ => null,
        };

    /// <summary>The URL a person would open, derived rather than read: the API address is of no use to a reader.</summary>
    public static string BrowserUrl(string organizationUrl, int workItemId) =>
        $"{organizationUrl.TrimEnd('/')}/_workitems/edit/{workItemId.ToString(CultureInfo.InvariantCulture)}";
}
