# Public Link Approval Implementation

## Summary

- Recorded the approved public-link architecture decision in governance and permission docs.
- Implemented backend notification preference persistence for workspace and resource watched/muted state.
- Kept public-link runtime behavior unchanged and did not broaden protected APIs.
- Added focused domain, API/service, and migration coverage.
- Backend restore, build, and tests passed.

## Scope

- Implementation scope: public-link governance resolution and notification preferences persistence.
- Backend root inspected and changed: `services/api`.
- Public-link runtime behavior was not rewritten.
- Deferred permission slices were not implemented.
- Frontend code was not changed.

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
- `docs/agent/reports/permission-system-completion-readiness.md`
- `docs/agent/reports/public-link-architecture-decision.md`

## Code Areas Inspected

- `services/api/src/Northstar.Api`
- `services/api/src/Northstar.Application`
- `services/api/src/Northstar.Domain`
- `services/api/src/Northstar.Infrastructure`
- `services/api/src/Northstar.Contracts`
- `services/api/tests`
- `services/api/src/Northstar.Infrastructure/Persistence/Migrations`
- `services/api/src/Northstar.Infrastructure/Persistence/NorthstarDbContext.cs`

## Public-Link Governance Updates

- `docs/agent/02-conflict-register.md` now records the approved public-link architecture decision while preserving conflict history.
- `docs/agent/00-project-state.md`, `docs/agent/skills/permissions.md`, and `docs/PERMISSION_SYSTEM_CONTRACT.md` now state the approved boundary:
  - public document links are supported;
  - public collection links are summary-only;
  - generic policy patch still rejects direct `linkMode = public`;
  - public share-link creation may internally set policy `LinkMode = public`;
  - anonymous public access is limited to dedicated public share-link endpoints;
  - protected APIs must not be widened.

## Notification Preference Implementation

- Added `PermissionNotificationPreference` domain entity with scope and watched/muted invariants.
- Added current-user preference read/upsert flow under `/api/v1/notifications/preferences`.
- Workspace preferences require authenticated workspace membership.
- Resource preferences require authenticated view access to the target document or collection.
- Fan-out filtering based on preferences was deferred in that slice and is now
  covered by `docs/agent/reports/permission-notification-fanout-implementation.md`.

## Data Model Changes

- Added `permission_notification_preferences`.
- Columns: `id`, `workspace_id`, `user_id`, `resource_type`, `resource_id`, `watched`, `muted`, `created_at`, `updated_at`.
- Added scope check constraint for workspace-vs-resource preference shape.
- Added resource type check constraint for `document` and `collection`.
- Added watched/muted mutual-exclusion check constraint.
- Added filtered unique indexes for workspace preference and resource preference uniqueness.
- Added EF configuration and `NorthstarDbContext` registration.
- Added EF migration `20260502063653_AddPermissionNotificationPreferencesPhase12`.

## API / Contract Changes

- Added contracts:
  - `PermissionNotificationPreferenceDto`
  - `PermissionNotificationPreferencesResponse`
  - `UpdatePermissionNotificationPreferenceRequest`
- Added protected routes:
  - `GET /api/v1/notifications/preferences?workspaceId=...`
  - `PUT /api/v1/notifications/preferences`
- No anonymous or public-link routes were changed.

## Application / Domain / Infrastructure Changes

- API controller remains thin and delegates to application service.
- Application service normalizes scope, validates IDs, checks membership/resource access, and orchestrates repository/unit-of-work persistence.
- Domain entity owns scope and watched/muted invariants.
- Infrastructure owns EF repository, entity configuration, migration, and DbContext registration.
- DI registrations were added for the application service and EF repository.

## Tests Added Or Updated

- Domain invariant tests for watched/muted mutual exclusion and workspace preference construction.
- API/service tests for:
  - unauthenticated rejection;
  - non-member rejection;
  - workspace preference upsert and read;
  - resource preference requiring view access;
  - watched/muted mutual exclusion.
- Migration test covering table, constraints, and filtered unique indexes.

## Validation Run

- PostgreSQL smoke environment follow-up:
  - `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` process scope: not set.
  - `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` user scope: not set.
  - `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` machine scope: not set.
  - `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` child validation shell scope: not set.
  - Exact smoke command inspected from `services/api/README.md`: `dotnet test .\Northstar.sln --filter PostgreSqlSmoke`.
  - Validation stop rule applied: restore/build/test/smoke were not rerun in this follow-up because the smoke connection string is not visible to the validation process.
  - Fixes made during this follow-up: none.
  - Remaining validation risk at that time: PostgreSQL smoke was pending until `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was set for the validation process.
  - Later user report: PostgreSQL smoke passed manually after setting the connection string.
- `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` availability check: not set in process, user, machine, or non-sandboxed validation process scope.
- `dotnet restore services/api/Northstar.sln`: passed after running outside the workspace sandbox because the .NET CLI needed access to its user profile/cache.
- `dotnet build services/api/Northstar.sln`: passed with 0 warnings and 0 errors.
- `dotnet test services/api/Northstar.sln`: passed.
  - `Northstar.Domain.Tests`: 43 passed.
  - `Northstar.Application.Tests`: 40 passed.
  - `Northstar.Api.Tests`: 114 passed.
- Fixes made during this validation pass: none.
- Remaining validation note: user later reported PostgreSQL smoke passed manually after setting the connection string.

## Not Run

- Follow-up `dotnet restore services/api/Northstar.sln`: not run because the task requires stopping when `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not visible to the validation process.
- Follow-up `dotnet build services/api/Northstar.sln`: not run because the task requires stopping when `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not visible to the validation process.
- Follow-up `dotnet test services/api/Northstar.sln`: not run because the task requires stopping when `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not visible to the validation process.
- Follow-up PostgreSQL smoke `dotnet test .\Northstar.sln --filter PostgreSqlSmoke`: not run because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not set for the validation process.
- PostgreSQL smoke: not run because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not set for the validation process.
- frontend build/test: not run because frontend code was not changed.
- Object storage integration: not run; not in scope.

## Remaining Deferred Permission Work

- group grant fan-out
- SCIM endpoint skeleton
- real IdP login
- MFA/recent-auth backend state
- production invite delivery provider
- frontend permission mutation workflows

## Smallest Safe Next Step

implement group grant fan-out
