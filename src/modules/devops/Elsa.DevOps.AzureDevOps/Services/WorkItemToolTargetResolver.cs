using Elsa.Workflows;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Where a work item tool reads and writes, and as whom.
/// </summary>
/// <param name="Project">
/// The project, when one is configured. Only the comment endpoint needs it; a field patch addresses a work item by id
/// alone.
/// </param>
public sealed record WorkItemToolTarget(string OrganizationUrl, string Token, string? Project);

/// <summary>
/// Resolves the organization, the token and the project a tool call runs against.
/// </summary>
/// <remarks>
/// The token comes from <see cref="AzureDevOpsTokenResolver.ResolveForActivityAsync"/> when there is an activity
/// context, which prefers the personal access token of the user the workflow runs for. That is what puts an agent's edit
/// on the right person's name instead of on a shared identity.
/// <para>
/// Without a context - a tool called over MCP, which has no workflow instance behind it - the token comes from
/// <see cref="AzureDevOpsTokenResolver.ResolveForCallerAsync"/> instead, which reads the caller of the ambient HTTP
/// request. Both paths end at the same per-user secret, so the same person is the same person either way; both also fall
/// back to the host's configured token, which for a read costs attribution rather than correctness.
/// </para>
/// </remarks>
public sealed class WorkItemToolTargetResolver(
    AzureDevOpsOrganizationUrlResolver organizationUrls,
    AzureDevOpsProjectResolver projects,
    AzureDevOpsTokenResolver tokens)
{
    /// <summary>
    /// The target, or the sentence to hand the model when the host is not configured for this.
    /// </summary>
    public async ValueTask<(WorkItemToolTarget? Target, string? Error)> ResolveAsync(
        ActivityExecutionContext? context,
        CancellationToken cancellationToken)
    {
        string? organizationUrl = organizationUrls.Resolve(null);

        if (string.IsNullOrWhiteSpace(organizationUrl))
            return (null, "No Azure DevOps organization is configured for this host. Set AzureDevOps:DefaultOrganizationUrl. Report this and stop; it is not something to work around.");

        string? token = context != null
            ? await tokens.ResolveForActivityAsync(context, null).ConfigureAwait(false)
            : await tokens.ResolveForCallerAsync(null, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(token))
            return (null, "No Azure DevOps token is available for this host. Set AzureDevOps:DefaultToken or the secret named by AzureDevOps:DefaultTokenSecretName. Report this and stop; it is not something to work around.");

        return (new WorkItemToolTarget(organizationUrl, token, projects.Resolve(null)), null);
    }
}
