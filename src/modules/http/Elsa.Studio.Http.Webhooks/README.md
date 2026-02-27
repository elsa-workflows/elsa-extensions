# Elsa.Studio.Http.Webhooks

Studio module for managing webhook sinks through the existing `Elsa.Http.Webhooks.Api` REST endpoints.

## Registration

```csharp
services.AddWebhooksModule(backendApiConfig);
```

## Feature Scope

- List webhook sinks (`GET /webhook-sinks`)
- Create webhook sink (`POST /webhook-sinks`)
- Update webhook sink (`POST /webhook-sinks/{id}`)
- Soft delete webhook sink (`DELETE /webhook-sinks/{id}`)
- Restore webhook sink (`POST /webhook-sinks/{id}/restore`)

## Authorization

Authorization policies are enforced by the backend API.

## Prerequisites

- Host registers `Elsa.Http.Webhooks.Api` on the backend.
- Studio host provides `BackendApiConfig` and remote API wiring.
- Webhook persistence feature is enabled on the backend.
