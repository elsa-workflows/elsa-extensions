# Implementation Plan: Webhook Sink Persistence & Management

**Branch**: `002-webhooks-sink-persistence` | **Date**: 2026-02-26 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-webhooks-sink-persistence/spec.md`

## Summary

Add persistent webhook sink management for the `http` module area by introducing storage abstractions,
provider packages (EF Core + MongoDB), and REST management endpoints for sink CRUD.

The runtime model uses exactly one active `IWebhookSinkProvider` selected through DI.
Configuration-based sinks remain backward compatible by keeping the existing provider path available.

## Technical Context

**Language/Version**: C# with multi-targeted .NET packages (`net8.0;net9.0;net10.0`)  
**Primary Dependencies**: `WebhooksCore`, `Elsa.Api.Common`, `Elsa.Persistence.EFCore.Common`, MongoDB driver package(s)  
**Storage**: EF Core provider and MongoDB provider for webhook sink records  
**Testing**: Existing `test/modules/**` patterns (unit + integration tests)  
**Target Platform**: Elsa server hosts in `src/workbench/**` and downstream host apps  
**Project Type**: Modular .NET extension packages (library + API endpoints)  
**Performance Goals**: Sink lookup and CRUD remain suitable for interactive management and runtime dispatch setup  
**Constraints**: Scope is restricted to `src/modules/http/**` and the feature spec paths  
**Scale/Scope**: New packages in `http` area plus integration into existing `Elsa.Http.Webhooks`

## Constitution Check

*GATE: Must pass before implementation and re-check at PR time.*

- [x] Module-owned specs: this feature is documented under `specs/002-...` and targets module docs in `src/modules/http/README.md`.
- [x] Contract-shaped dependencies: persistence behind abstractions (`IWebhookSinkStore`, provider contracts), API uses application services.
- [x] SemVer/backward compatibility: configuration-only behavior remains available when store provider not registered.
- [x] Secrets hygiene: no credential material introduced in spec artifacts.
- [x] Scope control: all runtime/package work constrained to `src/modules/http/**`.

## Project Structure

### Documentation (this feature)

```text
specs/002-webhooks-sink-persistence/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── webhook-sinks.openapi.yaml
└── checklists/
    └── requirements.md
```

### Source Code (planned)

```text
src/modules/http/
├── Elsa.Http.Webhooks/                          # existing runtime package
├── Elsa.Http.Webhooks.Persistence/              # new abstractions + app services
├── Elsa.Http.Webhooks.Persistence.EFCore/       # new EF Core provider
├── Elsa.Http.Webhooks.Persistence.MongoDb/      # new MongoDB provider
└── Elsa.Http.Webhooks.Api/                      # new REST endpoints package

test/modules/http/
└── ... new tests for persistence + API
```

**Structure Decision**: Create new packages under the `http` module area to preserve module boundaries and align with existing `*.Api` and `*.Persistence.EFCore` naming patterns.

## Phase Plan

### Phase 0 — Contracts & Abstractions

1. Add `Elsa.Http.Webhooks.Persistence` package.
2. Define contracts: `IWebhookSinkStore`, query models, result models, and application service interfaces.
3. Define DI registration extension methods for abstraction + default service wiring.

**Exit Criteria**: Runtime package can consume abstraction without storage-specific references.

### Phase 1 — Provider Implementations

1. Add `Elsa.Http.Webhooks.Persistence.EFCore` package with concrete store implementation.
2. Add `Elsa.Http.Webhooks.Persistence.MongoDb` package with concrete store implementation.
3. Ensure parity of behavior between providers (validation, duplicate handling, not-found semantics).

**Exit Criteria**: Both providers satisfy the same store contract tests.

### Phase 2 — Runtime Integration

1. Add store-backed `IWebhookSinkProvider` implementation in `http` module scope.
2. Wire provider registration so hosts can choose exactly one active sink provider via DI.
3. Keep configuration-based provider path intact for backward compatibility.

**Exit Criteria**: Runtime resolves sinks through active provider implementation.

### Phase 3 — Management API

1. Add `Elsa.Http.Webhooks.Api` package exposing sink CRUD endpoints.
2. Reuse application-level services from abstraction package.
3. Apply existing Elsa API auth policy and consistent error mapping.

**Exit Criteria**: API contract in `contracts/webhook-sinks.openapi.yaml` is implemented and testable.

### Phase 4 — Tests & Docs

1. Add tests for application services and both providers.
2. Add API endpoint tests for CRUD + validation/error semantics.
3. Update module/package READMEs for registration and configuration.

**Exit Criteria**: Acceptance criteria from `spec.md` and checklist are satisfied.

## Risks & Mitigations

- **Risk**: Divergent behavior between EF Core and MongoDB providers.  
  **Mitigation**: Provider-agnostic contract test suite executed against both implementations.

- **Risk**: Ambiguous DI setup for sink provider selection.  
  **Mitigation**: Enforce explicit registration APIs and document single-provider requirement.

- **Risk**: API model drift from future Studio needs.  
  **Mitigation**: Keep API resource model stable and version endpoints if breaking change is required.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Additional packages in `http` area | Required to separate contracts, providers, and API surface | Single package would mix concerns and increase coupling |
