# Frontend Permission Mutation Workflow V1

## Summary

- Implemented a document-scoped frontend workflow for direct user grants, group grants, grant edit/revoke, and supported resource policy mutation.
- Kept public links separate from generic policy mutation; `linkMode = public` is not exposed through Access Settings.
- Backend code was not changed.

## Scope

- Frontend implementation only.
- Target page: `apps/web/src/components/DocumentSharePermissionsPage.tsx`.
- No backend feature scope, migrations, SCIM UI, MFA UI, OIDC/SAML UI, or workspace member management UI.

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
- `docs/agent/reports/secure-invite-outbox-retry-implementation.md`

## Frontend Areas Inspected

- `apps/web/src/components/DocumentSharePermissionsPage.tsx`
- `apps/web/src/index.css`
- `apps/web/src/App.tsx`
- `apps/web/src/data/sharePermissionsData.ts`
- `apps/web/package.json`
- Backend route/DTO files were inspected for contract alignment only.

## Workflow Implemented

- The People tab can create direct user grants.
- The Groups tab can create direct group grants from workspace groups.
- Existing grants can be edited for role and expiry.
- Existing grants can be deliberately revoked.
- The Access Settings tab is enabled.

## Grant Mutation Behavior

- Grant create uses backend `availableRoles`.
- User grants require a UUID subject id before submit.
- Group grants use active workspace group ids from the groups API.
- Grant update preserves subject type/id and changes only role, expiry, and reason.
- Grant revoke requires a second confirm click.
- Permission data refreshes after successful mutations.

## Policy Mutation Behavior

- Access Settings supports `inheritanceMode`, `linkMode = disabled|internal|external`, and viewer/commenter default link role.
- Direct `linkMode = public` is not exposed.
- Public links remain tied to dedicated share-link APIs.

## Share / Invite Behavior

- Existing share-link and email-invite create/revoke workflows remain.
- Successful create/revoke refreshes related permission data.
- Create-response raw tokens are held only in dismissible one-time UI state.
- Raw tokens are not shown in active link/invite lists.

## Authorization And Read-Only Behavior

- 401/403 responses set forbidden status and disable mutation controls.
- Previously loaded readable data is retained if a later refresh returns 401/403.
- Frontend checks are UI affordances only; backend authorization remains authoritative.

## API Routes Wired

- `GET /api/v1/permissions/resources/document/{documentId}`
- `POST /api/v1/permissions/resources/document/{documentId}/grants`
- `PATCH /api/v1/permissions/resources/document/{documentId}/grants/{grantId}`
- `DELETE /api/v1/permissions/resources/document/{documentId}/grants/{grantId}`
- `PATCH /api/v1/permissions/resources/document/{documentId}/policy`
- Existing share-link, email-invite, access-request, and workspace-group routes remain wired.

## UI / UX Notes

- Reused the existing Share & Permissions page layout and styling.
- Added dense operational controls without a broad redesign.
- Added responsive form/grid behavior for compact viewports.
- No browser manual QA was run in this turn.

## Files Changed

- `apps/web/src/components/DocumentSharePermissionsPage.tsx`
- `apps/web/src/index.css`
- `docs/PERMISSION_SYSTEM_CONTRACT.md`
- `docs/agent/00-project-state.md`
- `docs/agent/skills/permissions.md`
- `docs/agent/reports/frontend-permission-mutation-workflow-v1.md`

## Tests / Validation Run

- `npm run build` from `apps/web`: passed; Vite reported the existing large-chunk warning.
- `npm test` from `apps/web`: passed; 73 comment regression tests passed.

## Not Run

- Backend validation: not run; backend code was not changed.
- PostgreSQL smoke: not run; backend code was not changed. User previously reported PostgreSQL smoke passed manually.
- Frontend browser/manual QA: not run.
- Object storage integration: not run; not in scope.

## Security Notes

- Public-link runtime behavior was not broadened.
- Generic resource policy mutation still does not expose `linkMode = public`.
- Raw share-link and invite tokens are displayed only in one-time create-response UI and can be dismissed.
- No permission state was written into Tiptap document JSON.

## Remaining Deferred Permission Work

- full MFA provider/enrollment/step-up enforcement
- full OIDC/SAML provider redirect/callback and secret management
- SCIM bulk operations
- SCIM complex filter grammar
- SCIM enterprise extension
- SCIM delete/deactivate behavior
- workspace member management UI
- SCIM management UI
- full frontend public-link interaction and browser acceptance hardening

## Smallest Safe Next Step

- harden frontend permission workflow
