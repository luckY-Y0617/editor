# Permission System V1 Doc Freeze

## Summary

- Froze permission system V1 in governance docs as `V1 complete / ship with documented deferred items`.
- Preserved the user-approved public-link boundary and deferred non-V1 register.
- No application, backend, frontend, test, migration, package, move, delete, rename, or archive changes were made.

## Scope

- Documentation/governance only.
- Updated project state, permission skill, permission contract, and stale public-link conflict wording.
- Created this freeze report.
- Did not implement code or run validation commands.

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
- `docs/PERMISSION_SYSTEM_CONTRACT.md`
- `docs/agent/reports/permission-system-v1-release-readiness.md`

## Governance Updates

- `docs/agent/00-project-state.md` now records permission system status as `V1 complete / ship with documented deferred items`.
- `docs/agent/skills/permissions.md` now instructs future agents to treat permission V1 as frozen unless the user explicitly opens permission V2 or a targeted follow-up.
- `docs/PERMISSION_SYSTEM_CONTRACT.md` now includes a V1 frozen surface and deferred non-V1 register.
- `docs/agent/02-conflict-register.md` now clarifies that the approved public-link decision is frozen for permission V1 while preserving conflict history.

## V1 Frozen Status

- Permission system V1 is complete for release governance purposes.
- Final recommendation carried forward from the readiness review: `Ship V1 with documented deferred items`.
- Deferred items are not V1 blockers and must not be marked implemented without an explicit future task.
- Future permission work should be scoped as either a targeted post-V1 fix or a permission V2 task.

## Public-Link Frozen Boundary

- Public document links are supported.
- Public collection links are supported as summary-only.
- Public links are created only through share-link APIs.
- Generic policy patch continues to reject direct `linkMode = public`.
- Public share-link creation may internally set policy `LinkMode = public`.
- Anonymous public access remains limited to:
  - `GET /api/v1/public/share-links/{token}/resolve`
  - `GET /api/v1/public/share-links/{token}/document`
  - `GET /api/v1/public/share-links/{token}/collection`
- Protected APIs must not be widened by public links.

## Deferred Register Preserved

- Full OIDC/SAML provider redirect/callback and secret management remains deferred.
- SCIM bulk operations remain deferred.
- SCIM complex filter grammar remains deferred.
- SCIM enterprise extension remains deferred.
- SCIM delete/deactivate behavior remains deferred.
- WebAuthn/passkeys remain deferred.
- SMS/email MFA providers remain deferred.
- MFA recovery codes remain deferred.
- MFA recovery/reset/admin reset flows remain deferred.
- Advanced SCIM compatibility beyond V1.1 remains deferred.
- Browser QA after Node runtime upgrade remains deferred.
- PostgreSQL smoke from the agent process remains deferred until `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is visible.

## Validation Status Carried Forward

- Latest readiness review recorded `dotnet restore services/api/Northstar.sln`: passed.
- Latest readiness review recorded `dotnet build services/api/Northstar.sln`: passed.
- Latest readiness review recorded `dotnet test services/api/Northstar.sln`: passed.
- Latest readiness review recorded `npm run build`: passed.
- Latest readiness review recorded `npm test`: passed.
- PostgreSQL smoke was not run by agent because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was not visible.
- User previously reported PostgreSQL smoke passed manually after setting the connection string.
- Browser QA was not run because local Node runtime is too old for Browser Use.

## Files Changed

- `docs/agent/00-project-state.md`
- `docs/agent/skills/permissions.md`
- `docs/PERMISSION_SYSTEM_CONTRACT.md`
- `docs/agent/02-conflict-register.md`
- `docs/agent/reports/permission-system-v1-doc-freeze.md`

## Not Changed

- No backend code changed.
- No frontend code changed.
- No tests changed.
- No migrations changed.
- No package files changed.
- No files were moved, deleted, renamed, or archived.
- Public-link runtime behavior was not broadened.

## Remaining Non-V1 Permission Work

- full OIDC/SAML provider redirect/callback and secret management
- SCIM bulk operations
- SCIM complex filter grammar
- SCIM enterprise extension
- SCIM delete/deactivate behavior
- WebAuthn/passkeys
- SMS/email MFA providers
- MFA recovery codes
- MFA recovery/reset/admin reset flows
- advanced SCIM compatibility beyond V1.1
- Browser QA after Node runtime upgrade
- PostgreSQL smoke from agent process when env var is visible

## Smallest Safe Next Step

connect existing frontend auth UI to backend auth APIs
