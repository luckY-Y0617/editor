# Secure Invite Outbox Retry Implementation

## Summary

- Implemented secret-safe email invite delivery outbox and retry state.
- Invite creation now enqueues a delivery outbox item when configured delivery is enabled and performs the first delivery attempt with the accept URL in memory.
- Added an application processor for due retry items. It can retry only when the caller supplies the one-time accept URL in memory; missing accept URLs fail closed.
- Public-link runtime behavior was not broadened.

## Scope

- Backend permission-system slice only.
- No frontend changes.
- No public-link behavior changes.
- No SCIM, OIDC/SAML, or MFA provider expansion.

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
- `docs/agent/reports/scim-provisioning-compatibility-hardening-v1-1.md`

## Code Areas Inspected

- `services/api/src/Northstar.Domain/Security`
- `services/api/src/Northstar.Application/Security`
- `services/api/src/Northstar.Infrastructure/Security`
- `services/api/src/Northstar.Infrastructure/Persistence`
- `services/api/tests/Northstar.Api.Tests/KnowledgeApiTests.cs`

## Outbox Behavior Implemented

- Added `EmailInviteDeliveryOutboxItem` domain state and `EmailInviteDeliveryOutboxStatuses`.
- Added EF mapping and migration for `email_invite_delivery_outbox`.
- Added `IEmailInviteDeliveryOutboxRepository` and EF repository.
- Updated invite creation to enqueue an outbox item for enabled delivery providers.
- Disabled/noop invite delivery remains the default and does not enqueue retry state.

## Retry Behavior Implemented

- Added `IEmailInviteDeliveryOutboxProcessor`.
- Processor sends due items through the configured invite delivery provider.
- Success marks the outbox item sent.
- Transient failure increments attempts and schedules retry until `max_attempts`.
- Terminal failure marks the outbox item failed/dead-lettered.
- Missing in-memory accept URL fails closed with `missing_accept_url` rather than persisting or reconstructing secrets.

## Secret-Safety Rules

- Outbox rows store retry metadata only.
- Outbox rows do not store raw invite tokens, token hashes, raw accept URLs, SMTP passwords, provider secrets, or password-like values.
- Provider receives the accept URL only in memory.
- Notification batch writes now de-dupe by existing `dedupe_key` before insert, preventing retry duplicate notifications from hitting the PostgreSQL unique constraint.

## Data Model / Migration Changes

- Added `email_invite_delivery_outbox` table.
- Added status and attempt check constraints.
- Added due, invite, and failed-status indexes.
- No raw token, token hash, accept URL, SMTP, password, or provider secret columns were added.

## API / Contract Changes

- No API routes changed.
- `docs/PERMISSION_SYSTEM_CONTRACT.md` now records secure invite outbox/retry delivery as implemented and keeps deferred work explicit.

## Application / Domain / Infrastructure Changes

- Domain owns outbox lifecycle transitions and invariants.
- Application owns enqueue/retry orchestration.
- Infrastructure owns EF persistence and due-item queries.
- Existing invite delivery provider boundary is reused.
- No EF entities are exposed through API responses.

## Tests Added Or Updated

- Invite create enqueues secret-safe outbox state.
- Retry due item sends through provider and marks sent.
- Terminal failure after max attempts marks failed and preserves delivery failure fan-out.
- Migration test verifies table, indexes, constraints, and forbidden secret columns.
- Notification repository batch insert now de-dupes persisted dedupe keys.

## Validation Run

- `dotnet restore services/api/Northstar.sln`: passed.
- `dotnet build services/api/Northstar.sln`: passed.
- `dotnet test services/api/Northstar.sln`: passed.

## Not Run

- PostgreSQL smoke: not run by agent; `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was not visible to this process.
- User previously reported PostgreSQL smoke passed manually.
- frontend build/test: not run; frontend code was not changed.
- object storage integration: not run; not in scope.

## Security Notes

- The outbox intentionally does not persist raw accept URLs, so autonomous durable retry cannot reconstruct an invite accept URL later.
- The current processor requires a trusted caller to provide the accept URL in memory for retry.
- This preserves hash-only invite token storage and avoids weakening token rules.

## Remaining Deferred Permission Work

- frontend permission mutation workflows
- full MFA provider/enrollment/step-up enforcement
- full OIDC/SAML provider redirect/callback and secret management
- SCIM bulk operations
- SCIM complex filter grammar
- SCIM enterprise extension
- SCIM delete/deactivate behavior

## Smallest Safe Next Step

- add frontend permission mutation workflow
