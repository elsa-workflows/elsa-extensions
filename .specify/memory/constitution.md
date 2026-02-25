<!--
Sync Impact Report

- Version change: N/A → 0.1.0
- Modified principles: Template placeholders → repo-specific principles
- Added sections: None (filled existing sections)
- Removed sections: None
- Templates requiring updates: ⚠ pending review (no template edits made)
- Deferred items:
	- TODO(RATIFICATION_DATE): confirm the original adoption date
-->

# Elsa Extensions Constitution

## Core Principles

### 1) Module-Owned Specs (README is the spec)
Each extension/module owns its specification and user-facing documentation.
The module README is the canonical spec for that module: purpose, public surface area,
configuration, usage, and limitations.

- No parallel “spec tree” that drifts from module READMEs.
- If a module has multiple packages (e.g., core/api/persistence/studio), maintain a
	module-level README at the folder root and link to package READMEs as needed.
- Any change that affects public behavior MUST update the relevant README/spec.

### 2) Contract-Shaped Dependencies (depend on interfaces, not internals)
Cross-module dependencies MUST be explicit and stable. If module A depends on module B,
that dependency is described and governed as a contract.

- Prefer consuming B via its public package surface (APIs, options, extension methods,
	activities/triggers, events/messages), not internal types.
- Avoid “reach-through” coupling (reading another module’s database tables, calling
	internal services, or reusing internal models).
- When a dependency is required, document it in A’s spec under “Dependencies” and
	link to B’s “Public Contract” section.

### 3) Package Compatibility & SemVer (don’t break consumers)
This repo ships NuGet packages; compatibility is a product feature.

- Public APIs MUST be versioned and treated as contracts.
- Breaking changes require:
	- a documented migration note (in module README and/or release notes)
	- a version bump consistent with semantic versioning
	- clear upgrade guidance for host apps (e.g., workbench).
- Use project references (`UseProjectReferences`) for local development, but validate
	that the packaged experience remains correct.

### 4) Security & Secrets Hygiene (non-negotiable)
No real credentials, tokens, or private keys are allowed in tracked files.

- Docs may reference placeholder values only (e.g., `your-token-here`).
- If examples require secrets, document how to provide them via supported secret
	management/configuration mechanisms.
- Any discovery of committed secrets triggers remediation (rotate/revoke where
	applicable) and an immediate documentation fix.

### 5) Quality Gates: Tests + Docs + Usability
Every module change must keep the extension usable by downstream hosts.

- Add/adjust automated tests when behavior changes (unit and/or integration where
	appropriate).
- Ensure “Getting Started” remains correct and minimal.
- Keep defaults safe and predictable; add configuration only when it’s necessary and
	document it.

## Architecture & Boundaries

- A “module” in this repo is a shippable package or a cohesive group of packages under
	`src/modules/<area>/...`.
- The workbench apps under `src/workbench/` compose modules; module specs should
	describe how to register/enable functionality in a host.
- Cross-module composition should flow through:
	- DI registration and extension methods (e.g., `UseXyz`)
	- activities/triggers and their inputs/outputs
	- API endpoints (when applicable) and their request/response semantics
	- events/messages (if used)

**Prohibited coupling**:

- Direct database/table coupling between modules.
- Referencing internal types across module boundaries (unless explicitly documented as
	part of a shared contract package).

## Development Workflow

- Prefer small, reviewable changes per module.
- For any module change that affects public behavior:
	- update that module’s README/spec
	- update examples (if present)
	- update tests
- For dependency changes:
	- document the dependency contract impact
	- confirm no unintended new coupling was introduced
- Keep build scripts and multi-targeting (`net8.0;net9.0;net10.0`) working.

## Governance
<!-- Example: Constitution supersedes all other practices; Amendments require documentation, approval, migration plan -->

This constitution defines non-negotiable rules for specifications, module boundaries,
and change management.

- Amendments are made via PR updating `.specify/memory/constitution.md`.
- Versioning policy:
	- MAJOR: removes/weakens a principle or materially changes governance
	- MINOR: adds a new principle/section or materially expands requirements
	- PATCH: clarifications and editorial changes
- Reviews MUST check:
	- module README/spec updated when public behavior changes
	- dependencies remain contract-shaped
	- no secrets or sensitive values added to tracked files

**Version**: 0.1.0 | **Ratified**: TODO(RATIFICATION_DATE) | **Last Amended**: 2026-02-25
