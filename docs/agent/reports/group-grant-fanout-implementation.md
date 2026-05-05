# Group Grant Fan-Out Implementation

## Summary

- Implemented in-app notification fan-out for group-subject resource grant create/update/revoke.
- Access-request created notifications now expand through group grants with resource `manage_permissions`.
- Public-link runtime behavior was not broadened.

## Scope

- Backend-only permission-system slice.
- Implemented group grant notification fan-out only.
- Did not implement SCIM, MFA/recent-auth, real IdP login, production invite delivery, or frontend workflows.

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
- `docs/agent/skills/backend-clean-architecture.md`
- `docs/agent/skills/data-model-migrations.md`
- `docs/PERMISSION_SYSTEM_CONTRACT.md`
- `docs/agent/reports/permission-notification-fanout-implementation.md`

## Code Areas Inspected

- `services/api/src/Northstar.Application/Security/ResourcePermissionManagementService.cs`
- `services/api/src/Northstar.Application/Security/AccessRequestService.cs`
- `services/api/src/Northstar.Application/Security/PermissionNotificationFanoutService.cs`
- `services/api/src/Northstar.Application/Security/IWorkspaceGroupRepository.cs`
- `services/api/src/Northstar.Infrastructure/Security/EfWorkspaceGroupRepository.cs`
- `services/api/src/Northstar.Domain/Security/ResourceAccessGrant.cs`
- `services/api/src/Northstar.Domain/Security/WorkspaceGroup.cs`
- `services/api/src/Northstar.Domain/Security/WorkspaceGroupMember.cs`
- `services/api/tests/Northstar.Api.Tests/KnowledgeApiTests.cs`
- `services/api/tests/Northstar.Application.Tests`

## Fan-Out Behavior Implemented

- `permission.grant_created` expands group-subject grants to active group members.
- `permission.grant_updated` expands group-subject grants to active group members.
- `permission.grant_revoked` expands group-subject grants to active group members.
- `access_request.created` includes active members of group grants whose role has resource `manage_permissions`.
- Fan-out excludes removed members, expired members, archived groups, users without active workspace membership, and the actor.
- Recipient expansion deduplicates workspace managers, direct user grants, group grants, and extra recipients.

## Notification Preference Behavior

- Matching resource-level muted preferences suppress group grant fan-out for that recipient/resource.
- Workspace-level muted preferences suppress fan-out when no resource-level preference overrides them.
- No notification preference API shape changed.

## Data Model / Migration Changes

- Reused `permission_notifications`.
- No new table was added.
- No migration was added.

## API / Contract Changes

- No API route or DTO shape changed.
- `docs/PERMISSION_SYSTEM_CONTRACT.md` was updated to document group grant fan-out as implemented.

## Application / Domain / Infrastructure Changes

- `ResourcePermissionManagementService` now delegates grant notifications to `PermissionNotificationFanoutService`.
- `AccessRequestService` now delegates access-request-created manager fan-out to `PermissionNotificationFanoutService`.
- `PermissionNotificationFanoutService` now expands group grant recipients and applies muted preference filtering.
- `IWorkspaceGroupRepository` / `EfWorkspaceGroupRepository` now expose active group member user-id expansion with active workspace membership filtering.
- Domain notification type keys were reused; no new notification types were added.

## Tests Added Or Updated

- Added API coverage for group grant create/update/revoke notifications to active group members.
- Added coverage that removed members, expired members, non-workspace members, and actors are not notified.
- Added muted preference suppression coverage for group grant revocation notification.
- Added access-request-created coverage for group grant manager expansion and recipient deduplication.
- Updated Application test fakes for the new group repository method.

## Validation Run

- `dotnet build services/api/Northstar.sln`: passed with 0 warnings and 0 errors.
- `dotnet test services/api/Northstar.sln`: passed.
- `dotnet restore services/api/Northstar.sln`: passed.

## Not Run

- PostgreSQL smoke: not run by this agent because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not visible to this process.
- User reported PostgreSQL smoke passed manually before this slice.
- frontend build/test: not run; frontend code was not changed.
- object storage integration: not run; not in scope.

## Security Notes

- Public-link behavior and anonymous access boundaries were not changed.
- Generic policy patch behavior was not changed.
- Group expansion filters out removed/expired group memberships and users without active workspace membership.
- Existing notification type keys are reused; no raw tokens, token hashes, passwords, password hashes, accept URLs, or provider secrets are involved in this slice.

## Remaining Deferred Permission Work

- SCIM endpoint skeleton
- real IdP login
- MFA/recent-auth backend state
- production invite delivery provider
- frontend permission mutation workflows

## Smallest Safe Next Step

implement SCIM endpoint skeleton
