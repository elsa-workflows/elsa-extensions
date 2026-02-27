using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.Webhooks.Persistence.Features;
using Elsa.Http.Webhooks.Persistence.MongoDb.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebhooksCore;

namespace Elsa.Http.Webhooks.Persistence.MongoDb;

[DependsOn(typeof(WebhookPersistenceFeature))]
public class MongoDbWebhookPersistenceFeature(IModule module) : FeatureBase(module)
{
    public Action<MongoDbWebhookPersistenceOptions> ConfigureOptions { get; set; } = _ => { };

    public MongoDbWebhookPersistenceFeature Configure(Action<MongoDbWebhookPersistenceOptions> configure)
    {
        ConfigureOptions += configure;
        return this;
    }

    public override void Configure()
    {
        Module.Configure<WebhookPersistenceFeature>(feature =>
        {
            feature.UseWebhookSinkStore(sp => sp.GetRequiredService<MongoDbWebhookSinkStore>());
        });
    }

    public override void Apply()
    {
        Services.AddOptions<MongoDbWebhookPersistenceOptions>().Configure(ConfigureOptions);
        Services.AddScoped<MongoDbWebhookSinkStore>();
        Services.AddSingleton<IWebhookSinkProvider, MongoDbWebhookSinkProvider>();

        Services.AddSingleton<IMongoDatabase>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoDbWebhookPersistenceOptions>>();
            var client = sp.GetService<IMongoClient>() ?? new MongoClient();
            return client.GetDatabase("Elsa");
        });
    }
}
