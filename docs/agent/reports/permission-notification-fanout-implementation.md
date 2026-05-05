# Permission Notification Fan-Out Implementation

## Summary

- Implemented share-link and email-invite in-app notification fan-out.
- Public-link runtime behavior was not broadened.
- Notification rows do not store raw tokens, token hashes, accept URLs, passwords, password hashes, or password proofs.
- Muted notification preferences suppress matching share-link/invite fan-out.

## Scope

- Backend-only permission-system slice.
- Implemented share-link and invite notification fan-out only.
- Did not implement group grant fan-out in that slice; it was implemented later in `docs/agent/reports/group-grant-fanout-implementation.md`.
- SCIM, MFA/recent-auth, real IdP login, production invite delivery, and frontend workflows remained out of scope.

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
- `docs/agent/reports/public-link-approval-implementation.md`

## Code Areas Inspected

- `services/api/src/Northstar.Application/Security/ShareLinkService.cs`
- `services/api/src/Northstar.Application/Security/EmailInviteService.cs`
- `services/api/src/Northstar.Application/Security/AccessRequestService.cs`
- `services/api/src/Northstar.Application/Security/PermissionNotificationService.cs`
- `services/api/src/Northstar.Application/Security/PermissionNotificationPreferenceService.cs`
- `services/api/src/Northstar.Domain/Security/PermissionNotification.cs`
- `services/api/src/Northstar.Domain/Security/PermissionNotificationTypes.cs`
- `services/api/src/Northstar.Domain/Security/ShareLink.cs`
- `services/api/src/Northstar.Domain/Security/ResourceEmailInvite.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations/PermissionNotificationConfiguration.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Migrations`
- `services/api/tests/Northstar.Api.Tests/KnowledgeApiTests.cs`

## Fan-Out Behavior Implemented

- `share_link.created`: notifies resource managers and direct user share/manage grant subjects, excluding the actor.
- `share_link.revoked`: notifies resource managers and direct user share/manage grant subjects, excluding the actor.
- `email_invite.created`: notifies resource managers and direct user share/manage grant subjects, excluding the actor.
- `email_invite.accepted`: notifies resource managers, direct user share/manage grant subjects, and inviter when known, excluding the actor.
- `email_invite.revoked`: notifies resource managers, direct user share/manage grant subjects, inviter when known, and accepted invitee when known, excluding the actor.
- `email_invite.delivery_failed`: emits when the existing invite delivery path returns `failed`.
- No anonymous public users are notified.

## Notification Preference Behavior

- Matching resource-level muted preferences suppress fan-out for that recipient/resource.
- Workspace-level muted preferences suppress fan-out when no resource-level preference overrides them.
- Watched/muted persistence was reused; no new preference API shape was added.

## Data Model / Migration Changes

- Reused `permission_notifications`.
- Added notification type constants and EF check-constraint values for:
  - `share_link.created`
  - `share_link.revoked`
  - `email_invite.created`
  - `email_invite.accepted`
  - `email_invite.revoked`
  - `email_invite.delivery_failed`
- Added migration `20260502092712_AddPermissionNotificationFanoutTypesPhase13`.

## API / Contract Changes

- No public API response shape changed.
- `docs/PERMISSION_SYSTEM_CONTRACT.md` was updated to document the implemented fan-out behavior.

## Application / Domain / Infrastructure Changes

- Added `IPermissionNotificationFanoutService` and `PermissionNotificationFanoutService`.
- Wired fan-out into share-link create/revoke and email-invite create/accept/revoke/delivery-failure flows within existing transactions.
- Registered the fan-out service in Application DI.
- Domain owns new notification type constants.
- Infrastructure owns the EF type check-constraint update and migration.

## Tests Added Or Updated

- Share-link create/revoke manager notification fan-out.
- Actor exclusion for share-link notification fan-out.
- Muted preference suppression for share-link revocation notification.
- Email invite create/accept/revoke notification fan-out.
- Delivery failure notification fan-out.
- Secret-free notification content checks for invite token, token hash, and accept URL.
- Migration test for the new notification type constraint values.

## Validation Run

- `dotnet ef migrations add AddPermissionNotificationFanoutTypesPhase13 --project services/api/src/Northstar.Infrastructure/Northstar.Infrastructure.csproj --startup-project services/api/src/Northstar.Api/Northstar.Api.csproj --context NorthstarDbContext --output-dir Persistence/Migrations`: passed.
- `dotnet build services/api/Northstar.sln`: passed with 0 warnings and 0 errors.
- `dotnet test services/api/Northstar.sln`: passed.
- `dotnet restore services/api/Northstar.sln`: passed.

## Not Run

- PostgreSQL smoke: not run by this agent because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not visible to this process.
- User reported PostgreSQL smoke passed manually before this slice.
- frontend build/test: not run; frontend code was not changed.
- object storage integration: not run; not in scope.

## Security Notes

- Public-link runtime behavior and anonymous access boundaries were not broadened.
- Generic policy patch behavior was not changed.
- Notifications use generic titles/bodies and `#permissions` action URL only.
- Notification content does not include raw share-link tokens, raw invite tokens, token hashes, accept URLs, passwords, password hashes, or password proofs.

## Remaining Deferred Permission Work

- SCIM endpoint skeleton
- real IdP login
- MFA/recent-auth backend state
- production invite delivery provider
- frontend permission mutation workflows

## Smallest Safe Next Step

implement SCIM endpoint skeleton
