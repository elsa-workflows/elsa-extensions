﻿using Elsa.Alterations.Features;
using JetBrains.Annotations;

namespace Elsa.Persistence.MongoDb.Modules.Alterations;

/// <summary>
/// Provides extensions to the <see cref="AlterationsFeature"/> feature.
/// </summary>
[PublicAPI]
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="AlterationsFeature"/> to use the <see cref="MongoAlterationsPersistenceFeature"/>.
    /// </summary>
    public static AlterationsFeature UseMongoDb(this AlterationsFeature feature, Action<MongoAlterationsPersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}
