using Elsa.Features.Services;
using Elsa.Http.OpenApi.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for configuring HTTP OpenAPI features on modules.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the HTTP OpenAPI feature.
    /// </summary>
    /// <param name="module">The module to install the feature on.</param>
    /// <param name="configure">Optional configuration action for HttpOpenApiFeature.</param>
    /// <returns>The module for chaining.</returns>
    public static IModule UseHttpOpenApi(this IModule module, Action<HttpOpenApiFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}
