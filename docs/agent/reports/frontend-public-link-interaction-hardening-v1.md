# Frontend Public-Link Interaction Hardening V1

## Summary

- Hardened document public-link creation on Share & Permissions using the existing share-link API only.
- Kept generic policy mutation non-public; `linkMode = public` is not exposed in Access Settings.
- Browser QA passed with a temporary local mock API and headless Chrome/CDP fallback.

## Scope

- Frontend public-link interaction and browser QA only.
- No backend code, migrations, package files, public anonymous routes, or protected API boundaries were changed.

## Docs Read

- `AGENTS.md`
- `docs/agent/00-project-state.md`
- `docs/agent/01-control-rules.md`
- `docs/agent/02-conflict-register.md`
- `docs/agent/03-required-reading-order.md`
- `docs/agent/04-implementation-protocol.md`
- `docs/agent/05-validation-protocol.md`
- `docs/agent/06-final-report-format.md`
- `docs/agent/skills/permissions.md`
- `docs/agent/skills/api-contracts.md`
- `docs/agent/skills/frontend-editor.md`
- `docs/PERMISSION_SYSTEM_CONTRACT.md`
- `docs/agent/reports/frontend-permission-admin-surfaces-v1.md`

## Frontend Areas Inspected

- `apps/web/src/App.tsx`
- `apps/web/src/components/DocumentSharePermissionsPage.tsx`
- `apps/web/src/components/PermissionAdminSurfacesPage.tsx`
- `apps/web/src/index.css`
- `apps/web/package.json`

## Backend Contract Areas Inspected

- `services/api/src/Northstar.Api/Controllers/PermissionsController.cs`
- `services/api/src/Northstar.Api/Controllers/PublicShareLinksController.cs`
- `services/api/src/Northstar.Contracts/Security/PermissionManagementDtos.cs`
- `services/api/src/Northstar.Application/Security/ShareLinkService.cs`
- `services/api/src/Northstar.Domain/Security/ShareLink.cs`
- `services/api/src/Northstar.Domain/Security/ShareLinkAudiences.cs`

## Public-Link Workflow Implemented

- Added document public-link creation controls to Share & Permissions.
- Public link creation sends `audience = public`, `roleKey = viewer`, required future `expiresAt`, and optional `password` through `POST /api/v1/permissions/resources/document/{documentId}/share-links`.
- Public link creation does not call policy patch and does not expose collection public-link document-content behavior.
- Active links continue to revoke through `DELETE /api/v1/permissions/share-links/{shareLinkId}`.

## Public-Link Token Safety

- Generated public URL/token are shown only in the create-response panel.
- Generated panel can be dismissed, after which the raw token is no longer visible.
- Active link list shows only metadata: audience, role, expiry, revoked state, and password-required state.
- Active link list does not show raw token, token hash, password, password hash, or password proof.
- No durable frontend storage was added for public tokens or passwords.

## Policy Boundary

- Access Settings remains limited to `disabled`, `internal`, and `external`.
- Direct generic policy patch for `linkMode = public` remains unavailable in the frontend.
- Public share-link creation remains separate from resource policy mutation.

## Public Collection Boundary

- No public collection content UI was added.
- Existing backend collection public-link behavior remains outside this document page workflow.

## API Routes Wired

- `GET /api/v1/permissions/resources/document/{documentId}/share-links`
- `POST /api/v1/permissions/resources/document/{documentId}/share-links`
- `DELETE /api/v1/permissions/share-links/{shareLinkId}`
- Public create response may display `/api/v1/public/share-links/{token}/resolve` returned by the backend.

## Read-Only / Unsupported States

- Missing API base URL or document ID keeps public-link mutation controls disabled.
- `401`/`403` share-link reads mark link operations unavailable without treating frontend checks as security.
- Previously loaded readable data is not intentionally cleared only because a later forbidden state occurs.

## Browser QA

- Browser Use bridge was attempted but unavailable because the Node REPL runtime is `v18.20.7`; Browser Use requires Node `>=22.22.0`.
- Started a temporary local mock API outside the repository.
- Started Vite with `VITE_NORTHSTAR_API_BASE_URL`, `VITE_NORTHSTAR_WORKSPACE_ID`, and `VITE_NORTHSTAR_SHARE_DOCUMENT_ID`.
- Used headless Chrome/CDP fallback.
- Verified desktop width `1920x1000`: public controls render, expiry is required before submit, generated public URL/token appears once, password input clears after create, generated panel dismisses, active link list does not expose secrets, revoke is visible, Access Settings does not expose `public`, no page-level horizontal overflow.
- Verified mobile-ish width `390x900`: public controls render and no page-level horizontal overflow.

## Files Changed

- `apps/web/src/components/DocumentSharePermissionsPage.tsx`
- `apps/web/src/index.css`
- `docs/PERMISSION_SYSTEM_CONTRACT.md`
- `docs/agent/00-project-state.md`
- `docs/agent/skills/permissions.md`
- `docs/agent/reports/frontend-public-link-interaction-hardening-v1.md`

## Tests / Validation Run

- `npm run build`: passed. Vite reported the existing large chunk warning.
- `npm test`: passed. 73 comment regression tests passed.
- Browser QA: passed with temporary mock API, Vite dev server, and headless Chrome/CDP fallback.

## Not Run

- Backend validation: not run; backend code was not changed.
- PostgreSQL smoke: not run; backend code was not changed.
- Object storage integration: not run; not in scope.

## Security Notes

- Public-link runtime behavior was not broadened.
- Protected APIs were not made public-link accessible.
- Generic policy patch still does not expose direct `linkMode = public`.
- Raw public tokens and passwords are not stored in localStorage/sessionStorage.
- Token hashes, password hashes, and password proofs are not shown in UI.

## Remaining Deferred Permission Work

- full MFA provider/enrollment/step-up enforcement
- full OIDC/SAML provider redirect/callback and secret management
- SCIM bulk operations
- SCIM complex filter grammar
- SCIM enterprise extension
- SCIM delete/deactivate behavior

## Smallest Safe Next Step

run end-to-end permission acceptance validation
