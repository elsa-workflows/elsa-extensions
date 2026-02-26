# Data Model: Webhook Sink Persistence & Management

## Core Entities

## WebhookSink

Represents the logical sink used by runtime dispatch.

- `Id` (string, required) — stable sink identifier.
- `Url` (uri, required) — destination endpoint.
- `IsEnabled` (bool, required) — enable/disable dispatch.
- `Name` (string, optional) — display name for management/UI.
- `Description` (string, optional)
- `Headers` (map<string,string>, optional)
- `Authentication` (object, optional, shape TBD by WebhooksCore compatibility)
- `CreatedAt` (datetime)
- `UpdatedAt` (datetime)

## WebhookSinkRecord (Persistence)

Provider-specific stored representation of `WebhookSink`.

- EF Core entity/table mapping
- MongoDB document/collection mapping
- Includes persisted fields for identity, url, status, metadata, auditing timestamps

## WebhookSinkDto (API)

Transport contract for REST requests/responses.

- Create/Update request payload models
- List/Get response model
- Validation errors map to standard API error payloads

## Service/Contract Models

## IWebhookSinkStore

Contract for provider implementations.

- `CreateAsync`
- `FindAsync` (by id)
- `ListAsync`
- `UpdateAsync`
- `DeleteAsync`

## IWebhookSinkManagementService

Application service used by API and runtime integration points.

- Orchestrates validation + store operations
- Converts domain/store outcomes into application-level results

## Invariants

- Sink `Id` must be unique within the active provider.
- Sink `Url` must be absolute and valid.
- Disabled sinks are excluded from runtime dispatch where applicable.
- Updates preserve `CreatedAt`; mutate `UpdatedAt`.
