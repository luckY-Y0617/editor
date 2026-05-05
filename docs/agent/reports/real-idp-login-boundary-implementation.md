# Real IdP Login Boundary Implementation

## Summary

- Implemented a default-disabled backend IdP login boundary.
- Reused existing user external identity fields and normal token issuance.
- Full OIDC/SAML provider redirect/callback, provider validation, and secret management remain deferred.

## Scope

- Backend-only permission-system slice.
- Implemented `POST /api/v1/auth/idp/login` for trusted external identity assertions.
- Did not broaden public-link behavior, implement full SCIM provisioning, implement production invite delivery, edit frontend code, implement MFA provider/enrollment/step-up enforcement, or add OIDC/SAML provider integration.

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
- `docs/agent/reports/mfa-recent-auth-state-implementation.md`

## Code Areas Inspected

- `services/api/src/Northstar.Api/Controllers/AuthController.cs`
- `services/api/src/Northstar.Api/Program.cs`
- `services/api/src/Northstar.Application/Security/AuthService.cs`
- `services/api/src/Northstar.Application/Security/AuthSecurityStateService.cs`
- `services/api/src/Northstar.Application/Security/IAuthRepository.cs`
- `services/api/src/Northstar.Application/Workspaces/IamSyncService.cs`
- `services/api/src/Northstar.Application/Workspaces/IIamSyncRepository.cs`
- `services/api/src/Northstar.Domain/Users/User.cs`
- `services/api/src/Northstar.Domain/Users/AuthEvent.cs`
- `services/api/src/Northstar.Infrastructure/Security/EfAuthRepository.cs`
- `services/api/src/Northstar.Infrastructure/Security/AuthOptions.cs`
- `services/api/src/Northstar.Infrastructure/Workspaces/EfIamSyncRepository.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- `services/api/tests/Northstar.Api.Tests/KnowledgeApiTests.cs`

## IdP Login Boundary Implemented

- Added `POST /api/v1/auth/idp/login`.
- Added request DTO fields: `provider`, `externalSubjectId`, `email`, and `displayName`.
- Added a default-disabled configuration policy with explicit allowed providers.
- Normalized provider consistently with IAM sync using trim and lowercase.
- Existing users mapped by `external_provider` + `external_subject_id` can log in and receive normal Northstar tokens.
- Existing local users can be bound by normalized email only when they have no conflicting external identity.
- Missing users are rejected; no users or workspace memberships are created implicitly.
- `auth.idp_login` now contributes to recent-auth state.

## Authentication And Authorization

- The route is `[AllowAnonymous]` because it is a login boundary.
- The boundary is disabled unless `Auth:IdpLogin:Enabled = true`.
- Enabled providers must be explicitly listed under `Auth:IdpLogin:AllowedProviders`.
- The implementation does not accept or persist provider secrets, OIDC tokens, SAML assertions, or raw credentials.

## Data Model / Migration Changes

- Reused existing `users.external_provider` and `users.external_subject_id`.
- Reused existing `auth_events`.
- No schema change was required.
- No EF migration was added.

## API / Contract Changes

- Added `IdpLoginRequest`.
- Added `POST /api/v1/auth/idp/login`.
- Existing auth token response shape is reused.

## Application / Domain / Infrastructure Changes

- API: `AuthController` delegates IdP login to `IAuthService`.
- Contracts: added IdP login request DTO.
- Application: added IdP login orchestration and policy interface.
- Domain: reused `User.ApplyExternalProfile` identity mapping invariant.
- Infrastructure: added external-user lookup and configured IdP login policy.

## Tests Added Or Updated

- Disabled IdP boundary returns `403 FORBIDDEN`.
- Existing local email user can be bound and receives tokens.
- IdP login does not create workspace membership implicitly.
- Existing external identity login returns tokens.
- Conflicting external identity returns `409 CONFLICT`.
- Unlinked external identity returns `401 UNAUTHORIZED` and does not create a user.
- Auth event metadata omits external subject ids, emails, tokens, and passwords.
- IdP login updates backend recent-auth state without reporting MFA as implemented.

## Validation Run

- `dotnet restore services/api/Northstar.sln`: passed.
- `dotnet build services/api/Northstar.sln`: passed with 0 warnings and 0 errors.
- `dotnet test services/api/Northstar.sln`: passed with 214 tests total.

## Not Run

- PostgreSQL smoke: not run by this agent because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not visible to this process.
- User previously reported PostgreSQL smoke passed manually.
- frontend build/test: not run; frontend code was not changed.
- object storage integration: not run; not in scope.

## Security Notes

- Public-link runtime behavior and anonymous access boundaries were not changed.
- Generic policy patch still must not accept direct `linkMode = public`.
- IdP login is not full SSO provider integration; it trusts only the configured backend boundary.
- Auth event metadata stores provider and optional failure reason only.
- No provider secrets, SAML/OIDC tokens, external assertions, raw credentials, token hashes, passwords, or callback URLs are persisted.

## Remaining Deferred Permission Work

- frontend permission mutation workflows
- full SCIM provisioning behavior
- dedicated SCIM bearer-token validation
- secure invite outbox/retry delivery
- full MFA provider/enrollment/step-up enforcement
- full OIDC/SAML provider redirect/callback and secret management

## Smallest Safe Next Step

implement dedicated SCIM bearer-token validation
