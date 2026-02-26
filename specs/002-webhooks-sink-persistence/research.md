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
