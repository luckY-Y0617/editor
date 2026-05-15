# Skill: Permissions

## When To Use

- Use for workspace RBAC.
- Use for roles, permissions, and catalog.
- Use for effective permission service.
- Use for resource policies/grants and group grants.
- Use for share links, public links, email invites, and external authenticated links.
- Use for link-management inventory, share-link access stats, share-link access audit, and copy/reveal/token-handling rules.
- Use for access requests.
- Use for permission notifications and audit events.
- Use for token generation, storage, and display.
- Use for authorization on list/search/export/context/activity/comments/attachments/files/version endpoints.
- Use for any protected backend query or mutation.

## Read First

- `AGENTS.md`
- `docs/agent/00-project-state.md`
- `docs/agent/01-control-rules.md`
- `docs/agent/02-conflict-register.md`
- `docs/agent/skills/backend-clean-architecture.md`
- `docs/agent/skills/api-contracts.md`
- `docs/agent/skills/data-model-migrations.md`
- Permission system contract/docs.
- Effective permission service implementation.
- Permission API/DTO/contracts.
- Permission tests.
- Audit/token/share-link handling code.
- Do not guess paths.

If exact docs are unknown, search exact terms:

- `permission contract`
- `effective permission`
- `workspace role`
- `resource_access_policies`
- `resource_access_grants`
- `share_links`
- `resource_email_invites`
- `access_requests`
- `permission_audit_events`
- `public link`
- `linkMode`
- `last owner`
- `raw token`
- `token hash`

## Current State Assumptions

- Permission system status is frozen as `V1 complete / ship with documented deferred items`.
- Future agents must treat permission V1 as closed unless the user explicitly opens a permission V2 task or a targeted follow-up.
- The V1 freeze is recorded in `docs/agent/reports/permission-system-v1-doc-freeze.md`.
- Permission contract states implementation through MFA provider step-up enforcement V1.
- Permission module phases are separate from backend mainline phases.
- Permission status is documented baseline, not proof current code matches.
- Public-link architecture is user-approved in `docs/agent/reports/public-link-architecture-decision.md`.
- Public collection links are supported as summary-only public links.
- Public `linkMode` remains rejected through generic policy patch; dedicated public share-link creation may internally set policy `LinkMode = public`.
- Frozen public-link behavior must remain:
  - public links are created only through share-link APIs;
  - generic policy patch rejects direct `linkMode = public`;
  - anonymous public access uses only dedicated public share-link endpoints;
  - public collection links are summary-only;
  - protected APIs must not be widened.
- Link-management follow-up rules are documented in `docs/PERMISSION_SYSTEM_CONTRACT.md` and `docs/agent/reports/link-management-product-rules-v1.md`. They are target rules, not implementation proof.
- Under current token rules, raw share-link tokens are returned only once at create time. Existing link list/detail views must not reveal or reconstruct token-bearing URLs.
- Notification preferences / watched-muted persistence, share-link/invite in-app notification fan-out, group grant notification fan-out, the workspace-scoped SCIM endpoint skeleton, the MFA/recent-auth backend state foundation, the real IdP login boundary, the production invite delivery provider boundary, dedicated SCIM bearer-token validation, minimal SCIM User/Group provisioning V1, SCIM Provisioning Compatibility Hardening V1.1, secure invite outbox/retry delivery, frontend permission mutation workflow V1 for document resource grants/policy settings, frontend permission admin surfaces V1 for workspace members and SCIM management, frontend public-link interaction hardening V1 for document public links, and backend TOTP MFA provider/enrollment/step-up enforcement are implemented.
- Deferred/missing according to docs include SCIM bulk/complex-filter/enterprise/delete-deactivate behavior and broader compatibility beyond V1.1, full OIDC/SAML provider redirect/callback and secret management, WebAuthn/passkeys, SMS/email MFA providers, MFA recovery codes, and MFA recovery/reset/admin reset flows.
- Deferred items are not V1 blockers and must not be marked implemented without an explicit future task.
- Code must be inspected before changing permission behavior.

## Must Preserve

