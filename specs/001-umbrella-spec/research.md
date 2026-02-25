# Research: Elsa Extensions Umbrella Spec

**Date**: 2026-02-25  
**Feature**: [spec.md](spec.md)

## Repo Structure Observations

- Extensions live under `src/modules/` and are grouped by module area (e.g., `agents`, `sql`, `http`, `persistence`, ...).
- Host apps (composition roots) live under `src/workbench/`:
  - `Elsa.Server.Web`
  - `Elsa.ServerAndStudio.Web`
  - `ElsaStudioWebAssembly`
- Module docs exist but are uneven:
  - Some areas have module-level READMEs (e.g., `src/modules/agents/README.md`, `src/modules/sql/README.md`).
  - Other docs are package-level READMEs (e.g., `src/modules/devops/Elsa.DevOps.GitHub/README.md`).

## Dependency/Coupling Observations

- Cross-module dependencies exist via `<ProjectReference>` between modules.
  - Example patterns:
    - provider packages referencing a base package (e.g., SQL clients referencing `Elsa.Sql`)
    - feature packages building on infrastructure modules (e.g., caching implementations referencing actors/servicebus)
- Modules also reference Elsa Core packages via NuGet, with a dual-mode pattern:
  - `Condition='$(UseProjectReferences)' == 'true'` uses project references into `elsa-core`
  - otherwise uses NuGet package references

## Implications for the Umbrella Spec

- The umbrella spec should not attempt to describe each module’s internal design.
- The umbrella spec should define:
  - what counts as a module public contract (registration/config/activities/APIs/events)
  - how dependencies must be documented and validated
  - what coupling is prohibited (especially persistence coupling)

## Gaps / Follow-ups

- Many module areas likely lack a canonical module-level README.
  - Short-term: catalog links can point to package-level docs.
  - Long-term: add module-area READMEs where missing.
- Workbench docs may contain sensitive example values; confirm secrets hygiene policy and replace real-looking values with placeholders if needed.
