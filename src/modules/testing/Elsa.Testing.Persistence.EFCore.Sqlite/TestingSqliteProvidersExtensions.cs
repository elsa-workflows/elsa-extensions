using System.Reflection;
using Elsa.Testing.Persistence.EFCore;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use Sqlite.
/// </summary>
public static class TestingSqliteProvidersExtensions
{
    private static Assembly Assembly => typeof(TestingSqliteProvidersExtensions).Assembly;
    
    /// <summary>
    /// Configures the feature to use Sqlite.
    /// </summary>
    public static EFCoreTestingPersistenceFeature UseSqlite(this EFCoreTestingPersistenceFeature feature, string? connectionString = null, ElsaDbContextOptions? options = null)
    {
        feature.UseSqlite(Assembly, connectionString, options);
        return feature;
    }
    
    public static EFCoreTestingPersistenceFeature UseSqlite(this EFCoreTestingPersistenceFeature feature, Func<IServiceProvider, string> connectionStringFunc, ElsaDbContextOptions? options = null)
    {
        feature.UseSqlite(Assembly, connectionStringFunc, options);
        return feature;
    }
}