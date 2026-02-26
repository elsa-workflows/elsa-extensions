using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.Webhooks.Persistence.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebhooksCore;

namespace Elsa.Http.Webhooks.Persistence.EFCore;

[DependsOn(typeof(WebhookPersistenceFeature))]
public class EFCoreWebhookPersistenceFeature<TDbContext>(IModule module) : FeatureBase(module) where TDbContext : DbContext
{
    public override void Configure()
    {
        Module.Configure<WebhookPersistenceFeature>(feature => feature.UseWebhookSinkStore(sp => sp.GetRequiredService<EFCoreWebhookSinkStore<TDbContext>>()));
    }

    public override void Apply()
    {
        Services.AddScoped<EFCoreWebhookSinkStore<TDbContext>>();
        Services.AddSingleton<IWebhookSinkProvider, EFCoreWebhookSinkProvider>();
    }
}
