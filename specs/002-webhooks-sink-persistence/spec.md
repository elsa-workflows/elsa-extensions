# Feature Specification: Webhook Sink Persistence & Management

**Feature Branch**: `002-webhooks-sink-persistence`  
**Created**: 2026-02-26  
**Status**: Draft  
**Input**: User description: "Enhance Elsa.Http.Webhooks with persistent webhook sink management: add abstractions and application services, EF Core and MongoDB persistence packages, and REST API package for CRUD management."

## Implementation Scope *(mandatory)*

**Module Area**: `http`  
**Primary Module**: `Elsa.Http.Webhooks`  
**Target Packages**: `Elsa.Http.Webhooks` (existing), `Elsa.Http.Webhooks.Persistence` (new), `Elsa.Http.Webhooks.Persistence.EFCore` (new), `Elsa.Http.Webhooks.Persistence.MongoDb` (new), `Elsa.Http.Webhooks.Api` (new)  
**In-Scope Paths**: `src/modules/http/Elsa.Http.Webhooks/**`, `src/modules/http/Elsa.Http.Webhooks.Persistence*/**`, `src/modules/http/Elsa.Http.Webhooks.Api/**`, `src/modules/http/README.md`, `specs/002-webhooks-sink-persistence/**`  
**Out-of-Scope Paths**: `src/modules/agents/**`, `src/modules/servicebus/**`, `src/modules/persistence/**` (except consumed package references), `src/workbench/**`, `test/modules/**` outside webhooks-related tests

**Scope Rules**:

- Runtime and API implementation for this feature stays within the `http` module area.
- Provider-specific persistence logic must remain behind the sink store/provider abstractions.
- Any required cross-module runtime code changes outside `http` require explicit spec amendment.

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Persist webhook sinks in storage (Priority: P1)

As an Elsa host operator, I can store webhook sinks in a database instead of app configuration,
so sink definitions survive restarts and can be managed consistently across environments.

**Why this priority**: This is the core capability gap today; without persistence, operational
management is limited and brittle.

**Independent Test**: Can be tested by creating a sink through an application service,
restarting the host, and verifying the sink remains available and is used by webhook dispatch.

**Acceptance Scenarios**:

1. **Given** persistent store integration is enabled, **When** a sink is created, **Then** it is stored and retrievable after application restart.
2. **Given** a store-backed sink provider is registered, **When** webhook dispatch resolves sinks, **Then** it resolves sinks exclusively through the active provider implementation.

---

### User Story 2 - Manage sinks through REST API (Priority: P2)

As an Elsa integrator, I can create, list, update, and delete webhook sinks through a REST API,
so external tools and future Studio UI can manage sinks programmatically.

**Why this priority**: API management is the enabling contract for Studio and automation.

**Independent Test**: Can be tested by exercising API endpoints end-to-end and confirming
store state changes and validation behavior.

**Acceptance Scenarios**:

1. **Given** a valid sink payload, **When** I call create endpoint, **Then** API returns created sink identifier and persisted sink data.
2. **Given** an existing sink, **When** I call update endpoint with valid changes, **Then** API returns updated representation and subsequent list/get reflect those changes.
3. **Given** an existing sink, **When** I call delete endpoint, **Then** sink is no longer returned by list/get operations.

---

### User Story 3 - Swap persistence providers without changing contracts (Priority: P3)

As a module developer, I can use EF Core or MongoDB persistence providers behind the same
abstractions, so hosts can choose storage technology without API or domain contract changes.

**Why this priority**: Provider flexibility is required for real-world Elsa deployments.

**Independent Test**: Can be tested by running the same service/API behavior tests against
EF Core and MongoDB implementations.

**Acceptance Scenarios**:

1. **Given** the EF Core provider is configured, **When** sink CRUD actions are executed, **Then** behavior matches the contract.
2. **Given** the MongoDB provider is configured, **When** sink CRUD actions are executed, **Then** behavior matches the same contract and API semantics.

---

### Edge Cases

