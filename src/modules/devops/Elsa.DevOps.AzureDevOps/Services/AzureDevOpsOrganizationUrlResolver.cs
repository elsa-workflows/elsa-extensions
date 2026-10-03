using Elsa.DevOps.AzureDevOps.Configuration;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Resolves the Azure DevOps organization URL an activity works against, falling back to the configured default
/// when none was supplied.
/// </summary>
public class AzureDevOpsOrganizationUrlResolver(IOptions<AzureDevOpsOptions> options)
{
    /// <summary>
    /// Returns the supplied organization URL, or the configured default organization URL when the supplied value is
    /// empty. Returns <c>null</c> when neither is available.
    /// </summary>
    public string? Resolve(string? organizationUrl) => Normalize(organizationUrl) ?? Normalize(options.Value.DefaultOrganizationUrl);

    /// <summary>
    /// Whether the given organization URL is the host's own configured one. Anything else is another organization,
    /// whose pull request IDs and repository IDs mean nothing here - which is what makes it worth keeping apart.
    /// Nothing supplied is the host's own organization: that is what <see cref="Resolve"/> reads it as too.
    /// </summary>
    public bool IsDefault(string? organizationUrl) =>
        AzureDevOpsOrganizationUrl.AreSame(Resolve(organizationUrl), options.Value.DefaultOrganizationUrl);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
