# SCIM Bearer Token Validation Implementation

## Summary

- Implemented dedicated workspace-scoped SCIM bearer-token validation for the existing SCIM skeleton.
- Added hash-only SCIM token persistence and owner/admin token management APIs.
- Public-link runtime behavior was not broadened, and full SCIM provisioning remains deferred.

## Scope

- Backend-only permission-system slice.
- Implemented dedicated SCIM bearer-token validation and token lifecycle management.
- Did not implement full SCIM User/Group provisioning, MFA provider/enrollment/step-up enforcement, full OIDC/SAML provider integration, frontend code, or new invite delivery providers.

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
- `docs/agent/reports/production-invite-delivery-provider-implementation.md`

## Code Areas Inspected

- `services/api/src/Northstar.Api/Controllers/ScimController.cs`
- `services/api/src/Northstar.Api/Program.cs`
- `services/api/src/Northstar.Api/Security/HttpCurrentUser.cs`
- `services/api/src/Northstar.Application/Security/ScimService.cs`
- `services/api/src/Northstar.Application/Security/IWorkspaceAccessService.cs`
- `services/api/src/Northstar.Application/Security/IShareLinkTokenService.cs`
- `services/api/src/Northstar.Application/Security/ShareLinkTokenService.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/NorthstarDbContext.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations/*`
- `services/api/tests/Northstar.Api.Tests/KnowledgeApiTests.cs`

## SCIM Bearer Token Behavior Implemented

- SCIM endpoints under `/api/v1/workspaces/{workspaceId}/scim/v2` now accept valid dedicated SCIM bearer tokens for the matching workspace.
- Owner/admin JWT access is preserved for SCIM discovery and unsupported provisioning responses.
- Unknown, expired, revoked, and workspace-mismatched SCIM bearer tokens return the same unauthorized boundary.
- SCIM User/Group provisioning endpoints still return explicit not-implemented validation responses after authentication.

## Token Lifecycle And Storage

- Added `ScimToken` domain entity and `scim_tokens` table.
- Raw SCIM token is generated only at create time and returned once.
- Only `token_hash` is persisted.
- Tokens support optional expiry, soft revoke, and `last_used_at`.
- Token list responses do not expose raw tokens or token hashes.

## Authentication And Authorization

- Added a thin HTTP bearer accessor in API and kept SCIM auth orchestration in Application.
- Token management APIs require authenticated owner/admin workspace manage permission.
- Dedicated SCIM bearer-token access does not create users, groups, workspace memberships, or provisioning state.

## Data Model / Migration Changes

- Added EF configuration for `scim_tokens`.
- Added migration `20260502201249_AddScimTokensPhase14`.
- Updated `NorthstarDbContextModelSnapshot`.
- Added unique index on `token_hash` and workspace active-token index.

## API / Contract Changes

- Added:
  - `POST /api/v1/workspaces/{workspaceId}/scim/tokens`
  - `GET /api/v1/workspaces/{workspaceId}/scim/tokens`
  - `DELETE /api/v1/workspaces/{workspaceId}/scim/tokens/{tokenId}`
- Added SCIM token DTOs in `Northstar.Contracts.Security`.
- Existing SCIM discovery/provisioning routes remain under `/api/v1/workspaces/{workspaceId}/scim/v2`.

## Application / Domain / Infrastructure Changes

- Domain: `ScimToken` lifecycle invariants.
- Application: token create/list/revoke service, SCIM bearer validation, repository/accessor interfaces.
- Infrastructure: EF repository, configuration, migration, DI registration.
- API: token management controller and bearer-token accessor registration.

## Tests Added Or Updated

- SCIM discovery accepts valid dedicated bearer token and does not create workspace membership.
- Cross-workspace, revoked, and expired SCIM tokens are rejected without distinct failure details.
- Unsupported SCIM User/Group provisioning behavior is preserved with dedicated bearer token auth.
- Token management requires owner/admin permission.
- Token creation returns raw token once and persists only hash.
- Token list/audit output does not expose raw token or token hash.
- Revoked token is rejected.
- Migration coverage asserts `scim_tokens` hash-only schema and indexes.

## Validation Run

- `dotnet restore services/api/Northstar.sln`: passed.
- `dotnet build services/api/Northstar.sln`: passed with 0 warnings and 0 errors.
- `dotnet test services/api/Northstar.sln`: passed with 223 tests total.

## Not Run

- PostgreSQL smoke: not run by this agent because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not visible to this process.
- User previously reported PostgreSQL smoke passed manually.
- frontend build/test: not run; frontend code was not changed.
- object storage integration: not run; not in scope.

## Security Notes

- Raw SCIM tokens and token hashes are not written to token list responses, storage output, or audit metadata.
- SCIM token failures use a generic unauthorized boundary.
- SCIM bearer tokens are workspace-scoped and do not grant access outside SCIM endpoints.
- Public-link runtime behavior and anonymous access boundaries were not changed.

## Remaining Deferred Permission Work

- frontend permission mutation workflows
- full SCIM provisioning behavior
- full MFA provider/enrollment/step-up enforcement
- full OIDC/SAML provider redirect/callback and secret management
- secure invite outbox/retry delivery

## Smallest Safe Next Step

implement full SCIM provisioning behavior
