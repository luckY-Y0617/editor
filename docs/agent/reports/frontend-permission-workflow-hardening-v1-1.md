# Frontend Permission Workflow Hardening V1.1

## Summary

- Hardened the document permission workflow by fixing frontend UUID validation for standard backend GUIDs.
- Browser QA confirmed the page reaches the ready state against a local mock API after the fix.
- Public-link runtime behavior was not broadened.
- Backend code was not changed.

## Scope

- Frontend hardening and browser QA only.
- Inspected and changed the document Share & Permissions workflow.
- No backend, migration, package, route, or public-link behavior changes were made.

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
- `docs/agent/reports/frontend-permission-mutation-workflow-v1.md`

## Frontend Areas Inspected

- `apps/web/src/components/DocumentSharePermissionsPage.tsx`
- `apps/web/src/index.css`
- `apps/web/src/App.tsx`
- `apps/web/package.json`

## Browser QA Performed

- Started a local mock permission API outside the repository.
- Started a Vite dev server with `VITE_NORTHSTAR_API_BASE_URL`, `VITE_NORTHSTAR_SHARE_DOCUMENT_ID`, and `VITE_NORTHSTAR_WORKSPACE_ID`.
- Used headless Chromium/Edge screenshots and DOM/CDP checks because the in-app Browser Use bridge could not start with the available Node REPL runtime.
- Verified desktop viewport at `1440x1000`.
- Verified mobile-ish viewport at `390x900`.
- Verified the ready state shows policy/effective-role data, People/Groups/Access Settings tabs, scoped grants, share links, and email invites.
- Verified mobile viewport did not report horizontal overflow.

## Hardening Changes

- Fixed `isUuid` in `DocumentSharePermissionsPage.tsx` to accept standard `8-4-4-4-12` GUIDs.
- This was required because standard backend GUIDs were previously rejected as invalid, leaving the page in `Not connected` state even when document/workspace IDs were configured.

## Grant Workflow QA

- Direct user grant form was visible in ready state.
- Empty user ID kept `Create user grant` disabled.
- Scoped grants rendered from API data.
- Edit/revoke controls were present for grants.
- Grant edit mode exposes titled save/cancel icon controls.
- Revoke flow exposes a deliberate confirmation state before actual revoke.

## Policy Workflow QA

- Access Settings tab was reachable.
- Policy UI contains inheritance mode, non-public link modes, and default link role controls.
- Supported policy values remain limited to inheritance, restricted, disabled, internal, external, viewer, and commenter.
- Direct `linkMode = public` was not exposed as a generic policy patch option.
- Public link guidance remains informational: public links are created only through dedicated share-link APIs.

## Share / Invite Token QA

- Existing active share-link and invite lists did not expose raw tokens.
- Creating an internal link showed a one-time generated-token panel.
- The generated-token panel was dismissible.
- Raw generated token remained limited to the create-response UI.

## Authorization And Read-Only QA

- Unconfigured existing dev-server state rendered disabled controls and `Not connected` labels without clearing static readable page context.
- Ready-state mock API rendered mutation controls only after the permission API was configured.
- Frontend checks remain UX-only and are not treated as security enforcement.

## API Routes Verified

- `GET /api/v1/permissions/resources/document/{documentId}`
- `POST /api/v1/permissions/resources/document/{documentId}/grants`
- `PATCH /api/v1/permissions/resources/document/{documentId}/grants/{grantId}`
- `DELETE /api/v1/permissions/resources/document/{documentId}/grants/{grantId}`
- `PATCH /api/v1/permissions/resources/document/{documentId}/policy`
- `GET /api/v1/workspaces/{workspaceId}/groups`
- Existing share-link and email-invite routes used by the page were exercised against the local mock API.

## Files Changed

- `apps/web/src/components/DocumentSharePermissionsPage.tsx`
- `docs/agent/reports/frontend-permission-workflow-hardening-v1-1.md`

## Validation Run

- `npm run build`: passed.
- `npm test`: passed; 73 comment regression tests passed.
- Browser QA: completed with local Vite, local mock API, and headless browser fallback.

## Not Run

- Backend validation: not run; backend code was not changed.
- PostgreSQL smoke: not run; backend code was not changed.
- Object storage integration: not run; not in scope.
- In-app Browser Use QA: not run; Node REPL runtime was v18.20.7 and the Browser Use bridge requires Node >= v22.22.0.

## Security Notes

- Public-link runtime behavior was not broadened.
- Generic policy patch UI still does not expose `public`.
- Raw share/invite tokens are only shown in one-time create-response UI and are not shown in long-lived lists.
- No backend authorization behavior was changed.

## Remaining Deferred Permission Work

- full MFA provider/enrollment/step-up enforcement
- full OIDC/SAML provider redirect/callback and secret management
- SCIM bulk operations
- SCIM complex filter grammar
- SCIM enterprise extension
- SCIM delete/deactivate behavior
- workspace member management UI
- SCIM management UI
- full frontend public-link interaction and browser acceptance hardening, if still deferred

## Smallest Safe Next Step

add workspace member management UI
