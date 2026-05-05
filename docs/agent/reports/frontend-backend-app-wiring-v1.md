# Frontend Backend App Wiring V1

## Summary

Connected existing frontend auth, workspace, search, notification, comment, permission, workspace admin, and SCIM management surfaces to the existing backend API shape through shared frontend API/auth helpers. No backend routes or permission V1 behavior were changed.

## Scope

- Frontend API/auth wiring only.
- Existing backend routes inspected for auth, bootstrap, documents, search, spaces, notifications, workspace members/groups, permissions, and SCIM token/discovery.
- Existing demo fallback remains where the UI still has local editor/demo behavior.

## Docs Read

- `AGENTS.md`
- `docs/agent/00-project-state.md`
- `docs/agent/01-control-rules.md`
- `docs/agent/02-conflict-register.md`
- `docs/agent/03-required-reading-order.md`
- `docs/agent/04-implementation-protocol.md`
- `docs/agent/05-validation-protocol.md`
- `docs/agent/06-final-report-format.md`
- `docs/agent/skills/api-contracts.md`
- `docs/agent/skills/frontend-editor.md`
- `docs/agent/skills/permissions.md`
- `docs/PERMISSION_SYSTEM_CONTRACT.md`
- `docs/agent/reports/permission-system-v1-doc-freeze.md`

## Frontend Areas Inspected

- `apps/web/src/App.tsx`
- `apps/web/src/components/NorthstarLoginPage.tsx`
- `apps/web/src/components/WorkspaceHomeTopBar.tsx`
- `apps/web/src/components/WorkspaceHomePage.tsx`
- `apps/web/src/components/SearchDiscoveryPage.tsx`
- `apps/web/src/components/WorkspaceUpdatesPage.tsx`
- `apps/web/src/components/DocumentSharePermissionsPage.tsx`
- `apps/web/src/components/PermissionAdminSurfacesPage.tsx`
- `apps/web/src/lib/commentRepository.ts`
- `apps/web/src/lib/permissionAdminApi.ts`

## Backend Route Areas Inspected

- `AuthController`
- `BootstrapController`
- `DocumentsController`
- `SearchController`
- `SpacesController`
- `NotificationsController`
- `PermissionsController`
- `WorkspacesController`
- `ScimController`
- `ScimTokensController`
- Auth, knowledge, workspace, security contract DTOs

## Unified API Client

- Added `apps/web/src/lib/apiClient.ts`.
- Centralizes API base URL resolution from `VITE_NORTHSTAR_API_BASE_URL` / `VITE_API_BASE_URL`.
- Adds `/api/v1` when the configured base URL does not already include it.
- Centralizes JSON request handling, standard API error parsing, auth headers, workspace ID lookup, UUID validation, and token storage helpers.

## Auth Wiring

- Added `apps/web/src/lib/authClient.ts`.
- Login page now calls `POST /api/v1/auth/login`.
- Successful login stores auth access/refresh tokens in the existing frontend auth storage namespace and navigates to `#home`.
- Top bar reads `GET /api/v1/auth/me` and `GET /api/v1/auth/security-state` when an access token is present.
- Top bar logout calls `POST /api/v1/auth/logout` when a refresh token exists, then clears auth state.
- Register and MFA UI were not added because those are outside this wiring slice.

## App Surface Wiring

- Workspace home loads `GET /api/v1/bootstrap` when configured and signed in, with explicit demo fallback states.
- Search page loads `GET /api/v1/bootstrap` for active space and then `GET /api/v1/search?q=...&spaceId=...`, with explicit demo fallback states.
- Comments repository now uses the shared API base URL and auth token helpers for existing comment endpoints.
- Updates page now uses the shared API base URL and auth headers for notification routes.

## Permission Surface Integration

- Permission admin API helper now uses the shared API base URL, auth headers, and API error parsing.
- Existing document permission page now uses the shared API base URL, workspace ID, UUID, and auth header helpers.
- Existing one-time public/share/invite/SCIM token UI remains page-local and is not moved into global auth/client state.

## Error / Unconfigured Behavior

- Missing API base URL shows explicit unconfigured/fallback text on connected surfaces.
- 401/403 on workspace/search/bootstrap surfaces reports sign-in or unavailable live data and keeps demo fallback where the existing UI had demo data.
- Permission and admin mutation surfaces retain disabled/forbidden behavior from prior V1 work.

## Token / Secret Safety

- Auth access/refresh tokens are the only durable frontend tokens stored by the shared auth helper.
- Raw public/share/invite/SCIM tokens remain one-time UI state only.
- Passwords, MFA codes, password proofs, token hashes, provider secrets, public-link passwords, and SCIM token hashes are not stored in localStorage/sessionStorage by this wiring.

## Routes Wired

- `POST /api/v1/auth/login`
- `POST /api/v1/auth/logout`
- `GET /api/v1/auth/me`
- `GET /api/v1/auth/security-state`
- `GET /api/v1/bootstrap`
- `GET /api/v1/search`
- Existing comment routes under `GET/POST /api/v1/documents/{documentId}/comments...`
- Existing notification routes under `/api/v1/notifications`
- Existing permission routes under `/api/v1/permissions`
- Existing workspace member/group routes under `/api/v1/workspaces/{workspaceId}`
- Existing SCIM token/discovery routes under `/api/v1/workspaces/{workspaceId}/scim...`

## Missing Or Unsupported Existing Routes

- Full editor document CRUD remains a wiring gap. The editor currently owns local draft state, import/export, comment anchor runtime state, and Tiptap revision behavior; connecting it safely should be a separate focused round.
- Register UI was not wired because the visible login page does not currently expose a register workflow.
- Full OIDC/SAML login UI remains deferred by the frozen permission V1 register.

## Files Changed

- `apps/web/src/assets/css/northstar-login.css`
- `apps/web/src/components/DocumentSharePermissionsPage.tsx`
- `apps/web/src/components/NorthstarLoginPage.tsx`
- `apps/web/src/components/SearchDiscoveryPage.tsx`
- `apps/web/src/components/WorkspaceHomePage.tsx`
- `apps/web/src/components/WorkspaceHomeTopBar.tsx`
- `apps/web/src/components/WorkspaceUpdatesPage.tsx`
- `apps/web/src/lib/apiClient.ts`
- `apps/web/src/lib/appApi.ts`
- `apps/web/src/lib/authClient.ts`
- `apps/web/src/lib/commentRepository.ts`
- `apps/web/src/lib/permissionAdminApi.ts`
- `docs/agent/reports/frontend-backend-app-wiring-v1.md`

## Tests / Validation Run

- `npm run build`: passed.
- `npm test`: passed, 73 comment regression tests.

## Browser QA

Not run. Local Node is `v18.20.7`, below the runtime previously required by Browser Use.

## Not Run

- Backend restore/build/test: not run; backend code was not changed.
- PostgreSQL smoke: not run; backend code was not changed.

## Security Notes

- Permission V1 frozen behavior was not changed.
- Public-link runtime behavior was not broadened.
- Public links still route through existing share-link APIs and direct generic policy patch to `linkMode = public` was not exposed.
- Frontend checks remain UX only and are not a security boundary.

## Remaining Gaps

- Full editor document CRUD/backend persistence wiring.
- Register UI workflow if the product wants self-service registration surfaced.
- Browser acceptance QA after Node runtime upgrade.

## Smallest Safe Next Step

connect remaining existing UI surfaces
