using Elsa.DevOps.AzureDevOps.Configuration;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Resolves credentials for Azure DevOps polling.
/// </summary>
public class AzureDevOpsPollingCredentialResolver(
    AzureDevOpsTokenResolver tokenResolver,
    IOptions<AzureDevOpsPollingOptions> pollingOptions)
{
    /// <summary>
    /// Returns the token the given polling family runs under: its own credential, then the one shared by every family
    /// under <c>AzureDevOps:Polling</c>, then the organization-wide default. Polling runs for no user, so it never
    /// reaches for a per-user token secret; conversely, a token configured under <c>AzureDevOps:Polling</c> is not
    /// handed to activities.
    /// </summary>
    /// <remarks>
    /// The shared rung is what keeps a family whose own secret is gone - never created, removed since, or named on a
    /// watcher instance that outlived the configuration - polling as polling. Without it the next credential is
    /// <see cref="AzureDevOpsOptions.DefaultTokenSecretName"/>, which is the PAT activities authenticate with when no
    /// user is known, and the one credential polling must never authenticate as.
    /// </remarks>
    public async Task<string?> GetTokenAsync(PollingFamilyOptions family, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(family);

        AzureDevOpsPollingOptions shared = pollingOptions.Value;

        return await tokenResolver.ResolveAsync(
            [
                new AzureDevOpsCredential(family.Token, family.TokenSecretName),
                new AzureDevOpsCredential(shared.Token, shared.TokenSecretName),
            ],
            cancellationToken).ConfigureAwait(false);
    }
}
