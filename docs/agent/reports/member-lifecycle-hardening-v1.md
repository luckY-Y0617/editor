# Member Lifecycle Hardening V1

Date: 2026-05-17

Scope: frontend hardening for workspace member lifecycle management in
Members. This report records UI and model guardrails only. It does not
add backend APIs, migrations, invitation lifecycle, group mutation UI, or Share
behavior changes.

## Implemented

- Kept member addition as add existing user by email, using the existing
  workspace member add API.
- Limited add-existing-user roles to `admin`, `editor`, and `viewer`; owner
  assignment remains unavailable in the add form.
- Centralized frontend member lifecycle guards for:
  - supported workspace roles: `owner`, `admin`, `editor`, `viewer`;
  - last active owner removal;
  - last active owner demotion;
  - owner role changes requiring confirmation;
  - current-user self removal and role change when `auth/me` identifies the
    current user.
- Preserved backend validation as authoritative for stale or incomplete
  frontend state.
- Added clearer member mutation context and error-safe UI states without fake
  success.
- Moved member management fully out of Settings; legacy Settings member hashes
  redirect to `#members`.
- Split left navigation into Members and Groups entries; Groups remains
  read/detail-only and Directory Sync remains read/status-only.
- Removed the identity surface connection/status summary cards; API/workspace
  readiness remains represented by actionable loading, empty, and error states
  in each live-backed section.

## Boundaries Preserved

- No backend API changes.
- No migrations.
- No pending workspace invitation lifecycle.
- No group create/delete/rename/member mutation UI.
- No IAM/SCIM/external group editing.
- No Share behavior changes.
- No workspace public scope.

## Validation Notes

- Frontend model tests cover member role guards, unsupported-role exclusion,
  owner-change confirmation modeling, current-user self-action guard, Settings
  transition routing, and group read-only/source labeling.
- Backend last-owner and unsupported workspace-role validation already exists
  in focused API tests and remains unchanged.
- PostgreSQL smoke remains environment-dependent and must only be run when
  `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is set.
