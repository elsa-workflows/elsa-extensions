# Research: Webhook Sink Persistence & Management

**Date**: 2026-02-26  
**Feature**: [spec.md](spec.md)

## Current-State Findings

- `Elsa.Http.Webhooks` currently relies on `WebhooksCore` and option-based sink/source registration.
- Existing feature registration methods (`RegisterSink`, `RegisterSinks`) configure `WebhookSinksOptions` in memory.
- Current module package has no persistence abstraction package and no sink-management API package.

## Relevant Repo Conventions

- API packages commonly use `*.Api` naming (e.g., `Elsa.Agents.Api`, `Elsa.Connections.Api`).
- EF Core persistence provider packages use `*.Persistence.EFCore` naming.
- `UseProjectReferences` dual-mode package/project-reference pattern is standard in `.csproj` files.

## Decisions Captured

1. **Single active sink provider**: only one `IWebhookSinkProvider` implementation is active via DI.
2. **API auth**: reuse existing Elsa API authorization conventions.
3. **REST package naming**: use `Elsa.Http.Webhooks.Api`.
4. **Scope**: runtime implementation remains inside `src/modules/http/**`.

## Open Technical Notes

- MongoDB package naming aligns with existing style as `Elsa.Http.Webhooks.Persistence.MongoDb`.
- Store contract should include duplicate handling semantics and deterministic not-found behavior.
- Dispatch/runtime integration should avoid hard dependency on provider-specific packages.

## Implementation Notes

- Added `Elsa.Http.Webhooks.Abstractions` for shared sink contracts and ID generation abstraction.
- Added `Elsa.Http.Webhooks.Persistence` for `IWebhookSinkStore`, `IWebhookSinkManagementService`, and default memory implementation.
- Added `Elsa.Http.Webhooks.Persistence.EFCore` and `Elsa.Http.Webhooks.Persistence.MongoDb` to own provider-specific `IWebhookSinkProvider` implementations.
- Added `Elsa.Http.Webhooks.Api` with CRUD + restore endpoints and conflict mapping for optimistic concurrency.

## Migration Guidance

1. Existing `Elsa.Http.Webhooks` config-based sink registration continues to work unchanged.
2. To move to persistent sinks, install `Elsa.Http.Webhooks.Persistence` plus one provider package.
3. Register one provider (`UseEntityFrameworkCore` or `UseMongoDb`) and then enable `UseWebhooksApi`.
4. Avoid registering multiple store-backed `IWebhookSinkProvider` implementations in the same host.
