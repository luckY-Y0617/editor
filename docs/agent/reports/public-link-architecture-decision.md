# Public Link Architecture Decision

## Summary

- This report proposes a final public-link architecture for Northstar permissions; it does not mark conflict-marked behavior as resolved.
- Recommended decision: support read-only public document links and tightly scoped read-only public collection links under dedicated anonymous public endpoints.
- Recommended decision: public links are created only through share-link APIs; generic policy patch must continue rejecting direct `linkMode = public`.
- Recommended decision: public links never widen protected authenticated APIs such as bootstrap, map, search, export, context, activity, comments, attachments, files, versions, or mutations.
- Permission-system completion should proceed only after this public-link architecture is explicitly approved.

## Scope

- Design/governance only.
- Backend root inspected: `services/api`.
- No application, backend, frontend, test, migration, package, or project code was changed.
- Existing public-link code was inspected to ground the recommendation, but conflict-marked behavior remains unresolved until user approval.

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

## Code Areas Inspected

- `services/api/src/Northstar.Api/Controllers/PublicShareLinksController.cs`
- `services/api/src/Northstar.Api/Controllers/ShareLinksController.cs`
- `services/api/src/Northstar.Api/Controllers/PermissionsController.cs`
- `services/api/src/Northstar.Api/Program.cs`
- `services/api/src/Northstar.Application/Security/ShareLinkService.cs`
- `services/api/src/Northstar.Application/Security/ResourcePermissionManagementService.cs`
- `services/api/src/Northstar.Application/Security/EffectivePermissionService.cs`
- `services/api/src/Northstar.Application/Security/PermissionPublicShareOptions.cs`
- `services/api/src/Northstar.Application/Security/IPublicShareCollectionQueryService.cs`
- `services/api/src/Northstar.Contracts/Security/PermissionManagementDtos.cs`
- `services/api/src/Northstar.Domain/Security/ShareLink.cs`
- `services/api/src/Northstar.Domain/Security/ShareLinkAudiences.cs`
- `services/api/src/Northstar.Domain/Security/LinkModes.cs`
- `services/api/src/Northstar.Domain/Security/ResourceAccessPolicy.cs`
- `services/api/src/Northstar.Infrastructure/Security/EfPublicShareCollectionQueryService.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations/ShareLinkConfiguration.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations/ResourceAccessPolicyConfiguration.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Migrations/20260430162513_AddPublicShareLinksAndInviteDeliveryPhase10.cs`
- `services/api/src/Northstar.Infrastructure/Persistence/Migrations/20260430165840_AddPublicCollectionLinksAndLinkPasswordsPhase11.cs`
- `services/api/tests/Northstar.Api.Tests/KnowledgeApiTests.cs`

## Current Code Findings

- `PublicShareLinksController` is `[AllowAnonymous]`, rate-limited, and exposes only `GET public/share-links/{token}/resolve`, `GET public/share-links/{token}/document`, and `GET public/share-links/{token}/collection`.
- `ShareLinksController` is `[Authorize]` and resolves non-public share links through `GET share-links/{token}/resolve`.
- `PermissionsController` is `[Authorize]` and creates/lists/revokes share links under `permissions/resources/{resourceType}/{resourceId}/share-links`.
- `ShareLinkService.CreateShareLinkAsync` creates high-entropy raw tokens, stores only token hashes, returns the raw token at create time, and internally sets policy link mode based on audience.
- `ShareLinkService.NormalizeAudience` allows `public` only when `PermissionPublicShareOptions.Enabled` is true.
- `ShareLinkService.EnsureAudienceRules` requires public links to be viewer-only, subject-email-free, expiring, and within configured max expiry.
- `ShareLinkService.GetActivePublicLinkAsync` validates feature flag, active token, public audience, expected resource type, viewer role, no subject email, expiry, password proof, and policy `LinkMode = public`.
- `ResourcePermissionManagementService.UpdatePolicyAsync` explicitly rejects direct policy patch to `LinkModes.Public`.
- `EffectivePermissionService.GetShareLinkRoleAsync` does not authorize `ShareLinkAudiences.Public` through protected authenticated effective-access paths.
- `EfPublicShareCollectionQueryService` returns collection metadata plus non-archived, non-deleted child document summaries, excluding children with restricted document policies.
- `PublicShareDocumentDto` currently contains document id, title, status, updated time, tags, content, and revision.
- `PublicShareCollectionDto` currently contains collection id, title, updated time, sort order, and child document summaries, not child document content.
- `ShareLinkConfiguration` has constraints for public viewer-only expiry and password hashes only for public links.
- `ResourceAccessPolicyConfiguration` allows `link_mode IN ('disabled', 'internal', 'external', 'public')` at schema level.
- Phase 10 migration added public document-link constraints; Phase 11 migration widened public links to collection and added `password_hash`.
- API tests cover feature-flag-off rejection, public document read, protected-path non-widening, public collection listing, password proof, policy mismatch, revoked/expired/unknown tokens, and migration shape.

