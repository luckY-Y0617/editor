# Settings Closure v2

## Status

- Settings Closure v2 completed as a frontend IA cleanup slice.
- Workspace Profile Closure was deferred because the inspected backend code exposes workspace members, groups, and IAM sync routes, but no workspace profile update endpoint / DTO / test contract.
- No backend API, migration, permission semantics, System / Instance Settings, Organization member mutation, workspace provisioning mutation, files/upload, public-link behavior, or Tiptap document JSON schema was changed.

## Current Settings Capability Inventory Summary

- Workspace profile: live read from bootstrap only; update remains deferred because no backend mutation contract was found in `WorkspacesController` or related tests.
- Workspace members: live member management exists on the dedicated Members surface; Settings now presents it as a deep link, not an embedded management table.
- Workspace notifications: workspace default preference remains the closed live mutation slice from v1; resource preferences remain scan/read-only from notification preferences.
- Library collections/documents: live operations exist in Libraries; Settings now presents summaries and deep links only.
- Resource share / permission links: live resource-level surfaces exist outside Settings; Settings does not duplicate or broaden public-link behavior.
- Organization Settings: organization profile rename remains live and owner-gated; organization members remain read-only inventory.
- System / Instance Settings, workspace provisioning, Organization member mutation, billing/domain/SSO/SCIM/audit/retention expansion, and files/upload remain deferred or out of scope.

## Selected Closure Slice and Rationale

- Selected slice: Settings IA cleanup.
- Rationale: Workspace profile mutation is not supported by the inspected backend contract, while members, share, and library operations already have task surfaces. The safest closure was to make Settings boundaries explicit and reduce half-finished action affordances.

## IA Cleanup Decisions

- Personal language/region remains in Workspace General for now, but copy states it is a local personal browser preference until a Personal Settings route exists.
- Workspace profile remains read-only with a clear no-contract reason.
- Workspace Members remains a link to `#workspace-members`; member mutation controls are not duplicated inside Settings.
- Library Collections/Documents are summaries with `Manage in Library` / `Open Library` links, not Settings-hosted operation panels.
- Resource Share / Permission Links remain resource-scoped; Settings only links/explains boundaries.
- Notification category preferences and email digest remain deferred with an explicit boundary note.

## UX Changes Made

- Added a "Recommended settings closure" section in Workspace General that explains why this round closes IA boundaries instead of adding a new mutation.
- Changed current library area from "Space Settings Entry" to "Current library summary".
- Changed library collection/document cards from live-operation framing to reused-summary framing.
- Replaced misleading "New document in Library" action inside Settings with "Open Library".
- Added localized copy for members, role boundaries, library operations, resource share, and workspace profile read-only state.
- Kept the scan-first Settings layout; no new route, tab, modal, prompt, or confirm flow was added.

## API / Backend Changes

- None.

## CSS Scope

- No CSS selectors were changed in this round.

## Tests Added / Updated

- `workspaceSettingsModel.test.ts`: added Settings IA cleanup slice expectations and updated workspace notification preference inventory from half-finished to live.
- `i18n.test.ts`: added fallback coverage for new Settings closure and boundary labels.

## Browser QA

- Browser Use IAB attempted first; it timed out during initialization, so local headless Chrome/CDP fallback was used.
- QA ran against Vite on port 5176 and existing live API on 7036 using `demo.admin@northstar.local`.
- Results recorded in `apps/web/.codex-settings-v2-qa.json`:
  - Workspace General contains recommended closure, workspace profile read-only reason, library boundary, and personal preference boundary.
  - Library Collections contains collection summary, library boundary copy, and `Manage in Library`.
  - Workspace Permissions contains resource share boundary, document permissions link, and access requests link.
  - 390px mobile viewport contains Workspace Settings and the closure section.
- Screenshots generated locally:
  - `apps/web/.codex-settings-v2-general-closure.png`
  - `apps/web/.codex-settings-v2-library-summary.png`
  - `apps/web/.codex-settings-v2-mobile-closure.png`

## Validation

- `npm test`: passed, 160 tests.
- `npm run build`: passed with existing Vite large chunk warning.

## Not Run / Reason

- Backend restore/build/test: not run because no backend code, API, contracts, migrations, or backend tests changed.
- PostgreSQL smoke: not run because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not set.

## Deferred Items

- Workspace profile mutation until a backend update contract exists.
- Personal Settings route extraction.
- Organization members mutation.
- Workspace provisioning mutation.
- Billing/domain/SSO/SCIM/audit/retention semantic expansion.
- Files/upload.
- Public-link behavior changes.
- System / Instance Settings.
