# Elsa.Http.Webhooks.Persistence.EFCore

Entity Framework Core implementation for `Elsa.Http.Webhooks` sink persistence.

## Registration

```csharp
module
    .UseWebhooks()
    .UseWebhookPersistence(x => x.UseEntityFrameworkCore());
```

This package provides:

- `IWebhookSinkStore` backed by EF Core (`EFCoreWebhookSinkStore`)
- runtime `IWebhookSinkProvider` backed by store (`EFCoreWebhookSinkProvider`)
- `WebhookPersistenceDbContext` with sink mapping and JSON storage for headers/filters

The package owns provider registration so `Elsa.Http.Webhooks` remains free of direct dependencies on persistence providers.
