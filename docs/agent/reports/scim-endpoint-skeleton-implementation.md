# SCIM Endpoint Skeleton Implementation

## Summary

- Implemented a workspace-scoped SCIM 2.0 endpoint skeleton under `/api/v1/workspaces/{workspaceId}/scim/v2`.
- Discovery endpoints return service-provider, schema, and resource-type shapes for workspace owners/admins.
- User and Group provisioning endpoints explicitly reject as not implemented and do not mutate users or groups.

## Scope

- Backend-only permission-system slice.
- Implemented SCIM endpoint skeleton only.
- Did not implement full SCIM provisioning, SCIM bearer-token validation, real IdP login, MFA/recent-auth, production invite delivery, frontend workflows, or public-link changes.

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
- `docs/agent/reports/group-grant-fanout-implementation.md`

## Code Areas Inspected

- `services/api/src/Northstar.Api/Program.cs`
- `services/api/src/Northstar.Api/Controllers/WorkspacesController.cs`
- `services/api/src/Northstar.Api/Controllers/PermissionsController.cs`
- `services/api/src/Northstar.Application/Workspaces/IamSyncService.cs`
- `services/api/src/Northstar.Application/Workspaces/IIamSyncService.cs`
- `services/api/src/Northstar.Application/Workspaces/IIamSyncRepository.cs`
- `services/api/src/Northstar.Contracts/Workspaces/IamSyncDtos.cs`
- `services/api/src/Northstar.Application/Security/WorkspaceAccessService.cs`
- `services/api/tests/Northstar.Api.Tests/KnowledgeApiTests.cs`

## SCIM Behavior Implemented

- Added `GET /api/v1/workspaces/{workspaceId}/scim/v2/ServiceProviderConfig`.
- Added `GET /api/v1/workspaces/{workspaceId}/scim/v2/Schemas`.
- Added `GET /api/v1/workspaces/{workspaceId}/scim/v2/ResourceTypes`.
- Added placeholder User and Group provisioning routes for `POST`, `PATCH`, `PUT`, and `DELETE`.
- Placeholder provisioning routes return `400 VALIDATION_ERROR` with explicit not-implemented messages.
- No user, group, workspace membership, or IAM-managed group mutations are performed by SCIM routes.

## Authentication And Authorization

- SCIM routes use the existing authenticated API boundary.
- All SCIM service methods require `IWorkspaceAccessService.EnsureCanManageWorkspaceAsync`.
- Workspace owners/admins can read SCIM discovery shapes.
- Unauthenticated requests are rejected by the existing JWT challenge path.
- Authenticated users without workspace manage permission are rejected with `403 FORBIDDEN`.
- Dedicated SCIM bearer-token validation remains deferred.

## Data Model / Migration Changes

- No data model change.
- No EF entity or configuration change.
- No migration added.

## API / Contract Changes

- Added SCIM DTOs in `Northstar.Contracts.Security`.
- Added `ScimController`.
- Added `IScimService` and `ScimService`.
- No existing API response shape changed.
- Public-link routes and protected public-link boundaries were not changed.

## Application / Domain / Infrastructure Changes

- API: thin controller delegates all behavior to `IScimService`.
- Contracts: SCIM discovery/resource DTOs were added.
- Application: SCIM service owns authorization and skeleton responses.
- Domain: no change.
- Infrastructure: no change.

## Tests Added Or Updated

- Added API test coverage that SCIM discovery returns the expected skeleton shape.
- Added API test coverage that unauthenticated SCIM management requests return `401 UNAUTHORIZED`.
- Added API test coverage that authenticated non-manager requests return `403 FORBIDDEN`.
- Added API test coverage that User and Group provisioning operations return explicit unsupported responses.

## Validation Run

- `dotnet restore services/api/Northstar.sln`: passed.
- `dotnet build services/api/Northstar.sln`: passed with 0 warnings and 0 errors.
- `dotnet test services/api/Northstar.sln`: passed.

## Not Run

- PostgreSQL smoke: not run by this agent because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not visible to this process.
- User previously reported PostgreSQL smoke passed manually.
- frontend build/test: not run; frontend code was not changed.
- object storage integration: not run; not in scope.

## Security Notes

- SCIM skeleton does not accept provider secrets, IdP tokens, SAML assertions, OIDC tokens, or SCIM bearer tokens.
- SCIM skeleton does not write audit events because it does not mutate users, groups, memberships, or permissions.
- SCIM skeleton does not expose token hashes, passwords, provider secrets, or internal EF entities.
- Public-link runtime behavior and anonymous access boundaries were not changed.

## Remaining Deferred Permission Work

- real IdP login
- production invite delivery provider
- frontend permission mutation workflows
- full SCIM provisioning behavior
- dedicated SCIM bearer-token validation

## Smallest Safe Next Step

implement real IdP login boundary
