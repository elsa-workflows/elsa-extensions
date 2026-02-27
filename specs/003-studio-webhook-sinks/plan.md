# Implementation Plan: Studio Webhook Sink Management

**Branch**: `003-studio-webhook-sinks` | **Date**: 2026-02-27 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-studio-webhook-sinks/spec.md`

## Summary

Implement `Elsa.Studio.Http.Webhooks` from skeleton to complete sink management UX using the existing REST contract from `Elsa.Http.Webhooks.Api`.

The module will provide list/create/edit/soft-delete/restore flows, permission-aware actions (visible but disabled when unauthorized), explicit conflict handling (refresh + manual retry), and reuse established Studio patterns from modules such as `Elsa.Studio.Agents` and `Elsa.Studio.Secrets`.

## Technical Context

**Language/Version**: C# with Blazor Razor components, multi-targeted .NET packages (`net8.0;net9.0;net10.0`)  
**Primary Dependencies**: `Elsa.Studio.Shared`, `Elsa.Studio.Workflows`, MudBlazor components, Radzen availability via shared Studio stack, Refit-style remote API registration through `AddRemoteApi<T>`  
**Storage**: N/A in Studio module (state persisted by backend via existing webhook sink API)  
**Testing**: Existing module test patterns under `test/modules/**`; component/service unit tests where practical plus integration checks against API client abstractions  
**Target Platform**: Browser-hosted Elsa Studio modules used by Server/Server+Studio hosts  
**Project Type**: Modular .NET extension package (Blazor UI module + remote API client contracts)  
**Performance Goals**: Initial sink list and CRUD UX responsive for typical admin datasets (spec SC-001 within 3s)  
**Constraints**: Use existing list contract (no pagination requirement); required form fields are `Name` and `Target URL`; conflict flow requires explicit refresh/manual retry; scope limited to declared paths  
**Scale/Scope**: Single Studio module (`Elsa.Studio.Http.Webhooks`) with focused UI, API client surface, and docs updates

## Constitution Check (Pre-Design)

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Module-owned specs: feature documented under `specs/003-studio-webhook-sinks/` and implementation scoped to `http` Studio module.
- [x] Contract-shaped dependencies: Studio consumes backend via remote API contracts; no backend persistence internals referenced.
- [x] Package compatibility & SemVer: no breaking public API changes required for this phase; behavior added inside module boundaries.
- [x] Security & secrets hygiene: no secret material introduced; credentials remain host/session-managed.
- [x] Quality gates: plan includes code changes, tests, and docs updates to keep module usable by downstream hosts.

## Project Structure

### Documentation (this feature)

```text
specs/003-studio-webhook-sinks/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── studio-webhook-sinks.md
└── tasks.md
```

### Source Code (planned)

```text
src/modules/http/
└── Elsa.Studio.Http.Webhooks/
    ├── Client/
    ├── UI/
    │   ├── Pages/
    │   ├── Components/
    │   └── Validators/
    ├── Menu/
    ├── Extensions/
    ├── Module.cs
    └── _Imports.razor

test/modules/http/
└── ... (new tests for Studio webhook module behavior)
```

**Structure Decision**: Keep all runtime changes in `Elsa.Studio.Http.Webhooks`, mirroring established module structure used by `Elsa.Studio.Agents` and `Elsa.Studio.Secrets` (remote API interface + page components + validators + menu/feature registration).

## Phase 0 — Research

1. Confirm Studio CRUD implementation pattern from existing modules (`Agents`, `Secrets`):
   - API client registration with `AddRemoteApi<T>(backendApiConfig)`
   - `MudTable` + server reload pattern
   - Dialog-based create flow and page-based edit flow
2. Validate UI component strategy:
   - Prefer MudBlazor for tables/forms/dialogs.
   - Use Radzen only if a required interaction is not covered well by MudBlazor.
3. Confirm API integration contract for webhooks sink endpoints and error semantics used by Studio.
4. Confirm sibling `elsa-studio` source alignment for framework usage and module composition.

**Exit Criteria**: All technical unknowns resolved and captured in `research.md` decisions.

## Phase 1 — Design & Contracts

1. Define data model for Studio-side entities/state objects:
   - sink list item model
   - create/edit form model
   - permission state/action availability model
   - operation result model (success/validation/conflict/auth/network)
2. Define interface/behavior contract artifact for this feature:
   - UI actions and expected API mappings
   - conflict and permission behavior
   - soft-delete + restore lifecycle behavior
3. Produce quickstart with concrete module wiring and manual verification flow.

**Exit Criteria**: `data-model.md`, `contracts/studio-webhook-sinks.md`, and `quickstart.md` are complete and internally consistent.

## Phase 2 — Implementation Planning

1. Add Webhooks remote API client interface in Studio module (matching existing backend endpoints and payloads).
2. Implement list page:
   - load sinks
   - default active-only view
   - include-deleted toggle behavior
   - per-row actions (edit/delete/restore)
3. Implement create/edit flows:
   - required fields (`Name`, `Target URL`)
   - validation and unsaved input preservation on failure
4. Implement permission-aware UX:
   - show all actions
   - disable unauthorized actions with explanation
5. Implement conflict/error handling:
   - conflict: require refresh + manual retry
   - map validation/auth/network failures to actionable UI feedback
6. Integrate menu, module registration, and docs updates.
7. Add tests for critical behaviors and regressions.

**Exit Criteria**: Feature stories in `spec.md` are covered by planned tasks and test strategy.

## Risks & Mitigations

- **Risk**: Backend contract drift between Studio and `Elsa.Http.Webhooks.Api`.  
  **Mitigation**: Keep API interface focused on current endpoint contract and capture mapping in `contracts/studio-webhook-sinks.md`.

- **Risk**: Permission behavior inconsistencies across screens.  
  **Mitigation**: Reuse existing permission-aware UI patterns and explicitly test disabled-action behavior.

- **Risk**: Conflict handling causes user confusion.  
  **Mitigation**: Standardized conflict messaging + explicit refresh action + no silent retries.

- **Risk**: Scope creep into pagination or advanced metadata editing.  
  **Mitigation**: Explicitly defer pagination and non-required fields to follow-up spec/tasks.

## Constitution Check (Post-Design)

- [x] Module-owned specs preserved and expanded with design artifacts.
- [x] Dependencies remain contract-shaped (Studio API client only; no internals coupling).
- [x] Backward compatibility preserved (module extension, no forced host behavior change).
- [x] Security posture unchanged (no new secret handling responsibilities in client).
- [x] Quality gates represented in planned test/docs deliverables.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
