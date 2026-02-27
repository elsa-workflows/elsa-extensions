# Elsa.Http.Webhooks.Persistence.EFCore

Entity Framework Core implementation for `Elsa.Http.Webhooks` sink persistence.

## Registration

```csharp
module
    .UseWebhooks()
    .UseWebhookPersistence(x => x.UseEntityFrameworkCore());
```

### Composed Host DbContext Mode

If your host owns the DbContext, you can compose webhook persistence into it without inheriting from `WebhooksDbContext`:

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
- `WebhooksDbContext` with sink mapping and JSON storage for headers/filters

## Migrations

Migrations are **consumer-owned**. This package does not ship provider-specific migrations; application developers generate and maintain migrations in their own solution.

### Standalone Context (`WebhooksDbContext`)

If you use `WebhooksDbContext` directly, create migrations in your own data/migrations project (not in this module package):

```bash
dotnet ef migrations add InitWebhookSinks \
    --context WebhooksDbContext \
    --project <your-app-data-or-migrations-project> \
    --startup-project <your-host-startup-project>

dotnet ef database update \
    --context WebhooksDbContext \
    --project <your-app-data-or-migrations-project> \
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

For workbench-style development hosts (such as `Elsa.Server.Web` and `Elsa.ServerAndStudio.Web`), follow the same rule: keep migrations in a consumer-owned project and use the host project only as `--startup-project`.

The package owns provider registration so `Elsa.Http.Webhooks` remains free of direct dependencies on persistence providers.
