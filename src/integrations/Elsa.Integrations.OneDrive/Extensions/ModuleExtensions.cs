using System;
using Elsa.Features.Services;
using Elsa.Integrations.OneDrive.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extensions for <see cref="IModule"/> to add OneDrive integration.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Adds OneDrive integration to the specified module.
    /// </summary>
    public static IModule UseOneDrive(this IModule module, Action<OneDriveFeature>? configure = null)
    {
        return module.Use(configure);
    }
}