- Every protected query/mutation is authorized server-side.
- UI checks are not security boundaries.
- Never trust client-supplied role, permission, workspace id, resource id, subject id, or effective access.
- Authorization is evaluated from persisted server state at request time.
- Use central effective permission service.
- Controllers must not reimplement permission ranking/effective logic.
- Expired grants are ignored.
- Revoked grants are ignored and retained for audit.
- Last workspace owner protection.
- Permission mutation and audit write in same transaction.
- Share/invite tokens high-entropy and hashed at rest.
- Raw tokens returned only once at create time.
- Secrets, token hashes, passwords, provider secrets, SAML/OIDC tokens, and accept URLs not written to audit metadata.
- Same effective access rules for search/export/context/activity/comments/attachments/files/version endpoints.
- Public anonymous share links use dedicated public endpoints outside authenticated effective-permission flow.
- Public anonymous access is limited to dedicated `/api/v1/public/share-links/{token}/resolve`, `/document`, and `/collection` endpoints.
- Public links must not widen bootstrap/map/search/export/list/context/activity/comments/attachments/files/version/mutation/permission APIs.

## Allowed Work

- Implement or fix explicitly documented permission behavior.
- Add/update permission catalog entries when documented behavior requires it.
- Add/update central effective permission service logic.
- Add/update resource policies/grants/group grants according to contract.
- Add/update audit writes in same transaction as permission mutations.
- Add tests for authorization, last-owner protection, revoked/expired grants, token handling, and public-link conflict-safe behavior.
- Add schema changes only when documented and validated through data-model skill.

## Forbidden Work

- Add ad hoc role checks in controllers.
- Trust frontend/UI checks as security boundary.
- Trust client-supplied effective access.
- Expose raw tokens after create.
- Store token hashes or passwords in plaintext/audit metadata.
- Let external links or email invites create workspace membership implicitly.
- Broaden public/share-token access into bootstrap/map/search/export/list.
- Silently enable public collection links.
- Silently enable general `linkMode = public`.
- Implement deferred capabilities as real without explicit instruction.
- Implement MFA/recent-auth through frontend-only flags.
- Add broader SCIM compatibility behavior beyond the approved/current slice, full OIDC/SAML provider integration, additional MFA provider/recovery flows, or additional production delivery providers unless docs explicitly move them into scope.
- Change role meanings without updating catalog and tests.
- Remove, downgrade, suspend, expire, or revoke last owner.
- Weaken authorization to pass tests.
- Resolve permission conflicts silently.

## Implementation Rules

### Authorization Flow

- Require authenticated user for authenticated protected endpoints.
- Load active workspace membership.
- Verify resource belongs to workspace.
- Build parent chain when relevant.
- Load policies, direct grants, and group grants.
- Ignore revoked/expired grants.
- Apply inherit/restricted behavior according to contract.
- Apply valid internal/external share-link or accepted-invite rules only where documented.
- Ignore public tokens in authenticated path.
- Select highest ranked role through central service.
- Check permission catalog action.
- Enforce management-role limits.

### Token Handling

- Generate high-entropy raw token only at creation.
- Store only hash at rest.
- Return raw token only once.
- Never log or audit raw token/hash/password/secret.
- Do not reconstruct accept URLs from stored raw tokens.

### Audit

- Permission mutation and audit write must be same transaction.
- Audit metadata must exclude raw tokens, token hashes, passwords, provider secrets, SMTP secrets, SAML/OIDC tokens, and accept URLs.
- Revoked grants retained for audit.

### Conflict Handling

- Before touching public collection links or public `linkMode`, read `docs/agent/02-conflict-register.md`.
- Preserve the approved public-link behavior unless a later explicit user decision changes it.
- Do not use file/comment/API work to resolve permission conflicts.

## Validation

- Run backend validation where applicable:
  - `dotnet restore`
  - `dotnet build`
  - `dotnet test`
- Run focused permission tests when available.
- Add tests for security-sensitive changes.
- For schema changes, use `docs/agent/skills/data-model-migrations.md`.
- PostgreSQL smoke only if env var is set and command actually runs.
- Do not claim invite delivery/provider integration unless actually tested.
- Do not weaken production authorization for tests.

## Final Report Notes

- List permission areas changed.
- State authorization impact.
- State token handling impact.
- State audit impact.
- State public-link conflict area touched or `None`.
- List role/catalog changes or `None`.
- List migrations changed or `None`.
- List tests run/not run.
- State deferred capabilities touched or `None`.
