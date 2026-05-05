# Production Invite Delivery Provider Implementation

## Summary

- Implemented a configuration-driven SMTP invite delivery provider boundary.
- Preserved disabled/noop invite delivery as the default behavior.
- Public-link runtime behavior was not broadened.

## Scope

- Backend-only permission-system slice.
- Implemented production invite delivery provider boundary for email invites.
- Did not implement full SCIM provisioning, dedicated SCIM bearer-token validation, MFA provider/enrollment/step-up enforcement, full OIDC/SAML provider integration, frontend code, background outbox, or retry delivery.

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
- `docs/agent/reports/real-idp-login-boundary-implementation.md`

## Code Areas Inspected

- `services/api/src/Northstar.Application/Security/EmailInviteDeliveryOptions.cs`
- `services/api/src/Northstar.Application/Security/IEmailInviteDeliveryService.cs`
- `services/api/src/Northstar.Application/Security/EmailInviteService.cs`
- `services/api/src/Northstar.Domain/Security/ResourceEmailInvite.cs`
- `services/api/src/Northstar.Domain/Security/EmailInviteDeliveryStatuses.cs`
- `services/api/src/Northstar.Infrastructure/Security/NoopEmailInviteDeliveryService.cs`
- `services/api/src/Northstar.Infrastructure/DependencyInjection.cs`
- `services/api/src/Northstar.Api/Controllers/PermissionsController.cs`
- `services/api/src/Northstar.Api/appsettings.json`
- `services/api/src/Northstar.Api/appsettings.Development.json`
- `services/api/tests/Northstar.Api.Tests/KnowledgeApiTests.cs`

## Invite Delivery Provider Implemented

- Added `SmtpEmailInviteDeliveryService`.
- Delivery provider selection is driven by `Permissions:EmailInvites:Delivery:Provider`.
- `Provider = smtp` selects the SMTP provider.
- `Provider = noop` keeps the existing noop provider behavior.
- Unsupported providers fail closed with `deliveryStatus = failed` and `deliveryErrorCode = unsupported_provider` unless tests or future provider registrations explicitly replace `IEmailInviteDeliveryService`.
- Disabled delivery still returns `deliveryStatus = disabled`.
- SMTP configuration errors return `deliveryStatus = failed` with `deliveryErrorCode = configuration_error`.
- SMTP send failures return `deliveryStatus = failed` with `deliveryErrorCode = provider_error`.

## Configuration

- Existing section remains `Permissions:EmailInvites:Delivery`.
- Added supported fields:
  - `FromEmail`
  - `FromName`
  - `Smtp:Host`
  - `Smtp:Port`
  - `Smtp:UseSsl`
  - `Smtp:Username`
  - `Smtp:Password`
  - `Smtp:TimeoutSeconds`
- No secrets were added to appsettings.

## Data Model / Migration Changes

- Reused existing `resource_email_invites` delivery fields.
- No table or column was added.
- No EF migration was added.

## API / Contract Changes

- No API response shape changed.
- Existing invite create response continues to return one-time raw invite token and URL only in the create response.

## Application / Domain / Infrastructure Changes

- Application: extended `EmailInviteDeliveryOptions` with SMTP provider configuration.
- Domain: no entity behavior change was required.
- Infrastructure: added SMTP provider implementation and configuration-driven DI selection.
- API: no controller change was required.

## Tests Added Or Updated

- Added coverage that default delivery remains disabled/noop.
- Added coverage that SMTP provider selection is configuration-driven.
- Added coverage that unsupported configured providers fail closed.
- Added coverage that incomplete SMTP config fails closed with `configuration_error`.
- Added checks that DB, audit, and notification metadata do not contain raw invite tokens, token hashes, accept URLs, SMTP passwords, or password-like values.
- Existing fake-provider success and failure tests continue to cover in-memory accept URL delivery and non-secret persisted failure state.

## Validation Run

- `dotnet restore services/api/Northstar.sln`: passed.
- `dotnet build services/api/Northstar.sln`: passed with 0 warnings and 0 errors.
- `dotnet test services/api/Northstar.sln`: passed with 217 tests total.

## Not Run

- PostgreSQL smoke: not run by this agent because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not visible to this process.
- User previously reported PostgreSQL smoke passed manually.
- frontend build/test: not run; frontend code was not changed.
- object storage integration: not run; not in scope.

## Security Notes

- The SMTP provider receives the accept URL in memory only for sending.
- Raw invite tokens, token hashes, accept URLs, SMTP passwords, and provider secrets are not persisted in DB, audit metadata, or notification metadata.
- Public-link runtime behavior and anonymous access boundaries were not changed.
- No background retry/outbox behavior was added.

## Remaining Deferred Permission Work

- frontend permission mutation workflows
- full SCIM provisioning behavior
- dedicated SCIM bearer-token validation
- full MFA provider/enrollment/step-up enforcement
- full OIDC/SAML provider redirect/callback and secret management
- secure invite outbox/retry delivery

## Smallest Safe Next Step

implement dedicated SCIM bearer-token validation
