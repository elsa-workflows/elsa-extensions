using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.Webhooks.Persistence.Entities;
using Elsa.Http.Webhooks.Persistence.Features;
using Elsa.Persistence.EFCore;
using Microsoft.Extensions.DependencyInjection;
using WebhooksCore;

namespace Elsa.Http.Webhooks.Persistence.EFCore;

[DependsOn(typeof(WebhookPersistenceFeature))]
public class EFCoreWebhookPersistenceFeature(IModule module) : PersistenceFeatureBase<EFCoreWebhookPersistenceFeature, WebhooksDbContext>(module)
{
    public override void Configure()
    {
        Module.Configure<WebhookPersistenceFeature>(feature => feature.UseWebhookSinkStore(sp => sp.GetRequiredService<EFCoreWebhookSinkStore>()));
    }

    public override void Apply()
    {
        base.Apply();
        AddEntityStore<WebhookSinkRecord, EFCoreWebhookSinkStore>();
        Services.AddSingleton<IWebhookSinkProvider, EFCoreWebhookSinkProvider>();
    }
}
