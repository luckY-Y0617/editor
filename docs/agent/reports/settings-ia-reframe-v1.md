# Settings IA Reframe v1

## Summary

- PC Settings IA was reframed from peer `workspace / current library / organization` scopes into a Settings center with secondary navigation groups.
- No backend API, DTO, permission, migration, or mutation semantics changed.
- Current Library is now treated as workspace context summary and a Libraries deep link, not a top-level Settings scope.

## PC IA Contract

- Primary app navigation remains unchanged: Home, Libraries, Search, Updates, Members, Settings.
- Settings secondary navigation:
  - Personal: Preferences.
  - Workspace: General, Notifications, Access & identity, Security, Integrations.
  - Organization: Profile, Workspaces, Members inventory.
  - Deferred: Plan, Developer.
- Default panel is Workspace / General.
- Library operations remain in Libraries.
- Workspace member management remains in Members.
- Access requests remain in Updates.
- Share and permission links remain resource-level surfaces from document or collection context.

## IA Changes Made

- Removed the old visible top-level Settings scope switch from the PC Settings surface.
- Removed the top status grid that made Settings feel like a dashboard of unfinished tabs.
- Added a left Settings secondary nav that expresses ownership boundaries directly.
- Kept Workspace General scan-first with workspace profile read-only state, current library summary, and available library links.
- Moved personal language/region preference into Personal / Preferences.
- Consolidated members, groups, access requests, and share boundary links under Workspace / Access & identity.
- Kept Organization profile rename in Organization / Profile and preserved owner-gated behavior.
- Kept Organization workspaces and members inventory read-only.

## Settings Nav Model

- Added `SettingsPanelId`, `SettingsNavGroup`, `SettingsNavItem`, and normalization helpers.
- New hashes use `#settings?panel=...`.
- Legacy hashes remain safe:
  - `#settings` maps to Workspace / General.
  - `#settings?scope=workspace&tab=general` maps to Workspace / General.
  - `#settings?scope=library&tab=collections` maps to Workspace / General with the library summary/deep link.
  - `#settings?scope=organization&tab=overview` maps to Organization / Profile.
  - `#settings?scope=organization&tab=members` maps to Organization / Members inventory.

## UX Changes Made

- PC Settings now shows a compact context strip for Workspace and API only.
- Current Library is visible only as content context, not as a peer setting boundary.
- Actionable surfaces are clearer:
  - Workspace notification default remains live.
  - Organization profile rename remains live and owner-gated.
  - Members, Updates, Libraries, and Share are links to task surfaces instead of duplicated forms.
- Unsupported areas remain visible as deferred.

## APIs Changed

None.

## Migrations Changed

None.

## CSS Scope

- Only `workspace-settings-*` selectors were added/changed.
- No Home, Library, Updates, Search, Login, PermissionAdmin, editor, or mobile-specific visual refactor was made.

## Tests Updated

- `workspaceSettingsModel` tests now cover the PC Settings nav groups and legacy hash normalization.
- `hashRouting` tests now cover `panel` parsing/creation and preserve legacy hash parsing.
- `i18n` fallback tests now cover new Settings IA labels.

## Browser QA

- Browser Use IAB was attempted first and failed because no Codex IAB backend was exposed.
- Local headless Chrome/CDP fallback was used against Vite on `127.0.0.1:5177`.
- PC QA passed:
  - Default Settings opens Workspace / General.
  - Secondary nav shows Personal, Workspace, Organization, Deferred.
  - Old `Workspace / Current Library / Organization` scope switch is absent.
  - Old status grid is absent.
  - Current Library is a summary plus Open Library link.
  - Workspace Notifications panel and live control remain visible.
  - Access & identity links to Members / Updates / Share-related task surfaces.
  - Organization Profile panel and rename entry remain visible.
  - Legacy library hash falls back to Workspace / General without a top-level library scope.
- QA artifact: `apps/web/.codex-settings-ia-reframe-v1-qa.json`.

## Validation

- `npm test`: passed, 162 tests.
- `npm run build`: passed with the existing Vite large chunk warning.

## Not Run / Reason

- Backend `dotnet restore/build/test`: not run because no backend code or API contract changed.
- PostgreSQL smoke: not run because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was not set and no database smoke was required for this frontend-only IA task.
- Mobile QA: intentionally not run; this task was PC-only.

## Deferred Items

- Workspace profile mutation remains deferred.
- Organization members mutation remains deferred.
- Workspace provisioning remains deferred.
- System / Instance Settings remain not implemented.
- Billing/domain/SSO/SCIM/audit/retention semantics remain deferred.
- Public-link conflict behavior was not touched.
