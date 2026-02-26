using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.Webhooks.Abstractions.Contracts;
using Elsa.Http.Webhooks.Features;
using Elsa.Http.Webhooks.Persistence.Contracts;
using Elsa.Http.Webhooks.Persistence.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.Webhooks.Persistence.Features;

[DependsOn(typeof(WebhooksFeature))]
public class WebhookPersistenceFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, IWebhookSinkStore> _storeFactory = sp => sp.GetRequiredService<MemoryWebhookSinkStore>();

    public WebhookPersistenceFeature UseWebhookSinkStore(Func<IServiceProvider, IWebhookSinkStore> factory)
    {
        _storeFactory = factory;
        return this;
    }

    public override void Apply()
    {
        Services.AddScoped(_storeFactory);
        Services.AddScoped<MemoryWebhookSinkStore>();
        Services.AddScoped<IWebhookSinkManagementService, WebhookSinkManagementService>();
        Services.AddSingleton<IGenerateWebhookSinkId, DefaultWebhookSinkIdGenerator>();
    }
}
