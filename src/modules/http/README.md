# HTTP Module Area

## Overview
This module area contains HTTP-related extensions and integrations.

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

## Package Catalog

- `Elsa.Http.Webhooks`: runtime feature root (no dependency on provider/API packages)
- `Elsa.Http.Webhooks.Abstractions`: shared sink contracts/types
- `Elsa.Http.Webhooks.Persistence`: store and management service contracts
- `Elsa.Http.Webhooks.Persistence.EFCore`: EF Core store-backed provider implementation
- `Elsa.Http.Webhooks.Persistence.MongoDb`: MongoDB store-backed provider implementation
- `Elsa.Http.Webhooks.Api`: management REST API endpoints
- `Elsa.Studio.Http.Webhooks`: Elsa Studio webhook sink management UI module

## Notes
- Keep this module-area README aligned with package-level READMEs in this folder.
