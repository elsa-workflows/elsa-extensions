# Feature Specification: Elsa Extensions Umbrella Spec

**Feature Branch**: `001-umbrella-spec`  
**Created**: 2026-02-25  
**Status**: Ready for Module Specs  
**Input**: User description: "Create umbrella specification: module catalog, dependency rules, and cross-cutting conventions for Elsa Extensions"

## Implementation Scope *(mandatory)*

**Module Area**: Cross-module (umbrella governance)  
**Primary Module**: N/A (documentation umbrella)  
**Target Packages**: Documentation and spec artifacts only  
**In-Scope Paths**: `specs/001-umbrella-spec/**`, `src/modules/**/README.md`, `.specify/**`  
**Out-of-Scope Paths**: Runtime code under `src/modules/**` (except documentation files), `src/workbench/**` runtime implementation

**Scope Rules**:

- Defines governance and module-boundary guidance only.
- Must not introduce runtime behavior changes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Understand the module landscape (Priority: P1)

As a contributor or consumer, I can quickly understand what extension modules exist,
what they do, and how they are composed into a host (e.g., workbench).

**Why this priority**: Without a shared map, module READMEs drift and integration becomes
trial-and-error.

**Independent Test**: Can be tested by reading this spec and locating at least one module
README for each major module area, plus the workbench host entrypoints.

**Acceptance Scenarios**:

1. **Given** I am new to the repo, **When** I open this umbrella spec, **Then** I can find
  the module areas under `src/modules/` and the host apps under `src/workbench/`.
2. **Given** I want to understand a module, **When** I follow links from this spec,
  **Then** I land on that module’s canonical README/spec.

---

### User Story 2 - Know and enforce dependency rules (Priority: P2)

As a module author, I know the allowed dependency patterns (contracts) so I can add a
module feature without creating tight coupling or breaking package consumers.

**Why this priority**: The repo contains real cross-module dependencies; without rules,
dependencies become ad-hoc and brittle.

**Independent Test**: Can be tested by reviewing a module change and verifying it follows
the listed dependency rules and documentation requirements.

**Acceptance Scenarios**:

1. **Given** module A needs something from module B, **When** I follow this spec,
  **Then** I can identify the approved contract mechanism (API/options/activities/events).
2. **Given** a proposed change introduces a new dependency, **When** I compare it to the
  rules in this spec, **Then** I can decide whether it is allowed and what documentation is
  required.

---

### User Story 3 - Apply cross-cutting conventions consistently (Priority: P3)

As a maintainer, I can point contributors to a single place for cross-cutting conventions
(secrets handling, versioning expectations, documentation rules) that apply across all modules.

**Why this priority**: Cross-cutting practices are easy to miss when only module READMEs
exist.

**Independent Test**: Can be tested by checking that a new module addition references the
same conventions and does not introduce contradictory guidance.

**Acceptance Scenarios**:

1. **Given** a new module is added, **When** I use this spec as a checklist, **Then** the
  new module’s README contains the minimum contract/config/getting-started sections.

---

### Edge Cases

- Module area exists but has no module-level README.
- A module has multiple packages and the “contract surface” is unclear.
- A module depends on another module’s persistence implementation (disallowed) and needs a
  contract alternative.
- A module README conflicts with this umbrella spec (umbrella spec should be updated or the
  module README corrected; do not allow silent drift).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The umbrella spec MUST define the repo’s module taxonomy:
  `src/modules/<area>/...` and `src/workbench/...`.
- **FR-002**: The umbrella spec MUST define what constitutes a module “public contract”
  (registration APIs, configuration surface, activities/triggers, endpoints, events/messages).
- **FR-003**: The umbrella spec MUST define the allowed cross-module dependency patterns and
  explicitly prohibit direct database/table coupling.
- **FR-004**: The umbrella spec MUST point to the constitution rules for documentation,
  dependency shape, versioning, and secrets hygiene.
- **FR-005**: The umbrella spec MUST link to canonical module READMEs where they exist, and
  MUST state that module READMEs remain the canonical module-level specs.
- **FR-006**: When a module introduces a dependency on another module, the module README MUST
  document that dependency and reference the dependency’s public contract.

### Key Entities *(include if feature involves data)*

