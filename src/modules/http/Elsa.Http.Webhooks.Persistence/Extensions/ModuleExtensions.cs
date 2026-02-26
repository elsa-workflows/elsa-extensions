using Elsa.Features.Services;
using Elsa.Http.Webhooks.Persistence.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseWebhookPersistence(this IModule module, Action<WebhookPersistenceFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
