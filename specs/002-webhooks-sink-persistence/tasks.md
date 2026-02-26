# Tasks: Webhook Sink Persistence & Management

**Input**: Design documents from `/specs/002-webhooks-sink-persistence/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: No explicit TDD/test-first request in this spec, so test tasks are omitted. Validation is done via quickstart scenarios and existing module test strategy.

**Organization**: Tasks are grouped by user story for independent delivery where possible.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story label (`[US1]`, `[US2]`, `[US3]`) for story-phase tasks
- Every task includes exact file path(s)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create package skeletons and solution wiring in `http` module area.

- [ ] T001 Create new package folder structures under `src/modules/http/Elsa.Http.Webhooks.Abstractions`, `src/modules/http/Elsa.Http.Webhooks.Persistence`, `src/modules/http/Elsa.Http.Webhooks.Persistence.EFCore`, `src/modules/http/Elsa.Http.Webhooks.Persistence.MongoDb`, and `src/modules/http/Elsa.Http.Webhooks.Api`
- [ ] T002 [P] Add project files `src/modules/http/Elsa.Http.Webhooks.Abstractions/Elsa.Http.Webhooks.Abstractions.csproj`, `src/modules/http/Elsa.Http.Webhooks.Persistence/Elsa.Http.Webhooks.Persistence.csproj`, `src/modules/http/Elsa.Http.Webhooks.Persistence.EFCore/Elsa.Http.Webhooks.Persistence.EFCore.csproj`, `src/modules/http/Elsa.Http.Webhooks.Persistence.MongoDb/Elsa.Http.Webhooks.Persistence.MongoDb.csproj`, and `src/modules/http/Elsa.Http.Webhooks.Api/Elsa.Http.Webhooks.Api.csproj`
- [ ] T003 Add new projects to solution file `Elsa.Extensions.sln`
- [ ] T004 [P] Add/update shared package references required by new packages in `Directory.Packages.props`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define shared contracts and application services that all stories depend on.

**⚠️ CRITICAL**: No user story implementation starts before this phase is complete.

- [ ] T005 Create shared sink contracts/types in `src/modules/http/Elsa.Http.Webhooks.Abstractions/Contracts/`
- [ ] T006 Move or add reusable types needed by both existing and new packages into `src/modules/http/Elsa.Http.Webhooks.Abstractions/`
- [ ] T007 Create persistence entity model(s) for webhook sinks (including soft-delete and concurrency token) in `src/modules/http/Elsa.Http.Webhooks.Persistence/Entities/`
- [ ] T008 Create store abstraction `IWebhookSinkStore` and query/result contracts in `src/modules/http/Elsa.Http.Webhooks.Persistence/Contracts/`
- [ ] T009 Create application service abstraction `IWebhookSinkManagementService` in `src/modules/http/Elsa.Http.Webhooks.Persistence/Services/`
- [ ] T010 Implement management service logic (ID strategy, duplicate checks, soft-delete semantics, restore path, optimistic concurrency contract) in `src/modules/http/Elsa.Http.Webhooks.Persistence/Services/`
- [ ] T011 Create feature/DI wiring for abstractions and persistence contracts in `src/modules/http/Elsa.Http.Webhooks.Persistence/Features/Feature.cs` and `src/modules/http/Elsa.Http.Webhooks.Persistence/Extensions/`
- [ ] T012 Add package READMEs for abstractions and persistence contract packages at `src/modules/http/Elsa.Http.Webhooks.Abstractions/README.md` and `src/modules/http/Elsa.Http.Webhooks.Persistence/README.md`

**Checkpoint**: Foundation ready — user story phases can proceed.

---

## Phase 3: User Story 1 - Persist webhook sinks in storage (Priority: P1) 🎯 MVP

**Goal**: Enable store-backed sink persistence and runtime sink resolution using a single active provider.

**Independent Test**: Register EF Core store-backed sink provider, create sink via management service, restart host, verify sink remains and runtime resolves via active provider.

### Implementation for User Story 1

- [ ] T013 [P] [US1] Implement EF Core store-backed `IWebhookSinkProvider` in `src/modules/http/Elsa.Http.Webhooks.Persistence.EFCore/`
- [ ] T014 [US1] Add EF Core provider registration extensions for single active provider selection in `src/modules/http/Elsa.Http.Webhooks.Persistence.EFCore/Extensions.cs`
- [ ] T015 [US1] Validate and document that `Elsa.Http.Webhooks` has no dependencies on new persistence/API packages in `src/modules/http/Elsa.Http.Webhooks/README.md`
- [ ] T016 [US1] Document host registration flow (existing config-based vs EF Core store-backed provider) in `src/modules/http/Elsa.Http.Webhooks.Persistence.EFCore/README.md`

**Checkpoint**: US1 is independently functional.

---

## Phase 4: User Story 2 - Manage sinks through REST API (Priority: P2)

**Goal**: Expose CRUD + restore management API for sinks using application services.

**Independent Test**: Use API endpoints to create/list/get/update/delete/restore sinks and verify conflict/error semantics.

### Implementation for User Story 2

- [ ] T017 [P] [US2] Create API DTOs for create/update/list/get/restore including concurrency token in `src/modules/http/Elsa.Http.Webhooks.Api/Contracts/`
- [ ] T018 [P] [US2] Create API feature/DI setup in `src/modules/http/Elsa.Http.Webhooks.Api/Features/Feature.cs` and `src/modules/http/Elsa.Http.Webhooks.Api/Extensions/`
- [ ] T019 [US2] Implement list and get endpoints in `src/modules/http/Elsa.Http.Webhooks.Api/Endpoints/WebhookSinks/`
- [ ] T020 [US2] Implement create endpoint supporting optional client ID and server-generated fallback in `src/modules/http/Elsa.Http.Webhooks.Api/Endpoints/WebhookSinks/`
- [ ] T021 [US2] Implement update endpoint with optimistic concurrency conflict mapping in `src/modules/http/Elsa.Http.Webhooks.Api/Endpoints/WebhookSinks/`
- [ ] T022 [US2] Implement soft-delete and restore endpoints in `src/modules/http/Elsa.Http.Webhooks.Api/Endpoints/WebhookSinks/`
- [ ] T023 [US2] Implement API error mapping and validation behavior aligned with `specs/002-webhooks-sink-persistence/contracts/webhook-sinks.openapi.yaml` in `src/modules/http/Elsa.Http.Webhooks.Api/`
- [ ] T024 [US2] Add package README for API usage and auth expectations at `src/modules/http/Elsa.Http.Webhooks.Api/README.md`

**Checkpoint**: US2 is independently functional.

---

## Phase 5: User Story 3 - Swap persistence providers without changing contracts (Priority: P3)

**Goal**: Deliver EF Core and MongoDB provider implementations with behavior parity.

**Independent Test**: Run identical sink CRUD/restore/concurrency flows against both providers with same application/API behavior.

### Implementation for User Story 3

- [ ] T025 [P] [US3] Implement EF Core DbContext and entity mapping for webhook sinks in `src/modules/http/Elsa.Http.Webhooks.Persistence.EFCore/DbContext.cs` and related configuration files
- [ ] T026 [US3] Implement EF Core store `IWebhookSinkStore` adapter in `src/modules/http/Elsa.Http.Webhooks.Persistence.EFCore/`
- [ ] T027 [US3] Ensure EF Core provider package fully owns its `IWebhookSinkProvider` implementation and DI setup in `src/modules/http/Elsa.Http.Webhooks.Persistence.EFCore/`
- [ ] T028 [P] [US3] Implement MongoDB store `IWebhookSinkStore` adapter and mapping classes in `src/modules/http/Elsa.Http.Webhooks.Persistence.MongoDb/`
- [ ] T029 [US3] Implement MongoDB store-backed `IWebhookSinkProvider` and DI registration in `src/modules/http/Elsa.Http.Webhooks.Persistence.MongoDb/`
- [ ] T030 [US3] Add package READMEs for provider setup at `src/modules/http/Elsa.Http.Webhooks.Persistence.EFCore/README.md` and `src/modules/http/Elsa.Http.Webhooks.Persistence.MongoDb/README.md`

**Checkpoint**: US3 is independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Ensure docs/contracts alignment and final integration polish.

- [ ] T031 [P] Update module-area documentation with new package catalog and dependency-direction rule in `src/modules/http/README.md`
- [ ] T032 [P] Reconcile API contract file with implemented endpoint/DTO names in `specs/002-webhooks-sink-persistence/contracts/webhook-sinks.openapi.yaml`
- [ ] T033 Validate quickstart scenarios and update commands/examples in `specs/002-webhooks-sink-persistence/quickstart.md`
- [ ] T034 Add implementation notes and migration guidance in `specs/002-webhooks-sink-persistence/research.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: starts immediately
- **Phase 2 (Foundational)**: depends on Phase 1; blocks all user stories
- **Phase 3 (US1)**: depends on Phase 2
- **Phase 4 (US2)**: depends on Phase 2 (can run in parallel with US1 after T010/T011)
- **Phase 5 (US3)**: depends on Phase 2; integrates best after US1 contracts are in active use
- **Phase 6 (Polish)**: depends on completion of desired user stories

