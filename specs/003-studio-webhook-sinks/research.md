# Phase 0 Research — Studio Webhook Sink Management

## Decision 1: Reuse established Studio CRUD module pattern (`Secrets`/`Agents`)

- **Decision**: Implement the Webhooks Studio module using the same structural pattern as `Elsa.Studio.Secrets` and `Elsa.Studio.Agents`: remote API interface + `MudTable` list page + dialog/page edit interactions.
- **Rationale**: This pattern is already proven in this codebase, aligns with existing DI/module wiring (`AddRemoteApi<T>` and `IBackendApiClientProvider`), and minimizes architectural risk.
- **Alternatives considered**:
  - Build a custom client/service stack with direct `HttpClient`: rejected due to inconsistency with existing modules and higher maintenance.
  - Implement all forms inline in one page: rejected due to reduced maintainability and poorer parity with existing UX conventions.

## Decision 2: UI component strategy is MudBlazor-first, Radzen-by-exception

- **Decision**: Use MudBlazor components for primary list/form/dialog UX; only introduce Radzen components if a required interaction cannot be achieved with MudBlazor.
- **Rationale**: Existing CRUD modules (`Secrets`, `Agents`) already implement this style with MudBlazor, and it keeps behavior/theming consistent. Radzen is available in the shared stack as a fallback.
- **Alternatives considered**:
  - Radzen-first implementation: rejected because module conventions and existing examples are predominantly MudBlazor for CRUD.
  - Mixed MudBlazor/Radzen without explicit need: rejected to avoid unnecessary UI complexity.

## Decision 3: API contract consumption remains as-is (no pagination changes)

- **Decision**: Consume existing `Elsa.Http.Webhooks.Api` endpoints as currently implemented; do not add pagination requirements in this feature.
- **Rationale**: Clarification decisions explicitly defer pagination, and the current feature scope is complete CRUD + soft-delete/restore lifecycle.
- **Alternatives considered**:
  - Add server-side pagination now: rejected due to scope expansion and backend contract changes.
  - Add client-side pseudo-pagination over full result set: rejected as unnecessary complexity for current scope.

## Decision 4: Comprehensive permission-aware UI behavior is deferred

- **Decision**: Do not require comprehensive permission-aware UI checks in this feature; defer to a dedicated follow-up feature.
- **Rationale**: Elsa and Elsa Studio do not yet provide a comprehensive permission-aware system suitable for full UX enforcement in this scope.
- **Alternatives considered**:
  - Implement ad-hoc UI permission checks now: rejected because it introduces inconsistent behavior and technical debt.
  - Block feature delivery until permission system exists: rejected because core webhook sink management can ship independently.

## Decision 5: Conflict handling is explicit refresh + manual retry

- **Decision**: On conflict responses, show conflict feedback, require refresh, and let user manually retry. No silent retry or force overwrite.
- **Rationale**: Matches clarification outcome and preserves safety for concurrent admin edits.
- **Alternatives considered**:
  - Auto-retry with latest version: rejected because it can obscure concurrent changes.
  - Force-overwrite with one-click confirmation: rejected for higher risk of accidental data loss.

## Decision 6: Minimum required create/edit fields are `Name` and `Target URL`

- **Decision**: Require only `Name` and `Target URL` in Studio create/edit UX for this feature; other metadata remains optional/deferred.
- **Rationale**: Aligns to clarified scope and keeps MVP complete without over-designing metadata UX.
- **Alternatives considered**:
  - Require event filters as mandatory: rejected based on clarification outcome.
  - Add extended fields now (headers/auth metadata): rejected as scope expansion.

## Decision 7: Include restore flow in this feature

- **Decision**: Implement restore action for soft-deleted sinks within the same module feature scope.
- **Rationale**: Soft-delete lifecycle is incomplete for operators without restore; API already supports restore operation.
- **Alternatives considered**:
  - Omit restore and keep delete-only: rejected because it creates operational risk and incomplete lifecycle coverage.
