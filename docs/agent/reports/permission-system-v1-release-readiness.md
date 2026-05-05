# Permission System V1 Release Readiness

## Summary

- Ran permission-system V1 release-readiness review across current governance docs, recent reports, targeted backend code, targeted frontend code, migrations, and validation commands.
- V1 readiness classification: `ready with deferred items`.
- No V1 release blockers were found during this review.
- No code fixes were made.
- Public-link runtime behavior was not broadened.

## Scope

- Release-readiness and governance review only.
- Small fixes were allowed only for clear V1 release-blocking regressions.
- No new permission features, public-link boundaries, SCIM advanced behavior, OIDC/SAML provider integration, MFA recovery flows, frontend surfaces, package files, migrations, moves, deletes, renames, or archive actions were added.

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
- `docs/agent/skills/data-model-migrations.md`
- `docs/agent/skills/frontend-editor.md`
- `docs/PERMISSION_SYSTEM_CONTRACT.md`
- `docs/agent/reports/permission-system-e2e-acceptance-v1.md`
- `docs/agent/reports/mfa-provider-step-up-enforcement-v1.md`

## Code Areas Inspected

- `services/api/src/Northstar.Api/Controllers/PermissionsController.cs`
- `services/api/src/Northstar.Api/Controllers/PublicShareLinksController.cs`
- `services/api/src/Northstar.Api/Controllers/AuthController.cs`
- `services/api/src/Northstar.Api/Controllers/ScimController.cs`
- `services/api/src/Northstar.Api/Controllers/ScimTokensController.cs`
- `services/api/src/Northstar.Application/Security/ResourcePermissionManagementService.cs`
- `services/api/src/Northstar.Application/Security/ShareLinkService.cs`
- `services/api/src/Northstar.Application/Security/EmailInviteService.cs`
- `services/api/src/Northstar.Application/Security/ScimService.cs`
- `services/api/src/Northstar.Application/Security/ScimTokenService.cs`
- `services/api/src/Northstar.Application/Security/AuthMfaService.cs`
- `services/api/src/Northstar.Application/Security/AuthSecurityStateService.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations/EmailInviteDeliveryOutboxItemConfiguration.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations/ScimTokenConfiguration.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations/UserMfaMethodConfiguration.cs`
- recent permission migrations through `20260503232004_AddUserMfaMethodsPhase16`
- `apps/web/src/components/DocumentSharePermissionsPage.tsx`
- `apps/web/src/components/PermissionAdminSurfacesPage.tsx`

## V1 Readiness Classification

`ready with deferred items`

## V1 Must-Have Checklist

