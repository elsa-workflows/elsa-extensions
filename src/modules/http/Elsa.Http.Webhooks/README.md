# Elsa.Http.Webhooks

Core HTTP webhooks runtime package for receiving and broadcasting webhook events.

## Package Role

- Configures the `WebhooksFeature` runtime.
- Exposes webhook endpoints, activities, handlers, and source/sink registration APIs.

## Dependency Direction

`Elsa.Http.Webhooks` is the runtime dependency root for webhook functionality.

- It does **not** depend on `Elsa.Http.Webhooks.Persistence`, `Elsa.Http.Webhooks.Persistence.EFCore`, `Elsa.Http.Webhooks.Persistence.MongoDb`, or `Elsa.Http.Webhooks.Api`.
- Provider packages depend on this package and register `IWebhookSinkProvider` implementations externally.
