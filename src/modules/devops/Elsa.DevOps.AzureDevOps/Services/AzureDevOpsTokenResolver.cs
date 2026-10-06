using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.Workflows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// One rung of a token lookup: a literal token and the name of the Elsa Secret holding one, either of which may be
/// empty. A rung that yields nothing is passed over rather than ending the lookup.
/// </summary>
public readonly record struct AzureDevOpsCredential(string? Token, string? TokenSecretName);

/// <summary>
/// Resolves the personal access token an activity or the polling workflow authenticates with, falling back to the
/// configured defaults when none was supplied.
/// </summary>
public class AzureDevOpsTokenResolver(
    IOptions<AzureDevOpsOptions> options,
    IAzureDevOpsSecretReader secrets,
    AzureDevOpsUserNameResolver userNameResolver,
    ILogger<AzureDevOpsTokenResolver> logger)
{
    /// <summary>
    /// Returns, in order of preference, the token the activity supplies, the secret holding the personal access token
    /// of the user the workflow runs for (see <see cref="AzureDevOpsOptions.UserTokenSecretNameFormat"/>), the
    /// configured default token, and the configured default token secret. Returns <c>null</c> when none of them
    /// yields a value.
    /// </summary>
    /// <remarks>
    /// The per-user secret is what keeps an activity on the caller's own PAT: work it creates or edits is then
    /// attributed to that person, and the PAT the polling workflow runs under stays out of reach of workflow authors.
    /// </remarks>
    public async ValueTask<string?> ResolveForActivityAsync(ActivityExecutionContext context, string? token)
    {
        AzureDevOpsOptions defaults = options.Value;
        CancellationToken cancellationToken = context.CancellationToken;

        return Normalize(token)
            ?? await GetSecretAsync(ResolveUserTokenSecretName(context), cancellationToken).ConfigureAwait(false)
            ?? Normalize(defaults.DefaultToken)
            ?? await GetSecretAsync(defaults.DefaultTokenSecretName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns, in order of preference, the supplied token, the secret named by <paramref name="tokenSecretName"/>,
    /// the configured default token, or the configured default token secret. Returns <c>null</c> when none of them
    /// yields a value.
    /// </summary>
    public ValueTask<string?> ResolveAsync(string? token, string? tokenSecretName, CancellationToken cancellationToken = default) =>
        ResolveAsync([new AzureDevOpsCredential(token, tokenSecretName)], cancellationToken);

    /// <summary>
    /// Returns the first token any of <paramref name="candidates"/> yields, and then the configured default token and
    /// default token secret. Returns <c>null</c> when none of them yields a value. Used by the polling workflows, which
    /// run for no user and so have no per-user secret to fall back to, but do have a credential per family and one
    /// shared by all of them.
    /// </summary>
    /// <remarks>
    /// Within one candidate a literal token wins over a secret name, exactly as it does between the defaults. Between
    /// candidates, one that yields nothing at all is passed over rather than ending the lookup: that is what lets a
    /// credential configured further out stand in for one whose secret was never created or has since been removed,
    /// instead of dropping straight to the defaults.
    /// </remarks>
    public async ValueTask<string?> ResolveAsync(
        IEnumerable<AzureDevOpsCredential> candidates,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        foreach (AzureDevOpsCredential candidate in candidates)
        {
            string? resolved = Normalize(candidate.Token)
                ?? await GetSecretAsync(candidate.TokenSecretName, cancellationToken).ConfigureAwait(false);

            if (resolved != null)
                return resolved;
        }

        AzureDevOpsOptions defaults = options.Value;

        return Normalize(defaults.DefaultToken)
            ?? await GetSecretAsync(defaults.DefaultTokenSecretName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns, in order of preference, the supplied token, the secret holding the personal access token of the caller
    /// of the ambient HTTP request, the configured default token, and the configured default token secret. Returns
    /// <c>null</c> when none of them yields a value. Used by a tool called over MCP: it runs inside the caller's own
    /// request and has no workflow instance to read a user from.
    /// </summary>
    public async ValueTask<string?> ResolveForCallerAsync(string? token, CancellationToken cancellationToken = default)
    {
        AzureDevOpsOptions defaults = options.Value;

        return Normalize(token)
            ?? await GetSecretAsync(ResolveCallerTokenSecretName(), cancellationToken).ConfigureAwait(false)
            ?? Normalize(defaults.DefaultToken)
            ?? await GetSecretAsync(defaults.DefaultTokenSecretName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the name of the secret holding the current user's token. Returns <c>null</c> when the lookup is
    /// switched off or when the workflow runs for no user.
    /// </summary>
    private string? ResolveUserTokenSecretName(ActivityExecutionContext context)
    {
        string? userName = userNameResolver.Resolve(context);

        if (userName == null)
            logger.LogDebug("No user is known for workflow instance {WorkflowInstanceId}, so no per-user Azure DevOps token secret is looked up", context.WorkflowExecutionContext.Id);

        return BuildUserTokenSecretName(userName);
    }

    /// <summary>
    /// The same name for a caller with no workflow instance behind it. Returns <c>null</c> when the request is
    /// unauthenticated, which is the polling and webhook case for the activity path as well: no user, no per-user
    /// secret, and the configured defaults are what is left.
    /// </summary>
    private string? ResolveCallerTokenSecretName()
    {
        string? userName = userNameResolver.ResolveForCaller();

        if (userName == null)
            logger.LogDebug("The caller of this request carries no account name, so no per-user Azure DevOps token secret is looked up");

        return BuildUserTokenSecretName(userName);
    }

    private string? BuildUserTokenSecretName(string? userName)
    {
        string? format = Normalize(options.Value.UserTokenSecretNameFormat);

        if (format == null || userName == null)
            return null;

        return format.Replace(AzureDevOpsOptions.UserPlaceholder, userName, StringComparison.OrdinalIgnoreCase);
    }

    private async ValueTask<string?> GetSecretAsync(string? secretName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secretName))
            return null;

        string? secret = await secrets.GetSecretAsync(secretName.Trim(), cancellationToken).ConfigureAwait(false);

        if (secret == null)
            logger.LogDebug("Azure DevOps secret {SecretName} holds no value", secretName);

        return Normalize(secret);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