- Creating two sinks with the same external identifier or name.
- Updating a sink while it is being used by in-flight dispatch.
- Provider unavailable (database down) while dispatch or management operations are requested.
- Migration from configuration-only sinks to a store-backed provider introduces duplicates.
- Validation of malformed sink URL, unsupported protocol, or invalid headers/auth metadata.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The solution MUST introduce a dedicated webhook persistence abstraction package for `Elsa.Http.Webhooks` (e.g., `Elsa.Http.Webhooks.Persistence`) that defines sink persistence contracts.
- **FR-002**: The abstraction package MUST define a sink store contract (e.g., `IWebhookSinkStore`) supporting create, list, get by identifier, update, and delete operations.
- **FR-003**: The solution MUST provide application-level services for webhook sink management, separated from transport concerns and reusable by API and future UI integrations.
- **FR-004**: The solution MUST provide an EF Core persistence package (e.g., `Elsa.Http.Webhooks.Persistence.EFCore`) implementing the sink store contract.
- **FR-005**: The solution MUST provide a MongoDB persistence package (e.g., `Elsa.Http.Webhooks.Persistence.MongoDb`) implementing the same sink store contract.
- **FR-006**: The solution MUST provide a REST API package (e.g., `Elsa.Http.Webhooks.Api`) exposing sink CRUD endpoints for machine and UI clients.
- **FR-007**: The API package MUST provide request validation and return deterministic error responses for invalid input, missing resources, and duplicate constraints.
- **FR-008**: The runtime sink provider used by webhook dispatch MUST support a store-backed implementation that resolves sinks from persistence.
- **FR-009**: Only one `IWebhookSinkProvider` implementation is active at runtime; hosts choose the provider through DI registration.
- **FR-010**: The feature MUST preserve backward compatibility for existing configuration-only hosts that do not enable persistence packages.
- **FR-011**: The feature MUST include module-level and package-level documentation updates covering registration, configuration, migrations, and API usage.
- **FR-012**: The API contract MUST support future Elsa Studio consumption without requiring breaking changes for basic sink management flows.

### Key Entities *(include if feature involves data)*

- **Webhook Sink**: A target endpoint definition for outbound webhook dispatch; includes identifier, URL, enabled status, optional metadata, and delivery-related settings.
- **Webhook Sink Record**: Persisted representation of a sink in a provider datastore (EF Core or MongoDB) mapped from/to the domain sink contract.
- **Webhook Sink Command/DTO**: API input/output models used to create/update/list sinks while isolating transport details from domain abstractions.
- **Sink Management Service**: Application-level service encapsulating validation, deduplication checks, and store operations.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A host can create, list, update, and delete webhook sinks using REST API with successful responses for valid requests and contract-compliant errors for invalid requests.
- **SC-002**: After restart, persisted sinks remain available and are used by webhook dispatch without reconfiguration.
- **SC-003**: The same acceptance tests for sink CRUD pass against both EF Core and MongoDB providers.
- **SC-004**: Existing hosts using configuration-only sink registration continue to work unchanged when persistence packages are not enabled.

## Proposed Package Boundaries

- **`Elsa.Http.Webhooks`** (existing): runtime dispatch integration and feature registration.
- **`Elsa.Http.Webhooks.Persistence`** (new): abstractions + domain/application contracts (including `IWebhookSinkStore`).
- **`Elsa.Http.Webhooks.Persistence.EFCore`** (new): EF Core implementation.
- **`Elsa.Http.Webhooks.Persistence.MongoDb`** (new): MongoDB implementation.
- **`Elsa.Http.Webhooks.Api`** (new): REST API endpoints + contract DTOs/application orchestration wiring.

## Assumptions

- Existing `WebhooksCore` sink model remains the foundational external sink contract.
- Authentication/authorization strategy for management endpoints follows existing Elsa API conventions unless overridden.
- Host applications activate exactly one `IWebhookSinkProvider` implementation through DI.
- Studio integration is out of scope for this spec and will be addressed in a follow-up spec.
