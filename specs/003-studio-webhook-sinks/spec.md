# Feature Specification: Studio Webhook Sink Management

**Feature Branch**: `003-studio-webhook-sinks`  
**Created**: 2026-02-27  
**Status**: Draft  
**Input**: User description: "We have a Blazor project called Elsa.Studio.Http.Webhooks, which is not yet fully implemented; in fact, it's more of an initial skeleton. We need to fully implement this module such that it allows users to manage webhook sinks. Webhooks sinks are exposed via a REST API as implemented in Elsa.Http.Webhooks.Api. Users should be able to list existing sinks, add new ones, edit and (soft) delete existing ones."

## Implementation Scope *(mandatory)*

**Module Area**: `http`  
**Primary Module**: `Elsa.Studio.Http.Webhooks`  
**Target Packages**: `Elsa.Studio.Http.Webhooks` (primary), `Elsa.Http.Webhooks.Api` (consumed contract only, no intentional behavior changes)  
**In-Scope Paths**: `src/modules/http/Elsa.Studio.Http.Webhooks/**`, `src/modules/http/README.md` (if module docs need updates), `specs/003-studio-webhook-sinks/**`, `test/modules/http/**` (if Studio module tests are added)  
**Out-of-Scope Paths**: `src/modules/http/Elsa.Http.Webhooks.Persistence*/**`, `src/modules/http/Elsa.Http.Webhooks/**` (except API consumption compatibility fixes), `src/workbench/**` runtime behavior changes unrelated to Studio module registration

**Scope Rules**:

- Studio module implementation MUST consume the existing webhook sink management API contract rather than duplicating backend logic.
- If API gaps block required user flows, the spec MAY be amended with minimal API contract extensions.
- Comprehensive permission-aware UI behavior is deferred to a separate future feature.
- UX and module behavior changes MUST remain limited to Studio webhook sink management concerns.
- Pagination changes to API or Studio list contracts are out of scope for this feature.

## Clarifications

### Session 2026-02-27

- Q: Should Studio include restore for soft-deleted sinks? → A: Yes, include restore in this feature.
- Q: Should pagination be required for sink listing in this feature? → A: No, use the current list contract without pagination requirements.
- Q: What are the minimum required fields for create/edit in this feature? → A: `Name` and `Target URL` only.
- Q: How should conflict responses be handled in Studio? → A: Show conflict, require refresh, and manual retry.
- Q: Should comprehensive permission-aware UI checks be required in this feature? → A: No, defer to a separate future feature.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View webhook sinks in Studio (Priority: P1)

As an operator, I can open the Webhooks page and view existing webhook sinks so I can understand current outbound webhook targets.

**Why this priority**: Listing is the entry point for all management actions and validates that Studio is correctly connected to the backend.

**Independent Test**: Can be tested by loading the Webhooks page against an environment with pre-seeded sinks and verifying the list renders expected records and soft-deleted visibility behavior.

**Acceptance Scenarios**:

1. **Given** the user opens the Webhooks page, **When** the API list request succeeds, **Then** Studio loads and displays sinks returned by the API list endpoint.
2. **Given** no sinks exist, **When** the user opens the Webhooks page, **Then** Studio displays an empty-state message with a clear path to create a sink.
3. **Given** the API request fails, **When** the page loads, **Then** Studio shows a recoverable error state and allows retry.

---

### User Story 2 - Create and edit webhook sinks (Priority: P2)

As an operator, I can create a new sink and edit an existing sink from Studio so I can maintain webhook delivery targets without using direct API tooling.

**Why this priority**: Create and edit flows provide the operational value of the module and remove manual REST client usage.

**Independent Test**: Can be tested by creating a sink, verifying it appears in the list, editing it, and verifying the updated values are persisted and reloaded from the API.

**Acceptance Scenarios**:

1. **Given** valid required fields (`Name` and `Target URL`), **When** the user submits the create form, **Then** Studio creates the sink through the API and shows the new sink in the list.
2. **Given** an existing sink, **When** the user updates sink fields and saves, **Then** Studio sends the update request and refreshes the list with updated data.
3. **Given** API validation or conflict errors, **When** create or edit is submitted, **Then** Studio surfaces actionable validation/conflict feedback without losing unsaved user input.

---

### User Story 3 - Soft delete and restore webhook sinks (Priority: P3)

As an operator, I can soft delete and restore sinks from Studio so that obsolete webhook targets are removed from active use while remaining recoverable.

**Why this priority**: Deletion is a critical lifecycle action and must align with backend soft-delete semantics.

**Independent Test**: Can be tested by deleting a sink from Studio, verifying it is removed from default active list results, then restoring it and verifying it appears in active list results again.

**Acceptance Scenarios**:

