# Elsa.Http.Webhooks.Persistence.EFCore

Entity Framework Core implementation for `Elsa.Http.Webhooks` sink persistence.

## Registration

```csharp
module
    .UseWebhooks()
    .UseWebhookPersistence(x => x.UseEntityFrameworkCore());
```

### Composed Host DbContext Mode

If your host owns the DbContext, you can compose webhook persistence into it without inheriting from `WebhookPersistenceDbContext`:

```csharp
public class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyWebhookPersistence();
        base.OnModelCreating(modelBuilder);
    }
}

module
    .UseWebhooks()
    .UseWebhookPersistence(x => x.UseEntityFrameworkCore<AppDbContext>());
```

This package provides:

- `IWebhookSinkStore` backed by EF Core (`EFCoreWebhookSinkStore`)
- runtime `IWebhookSinkProvider` backed by store (`EFCoreWebhookSinkProvider`)
- `WebhookPersistenceDbContext` with sink mapping and JSON storage for headers/filters

## Migrations

### Standalone Context (`WebhookPersistenceDbContext`)

```bash
dotnet ef migrations add InitWebhookSinks \
    --context WebhookPersistenceDbContext \
    --project src/modules/http/Elsa.Http.Webhooks.Persistence.EFCore \
    --startup-project <your-host-startup-project>

dotnet ef database update \
    --context WebhookPersistenceDbContext \
    --project src/modules/http/Elsa.Http.Webhooks.Persistence.EFCore \
    --startup-project <your-host-startup-project>
```

### Composed Host Context (`TDbContext`)

```bash
dotnet ef migrations add AddWebhookSinksToHostContext \
    --context <YourHostDbContext> \
    --project <your-host-data-project> \
    --startup-project <your-host-startup-project>

dotnet ef database update \
    --context <YourHostDbContext> \
    --project <your-host-data-project> \
    --startup-project <your-host-startup-project>
```

In composed mode, ensure your host `DbContext` calls `modelBuilder.ApplyWebhookPersistence()`.

The package owns provider registration so `Elsa.Http.Webhooks` remains free of direct dependencies on persistence providers.
