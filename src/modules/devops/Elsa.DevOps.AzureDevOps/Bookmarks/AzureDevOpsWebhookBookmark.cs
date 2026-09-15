using System.Text.Json.Serialization;

namespace Elsa.DevOps.AzureDevOps.Bookmarks;

/// <summary>
/// Bookmark payload used to match Azure DevOps Service Hook events.
/// </summary>
/// <param name="PullRequestId">
/// The pull request a Pull Request Merged or Updated trigger was restricted to, or <c>null</c> for one listening
/// project-wide. Written only when it has a value - see the remarks.
/// </param>
/// <remarks>
/// This record is a wire format: a suspended instance is matched to an arriving event by the hash of this payload's
/// JSON, and those hashes are already stored. So <see cref="PullRequestId"/> is omitted from the JSON when it is
/// <c>null</c> rather than serialized as <c>null</c>. Serializing it would change the hash of every unfiltered
/// bookmark this package ever wrote - the work item ones included - and every instance waiting on one would stop
/// being resumed, silently: a stimulus that matches nothing is not an error. With the member absent, an unfiltered
/// bookmark hashes exactly as it did before the filter existed, and only a bookmark that actually names a pull
/// request gets a new hash. <c>PullRequestTriggerIdentityTests</c> pins the serialized text.
/// </remarks>
public sealed record AzureDevOpsWebhookBookmark(
    string EventType,
    string ProjectId,
    string? WorkItemId = null,
    string? WorkItemType = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PullRequestId = null);