### User Story Dependencies

- **US1 (P1)**: depends only on Foundational phase and does not require runtime code changes in `Elsa.Http.Webhooks`
- **US2 (P2)**: depends on Foundational phase; can progress independently using application service contracts
- **US3 (P3)**: depends on Foundational phase; provider behavior parity depends on shared contracts from Phase 2

### Recommended Story Order

1. US1 (MVP persistence + runtime provider)
2. US2 (management API)
3. US3 (provider swap flexibility)

---

## Parallel Execution Examples

### User Story 1

- T013 and T014 can run in parallel once T011 is complete.

### User Story 2

- T017 and T018 can run in parallel, then endpoints T019–T022 can be split across contributors.

### User Story 3

- T025 and T028 can run in parallel, then T026/T027 and T029 follow per provider stream.

---

## Implementation Strategy

### MVP First (US1)

1. Complete Phase 1 + Phase 2
2. Deliver US1 (T013–T016)
3. Validate restart persistence + runtime resolution

### Incremental Delivery

1. Add US2 API after US1 contracts stabilize
2. Add US3 provider parity without changing API/contracts
3. Finish with Phase 6 documentation and contract reconciliation

### Team Strategy

- Developer A: Abstractions + persistence contracts + US1 provider integration
- Developer B: API package (US2)
- Developer C: Provider implementations (US3)
- Shared: Phase 6 docs/contract alignment
