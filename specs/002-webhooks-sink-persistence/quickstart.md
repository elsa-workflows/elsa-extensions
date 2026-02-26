# Quickstart: Webhook Sink Persistence & Management

## Goal

Enable persistent webhook sink management for `Elsa.Http.Webhooks` using provider packages and REST API.

## 1) Install packages

Install package set for selected provider:

- Core runtime: `Elsa.Http.Webhooks`
- Contracts/services: `Elsa.Http.Webhooks.Persistence`
- API: `Elsa.Http.Webhooks.Api`
- One provider:
  - `Elsa.Http.Webhooks.Persistence.EFCore`, or
  - `Elsa.Http.Webhooks.Persistence.MongoDb`

## 2) Register services

In host startup:

1. Enable webhooks feature.
2. Register persistence abstractions.
3. Register **exactly one** store-backed `IWebhookSinkProvider` implementation via DI.
4. Enable webhooks API endpoints.

Example:

```csharp
module
  .UseWebhooks()
  .UseWebhookPersistence(x => x.UseEntityFrameworkCore())
  // or: .UseWebhookPersistence(x => x.UseMongoDb())
  .UseWebhooksApi();
```

For EF Core, support two integration modes:

- **Standalone mode**: use provider-owned `WebhookPersistenceDbContext`.
- **Composed mode**: use host-owned `DbContext` and apply webhook mapping extensions.

## 2a) EF Core migrations (standalone context mode)

1. Ensure design-time setup can resolve `WebhookPersistenceDbContext`.
2. Generate migration:

```bash
dotnet ef migrations add InitWebhookSinks \
  --context WebhookPersistenceDbContext \
  --project src/modules/http/Elsa.Http.Webhooks.Persistence.EFCore \
  --startup-project <your-host-startup-project>
```

3. Apply migration:

```bash
dotnet ef database update \
  --context WebhookPersistenceDbContext \
  --project src/modules/http/Elsa.Http.Webhooks.Persistence.EFCore \
  --startup-project <your-host-startup-project>
```

## 2b) EF Core migrations (composed host context mode)

1. In your host `DbContext.OnModelCreating`, apply webhook sink model extensions from `Elsa.Http.Webhooks.Persistence.EFCore`.
2. Register webhook persistence EF Core provider using host-composed context overloads.
3. Generate migration using host context:

```bash
dotnet ef migrations add AddWebhookSinksToHostContext \
  --context <YourHostDbContext> \
  --project <your-host-data-project> \
  --startup-project <your-host-startup-project>
```

4. Apply migration using host context:

```bash
dotnet ef database update \
  --context <YourHostDbContext> \
  --project <your-host-data-project> \
  --startup-project <your-host-startup-project>
```

## 3) Validate management flow

1. Create sink through REST API.
2. List sinks and verify created sink exists.
3. Update sink and verify updated values are returned.
4. Delete sink and verify it no longer appears.

## 4) Validate runtime flow

1. Trigger a webhook event path that resolves sinks.
2. Confirm active sink provider is used.
3. Restart host and verify persisted sink remains available.

## 5) Backward compatibility check

Run host without persistence packages and confirm existing configuration-based sink setup still works.

## 6) Concurrency check

1. Read a sink and capture its `version`.
2. Update the sink with the captured `expectedVersion`.
3. Repeat update with the stale `expectedVersion` and confirm API returns conflict.