## Conflict Summary

- `recommended decision`: public document links should be supported.
- `recommended decision`: public collection links should be supported, but with a narrower collection-summary model than document links.
- `recommended decision`: generic policy patch should not allow direct `linkMode = public`; public mode is an internal consequence of creating an active public share link.
- `alternative`: remove public collection support and keep only public document links.
- `tradeoff`: removing collection links reduces anonymous surface area but wastes existing code/tests/migrations and leaves Phase 11 behavior undocumented.
- `implementation impact`: if approved, update conflict/governance docs to mark the selected behavior as user-approved before implementation cleanup.
- This report does not mark any conflict resolved; it records a proposed architecture for user approval.

## Recommended Public Link Model

- `recommended decision`: public links are read-only, token-scoped capabilities.
- `recommended decision`: public links do not create user identity, workspace membership, grants, email-invite acceptance, or effective permission for protected APIs.
- `recommended decision`: public links are created, listed, and revoked only through authenticated share-link APIs by users with `document.share` or `collection.share`.
- `recommended decision`: anonymous public access exists only under `/api/v1/public/share-links/...`.
- `recommended decision`: public document links and public collection links share the same token table, hash-at-rest rule, expiry rule, password rule, audit rule, and rate-limit policy.
- `recommended decision`: public links must be behind `Permissions:PublicShareLinks:Enabled`.
- `alternative`: separate document and collection feature flags.
- `tradeoff`: one flag is simpler and matches current code; separate flags allow safer staged rollout for collections.
- `implementation impact`: add a separate collection flag only if product wants collection public links staged independently.

## Public Document Link Behavior

- `recommended decision`: public document links are supported.
- Anonymous users may read only the dedicated public document DTO: document id, title, status, updated time, tags, content JSON, and revision.
- Public document read must require a valid active public token for the exact document resource.
- Public document read must require `LinkMode = public` on that document policy.
- Public document read must return not found for missing, revoked, expired, wrong-audience, wrong-resource, wrong-policy, or wrong-password cases.
- Public document links may not grant comment, edit, attachment, file, version, activity, context, search, export, or bootstrap access.
- `alternative`: expose only rendered/sanitized HTML or a reduced content projection.
- `tradeoff`: JSON content preserves editor fidelity but increases scrutiny on embedded metadata safety.
- `implementation impact`: keep Tiptap content metadata-safe and continue forbidding comments/tags/files/permissions/activity from being embedded in document JSON.

## Public Collection Link Behavior

- `recommended decision`: public collection links are supported as collection-summary links.
- Anonymous users may read only collection id, title, updated time, sort order, and visible child document summaries.
- Public collection response should not include child document content by default.
- Child document summaries must exclude deleted, archived, and restricted-policy documents.
- Public collection links must not imply access to each child document's public document endpoint unless a separate child document public link exists.
- Public collection links must require a valid active public token for the exact collection resource.
- Public collection links must require `LinkMode = public` on that collection policy.
- `alternative`: disable public collection links entirely until a frontend collection-public page exists.
- `tradeoff`: supporting collection summaries is useful and already implemented, but exposes more metadata than document-only links.
- `implementation impact`: if approved, retain collection public endpoint, tests, and schema; optionally add a `CollectionEnabled` feature flag if staged rollout is desired.

## Public `linkMode` Decision

- `recommended decision`: `linkMode = public` must not be directly patchable through generic permission policy APIs.
- `recommended decision`: public link creation may internally set the target policy `LinkMode = public` in the same transaction as share-link creation and audit.
- `recommended decision`: public policy state is a derived operational state controlled by public share-link workflow, not a general policy editor mode.
- `recommended decision`: policy patch may continue allowing only `disabled`, `internal`, and `external`.
- `alternative`: allow admins to patch `linkMode = public` directly.
- `tradeoff`: direct patch is simpler for power users but risks public mode without a corresponding active token, expiry, or password boundary.
- `implementation impact`: preserve current service behavior: `ShareLinkService.EnsureLinkPolicyAsync` may set public, while `ResourcePermissionManagementService.UpdatePolicyAsync` rejects direct public patch.

