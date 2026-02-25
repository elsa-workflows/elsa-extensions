# Alterations Module Area

## Overview
This module area contains alteration-related extensions and integrations.

## Public Contract
The public contract for packages in this module area is defined by their published package APIs and documentation.

- Registration surface (e.g., extension methods used by host applications)
- Configuration surface (options and supported configuration keys)
- Activities/triggers exposed to workflow authors (if applicable)
- Endpoints and messages/events exposed to consumers (if applicable)

## Dependency Contract Rules
- Depend on other module areas through documented public contracts only.
- Do not introduce direct persistence/table coupling across module boundaries.
- Document new cross-module dependencies in package and module docs.

## Notes
- Keep this module-area README aligned with package-level READMEs in this folder.
