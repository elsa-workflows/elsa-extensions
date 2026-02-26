using Elsa.Http.Webhooks.Persistence.EFCore;
using Elsa.Http.Webhooks.Persistence.Features;
using JetBrains.Annotations;

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
}
