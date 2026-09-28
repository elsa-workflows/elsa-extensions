using Elsa.Features.Services;
using Elsa.Mcp.Server.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extends <see cref="IModule"/> with methods to use the Model Context Protocol server.
/// </summary>
public static class McpServerModuleExtensions
{
    /// <summary>
    /// Installs the Model Context Protocol server feature.
    /// </summary>
    public static IModule UseMcpServer(this IModule module, Action<McpServerFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
