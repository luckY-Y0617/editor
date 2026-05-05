# SCIM Provisioning V1 Implementation

## Summary

- Implemented minimal SCIM 2.0 User/Group provisioning V1 for workspace-scoped SCIM endpoints.
- Provisioning mutations require valid dedicated workspace-scoped SCIM bearer-token authentication.
- Public-link runtime behavior was not broadened.

## Scope

- Backend-only permission-system slice.
- Implemented minimal SCIM Users and Groups list/get/create/patch behavior.
- Did not implement SCIM bulk operations, complex filter grammar, enterprise extension, hard delete, delete/deactivate, frontend code, full OIDC/SAML provider integration, MFA provider/enrollment/step-up enforcement, or secure invite outbox/retry delivery.

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
- `docs/agent/reports/scim-bearer-token-validation-implementation.md`

## Code Areas Inspected

- `services/api/src/Northstar.Api/Controllers/ScimController.cs`
- `services/api/src/Northstar.Application/Security/ScimService.cs`
- `services/api/src/Northstar.Application/Security/IScimService.cs`
- `services/api/src/Northstar.Application/Workspaces/IamSyncService.cs`
- `services/api/src/Northstar.Infrastructure/Security/EfScimTokenRepository.cs`
- `services/api/src/Northstar.Infrastructure/Workspaces/EfIamSyncRepository.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/NorthstarDbContext.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations/*`
- `services/api/src/Northstar.Domain/Users/User.cs`
- `services/api/src/Northstar.Domain/Security/WorkspaceGroup.cs`
- `services/api/src/Northstar.Domain/Security/WorkspaceGroupMember.cs`
- `services/api/src/Northstar.Domain/Workspaces/WorkspaceMember.cs`
- `services/api/tests/Northstar.Api.Tests/KnowledgeApiTests.cs`

## SCIM User Behavior Implemented

- Added `GET /api/v1/workspaces/{workspaceId}/scim/v2/Users`.
- Added `GET /api/v1/workspaces/{workspaceId}/scim/v2/Users/{userId}`.
- Added `POST /api/v1/workspaces/{workspaceId}/scim/v2/Users`.
- Added `PATCH /api/v1/workspaces/{workspaceId}/scim/v2/Users/{userId}`.
- User create maps `externalId` to `users.external_subject_id` with `external_provider = scim`.
- Existing local email users can be bound only when they have no conflicting external identity.
- SCIM-created workspace membership is viewer-only.
- User patch supports narrow profile updates and does not create passwords or `UserCredential` rows.

## SCIM Group Behavior Implemented

- Added `GET /api/v1/workspaces/{workspaceId}/scim/v2/Groups`.
- Added `GET /api/v1/workspaces/{workspaceId}/scim/v2/Groups/{groupId}`.
- Added `POST /api/v1/workspaces/{workspaceId}/scim/v2/Groups`.
- Added `PATCH /api/v1/workspaces/{workspaceId}/scim/v2/Groups/{groupId}`.
- Group create/patch syncs SCIM-managed group display name and member set.
- Group members reference SCIM-managed user ids in the same workspace.
- Local non-SCIM-managed groups are rejected by SCIM mutation APIs.
- Group delete/deactivate remains unsupported.

## Authentication And Authorization

- SCIM provisioning requires a valid dedicated SCIM bearer token for the route workspace.
- Owner/admin JWT fallback remains preserved for SCIM discovery only.
- Cross-workspace, revoked, expired, or unknown SCIM bearer tokens use the existing generic unauthorized boundary.

## Identity Mapping Rules

- SCIM provider key is `scim`.
- Users reuse `users.external_provider` and `users.external_subject_id`.
- Groups reuse `workspace_groups.external_provider` and `workspace_groups.external_group_id`.
- SCIM does not create owner/admin workspace membership.
- SCIM does not create local password credentials or store SCIM secrets.

## Data Model / Migration Changes

- No migration was added.
- Existing user, workspace member, workspace group, and workspace group member tables were sufficient.
- Existing filtered unique indexes for user external identity and workspace group external identity are reused.

## API / Contract Changes

- Added SCIM User/Group contract DTOs in `Northstar.Contracts.Security`.
- Added SCIM PATCH DTOs for narrow `add`/`replace` operations.
- SCIM service provider config now advertises patch/filter support for the implemented V1 subset.
- Existing unsupported PUT and DELETE endpoints remain explicit unsupported responses after SCIM bearer authentication.

## Application / Domain / Infrastructure Changes

- API: `ScimController` now exposes Users/Groups list/get/create/patch routes.
- Contracts: added SCIM resource, create, group member, meta, and patch DTOs.
- Application: `ScimService` now orchestrates provisioning, token authentication, validation, audit writes, and SCIM response shaping.
- Infrastructure: added EF-backed `IScimProvisioningRepository` implementation and DI registration.
- Domain: reused existing user, workspace membership, workspace group, and group member invariants.

## Tests Added Or Updated

- Updated SCIM discovery expectations for patch/filter support.
- Added provisioning authentication coverage requiring dedicated SCIM bearer token.
- Added SCIM user create/list/get/patch coverage.
- Added existing-local-email bind and conflicting-external-identity rejection coverage.
- Added viewer-only workspace membership and no-credential coverage for SCIM users.
- Added SCIM group create/list/get/patch member-sync coverage.
- Added local non-SCIM group mutation rejection coverage.
- Added unsupported filter/delete coverage.
- Added audit raw-token non-exposure checks for SCIM provisioning.

## Validation Run

- `dotnet restore services/api/Northstar.sln`: passed.
- `dotnet build services/api/Northstar.sln`: passed with 0 warnings and 0 errors.
- `dotnet test services/api/Northstar.sln`: passed with 225 tests total.

## Not Run

- PostgreSQL smoke: not run by this agent because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not visible to this process.
- User previously reported PostgreSQL smoke passed manually.
- frontend build/test: not run; frontend code was not changed.
- object storage integration: not run; not in scope.

## Security Notes

- Raw SCIM bearer tokens and token hashes are not returned by provisioning responses or written to audit metadata.
- SCIM provisioning does not widen public-link, bootstrap, map, search, export, context, activity, comments, attachments, files, versions, mutations, or permission APIs.
- SCIM user create does not create password credentials.
- SCIM group provisioning cannot mutate local non-SCIM-managed groups.
- SCIM delete/deactivate and hard-delete behavior remain unsupported.

## Remaining Deferred Permission Work

- frontend permission mutation workflows
- full MFA provider/enrollment/step-up enforcement
- full OIDC/SAML provider redirect/callback and secret management
- secure invite outbox/retry delivery
- SCIM bulk operations, complex filters, enterprise extension, delete/deactivate, and broader compatibility hardening

## Smallest Safe Next Step

harden SCIM provisioning compatibility