## Anonymous Access Boundary

- `recommended decision`: allow only:
  - `GET /api/v1/public/share-links/{token}/resolve`
  - `GET /api/v1/public/share-links/{token}/document`
  - `GET /api/v1/public/share-links/{token}/collection`
- Anonymous public endpoints should use identical not-found behavior for unauthorized, expired, revoked, policy-mismatch, password-failed, or unknown-token cases.
- Password proof should be accepted only through `X-Share-Link-Password` for now.
- Public endpoints must be rate-limited by remote IP or equivalent low-trust key.
- No anonymous mutation endpoints should be added for public links.

## Protected API Boundary

- `recommended decision`: public links must never widen authenticated protected APIs.
- Public tokens must not grant access to bootstrap, map, normal document get/list, search, export, context, activity, comments, attachments, files, versions, import, update, archive, restore, delete, or permission APIs.
- Public collection links must not grant normal collection/document APIs to anonymous users.
- Public tokens must not be accepted as bearer tokens, refresh tokens, invite tokens, session cookies, workspace membership, or grants.
- Authenticated users who also hold a public token must still be authorized through normal persisted permission state for protected APIs.

## Token And Password Rules

- `recommended decision`: raw public tokens are high entropy, returned only once at create time, and never persisted.
- Token hashes are persisted in `share_links.token_hash`.
- Public-link passwords are optional, supported only for `audience = public`, hashed in `share_links.password_hash`, and never returned.
- Public passwords must not be accepted for workspace/external links.
- Password proof should continue using `X-Share-Link-Password`; avoid query-string passwords because URLs leak through logs, browser history, and referrers.
- Public links must require expiry and enforce a configured max expiry.
- Public links must be viewer-only.
- Public links must reject `subjectEmail`.

## Audit And Security Rules

- Audit events should record share-link id, resource, audience, role, expiry, and `hasPassword`.
- Audit events must not record raw token, token hash, password, password hash, accept URL, or password proof.
- Share-link creation, policy update, and audit writes should remain in the same transaction.
- Revocation should be idempotent and audit the transition when it first occurs.
- Expired public links should not authorize, even if policy remains public.
- Public endpoint failures should avoid revealing whether a token exists, whether a password was wrong, or whether a policy mismatch occurred.

## Data Model Impact

- Current schema already supports the recommended baseline:
  - `share_links.audience` supports `public`.
  - `share_links.token_hash` stores token hash.
  - `share_links.password_hash` stores optional public password hash.
  - `share_links_public_viewer_expiry_check` enforces viewer-only expiring public links for document and collection resources.
  - `resource_access_policies.link_mode` supports `public`.
- No immediate migration is required for the recommended baseline.
- If product wants separate collection rollout, add a configuration option before adding schema.
- If future analytics are required, add separate public-link access event tables later; do not overload audit with anonymous read logs unless explicitly designed.

## API Contract Impact

- Current contracts already cover the recommended baseline:
  - `CreateShareLinkRequest`
  - `CreateShareLinkResponse`
  - `ResolvePublicShareLinkResponse`
  - `PublicShareDocumentResponse`
  - `PublicShareCollectionResponse`
- Recommended contract rule: `CreateShareLinkRequest.Audience = public` is the only API surface that should create public policy state.
- Recommended contract rule: `UpdateResourcePolicyRequest.LinkMode = public` remains rejected by the Application service.
- If separate collection rollout is needed, expose it as config/feature flag, not a new public contract field.

## Application / Domain / Infrastructure Impact

- API: keep public anonymous routes isolated in `PublicShareLinksController`; keep all permission mutation APIs authenticated.
- Application: keep public-link orchestration in `ShareLinkService`; do not move workflow logic into controllers.
- Domain: keep `ShareLink` and `ResourceAccessPolicy` invariants generic; public-specific operational checks can remain in Application until stronger domain invariants are needed.
- Infrastructure: keep EF query implementation for public collection read model in Infrastructure.
- Architecture risk to watch: public collection read model bypasses `EffectivePermissionService` by design; this is acceptable only because it is a dedicated anonymous read model with explicit public-token checks before query execution.
- Do not make public links runtime-dependent on old services.

## Frontend Impact

- Frontend should create public links only from a share-link workflow, not from direct policy editing.
- Frontend should display public-link creation as a guarded action requiring share permission.
- Frontend should show raw token/url only at creation time.
- Frontend should never store public-link passwords after submission.
- Frontend public pages should call only `/api/v1/public/share-links/{token}/resolve`, `/document`, or `/collection`.
- Frontend should treat protected app pages as unavailable to anonymous public-link visitors.
- Public collection pages should show collection metadata and child summaries only unless future approved behavior adds child content.

