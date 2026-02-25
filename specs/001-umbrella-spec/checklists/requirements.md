# Specification Quality Checklist: Elsa Extensions Umbrella Spec

**Purpose**: Validate completeness and usefulness of the umbrella spec before it becomes the shared context for module-level specs.
**Created**: 2026-02-25
**Feature**: ../spec.md

## Content Quality

- [ ] CQ-001 Uses repo terminology consistently (module, area, host, public contract)
- [ ] CQ-002 Avoids implementation details except where necessary to identify integration surfaces
- [ ] CQ-003 States clearly that module READMEs remain canonical module specs
- [ ] CQ-004 Links to at least one representative README per major module area (or notes gaps)

## Dependency & Boundary Rules

- [ ] DB-001 Defines "public contract" for modules (registration/config/activities/APIs/events)
- [ ] DB-002 Prohibits cross-module database/table coupling explicitly
- [ ] DB-003 Describes how to document a new cross-module dependency (where + what fields)
- [ ] DB-004 Addresses the dual dev modes (NuGet vs `UseProjectReferences`) at least at a high level

## Cross-cutting Conventions

- [ ] CC-001 References secrets hygiene rules (no real credentials committed)
- [ ] CC-002 References versioning/breaking change expectations (SemVer + migration notes)
- [ ] CC-003 Mentions testing/documentation expectations at the level of “public behavior changes require doc updates”

## Notes

- Check items off as completed: `[x]`
- Capture findings inline under the relevant item
