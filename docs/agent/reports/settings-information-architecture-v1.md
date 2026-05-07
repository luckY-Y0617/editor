# Settings Information Architecture + First Closure v1

## Status

- Settings IA inventory is recorded in the frontend settings model and surfaced in Workspace Settings General.
- First Settings closure slice selected and implemented: Workspace notification preference default.
- No backend API route, backend semantics, migration, Tiptap document JSON schema, System / Instance Settings, Organization member mutation, workspace provisioning mutation, files/upload, or public-link behavior was changed.

## Capability Inventory Summary

- Personal language/region: frontend live, local preference only; should split into Personal Settings later.
- Workspace profile update: backend mutation contract not found in the current inspected code; keep read-only/deferred.
- Workspace members: backend has live list/add/update/remove and prior frontend closure; keep as a linked task surface, not duplicated inside Settings.
- Workspace notification preferences: backend has live `GET /notifications/preferences` and `PUT /notifications/preferences`; previously read-only in Settings, selected for this closure.
- Resource share: backend and frontend closure exist for document share links; keep resource-scoped and move out of global Settings.
- Organization profile: backend/frontend rename v1 is live and owner-only; keep in Organization Settings.
- Organization members: backend read-only inventory exists; keep read-only, do not add Organization member mutation.
- Organization workspace provisioning: missing mutation contract; keep deferred.
- Library collection/document operations: backend/frontend operations exist on Libraries; do not duplicate routine operations in Settings.
- System / Instance Settings: not exposed.

## Proposed IA Contract

- Default Settings scope remains Workspace Settings.
- Workspace Settings owns current workspace overview, members deep link, notification preferences, permission/integration/security links, and explicit deferred Plan/Developer entries.
- Organization Settings owns organization profile rename and read-only organization inventory.
- Library Settings owns library metadata and summaries, with operational collection/document work linked back to Libraries.
- Resource Share remains entered from document/collection context, not global Settings.
- Unsupported actions stay visible as deferred or unavailable with reasons; no fake success states.

## Selected Slice

- Slice: Workspace notification preference default.
- Backend contract: `GET /api/v1/notifications/preferences` and `PUT /api/v1/notifications/preferences`.
- Request uses the existing workspace-level preference shape: `workspaceId`, `resourceType: null`, `resourceId: null`, `watched`, `muted`.
- Rationale: low-risk, user-facing Settings task with existing backend tests and no new permission semantics.

## UX Contract

- Entry: `#settings?scope=workspace&tab=notifications`.
- Default state: scan-first notifications section with workspace default control plus resource-specific watched/muted read list.
- Primary action: segmented control for Default / Watch workspace / Mute workspace.
- Loading/error/forbidden/unconfigured states disable the control with an explicit reason.
- Success shows `Preference updated.`
- Backend errors are surfaced directly from the Northstar API client error message.
- Category preferences and email digest remain deferred.

## Validation Record

- `npm test`: passed.
- `npm run build`: passed with existing Vite large chunk warning.
- Browser Use IAB: attempted, unavailable because no IAB backend was exposed.
- Browser QA fallback: local Chrome/CDP against Vite + live API passed.
- PostgreSQL smoke: not run because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not set.

## Browser QA Details

- Desktop Settings Notifications default state loaded with the workspace notification default control visible.
- `Mute workspace` clicked and returned visible success.
- `Default` clicked and returned visible success, restoring the workspace default preference.
- Narrow viewport at 390px kept the segmented control visible and usable.
- QA screenshots:
  - `apps/web/.codex-settings-notifications-default.png`
  - `apps/web/.codex-settings-notifications-muted.png`
  - `apps/web/.codex-settings-notifications-mobile.png`

## Deferred Items

- Personal Settings route extraction.
- Workspace profile mutation.
- Organization member mutation.
- Workspace provisioning mutation.
- Billing/domain/SSO/SCIM/audit/retention expansion.
- Files/upload.
- Public-link behavior changes.
