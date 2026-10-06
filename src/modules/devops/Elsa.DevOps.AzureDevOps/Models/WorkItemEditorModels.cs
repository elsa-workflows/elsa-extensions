using System.Globalization;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace Elsa.DevOps.AzureDevOps.Models;

/// <summary>
/// Which work item to read, and what to authenticate with.
/// </summary>
public sealed record WorkItemRequest(string OrganizationUrl, string Token, int WorkItemId);

/// <summary>
/// One field of one work item to write.
/// </summary>
public sealed record WorkItemFieldRequest(string OrganizationUrl, string Token, int WorkItemId, string ReferenceName, string? Value);

/// <summary>
/// A comment to add. The project is required: Azure DevOps serves comments per project, unlike fields.
/// </summary>
public sealed record WorkItemCommentRequest(string OrganizationUrl, string Token, string Project, int WorkItemId, string Text);

/// <summary>
/// A WIQL query to run, and how much of its result to bring back.
/// </summary>
/// <param name="Project">
/// The project the query runs in. Required: WIQL is scoped to a project, and <c>@project</c> binds to this.
/// </param>
public sealed record WorkItemQueryRequest(string OrganizationUrl, string Token, string Project, string Wiql, int MaxResults);

/// <summary>
/// One line of a search result: enough to decide which work item to open, and no more.
/// </summary>
/// <remarks>
/// Deliberately without the description. A search that returned descriptions would spend most of a turn's budget on text
/// the caller did not ask for; <see cref="WorkItemSnapshot"/> is for when it wants one work item in full.
/// </remarks>
public sealed record WorkItemRow(
    int Id,
    string? Type,
    string? State,
    string? Title,
    string? Tags,
    DateTimeOffset? ChangedAt,
    string Url)
{
    /// <summary>The fields a row needs, so the query does not fetch every field of every hit.</summary>
    public static readonly string[] Fields =
    [
        "System.WorkItemType",
        "System.State",
        "System.Title",
        "System.Tags",
        "System.ChangedDate",
    ];

    /// <summary>Projects a work item read for a search result.</summary>
    public static WorkItemRow From(WorkItem workItem, string organizationUrl)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationUrl);

        int id = workItem.Id ?? 0;

        return new WorkItemRow(
            id,
            WorkItemFieldReader.Text(workItem, "System.WorkItemType"),
            WorkItemFieldReader.Text(workItem, "System.State"),
            WorkItemFieldReader.Text(workItem, "System.Title"),
            WorkItemFieldReader.Text(workItem, "System.Tags"),
            WorkItemFieldReader.Moment(WorkItemFieldReader.Field(workItem, "System.ChangedDate")),
            WorkItemFieldReader.BrowserUrl(organizationUrl, id));
    }
}

/// <summary>
/// What a query found.
/// </summary>
/// <param name="MoreAvailable">
/// Whether the query matched more than was returned. A WIQL result carries no total, so this is established by asking for
/// one row more than the caller wanted rather than reported as a count that would have to be guessed.
/// </param>
public sealed record WorkItemSearchResult(IReadOnlyList<WorkItemRow> Items, bool MoreAvailable);

/// <summary>
/// What a write changed.
/// </summary>
public sealed record WorkItemFieldChange(int WorkItemId, string ReferenceName, string? Value, int? Revision);

/// <summary>
/// The comment that was added.
/// </summary>
public sealed record WorkItemCommentAdded(int WorkItemId, int? CommentId);

/// <summary>
/// The part of a work item worth handing to a language model.
/// </summary>
/// <remarks>
/// A <see cref="WorkItem"/> carries every field, link and relation it has, which passes the agent's tool-result limit on
/// its own and spends tokens on fields nobody reads. The same choice, for the same reason, as
/// <c>WorkflowInstanceDescription</c> in <c>Elsa.Mcp.Server</c>.
/// </remarks>
public sealed record WorkItemSnapshot(
    int Id,
    string? Type,
    string? State,
    string? Title,
    string? Description,
    string? AssignedTo,
    string? Tags,
    DateTimeOffset? ChangedAt,
    int? Revision,
    string Url)
{
    /// <summary>
    /// How much of a description travels to the model. The conversation trimmer shortens what is <em>stored</em>; what
    /// is sent is paid for in full, and a work item description can be tens of kilobytes of HTML.
    /// </summary>
    private const int MaxDescriptionCharacters = 4_000;

    /// <summary>
    /// Projects a work item read from Azure DevOps, deriving the URL a person would open from
    /// <paramref name="organizationUrl"/> rather than reporting the API address, which is of no use to a reader.
    /// </summary>
    public static WorkItemSnapshot From(WorkItem workItem, string organizationUrl)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationUrl);

        int id = workItem.Id ?? 0;

        return new WorkItemSnapshot(
            id,
            WorkItemFieldReader.Text(workItem, "System.WorkItemType"),
            WorkItemFieldReader.Text(workItem, "System.State"),
            WorkItemFieldReader.Text(workItem, "System.Title"),
            Shorten(WorkItemFieldReader.Text(workItem, "System.Description")),
            WorkItemFieldReader.Identity(WorkItemFieldReader.Field(workItem, "System.AssignedTo")),
            WorkItemFieldReader.Text(workItem, "System.Tags"),
            WorkItemFieldReader.Moment(WorkItemFieldReader.Field(workItem, "System.ChangedDate")),
            workItem.Rev,
            WorkItemFieldReader.BrowserUrl(organizationUrl, id));
    }

    private static string? Shorten(string? description) =>
        description == null || description.Length <= MaxDescriptionCharacters
            ? description
            : description[..MaxDescriptionCharacters] + " […] [shortened]";
}
