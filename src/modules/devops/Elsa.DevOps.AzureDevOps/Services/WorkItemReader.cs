using Elsa.DevOps.AzureDevOps.Models;
using Elsa.Workflows;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Reading work items on behalf of a tool: one work item, a filtered search, or a caller's own WIQL.
/// </summary>
/// <remarks>
/// <para>
/// Shared by the two adapters that offer these reads - the agent tool set of <c>Elsa.DevOps.AzureDevOps.AgentTools</c>
/// and the MCP tools of a host - because the parts worth getting right are not in either of them. The tag filter is
/// compared whole here, the row count is bounded here, and what Azure DevOps refused is turned into a sentence here. A
/// second copy of those rules is a second chance to drift apart.
/// </para>
/// <para>
/// Nothing throws for something the caller could put right: a missing id, a missing query and a refused query all come
/// back as text. Anything else - a broken connection, a serializer failure - travels on untouched, because a tool
/// reporting an empty result over a read that never happened is worse than one that fails.
/// </para>
/// </remarks>
public sealed class WorkItemReader(IWorkItemEditor editor, WorkItemToolTargetResolver targets)
{
    /// <summary>What a caller gets instead of a guess when it named no work item.</summary>
    public const string AskForTheWorkItem =
        "No work item id was given. Ask which work item this is about, then call the tool again with that id.";

    /// <summary>How many search results a caller gets when it names no number.</summary>
    public const int DefaultMaxResults = 25;

    /// <summary>The most a caller can ask for, however many it asks for.</summary>
    public const int MaxResultsCeiling = 50;

    /// <summary>
    /// One work item, or the sentence explaining why not. Pass the activity context when there is one: it is what
    /// resolves the personal access token of the user a workflow runs for.
    /// </summary>
    public async ValueTask<object> GetAsync(ActivityExecutionContext? activity, int? workItemId, CancellationToken cancellationToken = default)
    {
        if (workItemId is not > 0)
            return AskForTheWorkItem;

        (WorkItemToolTarget? target, string? error) = await targets.ResolveAsync(activity, cancellationToken).ConfigureAwait(false);

        if (target == null)
            return error!;

        try
        {
            return await editor
                .GetAsync(new WorkItemRequest(target.OrganizationUrl, target.Token, workItemId.Value), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (WorkItemEditorException exception)
        {
            return Refused(exception);
        }
    }

    /// <summary>
    /// The work items matching the filters that are filled in, newest change first. Every filter left blank is left out
    /// rather than matched against an empty string.
    /// </summary>
    public ValueTask<object> SearchAsync(
        ActivityExecutionContext? activity,
        string? searchText,
        string? workItemType,
        string? state,
        string? tag,
        string? assignedTo,
        int? maxResults,
        CancellationToken cancellationToken = default) =>
        RunQueryAsync(
            activity,
            WorkItemSearchWiql.Build(searchText, workItemType, state, tag, assignedTo),
            tag,
            maxResults,
            cancellationToken);

    /// <summary>
    /// The work items a caller's own WIQL matches, for what the filters cannot express: date ranges, area paths, OR
    /// conditions. A query Azure DevOps rejects comes back with its complaint, so it can be corrected and tried again.
    /// </summary>
    public ValueTask<object> QueryAsync(ActivityExecutionContext? activity, string? wiql, int? maxResults, CancellationToken cancellationToken = default) =>
        string.IsNullOrWhiteSpace(wiql)
            ? ValueTask.FromResult<object>("No wiql was given, so nothing was queried. Supply a WIQL query, for example: SELECT [System.Id] FROM WorkItems WHERE [System.State] = 'Active'.")
            : RunQueryAsync(activity, wiql, null, maxResults, cancellationToken);

    /// <summary>
    /// How many rows to fetch. Every row is paid for on the way to a model, so an absent or oversized request is
    /// bounded rather than honoured.
    /// </summary>
    public static int Bound(int? maxResults) =>
        maxResults switch
        {
            null or <= 0 => DefaultMaxResults,
            > MaxResultsCeiling => MaxResultsCeiling,
            _ => maxResults.Value,
        };

    /// <summary>
    /// Runs a query and hands back the rows, with the whole-tag comparison applied when a tag was asked for.
    /// </summary>
    /// <remarks>
    /// The tag filter is finished here rather than in the query because WIQL cannot do it: <c>CONTAINS</c> is a substring
    /// test over the semicolon-separated list, so a search for <c>problemId:19</c> also selects <c>problemId:190</c>.
    /// Handing that to a model has it comment on, or silence, the wrong fault.
    /// </remarks>
    private async ValueTask<object> RunQueryAsync(
        ActivityExecutionContext? activity,
        string wiql,
        string? tag,
        int? maxResults,
        CancellationToken cancellationToken)
    {
        (WorkItemToolTarget? target, string? error) = await targets.ResolveAsync(activity, cancellationToken).ConfigureAwait(false);

        if (target == null)
            return error!;

        if (string.IsNullOrWhiteSpace(target.Project))
            return "No Azure DevOps project is configured for this host, and a query runs within a project. Set AzureDevOps:DefaultProject. Report this and stop; it is not something to work around.";

        try
        {
            WorkItemSearchResult result = await editor
                .SearchAsync(
                    new WorkItemQueryRequest(target.OrganizationUrl, target.Token, target.Project, wiql, Bound(maxResults)),
                    cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(tag))
                result = new WorkItemSearchResult([.. result.Items.Where(row => HasTag(row, tag))], result.MoreAvailable);

            return result;
        }
        catch (WorkItemEditorException exception)
        {
            return Refused(exception);
        }
    }

    private static bool HasTag(WorkItemRow row, string tag) =>
        (row.Tags ?? string.Empty)
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Any(candidate => string.Equals(candidate, tag.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string Refused(WorkItemEditorException exception) =>
        $"Azure DevOps refused this: {exception.Message}";
}
