using Elsa.DevOps.AzureDevOps.Configuration;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Resolves the Azure DevOps project a trigger or activity works against, falling back to the configured default
/// when none was supplied.
/// </summary>
public class AzureDevOpsProjectResolver(IOptions<AzureDevOpsOptions> options)
{
    /// <summary>
    /// Returns the supplied project, or the configured default project when the supplied value is empty. Returns
    /// <c>null</c> when neither is available.
    /// </summary>
    public string? Resolve(string? project) => Normalize(project) ?? Normalize(options.Value.DefaultProject);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
