# Frontend Permission Admin Surfaces V1

## Summary

- Implemented frontend workspace member management and SCIM management surfaces using existing backend APIs only.
- Added hash routes for `#workspace-members`, `#workspace-groups`, and `#scim`.
- Public-link runtime behavior was not broadened, and backend code was not changed.

## Scope

- Frontend permission-admin implementation only.
- Backend route/contracts were inspected only to wire existing APIs.
- No backend, migration, package, public-link, SCIM provisioning, MFA, or OIDC/SAML behavior was changed.

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
- `docs/agent/reports/frontend-permission-workflow-hardening-v1-1.md`

## Frontend Areas Inspected

- `apps/web/src/App.tsx`
- `apps/web/src/components/DocumentSharePermissionsPage.tsx`
- `apps/web/src/components/WorkspaceHomeTopBar.tsx`
- `apps/web/src/components/WorkspaceUpdatesPage.tsx`
- `apps/web/src/components/WorkspaceHomePage.tsx`
- `apps/web/src/index.css`
- `apps/web/package.json`

## Backend Contract Areas Inspected

- `services/api/src/Northstar.Api/Program.cs`
- `services/api/src/Northstar.Api/Controllers/WorkspacesController.cs`
- `services/api/src/Northstar.Api/Controllers/ScimController.cs`
- `services/api/src/Northstar.Api/Controllers/ScimTokensController.cs`
- `services/api/src/Northstar.Contracts/Workspaces/WorkspaceMemberDtos.cs`
- `services/api/src/Northstar.Contracts/Workspaces/WorkspaceGroupDtos.cs`
- `services/api/src/Northstar.Contracts/Security/ScimDtos.cs`
- `services/api/src/Northstar.Application/Workspaces/WorkspaceMembersService.cs`
- `services/api/src/Northstar.Application/Workspaces/WorkspaceGroupService.cs`
- `services/api/src/Northstar.Application/Security/ScimService.cs`
- `services/api/src/Northstar.Application/Security/ScimTokenService.cs`

## Workspace Member Management UI

- Added member list, add member, role update, and deliberate remove flows.
- Owner assignment is not exposed in the add-member UI.
- Existing owner rows remain visible and backend ownership rules remain the enforcement boundary.
- Reactivate is shown as unavailable because no existing route was found.
- Member external provider/subject fields are not shown because `WorkspaceMemberDto` does not expose them.

## SCIM Management UI

- Added SCIM endpoint display and discovery status.
- Added SCIM capability summary from existing discovery/resource-type endpoints.
- Added SCIM token list/create/revoke using existing token management APIs.
- Raw SCIM token is shown only in the one-time create-response panel and can be dismissed.
- SCIM provisioning UI, bulk, complex filters, enterprise extension, and delete/deactivate remain deferred.

## API Routes Wired

- `GET /api/v1/workspaces/{workspaceId}/members`
- `POST /api/v1/workspaces/{workspaceId}/members`
- `PATCH /api/v1/workspaces/{workspaceId}/members/{userId}`
- `DELETE /api/v1/workspaces/{workspaceId}/members/{userId}`
- `GET /api/v1/workspaces/{workspaceId}/groups`
- `GET /api/v1/workspaces/{workspaceId}/scim/v2/ServiceProviderConfig`
- `GET /api/v1/workspaces/{workspaceId}/scim/v2/Schemas`
- `GET /api/v1/workspaces/{workspaceId}/scim/v2/ResourceTypes`
- `GET /api/v1/workspaces/{workspaceId}/scim/tokens`
- `POST /api/v1/workspaces/{workspaceId}/scim/tokens`
- `DELETE /api/v1/workspaces/{workspaceId}/scim/tokens/{tokenId}`

Missing route categories surfaced as gaps:

- workspace member reactivate route
- workspace member external identity fields in member list DTO
- full SCIM provisioning management UI routes beyond existing SCIM client endpoints

## Read-Only / Unsupported States

- Missing API base URL or workspace ID renders not-connected state and disables mutation controls.
- `401`/`403` responses render forbidden/unavailable states and do not turn frontend checks into security.
- Group mutation controls are read-only in this surface.
- SCIM provisioning operations are summarized, not exposed as frontend mutation UI.

## Token Safety

- SCIM token lists show token id/name/status/expiry/last-used metadata only.
- Raw SCIM token appears only immediately after create.
- Raw SCIM token can be dismissed and is not stored in durable frontend state.
- Token hashes are not displayed in token lists.
- No localStorage/sessionStorage token persistence was added.

## Authorization Behavior

- Frontend checks are UX-only.
- Workspace member and SCIM token management remain protected by existing backend authorization.
- No owner assignment shortcut was added to the add-member UI.
- No public-link behavior or anonymous access boundary was touched.

## Browser QA

- In-app Browser Use bridge could not start because the Node REPL runtime is v18.20.7 and Browser Use requires Node >= v22.22.0.
- Started a temporary local mock API outside the repository.
- Started Vite with `VITE_NORTHSTAR_API_BASE_URL` and `VITE_NORTHSTAR_WORKSPACE_ID`.
- Used headless Chrome/CDP fallback.
- Verified desktop `#workspace-members` at `1440x1000`: members, groups, tabs, disabled reactivate, no owner add option, no public-link text, no page-level horizontal overflow.
- Verified `#scim`: discovery, endpoint display, token list, token create one-time raw token panel, token dismiss, no raw token after dismiss, no page-level horizontal overflow.
- Verified mobile-ish `#workspace-members` at `390x900`: members render, tabs are reachable, no page-level horizontal overflow.
- Verified unavailable workspace state disables add-member mutation controls.

## Files Changed

- `apps/web/src/App.tsx`
- `apps/web/src/components/DocumentSharePermissionsPage.tsx`
- `apps/web/src/components/PermissionAdminSurfacesPage.tsx`
- `apps/web/src/index.css`
- `apps/web/src/lib/permissionAdminApi.ts`
- `docs/PERMISSION_SYSTEM_CONTRACT.md`
- `docs/agent/00-project-state.md`
- `docs/agent/skills/permissions.md`
- `docs/agent/reports/frontend-permission-admin-surfaces-v1.md`

## Tests / Validation Run

- `npm run build`: passed. Vite reported the existing large chunk warning.
- `npm test`: passed. 73 comment regression tests passed.
- Browser QA: completed with temporary mock API, local Vite, and headless Chrome/CDP fallback.

## Not Run

- Backend validation: not run; backend code was not changed.
- PostgreSQL smoke: not run; backend code was not changed.
- Object storage integration: not run; not in scope.

## Security Notes

- Public-link runtime behavior was not broadened.
- Generic policy patch behavior was not touched.
- SCIM raw tokens are not shown in long-lived token lists.
- SCIM token hashes are not shown.
- The frontend does not store raw SCIM tokens in durable state.
- Member and token authorization remains server-side.

## Remaining Deferred Permission Work

- full MFA provider/enrollment/step-up enforcement
- full OIDC/SAML provider redirect/callback and secret management
- SCIM bulk operations
- SCIM complex filter grammar
- SCIM enterprise extension
- SCIM delete/deactivate behavior
- full frontend public-link interaction and browser acceptance hardening, if still deferred

## Smallest Safe Next Step

harden frontend public-link interaction
