# Workspace Members & Groups IA Discovery V1

Date: 2026-05-16

Scope: code-level discovery for current Workspace Members, Groups/Teams/IAM
group capability and IA recommendation. This report does not implement member
or group management features.

## Sources Inspected

- Backend:
  - `services/api/src/Northstar.Api/Controllers/WorkspacesController.cs`
  - `services/api/src/Northstar.Application/Workspaces/WorkspaceMembersService.cs`
  - `services/api/src/Northstar.Application/Workspaces/WorkspaceGroupService.cs`
  - `services/api/src/Northstar.Application/Workspaces/IamSyncService.cs`
  - `services/api/src/Northstar.Application/Security/EffectivePermissionService.cs`
  - `services/api/src/Northstar.Application/Security/ResourcePermissionManagementService.cs`
  - `services/api/src/Northstar.Domain/Workspaces/WorkspaceMember.cs`
  - `services/api/src/Northstar.Domain/Security/WorkspaceGroup.cs`
  - `services/api/src/Northstar.Domain/Security/WorkspaceGroupMember.cs`
  - EF configurations and migrations for `workspace_members`,
    `workspace_groups`, `workspace_group_members`, `resource_access_grants`,
    and `scim_tokens`
  - focused API/domain/application tests in `services/api/tests`
- Frontend:
  - `apps/web/src/components/WorkspaceSettingsPage.tsx`
  - `apps/web/src/components/PermissionAdminSurfacesPage.tsx`
  - `apps/web/src/components/DocumentShareDrawer.tsx`
  - `apps/web/src/components/DocumentSharePermissionsPage.tsx`
  - `apps/web/src/lib/permissionAdminApi.ts`
  - `apps/web/src/lib/permissionAdminModel.ts`
  - `apps/web/src/lib/workspaceSettingsModel.ts`
  - `apps/web/src/lib/appApi.ts`

## Executive Summary

- Members are real backend resources, not demo-only UI. The backend has a
  `workspace_members` table/entity and APIs for list, add existing user by
  email, update role, and remove.
- Groups are real backend resources, not just permission metadata. The backend
  has workspace-scoped `workspace_groups` and `workspace_group_members`, local
  static group management APIs, IAM sync, and SCIM group provisioning.
- The frontend exposes live member management and live group read surfaces.
  Backend local group mutation exists, but current frontend group management is
  read/list focused and deliberately disables group mutation controls.
- External/IAM/SCIM groups are read-only through local group APIs and may still
  receive resource grants.
- The strongest IA option is `Members & Teams` as a workspace left-nav item,
  with Settings retaining redirect/link entries during migration.

## Capability Matrix

### Members

| Question | Current Status |
| --- | --- |
| Real backend model? | Yes. `WorkspaceMember` domain entity and `workspace_members` EF table. |
| Workspace member table/entity? | Yes. Composite key `(workspace_id, user_id)`. |
| List members? | Yes. `GET /api/v1/workspaces/{workspaceId}/members`. |
| Invite member? | No pending invite lifecycle here. `POST /members` adds an existing user by email; unknown email returns validation error. Email invites exist only for resource access, not workspace membership. |
| Remove member? | Yes. `DELETE /members/{userId}` with last-owner protection. |
| Change role? | Yes. `PATCH /members/{userId}` with role validation and last-owner protection. |
| Workspace roles? | `owner`, `admin`, `editor`, `viewer`. `commenter` is explicitly rejected for workspace membership. |
| Demo/static UI only? | No. Settings and permission admin member surfaces call real APIs when configured. |
| Current Members tab data source? | `apps/web/src/components/WorkspaceSettingsPage.tsx` calls `getPermissionWorkspaceMembers`, backed by `/workspaces/{workspaceId}/members`. |
| Tests? | Yes. API tests cover last-owner protection, commenter rejection, member helper flows, and member role effects across permission tests. |

### Groups / Teams

| Question | Current Status |
| --- | --- |
| Real group model? | Yes. `WorkspaceGroup` and `WorkspaceGroupMember` domain entities. |
| Workspace-scoped or org-scoped? | Workspace-scoped. Group rows carry `workspace_id`; no org-scoped group model was verified. |
| Group membership? | Yes. `workspace_group_members` with soft removal and optional `expires_at`. |
| Create/update/delete group? | Backend supports create, update, and archive for local static groups. Delete is archive semantics through `DELETE /groups/{groupId}`. |
| Add/remove group members? | Backend supports add and remove for local mutable groups. |
| Group grants? | Yes. `resource_access_grants.subject_type` supports `group`. |
| Only referenced by permissions? | No. Groups have their own workspace APIs plus permission grant integration. |
| IAM/SCIM/external source? | Yes. External groups store `external_provider`, `external_group_id`, and `external_synced_at`. IAM sync and SCIM provisioning can create/update these. |
| Read-only? | External/IAM/SCIM groups are read-only through normal local group APIs. Local static groups are backend-editable. |
| Mock metadata only? | No. Frontend reads real group metadata; editable group UI is not exposed in current V1. |
| Tests? | Yes. API tests cover create/update/archive, add/remove member audit, group grant authorization/fan-out, IAM-managed read-only enforcement, IAM sync, SCIM provisioning, and migrations. |

