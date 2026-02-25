# Implementation Plan: Elsa Extensions Umbrella Spec

**Branch**: `001-umbrella-spec` | **Date**: 2026-02-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-umbrella-spec/spec.md`

## Summary

Create and maintain a single umbrella specification that provides:

- A repo/module map (module areas + host apps)
- A clear definition of each module’s **public contract**
- Dependency rules to keep the modular monolith “modular” (contract-shaped coupling)
- Cross-cutting conventions (secrets hygiene, SemVer + migration notes, doc gates)

The umbrella spec does **not** replace module READMEs; it provides shared context and rules
so per-module specs stay consistent.

## Technical Context

**Language/Version**: C#/.NET (multi-targeted packages: `net8.0;net9.0;net10.0`)  
**Primary Dependencies**: `dotnet` SDK + MSBuild; NuGet packaging; Git  
**Storage**: N/A (documentation-only feature)  
**Testing**: Existing `dotnet test` projects (not required for doc-only changes)  
**Target Platform**: Cross-platform (.NET on Linux/Windows/macOS; this plan authored on macOS)  
**Project Type**: .NET extensions repo (many NuGet packages) + workbench host apps  
**Performance Goals**: N/A  
**Constraints**: Keep module boundaries explicit; avoid introducing new coupling in docs  
**Scale/Scope**: Dozens of module areas under `src/modules/`; 3 workbench hosts under `src/workbench/`

## Constitution Check

*GATE: Must pass before considering this umbrella spec “ready to be shared context”.*

Source of truth: `.specify/memory/constitution.md`

- [ ] Module-owned specs: module READMEs remain canonical for module behavior
- [ ] Contract-shaped dependencies: umbrella spec defines approved contract surfaces
- [ ] SemVer + compatibility: guidance exists for breaking changes + migration notes
- [ ] Secrets hygiene: umbrella spec and linked docs avoid real credentials/tokens
- [ ] Quality gates: requires doc updates when public behavior changes

## Project Structure

### Documentation (this feature)

```text
specs/001-umbrella-spec/
├── spec.md                         # Umbrella spec content (current draft)
├── plan.md                         # This file
├── research.md                     # Repo facts used by this plan
└── checklists/
    └── requirements.md             # Spec quality checklist
```

### Source Code (repository root)

```text
src/
├── modules/                         # Extension modules grouped by area
└── workbench/                       # Host apps (composition roots)

doc/
└── release-notes/

test/
└── ...
```

**Structure Decision**: Documentation-only under `specs/001-umbrella-spec/`, linking into
existing READMEs under `src/modules/**/README.md` and `src/workbench/**/README.md`.

## Phases

### Phase 0 — Research (doc inventory + coupling patterns)

- Confirm how modules are organized (areas under `src/modules/`)
- Identify which module areas already have canonical READMEs vs package-level docs
- Confirm dependency patterns (project references between modules, and `UseProjectReferences` mode)

**Output**: `research.md`

### Phase 1 — Refine the umbrella spec

- Expand “Module Catalog” to cover all module areas (initially link to folder roots; add
  module-area READMEs where missing)
- Add a “Public Contract” checklist that module READMEs can copy/paste
- Add explicit rules for:
  - allowed dependency directions
  - how to document dependencies (where + what)
  - how to manage shared abstractions (when they’re permitted)

**Output**: update `spec.md`

### Phase 2 — Operationalize

- Add a short contribution guideline snippet to point contributors to the umbrella spec
  (optional; only if it doesn’t conflict with existing contribution docs)
- (Optional) generate a simple dependency overview diagram from `.csproj` `<ProjectReference>`
  edges for reviewer context

**Output**: follow-up tasks via `/speckit.tasks`

## Complexity Tracking

No constitution violations anticipated; this is documentation-only and keeps existing module
ownership intact.
