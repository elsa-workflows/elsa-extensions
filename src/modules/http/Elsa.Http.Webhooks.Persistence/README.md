# Elsa.Http.Webhooks.Persistence

Provides persistence contracts and sink management services for `Elsa.Http.Webhooks`.

## Public Contract

- `IWebhookSinkStore` for persistence providers.
- `IWebhookSinkManagementService` for create/list/update/soft-delete/restore operations.
- `WebhookPersistenceFeature` and `UseWebhookPersistence` for DI registration.

## Dependency Direction

This package depends on `Elsa.Http.Webhooks` and `Elsa.Http.Webhooks.Abstractions`. The existing `Elsa.Http.Webhooks` package does not depend on this package.
