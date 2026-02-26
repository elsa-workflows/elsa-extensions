using Elsa.Features.Services;
using Elsa.Http.Webhooks.Api.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ModuleExtensions
{
    public static IModule UseWebhooksApi(this IModule module, Action<WebhooksApiFeature>? configure = null)
    {
        return module.Use(configure);
    }
}