### IAM / External Directory

| Question | Current Status |
| --- | --- |
| SCIM/OIDC/SSO/IAM models or docs? | Yes. IAM sync DTO/service, SCIM controllers/services/tokens, user external identity fields, and group external metadata exist. |
| Should groups be synced externally? | Supported for IAM/SCIM-managed groups. The code also supports local static groups, so groups are not exclusively external. |
| Does the project say groups should not be app-managed? | No. Code supports app-managed local static groups. It rejects local mutations only for external/IAM-managed groups. |
| SCIM provisioning UI? | Not exposed. Frontend exposes SCIM discovery and token management, not SCIM user/group provisioning forms. |
| IAM sync UI? | Not exposed. Backend `POST /workspaces/{workspaceId}/iam/sync` exists. |
| OIDC/SAML UI? | Not exposed; full provider redirect/callback remains deferred. |

### Permission Grants Relationship

| Question | Current Status |
| --- | --- |
| People grants resource scopes | `document` and `collection` through resource grant APIs. Workspace access is represented by `workspace_members`, not `resource_access_grants`. |
| Group grants resource scopes | `document` and `collection` through resource grant APIs. |
| Library grants? | Not verified as `resource_access_grants`; library appears in public share-link scope, not general scoped grants. |
| Workspace grants? | No `resource_access_grants` workspace scope; workspace RBAC uses `workspace_members`. |
| Advanced Permissions group grants? | Yes for document advanced permissions; frontend can display groups and create document grants with `subjectType = "group"`. |
| Does group membership modification affect grants? | Yes at authorization time: effective permission resolves active user group ids and active group grants. Removed/expired memberships are ignored without rewriting grants. |
| Inheritance plus group grant? | Yes. Effective permission combines workspace fallback, collection/document inheritance, direct user grants, and group grants; restricted mode allows owner/admin escape and scoped grant paths. |

## Frontend Surface Discovery

- Workspace Settings:
  - `#settings?scope=workspace&tab=members` is live-backed and exposes member
    add, role update, and removal through existing APIs.
  - `#settings?scope=workspace&tab=permissions` shows group and boundary
    information. It is not a full group management UI.
  - `#settings?scope=workspace&tab=integrations` exposes SCIM discovery and
    SCIM bearer-token list/create/revoke.
- Permission Admin surface:
  - `PermissionAdminSurfacesPage` has Members, Groups, and SCIM tabs.
  - Members are editable.
  - Groups are listed with disabled mutation control; local groups say "Group
    mutation UI is outside V1"; IAM groups say they are read-only.
- Document Share Drawer / Advanced Permissions:
  - Document Share Drawer loads workspace members and groups for access
    summaries and grant subject context.
  - Document Advanced Permissions can use group subject grants.
  - IAM-managed group source metadata is displayed as read-only context.

## IA Recommendation

Recommended left navigation:

```text
Workspace
- Home
- Library
- Access & Sharing
- Members & Teams
- Settings
```

Recommended page tabs:

- Members
- Groups
- SCIM / Directory
- Roles & Access Summary

Why this recommendation:

- "Members" alone under-names the real group and SCIM capability.
- "Teams" alone overstates product language because the backend term is group
  and external directory groups are not necessarily teams.
- "Identity & Access" is accurate for enterprise IAM, but too broad because
  Access & Sharing already owns share-link governance and document-level access
  operations.
- "Members & Teams" fits the product mental model while allowing backend/API
  copy to continue using `group` where appropriate.

Settings migration recommendation:

- Do not move navigation in this docs round.
- In the next implementation round, create the left-nav entry and route while
  keeping existing Settings Members/Permissions/Integrations entries as
  redirects or links.
- Keep document Advanced Permissions document-scoped and keep Access & Sharing
  as link governance.

If groups remain read-only/IAM-synced in a given row:

- Show source provider and external id.
- Disable edit/archive/member mutation controls with an explicit read-only
  reason.
- Continue allowing group grants where backend allows the group and the group
  is not archived.

If local group management is enabled in a future round:

- First version should include group list/detail, create local static group,
  rename/description, archive, add/remove active workspace members, and audit
  visibility.
- Do not expose dynamic group editing locally; dynamic/external groups should
  remain directory-managed.

## Recommended Next Implementation Round

Create a `Members & Teams` workspace route without changing backend behavior:

- Move the existing live member table/forms into the new page.
- Move current group read table into the new page.
- Add read-only group detail and member list using existing group detail API.
- Keep group mutation disabled in the first move unless explicitly requested.
- Add redirects/links from Settings Members, Permissions Groups, and
  Integrations SCIM.
- Do not add member invitation, organization-level members, editable IAM group
  management, or workspace public share in that round.

## Validation

This report is based on code inspection only. No application code changed and
no backend/frontend test suite was run for this documentation-only discovery.