| Area | Status | Evidence | Notes |
|---|---|---|---|
| Workspace RBAC | ready | Workspace member/admin checks are used by permission, SCIM token, and admin surfaces. | Full workspace member UI exists; advanced member flows beyond current API remain non-V1. |
| Scoped document/collection permissions | ready | `PermissionsController` exposes resource permission, policy, and grant routes for typed resources. | Document workflow is the primary frontend V1 surface. |
| Effective permission service | ready | `EffectivePermissionService` / `EffectivePermissionQueryService` exist and are covered by backend tests. | No release blocker found. |
| Direct user grants | ready | `ResourcePermissionManagementService` creates, updates, revokes user grants and validates active workspace members. | Step-up enforced when actor has MFA enabled. |
| Group grants | ready | Grant subject type supports `group`; group fan-out is routed through notification fan-out service. | Advanced IAM/SCIM relationship remains separately deferred. |
| Temporary grants | ready | Grant expiry is normalized and future expiry is enforced. | No release blocker found. |
| Access requests | ready | `PermissionsController` exposes create/list/resource/review/cancel access-request routes. | Included in notification and manager fan-out coverage. |
| Permission notifications | ready | Notification fan-out service is integrated with grants, share links, email invites, and access requests. | Delivery transport beyond in-app notifications is not part of this item. |
| Notification preferences / watched-muted | ready | Notification preference service and `permission_notification_preferences` migration are present. | Used for muted preference suppression where supported. |
| Share links | ready | `ShareLinkService` handles create/list/revoke and returns raw token only in create response. | Public-link behavior remains share-link API owned. |
| External authenticated links | ready | Share-link audience handling supports external links with subject email and protected resolver path. | Public links are rejected from authenticated share-token resolver. |
| Email invites | ready | `EmailInviteService` supports create/resolve/accept/revoke with token-hash lookup. | Raw invite token and accept URL are create-response only. |
| Public document links | ready | `PublicShareLinksController` exposes public document route; `ShareLinkService` returns sanitized document response. | Only dedicated public endpoint is anonymous. |
| Public collection summary links | ready | Public collection route exists and is backed by collection summary query service. | Child document content is intentionally not public through collection links. |
| Public-link password support | ready | Public passwords are hashed through password hash service; DTOs expose only `hasPassword`. | Password proof remains header-based. |
| Public-link anonymous boundary | ready | Anonymous controller exposes only resolve/document/collection public share-link routes. | Rate limiting is applied at the controller. |
| Public-link protected API boundary | ready | No inspected bootstrap/map/search/export/context/activity/comments/attachments/files/versions/mutation/permission API widening was found. | Browser acceptance remains deferred because Browser Use cannot run on current Node. |
| Permission audit | ready | Permission audit route exists and services write audit events for policy, grant, share-link, invite, SCIM, and MFA-relevant actions. | Metadata review found no raw token/password fields in inspected areas. |
| SCIM endpoint skeleton | ready | SCIM discovery routes exist under `/api/v1/workspaces/{workspaceId}/scim/v2`. | Discovery advertises limited capabilities. |
| SCIM bearer token validation | ready | `scim_tokens` stores token hash; SCIM token service returns raw token only at create. | Cross-workspace validation is covered by prior tests. |
| SCIM provisioning V1 | ready | SCIM Users/Groups list/get/create/patch/put routes exist; delete returns unsupported validation behavior. | Bulk, complex filters, enterprise extension, and delete/deactivate are deferred. |
| SCIM compatibility hardening V1.1 | ready | Narrow filters, bounded paging, and restricted PUT/PATCH behavior are implemented. | Advanced client compatibility remains deferred. |
| Real IdP login backend boundary | ready | `POST /api/v1/auth/idp/login` exists and records IdP login through auth service. | Full OIDC/SAML redirect/callback and secret management are deferred. |
| Production invite delivery provider boundary | ready | Invite delivery options/provider boundary and delivery status behavior exist. | Secure outbox/retry covers reliable delivery state. |
| Secure invite outbox/retry | ready | `email_invite_delivery_outbox` stores provider/status/attempt/error state without raw token or accept URL columns. | Background host/outbox scheduling beyond direct processor remains non-V1 if not configured. |
| MFA TOTP enrollment/verification/disable | ready | Auth routes exist for TOTP enroll/verify/disable; `user_mfa_methods` stores `secret_ciphertext`. | WebAuthn, SMS/email MFA, and recovery flows are deferred. |
| MFA step-up enforcement for high-risk permission mutations | ready | Step-up checks are present in grant, policy, share-link, invite, workspace member, and SCIM token mutation services. | Enforcement applies when actor has MFA enabled. |
| Frontend document permission mutation workflow | ready | `DocumentSharePermissionsPage.tsx` supports grant create/update/revoke, policy mutation, share links, invites, and public-link creation via share-link API. | Browser QA gap remains environment-related. |
| Frontend permission admin surfaces | ready | `PermissionAdminSurfacesPage.tsx` provides workspace member/group and SCIM token/discovery surfaces. | SCIM provisioning UI is intentionally not a broad admin console. |
| Frontend public-link interaction hardening | ready with caveat | Public-link UI requires future expiry, sends `audience = public`, clears password after create, and shows raw output only in dismissible create-response state. | Browser acceptance not run because Browser Use cannot run on Node v18.20.7. |
| Tests and validation coverage | ready with caveat | Backend restore/build/test and frontend build/test passed in this review. | PostgreSQL smoke was not run by agent because env var was not visible. |

## Public-Link Boundary Review

- Generic permission policy patch still rejects direct `linkMode = public` in `ResourcePermissionManagementService.UpdatePolicyAsync`.
- Public links are created only through share-link APIs.
- Public share-link creation may internally set resource policy to public, preserving the approved architecture decision.
- Anonymous public access remains limited to:
  - `GET /api/v1/public/share-links/{token}/resolve`
  - `GET /api/v1/public/share-links/{token}/document`
  - `GET /api/v1/public/share-links/{token}/collection`
- Public collection links remain summary-only.
- No inspected code widened bootstrap, map, search, export, context, activity, comments, attachments, files, versions, mutation, or permission APIs for public anonymous tokens.

