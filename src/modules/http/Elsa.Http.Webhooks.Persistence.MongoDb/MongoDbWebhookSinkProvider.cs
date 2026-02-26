using Elsa.Http.Webhooks.Persistence.Contracts;
using WebhooksCore;

namespace Elsa.Http.Webhooks.Persistence.MongoDb;

public class MongoDbWebhookSinkProvider(IServiceScopeFactory scopeFactory) : IWebhookSinkProvider
{
    public async ValueTask<IEnumerable<WebhookSink>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var sinkStore = scope.ServiceProvider.GetRequiredService<IWebhookSinkStore>();
        var sinks = await sinkStore.ListAsync(cancellationToken: cancellationToken);

        return sinks
            .Where(x => !x.IsDeleted && x.IsEnabled)
            .Select(x => new WebhookSink
            {
                Id = x.Id,
                Name = x.Name,
                Url = x.Url,
                Filters = x.Filters
            })
            .ToList();
    }
}
