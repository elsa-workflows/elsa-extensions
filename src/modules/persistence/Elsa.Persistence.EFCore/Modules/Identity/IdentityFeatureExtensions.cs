﻿using Elsa.Identity.Features;

namespace Elsa.Persistence.EFCore.Modules.Identity;

/// <summary>
/// Provides extensions for <see cref="IdentityFeature"/> 
/// </summary>
public static class IdentityFeatureExtensions
{
    /// <summary>
    /// Sets up the EF Core persistence provider for the User, Application and Role stores. 
    /// </summary>
    public static IdentityFeature UseEntityFrameworkCore(this IdentityFeature feature, Action<EFCoreIdentityPersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}