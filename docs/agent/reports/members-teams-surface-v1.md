# Members & Teams Surface V1

Date: 2026-05-17

Scope: frontend IA implementation for workspace Members & Teams. This report
records the completed UI boundary and deferred items. It does not add backend
APIs, migrations, or Share behavior.

## Implemented

- Added first-class workspace left-nav entry `Members & Teams` / `成员与团队`.
- Added protected hash route `#members`.
- Added tabs:
  - Members
  - Teams
  - Directory Sync
  - Permissions Summary
- Members reuses existing workspace member APIs:
  - list members
  - add existing user by email
  - change role
  - remove member
- Teams uses existing workspace group list metadata for read/detail display.
- Directory Sync shows SCIM discovery and SCIM token status and links token
  management back to Settings Integrations.
- Permissions Summary is read-only and points users to the owning surfaces for
  resource grants and share-link governance.
- Legacy workspace identity hashes now canonicalize into Members & Teams:
  - `#workspace-members` -> `#members`
  - `#workspace-groups` / `#groups` / `#permission-admin` -> `#members?tab=teams`
  - `#scim` -> `#members?tab=directory`

## Boundaries Preserved

- No backend API changes.
- No migrations.
- No pending workspace invitation lifecycle.
- No group create/delete/rename/member mutation UI.
- No IAM/SCIM/external group editing.
- No permission editing inside Members & Teams.
- No Share V1 behavior changes.
- No workspace public share.

## Deferred

- Full pending workspace invitation lifecycle.
- Local/static group editing UI.
- Group member detail API integration beyond list metadata.
- IAM sync trigger UI.
- SCIM provisioning UI.
- Moving or removing Settings transition tabs.

## Validation Notes

The implementation adds focused frontend model/routing tests. PostgreSQL smoke
remains environment-dependent and must only be run when
`NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is set.
