# Permission System E2E Acceptance V1

## Summary

- Ran code-level acceptance checks across the completed permission-system backend and frontend slices.
- Frontend build/test and backend restore/build/test passed.
- No fixes were made; public-link runtime behavior was not broadened.

## Scope

- Validation-led acceptance pass only.
- Small fixes were allowed only for discovered contract, UI, or security regressions.
- No new permission features, backend APIs, public-link boundaries, package files, or migrations were changed.

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
- `docs/agent/skills/frontend-editor.md`
- `docs/PERMISSION_SYSTEM_CONTRACT.md`
- `docs/agent/reports/frontend-public-link-interaction-hardening-v1.md`

## Code Areas Inspected

- `apps/web/src/components/DocumentSharePermissionsPage.tsx`
- `apps/web/src/components/PermissionAdminSurfacesPage.tsx`
- `apps/web/src/lib/permissionAdminApi.ts`
- `apps/web/src/App.tsx`
- `apps/web/package.json`
- `services/api/src/Northstar.Api/Controllers/PermissionsController.cs`
- `services/api/src/Northstar.Api/Controllers/PublicShareLinksController.cs`
- `services/api/src/Northstar.Api/Controllers/ScimController.cs`
- `services/api/src/Northstar.Api/Controllers/ScimTokensController.cs`
- `services/api/src/Northstar.Api/Controllers/AuthController.cs`
- `services/api/src/Northstar.Application/Security/ResourcePermissionManagementService.cs`
- `services/api/src/Northstar.Application/Security/ShareLinkService.cs`
- `services/api/src/Northstar.Application/Security/PermissionNotificationFanoutService.cs`
- `services/api/src/Northstar.Application/Security/ScimService.cs`
- `services/api/src/Northstar.Application/Security/ScimTokenService.cs`
- `services/api/src/Northstar.Application/Security/AuthSecurityStateService.cs`
- `services/api/src/Northstar.Application/Security/AuthService.cs`
- `services/api/src/Northstar.Application/Security/EmailInviteDeliveryOutboxProcessor.cs`
- `services/api/src/Northstar.Domain/Security/ResourceEmailInvite.cs`
- `services/api/src/Northstar.Domain/Security/ScimToken.cs`
- `services/api/src/Northstar.Domain/Security/EmailInviteDeliveryOutboxItem.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations/EmailInviteDeliveryOutboxItemConfiguration.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Migrations/20260503015211_AddEmailInviteDeliveryOutboxPhase15.cs`

## Frontend Acceptance Findings

- `#permissions` is routed to `DocumentSharePermissionsPage`.
- Grant create/update/revoke uses the existing document resource grant routes:
  - `POST /api/v1/permissions/resources/document/{documentId}/grants`
  - `PATCH /api/v1/permissions/resources/document/{documentId}/grants/{grantId}`
  - `DELETE /api/v1/permissions/resources/document/{documentId}/grants/{grantId}`
- Policy mutation uses `PATCH /api/v1/permissions/resources/document/{documentId}/policy`.
- Access Settings exposes `disabled`, `internal`, and `external`; it does not expose direct `public` policy patching.
- Public document link creation uses only the share-link create API and sends `audience = public`, viewer role, future expiry, and optional password.
- One-time share/invite token panels are dismissible.
- Active share-link and invite lists show metadata only and do not show raw tokens, token hashes, passwords, password hashes, or password proofs.
- `#workspace-members`, `#workspace-groups`, and `#scim` use existing frontend routes and existing backend API categories.
- Frontend inspected code reads auth access tokens from localStorage for Authorization headers, but did not write raw public tokens, SCIM tokens, invite tokens, passwords, or password proofs to localStorage/sessionStorage.
- No permission data was found being written to Tiptap document JSON in inspected frontend permission surfaces.

## Backend Acceptance Findings

- `PermissionsController` exposes the expected resource policy, grant, share-link, email-invite, audit, and access-request routes under the existing `/api/v1` base path.
- `ResourcePermissionManagementService.UpdatePolicyAsync` still rejects direct `linkMode = public`.
- `PublicShareLinksController` exposes only:
  - `GET /api/v1/public/share-links/{token}/resolve`
  - `GET /api/v1/public/share-links/{token}/document`
  - `GET /api/v1/public/share-links/{token}/collection`
- Public share-link endpoints are anonymous but rate-limited through `PublicShareRateLimitPolicyNames.PublicShareLinks`.
- Public password proof remains `X-Share-Link-Password`.
- `ShareLinkService` returns raw share-link tokens only in create responses and stores only token hashes.
- `ShareLinkDto` exposes `hasPassword` metadata, not password or password hash.
- Document/comment authenticated share-token paths remain separate from public anonymous endpoints; public tokens are rejected by the authenticated share-link resolver path.
- Files and attachments controllers did not show public-link token widening in inspected route surfaces.