## Token / Secret Safety Review

- Share-link raw tokens are generated and returned only in `CreateShareLinkResponse`; stored state uses token hash.
- Invite raw tokens and accept URLs are returned only in invite create response; invite lookup uses token hash.
- SCIM raw tokens are returned only in `CreateScimTokenResponse`; token list DTOs omit raw token and token hash.
- Public-link passwords are hashed and list/resolve DTOs expose only `hasPassword`.
- Invite outbox configuration stores workspace, invite id, recipient email, provider, status, attempt state, timestamps, and non-secret error fields; no raw invite token, raw accept URL, token hash, SMTP secret, provider secret, or password-like column was found.
- TOTP MFA stores `secret_ciphertext`, not plaintext secret or code.
- TOTP verification events record method metadata only and do not include TOTP secret/code.
- Inspected audit and notification metadata does not expose raw tokens, token hashes, passwords, password hashes, password proofs, provider secrets, or TOTP codes.

## MFA / Step-Up Review

- `POST /api/v1/auth/mfa/totp/enroll` creates pending TOTP enrollment and returns provisioning data only at enrollment time.
- Pending enrollment does not count as MFA enabled.
- `POST /api/v1/auth/mfa/totp/verify` validates server-side TOTP and records backend step-up/recent-auth state.
- `POST /api/v1/auth/mfa/totp/disable` requires step-up when MFA is enabled.
- `GET /api/v1/auth/security-state` reports recent auth, MFA enabled, MFA verified, step-up requirement, and supported methods from backend state.
- Step-up enforcement is present for high-risk permission mutations covered by V1.
- WebAuthn/passkeys, SMS/email MFA, recovery codes, and recovery/reset/admin reset flows remain deferred.

## SCIM Review

- SCIM discovery and provisioning routes exist under `/api/v1/workspaces/{workspaceId}/scim/v2`.
- SCIM bearer-token management exists under `/api/v1/workspaces/{workspaceId}/scim/tokens`.
- SCIM tokens are workspace-scoped and hash-only at rest.
- Minimal Users/Groups list/get/create/patch/put behavior exists.
- Delete/deactivate returns explicit unsupported behavior; hard delete is not implemented.
- Bulk, complex filter grammar, enterprise extension, delete/deactivate, and advanced compatibility remain non-V1 deferred items.

## Frontend Permission Workflow Review

- `#permissions` is backed by `DocumentSharePermissionsPage.tsx`.
- Grant create/update/revoke uses existing permission grant routes.
- Access Settings exposes supported non-public policy modes and does not expose direct `public` link mode patching.
- Public document links are created only through share-link creation with `audience = public`, viewer role, future expiry, and optional password.
- One-time public/share/invite token output is dismissible and raw tokens are not shown in active lists.
- `#workspace-members`, `#workspace-groups`, and `#scim` are backed by `PermissionAdminSurfacesPage.tsx`.
- SCIM token management shows raw token only in create-response state and omits token hashes from lists.
- Frontend permission surfaces read auth access token from existing auth localStorage keys but did not show durable writes of raw public tokens, SCIM tokens, invite tokens, passwords, or password proofs.

## Data Model / Migration Review

- Recent permission migrations present:
  - `20260430165840_AddPublicCollectionLinksAndLinkPasswordsPhase11`
  - `20260502063653_AddPermissionNotificationPreferencesPhase12`
  - `20260502092712_AddPermissionNotificationFanoutTypesPhase13`
  - `20260502201249_AddScimTokensPhase14`
  - `20260503015211_AddEmailInviteDeliveryOutboxPhase15`
  - `20260503232004_AddUserMfaMethodsPhase16`
- Reviewed EF configurations for invite outbox, SCIM tokens, and MFA methods.
- No schema release blocker was found during this review.
- PostgreSQL smoke was not run by agent because the smoke connection string was not visible to this process.

## Test Coverage Review

- Backend validation passed:
  - `Northstar.Domain.Tests`: 43 passed.
  - `Northstar.Application.Tests`: 40 passed.
  - `Northstar.Api.Tests`: 152 passed.
- Frontend validation passed:
  - `npm run build` passed with the existing Vite large chunk warning.
  - `npm test` passed with 73 comment regression tests.
- Remaining validation caveats:
  - PostgreSQL smoke not run by agent due invisible env var.
  - Browser QA not run because Browser Use Node runtime is too old.

