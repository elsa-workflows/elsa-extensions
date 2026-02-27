# Elsa.Http.Webhooks.Api

Management API for webhook sinks.

## Registration

```csharp
module
    .UseWebhooks()
    .UseWebhookPersistence()
    .UseWebhooksApi();
```

## Endpoints

- `GET /webhook-sinks`
- `GET /webhook-sinks/{id}`
- `POST /webhook-sinks`
- `POST /webhook-sinks/{id}`
- `DELETE /webhook-sinks/{id}`
- `POST /webhook-sinks/{id}/restore`

Authorization follows existing Elsa API conventions via `ConfigurePermissions`.
