using Elsa.Testing.Core.Features;
using Elsa.Testing.Persistence.EFCore;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Testing.Persistence;

/// <summary>
/// Provides extensions to the <see cref="SecretManagementFeature"/> feature.
/// </summary>
[PublicAPI]
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="SecretManagementFeature"/> to use EF Core persistence providers.
    /// </summary>
    public static TestingFeature UseEntityFrameworkCore(this TestingFeature feature, Action<EFCoreTestingPersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}