- **Module Area**: A category under `src/modules/` (e.g., `agents`, `sql`, `http`).
- **Module / Package**: A shippable unit (typically a NuGet package) under a module area.
- **Public Contract**: The supported integration surface consumers rely on.
- **Host App**: A composition root (e.g., projects under `src/workbench/`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new contributor can locate module areas and at least three representative
  module READMEs in under 5 minutes.
- **SC-002**: For a new cross-module dependency, reviewers can validate “contract-shaped”
  compliance using this spec without requiring tribal knowledge.
- **SC-003**: Module READMEs do not contradict the umbrella spec without an explicit update.

---

## Proposed Umbrella Content (initial draft)

This section is the initial umbrella content that will later be moved/refined into a
stable location (either within this spec or as a dedicated doc referenced by module READMEs).

### Repo Map

- **Extensions (primary)**: `src/modules/`
  - Organized by area folders (e.g., `agents`, `sql`, `http`, `persistence`, `storage`, ...)
- **Workbench hosts (composition roots)**: `src/workbench/`
  - `Elsa.Server.Web`
  - `Elsa.ServerAndStudio.Web`
  - `ElsaStudioWebAssembly`

### Module Catalog

This catalog links each top-level module area to its module-level README.

- **Actors**: [src/modules/actors/README.md](../../src/modules/actors/README.md)
- **Agents**: [src/modules/agents/README.md](../../src/modules/agents/README.md)
- **Alterations**: [src/modules/alterations/README.md](../../src/modules/alterations/README.md)
- **Caching**: [src/modules/caching/README.md](../../src/modules/caching/README.md)
- **CMS**: [src/modules/cms/README.md](../../src/modules/cms/README.md)
- **Communication**: [src/modules/communication/README.md](../../src/modules/communication/README.md)
- **Connections**: [src/modules/connections/README.md](../../src/modules/connections/README.md)
- **Data**: [src/modules/data/README.md](../../src/modules/data/README.md)
- **DevOps**: [src/modules/devops/README.md](../../src/modules/devops/README.md)
- **Diagnostics**: [src/modules/diagnostics/README.md](../../src/modules/diagnostics/README.md)
- **Dropins**: [src/modules/dropins/README.md](../../src/modules/dropins/README.md)
- **Email**: [src/modules/email/README.md](../../src/modules/email/README.md)
- **HTTP**: [src/modules/http/README.md](../../src/modules/http/README.md)
- **IO**: [src/modules/io/README.md](../../src/modules/io/README.md)
- **Labels**: [src/modules/labels/README.md](../../src/modules/labels/README.md)
- **Persistence**: [src/modules/persistence/README.md](../../src/modules/persistence/README.md)
- **Retention**: [src/modules/retention/README.md](../../src/modules/retention/README.md)
- **Runtimes**: [src/modules/runtimes/README.md](../../src/modules/runtimes/README.md)
- **Scheduling**: [src/modules/scheduling/README.md](../../src/modules/scheduling/README.md)
- **Secrets**: [src/modules/secrets/README.md](../../src/modules/secrets/README.md)
- **ServiceBus**: [src/modules/servicebus/README.md](../../src/modules/servicebus/README.md)
- **Slack**: [src/modules/slack/README.md](../../src/modules/slack/README.md)
- **SQL**: [src/modules/sql/README.md](../../src/modules/sql/README.md)
- **Storage**: [src/modules/storage/README.md](../../src/modules/storage/README.md)
- **Telecom**: [src/modules/telecom/README.md](../../src/modules/telecom/README.md)
- **Testing**: [src/modules/testing/README.md](../../src/modules/testing/README.md)
- **Workflows**: [src/modules/workflows/README.md](../../src/modules/workflows/README.md)

Hosts:

- **Server (host)**: [src/workbench/Elsa.Server.Web/README.md](../../src/workbench/Elsa.Server.Web/README.md)
- **Server + Studio (host)**: [src/workbench/Elsa.ServerAndStudio.Web/README.md](../../src/workbench/Elsa.ServerAndStudio.Web/README.md)
- **Studio WASM (host)**: [src/workbench/ElsaStudioWebAssembly/README.md](../../src/workbench/ElsaStudioWebAssembly/README.md)

### Module “Public Contract” Definition

A module’s public contract is comprised of:

- Registration API (e.g., DI/extension methods like `UseXyz`, `AddXyz`)
- Configuration surface (options, config keys, supported appsettings sections)
- Activities/triggers (names + input/output semantics)
- API endpoints (if any) and their request/response/error semantics
- Events/messages published (if any) with payload schema and idempotency expectations

### Dependency Rules (summary)

- Modules MAY depend on Elsa Core packages (via NuGet or project refs).
- Modules MAY depend on other modules only through documented public contracts.
- Modules MUST NOT depend on other modules’ persistence tables/schemas.
- When a module depends on another module, it MUST document:
  - what contract it consumes
  - any version constraints
  - any failure modes relevant to consumers

### Cross-cutting Conventions

- Secrets and tokens are never committed; docs use placeholders.
- Breaking changes require migration notes and versioning discipline.
- Module README is the canonical spec for module behavior.
