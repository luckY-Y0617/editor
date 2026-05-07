# Frontend Capability Closure v1

## Status

- Closure slice selected: Workspace Members / Permissions.
- Implementation status: frontend closure improvement completed for current workspace resolution.
- No backend APIs, migrations, permission semantics, System / Instance Settings, Organization member mutation, workspace provisioning, files/upload, or public-link behavior were changed.
- A minimal backend query bug was fixed for workspace group listing because it directly affected the selected Members page.

## Capability Inventory Summary

- Libraries / Collections / Documents: backend exposes document CRUD/location/archive/restore/delete and collection create/update/delete/reorder; frontend has live Libraries operations and tests.
- Members / Permissions: backend exposes workspace members list/add/update/remove plus groups and SCIM surfaces; frontend has live members UI but depended on an explicit workspace id before this round.
- Share / permission links: backend exposes resource permissions, grants, share links, email invites, access requests, and public share-link endpoints; frontend has a substantial share surface, but public-link conflict boundaries require careful follow-up.
- Updates / Notifications: backend exposes notification feed, mark-read, mark-all-read, and preferences; frontend has live feed/read actions, while category preference mutations remain deferred.
- Workspace Settings: mostly read-only/live summaries with deferred mutation surfaces.
- Organization Settings: organization profile rename is implemented and QA accepted; broader organization mutations remain deferred.
- Comments: comment v1 is backend-backed and covered by frontend regression tests.
- Files / Upload / Attachments: controllers exist, but files remain Phase 6 target and were not selected for this closure slice.

## Selected Slice Rationale

- Workspace Members is a daily admin flow with backend APIs and tests already present.
- Frontend already had the mutation UI for add/update/remove, so this round could close an actual usability gap with minimal diff.
- The gap was not backend semantics; it was that the page could show `Not connected` unless `VITE_NORTHSTAR_WORKSPACE_ID` was configured.
- The fix makes the page derive the current workspace from authenticated bootstrap data when no explicit workspace id is configured.
- Browser QA also exposed `/workspaces/{id}/groups` returning `500` from an EF translation issue; this was fixed by ordering before projection in the repository query.

## UX Contract

- Entry: `#workspace-members`.
- Default state: resolve current workspace, load live workspace members, groups, SCIM discovery, and SCIM tokens.
- Primary actions: owner/admin can add existing users, update non-owner roles, and remove members; backend remains authoritative for last-owner and step-up checks.
- Disabled state: viewer/editor/unknown roles see disabled controls and capability reasons.
- Loading/error: API and workspace resolution state are surfaced in the summary metrics instead of silently showing demo success.
- Backend errors: mutation errors preserve Northstar API messages through `toPermissionMutationError`.
- API unconfigured: page remains honest as not connected; no mock success.
- Browser QA: verify owner default, add/remove member, viewer disabled state, and narrow viewport.

## Validation Record

- `npm test`: passed, 150 tests.
- `npm run build`: passed with existing Vite large chunk warning.
- `dotnet restore services/api/Northstar.sln`: passed.
- `dotnet build services/api/Northstar.sln`: failed because running `Northstar.Api (23336)` locked Debug output files.
- `dotnet build services/api/Northstar.sln -c Release -p:UseSharedCompilation=false --no-restore`: passed.
- `dotnet test services/api/Northstar.sln -c Release --no-restore`: passed.
- Browser Use: attempted; IAB initialization timed out.
- Browser QA fallback: local headless Chrome CDP against live Vite and live API passed functional flow.
- PostgreSQL smoke: not run because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not set.

## Browser QA Details

- Owner members page loaded and member controls were enabled.
- Owner added a registered QA member through the UI.
- Owner removed the QA member through the two-step UI confirmation.
- Viewer saw member management disabled.
- Narrow viewport displayed the members page without blocking the flow.
- Browser environment had a configured workspace id, so the visible workspace source was configured; bootstrap fallback was covered by focused model test.
- Before the backend query fix, Group Sources showed unavailable because the groups endpoint returned `500`; the fix was validated by Release build/test, but the already-running local Debug API was not restarted.

## Deferred Items

- Share / permission-link closure.
- Updates / notification read-state error feedback and preference mutation closure.
- Organization member mutation.
- Workspace provisioning mutation.
- Files/upload closure.
- System / Instance Settings.
- Billing / domain / SSO / SCIM expansion / audit / retention semantic expansion.
