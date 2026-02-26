# Elsa.Http.Webhooks.Abstractions

Shared contracts and types consumed by the Webhooks runtime, persistence providers, and API package.

## Public Contract

- `PersistedWebhookSink` storage-facing sink model.
- `WebhookSinkFilter` listing/filter contract.
- `IGenerateWebhookSinkId` abstraction for server-generated sink IDs.

## Dependency Rule

This package is dependency-safe for cross-package sharing and is intended to avoid reverse dependencies on provider/API packages from `Elsa.Http.Webhooks`.
