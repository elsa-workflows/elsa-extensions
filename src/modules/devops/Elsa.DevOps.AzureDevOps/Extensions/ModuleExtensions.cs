using Elsa.Features.Services;
using Elsa.DevOps.AzureDevOps.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="IModule"/> with methods to use Azure DevOps integration.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the Azure DevOps feature.
    /// </summary>
    public static IModule UseAzureDevOps(this IModule module, Action<AzureDevOpsFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
