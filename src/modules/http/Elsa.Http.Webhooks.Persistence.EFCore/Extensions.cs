using Elsa.Http.Webhooks.Persistence.EFCore;
using Elsa.Http.Webhooks.Persistence.Features;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

[PublicAPI]
public static class Extensions
{
    public static WebhookPersistenceFeature UseEntityFrameworkCore(this WebhookPersistenceFeature feature, Action<EFCoreWebhookPersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }

    public static WebhookPersistenceFeature UseEntityFrameworkCore<TDbContext>(this WebhookPersistenceFeature feature, Action<EFCoreWebhookPersistenceFeature<TDbContext>>? configure = null) where TDbContext : DbContext
    {
        feature.Module.Configure(configure);
        return feature;
    }
}
