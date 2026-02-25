# Feature Specification: Elsa Extensions Umbrella Spec

**Feature Branch**: `001-umbrella-spec`  
**Created**: 2026-02-25  
**Status**: Draft  
**Input**: User description: "Create umbrella specification: module catalog, dependency rules, and cross-cutting conventions for Elsa Extensions"

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

[Add more user stories as needed, each with an assigned priority]

### Edge Cases

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right edge cases.
-->

- Module area exists but has no module-level README.
- A module has multiple packages and the “contract surface” is unclear.
- A module depends on another module’s persistence implementation (disallowed) and needs a
  contract alternative.
- A module README conflicts with this umbrella spec (umbrella spec should be updated or the
  module README corrected; do not allow silent drift).

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

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

<!--
  ACTION REQUIRED: Define measurable success criteria.
  These must be technology-agnostic and measurable.
-->

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

### Module Catalog (starting point)

This list is intentionally minimal: it links to existing “canonical” module docs where
they exist today. Over time, expand this list to cover all module areas.

- **Agents**: [src/modules/agents/README.md](../../src/modules/agents/README.md)
- **SQL**: [src/modules/sql/README.md](../../src/modules/sql/README.md)
- **DevOps / GitHub** (package-level doc): [src/modules/devops/Elsa.DevOps.GitHub/README.md](../../src/modules/devops/Elsa.DevOps.GitHub/README.md)
- **HTTP / OpenAPI** (package-level doc): [src/modules/http/Elsa.Http.OpenApi/README.md](../../src/modules/http/Elsa.Http.OpenApi/README.md)
- **Diagnostics / Logging** (package-level doc): [src/modules/diagnostics/Elsa.Logging/README.md](../../src/modules/diagnostics/Elsa.Logging/README.md)

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
