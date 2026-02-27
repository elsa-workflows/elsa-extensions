# Studio Contract — Webhook Sink Management

## Scope

Defines the Studio-to-API behavior contract for managing webhook sinks in `Elsa.Studio.Http.Webhooks`.

This feature remains backward compatible for hosts that include the module package but do not navigate to webhook management routes.

## API Mapping Contract

Studio actions map to existing webhook sink management endpoints:

- List sinks -> `GET /webhook-sinks`
- Get sink -> `GET /webhook-sinks/{id}`
- Create sink -> `POST /webhook-sinks`
- Update sink -> `POST /webhook-sinks/{id}`
- Soft delete sink -> `DELETE /webhook-sinks/{id}`
- Restore sink -> `POST /webhook-sinks/{id}/restore`

## Request/Response Behavior

### List
- Default behavior: request active sinks only.
- Optional include-deleted behavior: explicit UI toggle/filter.

### Create
- Required input fields for this feature: `name`, `targetUrl`.
- On success: created sink appears in list.
- On validation error: show field or form errors; keep entered values.

### Update
- Edit uses current sink values loaded from API.
- On conflict: no silent retry; show conflict guidance and require refresh before retry.

### Delete (Soft)
- Confirmation required before action.
- On success: sink disappears from default active list.

### Restore
- Restore available when sink is soft-deleted and user has permission.
- On success: sink reappears in default active list.

## Permission Contract

- Read/create/update/delete/restore permissions are evaluated by backend policy.
- UI behavior for unauthorized operations:
  - action remains visible
  - action is disabled
  - disabled reason is shown to user

## Error Handling Contract

Studio normalizes backend failures into user-facing outcomes:

- Validation -> actionable field/form feedback
- Conflict -> refresh-required guidance + manual retry path
- Unauthorized -> permission message and disabled action state
- Network/transport -> retry-capable error state
- Not found (stale item) -> notify and refresh list

## Non-Goals for This Feature

- Adding pagination requirements to sink list contract.
- Introducing additional mandatory sink fields beyond `name` and `targetUrl`.
- Extending backend API shape beyond current endpoint contract.
