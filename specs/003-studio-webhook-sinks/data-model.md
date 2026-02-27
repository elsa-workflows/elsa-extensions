# Data Model — Studio Webhook Sink Management

## Entity: WebhookSinkListItem

Represents a webhook sink row in the list view.

### Fields
- `id` (string, required)
- `name` (string, required)
- `targetUrl` (string, required)
- `version` (string, optional but expected for update/delete conflict semantics)
- `isDeleted` (bool, required)
- `status` (derived enum/string, required: `Active` | `Deleted`)

### Validation Rules
- `id` must be non-empty.
- `name` displayed as non-empty text; fallback display for invalid payloads.
- `targetUrl` displayed as non-empty text; invalid values flagged in UI.

## Entity: WebhookSinkInputModel

Represents create/edit form input state in Studio.

### Fields
- `name` (string, required)
- `targetUrl` (string, required)
- `version` (string, optional; used for optimistic concurrency)

### Validation Rules
- `name`: required, non-whitespace.
- `targetUrl`: required, valid URL format accepted by backend.
- No additional fields are required for this feature.

## Entity: WebhookSinkListQuery

Represents list retrieval options.

### Fields
- `includeDeleted` (bool, required; default `false`)
- `name` (string, optional filter)

### Validation Rules
- Default query must exclude soft-deleted sinks.
- Including deleted sinks is explicit user intent.

## Entity: WebhookSinkOperationResult

Represents normalized action outcome for create/update/delete/restore.

### Fields
- `outcome` (enum: `Success` | `ValidationError` | `Conflict` | `Unauthorized` | `TransportError` | `NotFound`)
- `message` (string)
- `fieldErrors` (dictionary/string list, optional)
- `requiresRefresh` (bool)

### Validation Rules
- `Conflict` outcomes set `requiresRefresh = true`.
- `ValidationError` outcomes preserve form input state and field-level messages.

## Relationships

- One `WebhookSinkListQuery` produces many `WebhookSinkListItem` records.
- One `WebhookSinkListItem` maps to one `WebhookSinkInputModel` for editing.
- Every user mutation action produces one `WebhookSinkOperationResult`.

## State Transitions

### Sink Lifecycle (UI-visible)
- `Active` -> `Deleted` via soft delete action.
- `Deleted` -> `Active` via restore action.

### Form Lifecycle
- `Pristine` -> `Dirty` after input changes.
- `Dirty` -> `Submitting` on save.
- `Submitting` -> `Saved` on success.
- `Submitting` -> `Dirty` on validation/conflict/transport failure (input retained).

### Conflict Lifecycle
- `ConflictDetected` -> `RefreshRequired` -> `RetryAllowed` after reload.
