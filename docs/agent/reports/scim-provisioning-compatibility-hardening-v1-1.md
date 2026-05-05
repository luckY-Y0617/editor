# SCIM Provisioning Compatibility Hardening V1.1

## Summary

- Hardened SCIM Provisioning V1 for basic client compatibility.
- Added restricted PUT replace support for SCIM-managed Users and Groups.
- Strengthened tests for narrow filters, bounded pagination, local-group mutation rejection, and token-secret non-exposure.
- Public-link runtime behavior was not broadened.

## Scope

- Backend-only SCIM compatibility hardening.
- No frontend code changed.
- No migrations added.
- No SCIM bulk operations, complex filter grammar, enterprise extension, or delete/deactivate behavior implemented.

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
- `docs/agent/reports/scim-provisioning-v1-implementation.md`

## Code Areas Inspected

- `services/api/src/Northstar.Api/Controllers/ScimController.cs`
- `services/api/src/Northstar.Application/Security/IScimService.cs`
- `services/api/src/Northstar.Application/Security/ScimService.cs`
- `services/api/src/Northstar.Application/Security/IScimProvisioningRepository.cs`
- `services/api/src/Northstar.Infrastructure/Security/EfScimProvisioningRepository.cs`
- `services/api/src/Northstar.Domain/Users/User.cs`
- `services/api/src/Northstar.Domain/Security/WorkspaceGroup.cs`
- `services/api/src/Northstar.Domain/Security/WorkspaceGroupMember.cs`
- `services/api/tests/Northstar.Api.Tests/KnowledgeApiTests.cs`

## Compatibility Behavior Implemented

- SCIM User and Group PUT routes now call application-layer restricted replace methods.
- Existing SCIM create/patch invariants are reused for token auth, identity preservation, no password/credential creation, and local group mutation rejection.
- Stable API validation errors remain the current SCIM error boundary; global non-SCIM error middleware was not changed.

## PUT Replace Behavior

- `PUT /api/v1/workspaces/{workspaceId}/scim/v2/Users/{userId}` updates SCIM-managed user profile fields when the request preserves the existing `externalId`.
- User PUT rejects `externalId` changes and `active = false`.
- `PUT /api/v1/workspaces/{workspaceId}/scim/v2/Groups/{groupId}` updates SCIM-managed group display name and syncs members when a member list is supplied.
- Group PUT rejects `externalId` changes and rejects local non-SCIM-managed groups.
- DELETE/deactivate remains unsupported for Users and Groups.

## Filter And Pagination Behavior

- Users support no filter, `userName eq`, and `externalId eq`.
- Groups support no filter, `displayName eq`, and `externalId eq`.
- Unsupported filters return `400 VALIDATION_ERROR` with the stable message `Unsupported SCIM filter.`
- `startIndex` is normalized to at least `1`.
- `count` is clamped by the existing SCIM page-size maximum.

## Discovery / Schema Accuracy

- `ServiceProviderConfig` continues to advertise PATCH and filter support, and continues to reject bulk, sort, password change, and etag support.
- Discovery behavior was not expanded beyond the existing owner/admin or valid SCIM bearer-token boundary.

## Authentication And Authorization

- Provisioning mutations require valid dedicated workspace-scoped SCIM bearer-token authentication.
- Owner/admin JWT fallback remains discovery-only.
- Cross-workspace, expired, revoked, or unknown SCIM tokens remain rejected through the generic SCIM unauthorized boundary.

## Data Model / Migration Changes

- No migration was added.
- Existing `users`, `workspace_members`, `workspace_groups`, and `workspace_group_members` tables remain sufficient.
- No SCIM shadow tables, provider secret tables, or delete/deactivate fields were added.

## API / Contract Changes

- Added application support for:
  - `PUT /api/v1/workspaces/{workspaceId}/scim/v2/Users/{userId}`
  - `PUT /api/v1/workspaces/{workspaceId}/scim/v2/Groups/{groupId}`
- Updated permission governance docs to record V1.1 behavior and remaining deferred SCIM work.
- No public-link APIs changed.

## Application / Domain / Infrastructure Changes

- API: `ScimController` now forwards PUT Users/Groups to `IScimService`.
- Application: `ScimService` implements restricted replace orchestration and audit writes.
- Domain: reused existing user and workspace group invariants.
- Infrastructure: no repository or EF schema changes were needed.

## Tests Added Or Updated

- Added SCIM User PUT replace coverage.
- Added SCIM Group PUT replace coverage.
- Added narrow User/Group filter coverage.
- Added pagination normalization coverage.
- Added stable unsupported-filter validation coverage.
- Added local non-SCIM-managed group PUT rejection coverage.
- Added raw SCIM token non-exposure checks for PUT responses and audit metadata.

## Validation Run

- `dotnet restore services/api/Northstar.sln`: passed.
- `dotnet build services/api/Northstar.sln`: passed with 0 warnings and 0 errors.
- `dotnet test services/api/Northstar.sln`: passed with 228 tests total.

## Not Run

- PostgreSQL smoke: not run by this agent because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was not visible to this process.
- User previously reported PostgreSQL smoke passed manually.
- frontend build/test: not run; frontend code was not changed.
- object storage integration: not run; not in scope.

## Security Notes

- Raw SCIM bearer tokens and token hashes are not returned by provisioning responses or written to audit metadata.
- SCIM PUT replace does not create owner/admin workspace membership.
- SCIM PUT replace does not create local password credentials.
- SCIM PUT replace cannot mutate local non-SCIM-managed groups.
- Public-link behavior and protected API boundaries were not changed.

## Remaining Deferred Permission Work

- frontend permission mutation workflows
- full MFA provider/enrollment/step-up enforcement
- full OIDC/SAML provider redirect/callback and secret management
- secure invite outbox/retry delivery
- SCIM bulk operations
- SCIM complex filter grammar
- SCIM enterprise extension
- SCIM delete/deactivate behavior
- broader SCIM compatibility beyond V1.1

## Smallest Safe Next Step

implement secure invite outbox/retry delivery
