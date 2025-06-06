﻿using Elsa.Persistence.Dapper.Features;
using FluentMigrator.Runner;
using JetBrains.Annotations;

namespace Elsa.Persistence.Dapper.Extensions;

/// <summary>
/// Provides extensions to add the Dapper migrations feature.
/// </summary>
[PublicAPI]
public static class DapperMigrationsFeatureExtensions
{
    /// <summary>
    /// Adds the Dapper migrations feature. 
    /// </summary>
    public static DapperFeature UseMigrations(this DapperFeature feature, Action<IMigrationRunnerBuilder> configure) => feature.UseMigrations(migrations => migrations.ConfigureRunner = configure);
    
    /// <summary>
    /// Adds the Dapper migrations feature. 
    /// </summary>
    public static DapperFeature UseMigrations(this DapperFeature feature, Action<DapperMigrationsFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}