## Required Tests

- Public document creation succeeds only when feature flag is enabled, role is viewer, expiry is present, expiry is within max, subject email is absent, and resource share permission is present.
- Public document read succeeds only through dedicated public route and does not authorize protected routes.
- Public collection read succeeds only through dedicated public route and returns only collection metadata and unrestricted child summaries.
- Public collection link does not allow `/document` public endpoint with a collection token.
- Public document link does not allow `/collection` public endpoint with a document token.
- Direct policy patch to `linkMode = public` remains rejected.
- Public share-link creation internally sets policy `LinkMode = public` with audit metadata that excludes secrets.
- Revoked, expired, unknown, password-missing, password-wrong, feature-flag-off, and policy-mismatch cases return not found or the established safe error.
- Password hash is stored; raw password, password proof, raw token, and token hash are not exposed in API DTOs or audit metadata.
- Public token does not broaden bootstrap, map, search, export, context, activity, comments, attachments, files, versions, or mutation APIs.
- Public collection behavior has explicit tests for archived/deleted/restricted child documents.
- If a separate collection feature flag is added, tests must cover document-enabled/collection-disabled behavior.

## Permission System Completion Plan

- Public-link conflict resolution: get explicit user approval for this architecture, then update conflict/governance docs in a separate governance round before code changes.
- Notification preferences / watched / muted persistence: define resource scope, add schema/entities/contracts/services, then connect notification filtering/delivery decisions.
- Share-link/invite notification fan-out: after preferences exist, notify relevant resource watchers/managers for share-link and invite events without notifying anonymous public users.
- Group grant fan-out: notify active group members when group grants affect access, respecting future watched/muted preferences.
- SCIM endpoint skeleton: add public SCIM 2.0 route skeleton only after clarifying auth, supported resources, and relationship to existing IAM sync.
- IAM sync relationship to SCIM: treat current `POST workspaces/{workspaceId}/iam/sync` as an admin/internal sync API; SCIM should be a protocol adapter that writes through the same Application services or shared abstractions.
- Real IdP login boundary: keep IAM mapping separate from authentication until OIDC/SAML login provider, secrets, callback routes, and user-linking rules are explicitly designed.
- MFA/recent-auth backend state: add server-side state and enforcement for high-risk permission mutations; do not rely on frontend-only flags.
- Focused tests: add tests with each implementation slice rather than one broad refactor.
- Validation commands to run later: `dotnet restore`, `dotnet build`, `dotnet test`, PostgreSQL smoke when `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is configured, and frontend build/test when frontend mutation workflow is touched.

## Recommended Implementation Order

1. Approve public-link architecture decision.
2. Update governance/conflict docs to record the approved public-link behavior.
3. Add focused tests that lock the approved public-link behavior if gaps remain.
4. Implement notification preferences / watched / muted persistence.
5. Implement share-link and invite notification fan-out using preference rules.
6. Implement group grant fan-out using preference rules.
7. Implement MFA/recent-auth backend state and enforce it on high-risk permission mutations.
8. Implement SCIM endpoint skeleton as a protocol layer over IAM sync foundations.
9. Define real IdP login as a separate auth phase.
10. Run backend validation and PostgreSQL smoke; run frontend validation when frontend workflows are changed.

## Smallest Safe Next Step

approve public-link architecture decision

## Open Questions

- Should public collection links use the existing global `Permissions:PublicShareLinks:Enabled` flag, or should collection links get a separate flag?
- Should public collection pages ever include child document content, or should they remain summary-only?
- Should public document content JSON be additionally sanitized before returning, beyond the existing rule that metadata must not be stored in Tiptap JSON?
- Should public read access be logged as separate analytics/security events?
- Which notification preference scopes are required: global, workspace, resource watched/muted, or all of them?
- Which mutations require MFA/recent-auth step-up: public link create/revoke, grants, group membership, invites, policy change, IAM sync, or all high-risk actions?
- What authentication model should SCIM use: bearer token per workspace, tenant-wide token, or future IdP-managed credentials?

## Not Run

- `dotnet restore`: not run; design-only task.
- `dotnet build`: not run; design-only task.
- `dotnet test`: not run; design-only task.
- PostgreSQL smoke: not run; design-only task.
- frontend build/test: not run; design-only task.
