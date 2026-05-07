# Organization Profile Rename v1 Closeout

## Status

- Organization profile rename v1 is implemented and QA accepted.
- This closeout is documentation-only. It does not add product behavior, schema, API routes, or frontend controls.
- System / Instance Settings, Organization member mutation, workspace provisioning mutation, and billing/domain/SSO/SCIM/audit/retention semantics remain deferred.

## Current Code Verification

Verified in current code, not inferred from prior docs:

- `services/api/src/Northstar.Api/Controllers/OrganizationsController.cs` exposes:
  - `GET /api/v1/organizations/{organizationId}/profile`
  - `PATCH /api/v1/organizations/{organizationId}/profile`
  - `GET /api/v1/organizations/{organizationId}/members`
- `services/api/src/Northstar.Application/Organizations/OrganizationSettingsCommandService.cs` owns the profile update use case, validation, authorization check, duplicate slug conflict check, and post-save response projection.
- `services/api/src/Northstar.Contracts/Organizations/OrganizationDtos.cs` owns `UpdateOrganizationProfileRequest` and `OrganizationProfileResponse`.
- `services/api/src/Northstar.Infrastructure/Organizations/EfOrganizationSettingsRepository.cs` owns EF reads/update lookup and slug uniqueness checks.
- `services/api/src/Northstar.Application/Security/PermissionCatalog.cs` grants `organization.manage_settings` only to `owner`.
- `apps/web/src/lib/appApi.ts` exposes `updateOrganizationProfile`.
- `apps/web/src/lib/workspaceSettingsModel.ts` derives `canEditOrganizationProfile`, disabled reason, mutation status, and trimmed/normalized update request data.
- `apps/web/src/components/WorkspaceSettingsPage.tsx` renders Organization Overview as read-only by default with an owner-only inline edit panel.

## Authorization Rule

- Action key: `organization.manage_settings`.
- Rule: only a user who is an active `owner` in at least one workspace under the organization can update the organization profile.
- `admin`, `editor`, and `viewer` do not receive `organization.manage_settings`.
- Frontend capability checks are UI convenience only. Backend authorization remains authoritative and returns Northstar error envelopes for forbidden requests.

## API Contract

- Endpoint: `PATCH /api/v1/organizations/{organizationId}/profile`.
- Request DTO: `UpdateOrganizationProfileRequest`.
- Request fields:
  - `name`
  - `slug`
- Response DTO: existing `OrganizationProfileResponse`.
- Validation:
  - `name` is trimmed, required, and bounded to 120 characters.
  - `slug` is normalized/validated as a lowercase backend slug and bounded to 80 characters.
  - Duplicate slug returns `CONFLICT`.
- Error behavior:
  - `/api/v1` Northstar error envelope is preserved.
  - EF entities are not exposed.

## UX Closeout

- Organization Overview remains scan-first and read-only by default.
- Owners see a small inline edit panel for organization name and backend slug.
- The edit flow has explicit Save and Cancel controls.
- Save disables repeat submission while saving.
- Backend errors are displayed in the inline panel instead of being swallowed.
- Success refreshes/safely updates local profile state and shows `Profile updated`.
- Slug help states that the slug is stored on the organization profile and does not enable full organization URL routing in this slice.
- Non-owner users see rename unavailable with owner-required / insufficient-permission reason instead of an executable edit action.

## Browser QA

Browser Use status:

- Attempted Browser Use after Node was corrected to v26.
- Browser Use still could not run because Codex IAB backend was not exposed: `iabBrowsers=0`.
- Fallback used local headless Chrome through Chrome DevTools Protocol against the live Vite app and live API.

Chrome/CDP QA result:

- Owner read-only Organization Overview state: passed.
- Owner edit panel open state: passed.
- Save / Cancel controls visible: passed.
- Slug help visible and clear: passed.
- Cancel returns to read-only state: passed.
- Save success updates local UI with new name/slug and success state: passed.
- Viewer disabled/unavailable rename state: passed.
- Admin disabled/unavailable rename state: passed.
- Editor disabled/unavailable rename state: passed.
- After QA, the organization profile was restored to `Northstar / northstar`.

QA artifacts generated locally and should not be treated as product artifacts:

- `.codex-org-profile-owner-readonly-scoped.png`
- `.codex-org-profile-owner-success-scoped.png`
- `.codex-org-profile-owner-edit.png`
- `.codex-org-profile-viewer-disabled.png`
- `.codex-org-profile-admin-disabled.png`
- `.codex-org-profile-editor-disabled.png`

## Validation Record

Commands previously run for the implementation/stabilization slice:

- `dotnet restore services/api/Northstar.sln`: passed.
- `dotnet build services/api/Northstar.sln`: passed.
- `dotnet test services/api/Northstar.sln`: passed.
- `npm test`: passed.
- `npm run build`: passed with the existing Vite large chunk warning only.

This closeout report did not change application code, so backend/frontend validation was not rerun for this documentation-only step.

PostgreSQL smoke:

- Not run.
- Reason: `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not set in the agent environment.

## Demo Data Guardrails

Local demo data was created through existing public application APIs for frontend inspection only. It is not seed baseline, not a migration, and not a product contract.

Demo users:

- `demo.admin@northstar.local` / `Northstar.test.123!` / workspace `admin`
- `demo.editor@northstar.local` / `Northstar.test.123!` / workspace `editor`
- `demo.viewer@northstar.local` / `Northstar.test.123!` / workspace `viewer`

Demo collections:

- `Demo - Product Notes`
- `Demo - Customer Signals`
- `Demo - QA Archive`

Demo documents:

- `Demo - Roadmap Brief`
- `Demo - Launch Checklist`
- `Demo - Interview Highlights`
- `Demo - Support Themes`
- `Demo - Regression Notes`

Cleanup guidance:

- Do not add a production cleanup endpoint for this data.
- Use existing owner-authenticated APIs or UI only.
- Remove demo members through existing workspace member removal.
- Remove demo documents first, then remove now-empty demo collections through existing collection delete.
- Keep the `Demo -` prefix if adding more local inspection data so it remains easy to identify.

## Diff Hygiene Notes

Observed working tree status includes many pre-existing dirty/untracked files, generated `dist` artifacts, QA screenshots, and local Vite logs. This report does not stage or commit anything.

Files/artifacts that should be excluded from a focused rename v1 commit unless intentionally selected:

- `.codex-*.png`
- `.codex-*.log`
- `apps/web/.codex-*.log`
- generated `apps/web/dist/**`

No closeout changes were made to:

- `services/api-old`
- old Go file service
- Tiptap document JSON schema
- public-link conflict behavior
- files/upload behavior
- System / Instance Settings
- Organization member mutation
- workspace provisioning mutation
- billing/domain/SSO/SCIM/audit/retention semantics

## Deferred Items

- Organization members mutation.
- Workspace provisioning mutation.
- System / Instance Settings.
- Billing / domain / SSO / SCIM / audit / retention semantics.
- Full organization URL routing semantics for organization slug.
- PostgreSQL smoke from the agent environment when `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is available.