1. **Given** an existing active sink, **When** the user confirms delete, **Then** Studio calls the delete endpoint and removes the sink from the default list.
2. **Given** a stale version/conflict is returned by the API during delete, **When** the action fails, **Then** Studio informs the user and prompts refresh before retry.
3. **Given** a soft-deleted sink, **When** the user triggers restore, **Then** Studio calls the restore endpoint and the sink reappears in the default active list.

### Edge Cases

- Concurrent edits by multiple users result in conflict responses for stale versions.
- API is reachable but returns partial/invalid payload fields for one or more sinks.
- Network interruption occurs during save/delete and leaves uncertain final state.
- Sink list is large enough to require responsive loading behavior.
- A sink referenced in the UI no longer exists when opening edit details.
- Large sink-volume pagination behavior is deferred to a follow-up feature.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Studio Webhooks page MUST list webhook sinks by consuming the API endpoint for sink listing.
- **FR-001a**: The Studio module MUST consume the current sink list contract as-is and MUST NOT require pagination changes for feature acceptance.
- **FR-002**: The Studio module MUST provide an add-sink flow that captures required fields (`Name` and `Target URL`) and submits them to the API create endpoint.
- **FR-003**: The Studio module MUST provide an edit-sink flow that preloads and persists required fields (`Name` and `Target URL`) through the API update endpoint.
- **FR-003a**: Event filters are not required input for this feature and remain optional/deferred.
- **FR-004**: The Studio module MUST provide a delete action that performs soft delete through the API delete endpoint.
- **FR-004a**: The Studio module MUST provide a restore action for soft-deleted sinks through the API restore endpoint.
- **FR-005**: The default list view MUST exclude soft-deleted sinks unless an explicit include-deleted view/filter is enabled.
- **FR-006**: The module MUST surface API validation, conflict, authorization, and transport errors with user-readable messages and recovery actions.
- **FR-006a**: On conflict responses, Studio MUST require explicit refresh and manual retry, and MUST NOT auto-retry or force-overwrite.
- **FR-007**: The module MUST preserve unsaved user-entered data when API save attempts fail due to validation or transient errors.
- **FR-008**: The module MUST include loading, empty, success, and failure states for list and mutation operations.
- **FR-009**: The module MUST keep navigation entry and route discoverable through the existing Studio menu group.
- **FR-010**: The feature MUST remain backward compatible with existing hosts that include the Studio module but do not yet navigate to webhook management.
- **FR-011**: Module documentation MUST describe configuration prerequisites for API connectivity.

### Key Entities *(include if feature involves data)*

- **Webhook Sink View Model**: Studio representation of a sink record shown in list and edit contexts (identifier, name, target URL, status, version; optional metadata not required by this feature).
- **Webhook Sink Form State**: Mutable client-side state for create/edit interactions, including validation messages and dirty/submit status.
- **Webhook Sink List Query State**: Client-side state for list retrieval options (search/filter, include deleted toggle, pagination/sorting if present).
- **Webhook Sink Action Result**: Normalized operation result used by UI to render success, validation errors, conflicts, and retry guidance.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Authorized users can load the sink list in Studio and view existing sinks within 3 seconds at p95 for a reference dataset of 120 sinks (100 active, 20 soft-deleted) measured over 20 runs.
- **SC-001a**: Feature acceptance is independent of introducing pagination behavior.
- **SC-002**: Users can successfully create a new sink from Studio end-to-end in under 90 seconds on first attempt.
- **SC-003**: Users can update an existing sink and observe persisted changes reflected in the list without page reload in at least 19 of 20 consecutive successful update attempts.
- **SC-004**: Users can soft delete a sink from Studio and the sink no longer appears in the default active list immediately after completion.
- **SC-004a**: Users can restore a soft-deleted sink from Studio and the sink reappears in the default active list immediately after completion.
- **SC-005**: In conflict or validation failure scenarios, users receive actionable feedback and can recover without losing unsaved input in at least 19 of 20 scripted failure-case trials.

## Assumptions

- The existing webhook sink API endpoints in `Elsa.Http.Webhooks.Api` remain the source of truth for sink CRUD behavior.
- The Studio module uses existing Elsa Studio authentication/session context and does not introduce a new auth model.
- Soft-delete semantics are enforced by the API and persistence layer; Studio reflects, but does not redefine, those rules.
- Include-deleted and restore behavior are exposed in UI and follow existing API contracts.
- Pagination concerns are explicitly deferred and not required for this feature.
- Comprehensive permission-aware UI behavior is explicitly deferred to a separate future feature.
- Create/edit minimum field set in this feature is `Name` and `Target URL`; additional sink metadata is deferred unless already provided by API defaults.
