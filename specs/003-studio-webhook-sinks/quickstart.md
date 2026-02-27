# Quickstart — Studio Webhook Sink Management

## Prerequisites

- Feature branch: `003-studio-webhook-sinks`
- Host app includes Studio and registers `Elsa.Studio.Http.Webhooks` module.
- Backend host exposes `Elsa.Http.Webhooks.Api` endpoints and proper permissions.

## 1) Register the Studio module

In the Studio host composition root, ensure webhook module registration is included, following existing module registration conventions.

## 2) Configure backend API connection

Use the existing `BackendApiConfig` setup used by other Studio modules. Ensure Studio can resolve remote API clients through `IBackendApiClientProvider`.

## 3) Navigate to Webhooks page

- Open Studio.
- Use settings menu entry `Webhooks`.
- Verify list loads from backend endpoint.

## 4) Verify core flows

### List
- Confirm active sinks load by default.
- Confirm empty state and retry behavior on failure.

### Create
- Create sink with required fields: `Name`, `Target URL`.
- Confirm sink appears in list.

### Edit
- Update name or target URL.
- Confirm persisted values after reload.

### Soft Delete
- Delete sink and confirm it disappears from default active list.

### Restore
- Include deleted sinks in view.
- Restore sink and confirm it returns to active list.

## 5) Verify permission behavior

- As a user without specific permissions, confirm actions are visible but disabled.
- Confirm disabled actions display clear permission guidance.

## 6) Verify conflict behavior

- Simulate concurrent edit/delete conflict.
- Confirm Studio shows conflict message and requires refresh + manual retry.

## 7) Test & docs checklist

- Add/execute tests for API client flow and key UI behaviors.
- Validate backward compatibility by starting a host that registers the module but does not navigate to Webhooks; confirm startup remains healthy.
- Verify deferred scope is preserved: no pagination dependency is introduced and event filters remain optional.
- Update module README/docs if registration/usage notes changed.

## 8) Success criteria measurement protocol

- Use a reference dataset of 120 sinks total (100 active, 20 soft-deleted).
- Run all measurements in the same host profile after one warm-up request.
- For list-load timing, execute 20 runs and verify p95 list-load time is <= 3 seconds.
- For update persistence, execute 20 consecutive successful update attempts and verify list reflects persisted values without full-page reload in at least 19 runs.
- For validation/conflict recovery, execute 20 scripted failure-case trials and verify recovery without unsaved-input loss in at least 19 runs.
