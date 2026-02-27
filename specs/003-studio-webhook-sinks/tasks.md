# Tasks: Studio Webhook Sink Management

**Input**: Design documents from `/specs/003-studio-webhook-sinks/`  
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/, quickstart.md

**Tests**: No explicit TDD requirement in the feature specification; test tasks are not mandatory in this task list.

**Organization**: Tasks are grouped by user story to enable independent implementation and validation.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete tasks)
- **[Story]**: User story label (`[US1]`, `[US2]`, `[US3]`) for user-story phase tasks
- Every task includes an exact file path

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare Studio webhook module structure and baseline registration wiring.

- [ ] T001 Create UI folder structure `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages`, `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Components`, and `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Validators`
- [ ] T002 Create client folder `src/modules/http/Elsa.Studio.Http.Webhooks/Client` for remote API contracts
- [ ] T003 [P] Update namespace imports in `src/modules/http/Elsa.Studio.Http.Webhooks/_Imports.razor` to align with module UI patterns
- [ ] T004 [P] Update package metadata and dependencies in `src/modules/http/Elsa.Studio.Http.Webhooks/Elsa.Studio.Http.Webhooks.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implement core plumbing required by all user stories.

**⚠️ CRITICAL**: User story implementation starts only after this phase completes.

- [ ] T005 Create webhook sink API interface in `src/modules/http/Elsa.Studio.Http.Webhooks/Client/IWebhookSinksApi.cs`
- [ ] T006 Create request/response DTO mappings in `src/modules/http/Elsa.Studio.Http.Webhooks/Client/WebhookSinkApiModels.cs`
- [ ] T007 Add module service registration with `AddRemoteApi<IWebhookSinksApi>` in `src/modules/http/Elsa.Studio.Http.Webhooks/Extensions/ServiceCollectionExtensions.cs`
- [ ] T008 Create shared page state models in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/Models/WebhookSinkPageModels.cs`
- [ ] T009 Create permission/action availability helper in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/Services/WebhookSinkActionAvailabilityService.cs`
- [ ] T010 Create operation result/error normalization helper in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/Services/WebhookSinkOperationResultMapper.cs`
- [ ] T011 Create form validator for required fields in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Validators/WebhookSinkInputValidator.cs`
- [ ] T012 Update module feature registration in `src/modules/http/Elsa.Studio.Http.Webhooks/Module.cs`

**Checkpoint**: Core API client, validation, and module plumbing are ready.

---

## Phase 3: User Story 1 - View webhook sinks in Studio (Priority: P1) 🎯 MVP

**Goal**: Provide a functional Webhooks list page with loading, empty, error, and permission-aware action visibility states.

**Independent Test**: Open `/webhooks`, load existing sinks, verify empty/retry states, and verify unauthorized actions are visible but disabled with guidance.

### Implementation for User Story 1

- [ ] T013 [P] [US1] Implement webhooks list page markup in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/WebhookSinks.razor`
- [ ] T014 [US1] Implement list page code-behind with server reload and retry in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/WebhookSinks.razor.cs`
- [ ] T015 [P] [US1] Add include-deleted toggle and query-state binding in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/WebhookSinks.razor`
- [ ] T016 [US1] Implement default active-only list query behavior in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/WebhookSinks.razor.cs`
- [ ] T017 [P] [US1] Add disabled-action tooltip/message rendering for missing permissions in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/WebhookSinks.razor`
- [ ] T018 [US1] Replace skeleton route page by routing to list page in `src/modules/http/Elsa.Studio.Http.Webhooks/Pages/Index.razor`

**Checkpoint**: US1 is independently functional and demoable.

---

## Phase 4: User Story 2 - Create and edit webhook sinks (Priority: P2)

**Goal**: Enable create and edit flows with required fields (`Name`, `Target URL`) and robust validation/conflict handling.

**Independent Test**: Create a sink, edit it, verify persistence after reload, and confirm validation/conflict feedback preserves unsaved input.

### Implementation for User Story 2

