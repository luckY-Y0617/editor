# MFA Provider Step-Up Enforcement V1

## Summary

- Implemented backend-backed TOTP MFA enrollment, verification, disable, and step-up enforcement.
- Added durable `user_mfa_methods` persistence with protected secret ciphertext, not plaintext TOTP secrets.
- Enforced backend step-up for selected high-risk permission mutations when the acting user has MFA enabled.
- Public-link runtime behavior was not broadened.

## Scope

- Backend MFA/recent-auth and high-risk permission enforcement only.
- No frontend code, public-link behavior, SCIM provisioning compatibility, OIDC/SAML provider integration, WebAuthn/passkeys, SMS/email MFA, or recovery-code flows were added.

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
- `docs/agent/skills/data-model-migrations.md`
- `docs/PERMISSION_SYSTEM_CONTRACT.md`
- `docs/agent/reports/permission-system-e2e-acceptance-v1.md`

## Code Areas Inspected

- `services/api/src/Northstar.Api/Controllers/AuthController.cs`
- `services/api/src/Northstar.Application/Security/AuthSecurityStateService.cs`
- `services/api/src/Northstar.Application/Security/AuthService.cs`
- `services/api/src/Northstar.Application/Security/IAuthRepository.cs`
- `services/api/src/Northstar.Application/Security/ResourcePermissionManagementService.cs`
- `services/api/src/Northstar.Application/Security/ShareLinkService.cs`
- `services/api/src/Northstar.Application/Security/EmailInviteService.cs`
- `services/api/src/Northstar.Application/Security/ScimTokenService.cs`
- `services/api/src/Northstar.Application/Workspaces/WorkspaceMembersService.cs`
- `services/api/src/Northstar.Infrastructure/Security/EfAuthRepository.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/NorthstarDbContext.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations/*`
- `services/api/tests/Northstar.Api.Tests/KnowledgeApiTests.cs`

## MFA Enrollment Behavior

- `POST /api/v1/auth/mfa/totp/enroll` creates a pending TOTP method for the authenticated user.
- Enrollment returns the TOTP secret and provisioning URI only in the enrollment response.
- Pending enrollment does not count as MFA enabled.
- Existing enabled TOTP enrollment returns conflict; a previous pending enrollment is disabled before a new pending method is created.

## MFA Verification / Step-Up Behavior

- `POST /api/v1/auth/mfa/totp/verify` verifies a server-side TOTP code.
- Successful verification marks the method enabled, records last-used state, and writes a secret-safe `auth.mfa_verified` auth event.
- `GET /api/v1/auth/security-state` now reports MFA enabled, MFA verified, MFA verification timestamp, step-up requirement, and `totp` as the supported method when enabled.
- Step-up uses backend auth events and a configurable short window.

## MFA Disable Behavior

- `POST /api/v1/auth/mfa/totp/disable` requires backend step-up when MFA is enabled.
- Disable marks the active method disabled and writes `auth.mfa_disabled`.
- No plaintext secret is returned.

## High-Risk Enforcement Behavior

Step-up is enforced when the current user has enabled MFA for:

- resource grant create/update/revoke;
- resource policy update;
- share-link create/revoke;
- email invite create/revoke;
- workspace member add/update/remove;
- SCIM token create/revoke.

Users without enabled MFA continue through existing authorization behavior.

## API / Contract Changes

- Added `TotpEnrollmentResponse`.
- Added `VerifyTotpRequest`.
- Added authenticated routes:
  - `POST /api/v1/auth/mfa/totp/enroll`
  - `POST /api/v1/auth/mfa/totp/verify`
  - `POST /api/v1/auth/mfa/totp/disable`
- Extended `AuthSecurityStateResponse` behavior without changing its shape.

## Data Model / Migration Changes

- Added `UserMfaMethod` domain entity.
- Added `user_mfa_methods` table through migration `20260503232004_AddUserMfaMethodsPhase16`.
- Table fields include `user_id`, `method_type`, `secret_ciphertext`, `status`, `created_at`, `verified_at`, `disabled_at`, and `last_used_at`.
- Migration includes check constraints for TOTP method type and MFA method status.
- Migration includes a filtered unique index for active `(user_id, method_type)` rows.
- No plaintext TOTP secret or TOTP code columns were added.

## Application / Domain / Infrastructure Changes

- Domain owns MFA method lifecycle state and invariants.
- Application owns TOTP orchestration, security-state calculation, and high-risk step-up enforcement.
- Infrastructure owns EF persistence and AES-GCM secret protection.
- Controllers remain thin and delegate to Application services.
- Application/Domain do not depend on EF or ASP.NET controller types.

## Frontend Handling

- No frontend code changed.
- Existing frontend mutation surfaces will receive the existing stable API error response when step-up is required.

## Tests Added Or Updated

- Added API tests for TOTP enrollment, pending state, invalid code rejection, valid verification, security-state updates, protected secret storage, secret-safe auth events, disable requiring step-up, and high-risk mutation rejection before step-up.
- Existing permission tests still pass.

## Validation Run

- `dotnet restore services/api/Northstar.sln`: passed.
- `dotnet build services/api/Northstar.sln`: passed with 0 warnings and 0 errors.
- `dotnet test services/api/Northstar.sln`: passed.
  - `Northstar.Domain.Tests`: 43 passed.
  - `Northstar.Application.Tests`: 40 passed.
  - `Northstar.Api.Tests`: 152 passed.

## PostgreSQL Smoke

- Not run by agent.
- `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was checked in this agent process and was not visible.
- The connection string value was not printed.

## Browser QA

- Not run; no frontend code changed.

## Not Run

- frontend build/test: not run; frontend code was not changed.
- PostgreSQL smoke: not run because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was not visible to this agent process.
- Browser QA: not run; no frontend code changed.
- Object storage integration: not run; not in scope.

## Security Notes

- TOTP secrets are protected before persistence and are not returned after enrollment.
- TOTP codes are not stored in domain state, audit metadata, or auth event metadata.
- High-risk enforcement is backend-owned and does not rely on frontend state.
- Public-link, SCIM provisioning, OIDC/SAML, and invite delivery behavior were not broadened.

## Remaining Deferred Permission Work

- full OIDC/SAML provider redirect/callback and secret management;
- SCIM bulk operations;
- SCIM complex filter grammar;
- SCIM enterprise extension;
- SCIM delete/deactivate behavior;
- WebAuthn/passkeys, SMS/email MFA, and recovery codes;
- MFA recovery/reset flows.

## Smallest Safe Next Step

run release-readiness review