## Public-Link Boundary Findings

- Public document links are created through authenticated share-link creation only.
- Public share-link creation may return `/api/v1/public/share-links/{token}/resolve` once in the create response.
- Generic policy patch does not accept `public`.
- Public anonymous access remains isolated to the public share-link controller.
- No inspected code widened bootstrap, map, search, export, attachments, files, versions, mutation, or permission APIs to public anonymous tokens.

## Token / Secret Safety Findings

- Share-link list DTOs expose no raw token or token hash.
- Email invite domain state stores `TokenHash`; raw invite token is not a domain property.
- Invite delivery outbox entity/configuration/migration stores workspace, invite id, recipient email, provider, status, attempts, next/last attempt timestamps, sent/failed timestamps, and non-secret error fields.
- Invite outbox inspected schema does not contain raw invite token, raw accept URL, token hash, SMTP secret, provider secret, or password-like columns.
- Notification fan-out metadata uses resource/action references and `#permissions` action URLs; no raw token/password/hash fields were observed.
- SCIM token list DTOs expose no raw token or token hash.
- SCIM token creation returns `rawToken` once and persists only `TokenHash`.
- IdP auth event metadata records provider/reason only; no external subject id, assertion, token, or secret metadata was observed.

## SCIM Findings

- SCIM discovery and provisioning routes exist under `/api/v1/workspaces/{workspaceId}/scim/v2`.
- SCIM token management routes exist under `/api/v1/workspaces/{workspaceId}/scim/tokens`.
- SCIM provisioning paths require workspace-scoped SCIM bearer token validation for user/group list/get/create/update paths.
- SCIM token validation hashes the bearer token, checks workspace scope, active state, and updates `last_used_at`.
- SCIM bulk, complex filter grammar, enterprise extension, and delete/deactivate behavior remain deferred.

## MFA / Recent-Auth Findings

- `GET /api/v1/auth/security-state` is authenticated.
- Recent-auth is backend-derived from successful auth events.
- MFA state reports disabled/unverified until real provider/enrollment exists.
- No inspected code falsely reports real MFA provider support.

## Invite Delivery / Outbox Findings

- Secure invite outbox state exists through `EmailInviteDeliveryOutboxItem`, EF configuration, and migration `20260503015211_AddEmailInviteDeliveryOutboxPhase15`.
- Outbox processing requires accept URLs supplied in memory and fails closed with `missing_accept_url` when unavailable.
- Retry state includes attempt count, max attempts, next/last attempt time, sent/failed time, provider, status, and non-secret error metadata.
- Delivery failure notification fan-out is preserved and secret-safe in inspected code.

## Fixes Made

None.

## Files Changed

- `docs/agent/reports/permission-system-e2e-acceptance-v1.md`: created acceptance validation report.

## Validation Run

- `npm run build` from `apps/web`: passed. Vite emitted the existing large chunk warning.
- `npm test` from `apps/web`: passed. 73 comment regression tests passed.
- `dotnet restore services/api/Northstar.sln`: passed.
- `dotnet build services/api/Northstar.sln`: passed with 0 warnings and 0 errors.
- `dotnet test services/api/Northstar.sln`: passed.
  - `Northstar.Domain.Tests`: 43 passed.
  - `Northstar.Application.Tests`: 40 passed.
  - `Northstar.Api.Tests`: 149 passed.

## PostgreSQL Smoke

- Not run by agent.
- `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was checked in this agent process and was not visible.
- The connection string value was not printed.

## Browser QA

- Not run.
- Browser Use initialization was attempted through the required Node REPL workflow.
- Browser QA not run because local Node version is too old for Browser Use: Node REPL found `v18.20.7`, while Browser Use requires `>=22.22.0`.
- No fallback browser harness was created in this validation round.

## Not Run

- PostgreSQL smoke: not run because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was not visible to this agent process.
- Browser QA: not run because local Node version is too old for Browser Use.
- Object storage integration: not run; not in scope.

## Remaining Deferred Permission Work

- full MFA provider/enrollment/step-up enforcement
- full OIDC/SAML provider redirect/callback and secret management
- SCIM bulk operations
- SCIM complex filter grammar
- SCIM enterprise extension
- SCIM delete/deactivate behavior

## Acceptance Result

accepted with minor gaps

Reason: code-level acceptance checks and build/test validation passed, with no fixes required. PostgreSQL smoke and Browser QA were not run in this agent process due environment/tooling visibility gaps.

## Smallest Safe Next Step

run release-readiness review