- [ ] T019 [P] [US2] Implement create dialog UI in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Components/CreateWebhookSinkDialog.razor`
- [ ] T020 [US2] Implement create dialog logic and submit pipeline in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Components/CreateWebhookSinkDialog.razor.cs`
- [ ] T021 [P] [US2] Implement edit page UI in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/WebhookSink.razor`
- [ ] T022 [US2] Implement edit page load/save flow in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/WebhookSink.razor.cs`
- [ ] T023 [US2] Wire create action from list page to dialog in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/WebhookSinks.razor.cs`
- [ ] T024 [US2] Wire row edit navigation/action from list page in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/WebhookSinks.razor.cs`
- [ ] T025 [US2] Enforce required field validation (`Name`, `Target URL`) in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Validators/WebhookSinkInputValidator.cs`
- [ ] T026 [US2] Implement conflict handling (refresh + manual retry, no auto-overwrite) in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/Services/WebhookSinkOperationResultMapper.cs`
- [ ] T027 [US2] Ensure unsaved form values persist on validation/conflict/network failure in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Components/CreateWebhookSinkDialog.razor.cs` and `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/WebhookSink.razor.cs`

**Checkpoint**: US2 is independently functional and demoable.

---

## Phase 5: User Story 3 - Soft delete and restore webhook sinks (Priority: P3)

**Goal**: Complete sink lifecycle operations with soft delete and restore actions in Studio.

**Independent Test**: Soft delete a sink and confirm removal from active list; include deleted and restore it; confirm reappearance in active list.

### Implementation for User Story 3

- [ ] T028 [US3] Implement delete confirmation flow in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/WebhookSinks.razor.cs`
- [ ] T029 [US3] Implement soft-delete API invocation and list refresh in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/WebhookSinks.razor.cs`
- [ ] T030 [P] [US3] Implement restore action UI for deleted sinks in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/WebhookSinks.razor`
- [ ] T031 [US3] Implement restore API invocation and active-list reinstate in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/WebhookSinks.razor.cs`
- [ ] T032 [US3] Apply permission-aware disabled behavior to delete/restore actions in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/WebhookSinks.razor`
- [ ] T033 [US3] Handle stale/not-found outcomes for delete/restore with refresh guidance in `src/modules/http/Elsa.Studio.Http.Webhooks/UI/Pages/Services/WebhookSinkOperationResultMapper.cs`

**Checkpoint**: US3 is independently functional and demoable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finalize documentation, wiring consistency, and verification checklist.

- [ ] T034 [P] Update webhooks menu metadata/path consistency in `src/modules/http/Elsa.Studio.Http.Webhooks/Menu/WebhooksMenu.cs`
- [ ] T035 [P] Add module README with registration, permissions, and API prerequisites in `src/modules/http/Elsa.Studio.Http.Webhooks/README.md`
- [ ] T036 [P] Update module-area documentation references in `src/modules/http/README.md`
- [ ] T037 Validate quickstart scenarios against implemented UI flow in `specs/003-studio-webhook-sinks/quickstart.md`
- [ ] T038 Run regression/build validation for module package in `src/modules/http/Elsa.Studio.Http.Webhooks/Elsa.Studio.Http.Webhooks.csproj`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: no dependencies
- **Phase 2 (Foundational)**: depends on Phase 1 and blocks user stories
- **Phase 3 (US1)**: depends on Phase 2
- **Phase 4 (US2)**: depends on Phase 2 and builds on list UX from US1
- **Phase 5 (US3)**: depends on Phase 2 and builds on list actions from US1
- **Phase 6 (Polish)**: depends on completion of desired user stories

### User Story Dependencies

- **US1 (P1)**: first deliverable and MVP baseline
- **US2 (P2)**: depends on list foundations from US1 for entry/navigation
- **US3 (P3)**: depends on list/action framework from US1

### Suggested Story Completion Order

1. **US1** → MVP list page
2. **US2** → create/edit flows
3. **US3** → delete/restore lifecycle completion

---

## Parallel Execution Examples

### User Story 1

- T013 and T015 can run in parallel (separate list markup concerns in same feature slice).
- T017 can run in parallel with T014 once action model contracts are in place.

### User Story 2

- T019 and T021 can run in parallel (create dialog UI and edit page UI).
- T025 and T026 can run in parallel (validation and conflict result mapping).

### User Story 3

- T030 can run in parallel with T028/T029 (restore UI vs delete flow logic).

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1 (Setup)
2. Complete Phase 2 (Foundational)
3. Complete Phase 3 (US1)
4. Validate list/empty/error/permission-disabled behavior

### Incremental Delivery

1. Deliver US1 as operational baseline
2. Add US2 create/edit behavior
3. Add US3 delete/restore lifecycle
4. Finish with Phase 6 docs and verification

### Parallel Team Strategy

1. One engineer handles API client + foundational contracts (Phase 2)
2. One engineer builds list UI (US1)
3. One engineer builds create/edit (US2) after US1 scaffolding
4. One engineer adds lifecycle actions (US3) and polish/docs
