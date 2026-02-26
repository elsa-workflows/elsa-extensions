# Elsa.Http.Webhooks.Persistence.MongoDb

MongoDB implementation for webhook sink persistence.

## Registration

```csharp
module
    .UseWebhooks()
    .UseWebhookPersistence(x => x.UseMongoDb());
```

This package provides:

- `IWebhookSinkStore` MongoDB adapter (`MongoDbWebhookSinkStore`)
- runtime store-backed `IWebhookSinkProvider` (`MongoDbWebhookSinkProvider`)
- feature registration and options (`MongoDbWebhookPersistenceFeature`)
