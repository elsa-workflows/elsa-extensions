using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Http.Webhooks.Client;
using Elsa.Studio.Http.Webhooks.Menu;
using Elsa.Studio.Http.Webhooks.UI.Pages.Services;
using Elsa.Studio.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Http.Webhooks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebhooksModule(this IServiceCollection services, BackendApiConfig backendApiConfig)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, WebhooksMenu>()
            .AddScoped<WebhookSinkOperationResultMapper>()
            .AddRemoteApi<IWebhookSinksApi>(backendApiConfig);
    }

    public static IServiceCollection AddWebhooksModule(this IServiceCollection services)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, WebhooksMenu>()
            .AddScoped<WebhookSinkOperationResultMapper>();
    }
}