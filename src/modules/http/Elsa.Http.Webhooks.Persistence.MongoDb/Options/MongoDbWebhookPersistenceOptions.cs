namespace Elsa.Http.Webhooks.Persistence.MongoDb.Options;

public class MongoDbWebhookPersistenceOptions
{
    public string CollectionName { get; set; } = "WebhookSinks";
}
