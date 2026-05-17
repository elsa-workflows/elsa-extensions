using Elsa.Extensions;
using Elsa.FakeData.Features;
using Elsa.Features.Services;

namespace Elsa.FakeData.Extensions;

/// <summary>
/// Provides extension methods for configuring fake data services.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Installs the FakeData module.
    /// </summary>
    public static IModule UseFakeData(this IModule module, Action<FakeDataFeature>? configure = null)
    {
        return module.Use(configure);
    }
}