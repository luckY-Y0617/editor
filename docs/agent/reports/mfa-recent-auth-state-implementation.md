# MFA Recent-Auth State Implementation

## Summary

- Implemented backend-derived recent-auth state for authenticated users.
- Added a narrow `GET /api/v1/auth/security-state` endpoint.
- MFA provider/enrollment and step-up enforcement remain deferred.

## Scope

- Backend-only permission-system slice.
- Implemented MFA/recent-auth backend state foundation only.
- Did not broaden public-link behavior, implement real IdP login, implement full SCIM provisioning, implement production invite delivery, edit frontend code, implement MFA provider enrollment, or enforce step-up on existing permission mutations.

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
- `docs/agent/reports/scim-endpoint-skeleton-implementation.md`

## Code Areas Inspected

- `services/api/src/Northstar.Api/Controllers/AuthController.cs`
- `services/api/src/Northstar.Api/Security/HttpCurrentUser.cs`
- `services/api/src/Northstar.Api/Security/HttpAuthRequestContext.cs`
- `services/api/src/Northstar.Application/Security/AuthService.cs`
- `services/api/src/Northstar.Application/Security/IAuthService.cs`
- `services/api/src/Northstar.Application/Security/IAuthRepository.cs`
- `services/api/src/Northstar.Application/Security/ICurrentUser.cs`
- `services/api/src/Northstar.Application/Security/IAuthRequestContext.cs`
- `services/api/src/Northstar.Application/Security/ITokenService.cs`
- `services/api/src/Northstar.Infrastructure/Security/EfAuthRepository.cs`
- `services/api/src/Northstar.Infrastructure/Security/TokenService.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations/AuthEventConfiguration.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations/UserCredentialConfiguration.cs`
- `services/api/tests/Northstar.Api.Tests/KnowledgeApiTests.cs`

## MFA / Recent-Auth Behavior Implemented

- Recent-auth state is read from successful backend `auth.login` and `auth.register` events.
- Refresh-token rotation writes `auth.refresh` but does not replace recent-auth state.
- Security state reports `hasRecentAuth` using a 15-minute backend window.
- Security state reports MFA as not enabled and not verified because no real backend MFA provider/enrollment exists.
- Security state reports whether high-risk actions would need step-up based on missing/stale recent-auth, without enforcing step-up on existing mutations.

## Authentication And Authorization

- `GET /api/v1/auth/security-state` requires authentication.
- The endpoint uses the existing JWT-authenticated current-user boundary.
- The service reloads the user from backend persistence before returning state.
- Client-supplied recent-auth headers or flags are ignored.

## Data Model / Migration Changes

- Reused existing `auth_events` for durable recent-auth state.
- No new table or column was added.
- No EF migration was added.

## API / Contract Changes

- Added `AuthSecurityStateResponse`.
- Added `GET /api/v1/auth/security-state`.
- No existing API response shape changed.

## Application / Domain / Infrastructure Changes

- API: `AuthController` delegates security-state reads to `IAuthSecurityStateService`.
- Contracts: added security-state DTO.
- Application: added `IAuthSecurityStateService` and implementation.
- Infrastructure: added `IAuthRepository.GetLatestSuccessfulAuthEventAsync` implementation.
- Domain: no change.

## Tests Added Or Updated

- Added unauthenticated `security-state` rejection coverage.
- Added authenticated security-state coverage for backend recent-auth and no MFA provider.
- Added coverage that client-supplied recent-auth headers do not drive returned state.
- Added coverage that refresh does not replace recent-auth.

## Validation Run

- `dotnet restore services/api/Northstar.sln`: passed.
- `dotnet build services/api/Northstar.sln`: passed with 0 warnings and 0 errors.
- `dotnet test services/api/Northstar.sln`: passed with 208 tests total.

## Not Run

- PostgreSQL smoke: not run by this agent because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not visible to this process.
- User previously reported PostgreSQL smoke passed manually.
- frontend build/test: not run; frontend code was not changed.
- object storage integration: not run; not in scope.

## Security Notes

- No raw tokens, token hashes, passwords, MFA secrets, recovery codes, OTP seeds, provider secrets, SAML/OIDC tokens, or accept URLs are exposed.
- No MFA provider or enrollment state is falsely reported as implemented.
- Recent-auth state is currently user-level from `auth_events`; session-bound step-up remains part of the deferred full enforcement work.
- No existing permission mutation behavior was weakened or broadened.
- Public-link runtime behavior and anonymous access boundaries were not changed.

## Remaining Deferred Permission Work

- production invite delivery provider
- frontend permission mutation workflows
- full SCIM provisioning behavior
- full OIDC/SAML provider redirect/callback and secret management
- full MFA provider/enrollment/step-up enforcement

## Smallest Safe Next Step

implement production invite delivery provider