## Validation Run

- `dotnet restore services/api/Northstar.sln`: passed.
- `dotnet build services/api/Northstar.sln`: passed with 0 warnings and 0 errors.
- `dotnet test services/api/Northstar.sln`: passed.
  - `Northstar.Domain.Tests`: 43 passed.
  - `Northstar.Application.Tests`: 40 passed.
  - `Northstar.Api.Tests`: 152 passed.
- `npm run build` from `apps/web`: passed. Vite emitted the existing large chunk warning.
- `npm test` from `apps/web`: passed. 73 comment regression tests passed.

## PostgreSQL Smoke

- Not run by agent.
- `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was checked in this agent process and was not visible.
- The connection string value was not printed.
- User previously reported PostgreSQL smoke passed manually after setting the connection string.

## Browser QA

- Not run.
- Browser Use was checked through the required Node REPL path.
- Browser QA not run because local Node version is too old for Browser Use: the runtime reports Node `v18.20.7`, while Node REPL requires `>= v22.22.0`.
- No fallback browser harness was created in this release-readiness round.

## Deferred Register

| Deferred Item | Category | Reason Deferred | V1 Impact | Required Future Trigger |
|---|---|---|---|---|
| Full OIDC/SAML provider redirect/callback and secret management | IdP / SSO | V1 includes only backend trusted IdP login boundary. | Non-blocking for V1 because current boundary is explicit and tested. | Product decision to support live external provider redirects/callbacks. |
| SCIM bulk operations | SCIM | V1 supports minimal user/group provisioning, not bulk. | Non-blocking for V1. | Enterprise SCIM client requires bulk endpoint compatibility. |
| SCIM complex filter grammar | SCIM | V1 supports narrow `eq` filters only. | Non-blocking for V1. | Client compatibility requires `and`, `or`, nested attributes, or broader grammar. |
| SCIM enterprise extension | SCIM | Enterprise attributes are outside minimal V1 provisioning. | Non-blocking for V1. | Customer/IdP requires enterprise user schema fields. |
| SCIM delete/deactivate behavior | SCIM | V1 avoids hard delete and returns explicit unsupported behavior. | Non-blocking for V1. | Product approves soft deactivation semantics and tests. |
| Advanced SCIM compatibility beyond V1.1 | SCIM | Basic real-client compatibility was hardened, deeper compatibility remains open-ended. | Non-blocking for V1. | Compatibility testing against specific IdP reveals required behavior. |
| WebAuthn/passkeys | MFA | V1 implements TOTP only. | Non-blocking for V1. | Product requires phishing-resistant MFA. |
| SMS/email MFA providers | MFA | V1 implements TOTP only and avoids weaker/provider-dependent factors. | Non-blocking for V1. | Product approves SMS/email MFA risk and delivery requirements. |
| MFA recovery codes | MFA | Recovery-code storage and lifecycle were not included in TOTP V1. | Non-blocking for V1 if operational recovery is deferred. | Product defines recovery-code UX, storage, and rotation requirements. |
| MFA recovery/reset/admin reset flows | MFA | Admin reset and account recovery are high-risk and need separate design. | Non-blocking for V1 if support process is documented separately. | Product requires self-service or admin recovery. |
| PostgreSQL smoke in agent process | Validation | Env var was not visible to this agent process. | Caveat only; user previously reported manual smoke success. | Make `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` visible to agent process and rerun smoke. |
| Browser QA | Validation | Browser Use cannot run because local Node runtime is too old. | Caveat only; build/test and code-level checks passed. | Upgrade Node runtime to `>=22.22.0` or set `NODE_REPL_NODE_PATH` to a compatible runtime. |
| Frontend public-link browser acceptance | Frontend validation | Browser QA could not run in current environment. | Caveat only; code-level and build/test validation passed. | Run browser acceptance after Browser Use runtime is available. |

## Release Blockers

No V1 release blockers found during this review.

## Fixes Made

None.

## Files Changed

- `docs/agent/reports/permission-system-v1-release-readiness.md`: created release-readiness report and deferred register.

## Final V1 Recommendation

`Ship V1 with documented deferred items`

Reason: backend and frontend validation passed, V1 must-have items are implemented or caveated, public-link and secret boundaries remain intact, and remaining work is explicitly non-V1 or environment validation related.

## Smallest Safe Next Step

freeze permission system V1 docs
