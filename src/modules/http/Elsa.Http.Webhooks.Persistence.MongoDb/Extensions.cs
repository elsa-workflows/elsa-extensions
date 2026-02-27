using Elsa.Http.Webhooks.Persistence.Features;
using Elsa.Http.Webhooks.Persistence.MongoDb;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

[PublicAPI]
public static class Extensions
{
    public static WebhookPersistenceFeature UseMongoDb(this WebhookPersistenceFeature feature, Action<MongoDbWebhookPersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}
