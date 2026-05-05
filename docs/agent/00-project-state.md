# Project State

This file is the project fact layer for future agents. It records documented state. It is not proof that current code still matches documentation.

## Project Identity

- Project: Northstar / Northstar Atlas Library.
- Product type: workspace-based knowledge editor/library.
- Editor content: rich-text Tiptap document content.
- Backend rebuild target: `services/api`.

## Backend Architecture

- Architecture: ASP.NET Core Modular Monolith + Clean Architecture.
- Projects:
  - `Northstar.Api`
  - `Northstar.Application`
  - `Northstar.Domain`
  - `Northstar.Infrastructure`
  - `Northstar.Contracts`
- Dependency direction:
  - `Api -> Application / Contracts / Infrastructure`
  - `Application -> Domain / Contracts`
  - `Infrastructure -> Application / Domain / Contracts`
  - `Domain -> no project dependency`

## Runtime Shape

- Runtime flow:
  - Frontend -> Northstar.Api -> PostgreSQL -> Object Storage -> Redis optional.
- Old Go file service at `E:\ClayMo\services\file-service` is not a runtime dependency.
- Old `services/api-old` is read-only reference only.

## Current Backend Mainline State

- Status: documented baseline.
- Backend mainline: Phase 5 completed.
- Current or next target: Phase 6.
- Phase 6 scope:
  - upload sessions
  - files
  - document attachments
  - private access
  - file outbox
  - Tiptap file reference validation

## Phase 5 Validation State

- Status: documented baseline, not blindly verified current code.
- `dotnet restore`: passed.
- `dotnet build`: passed.
- `dotnet test`: passed.
- Test count: 37 tests passed.
- Migration script: generated.
- Pending model changes: none.
- PostgreSQL smoke profile: exists.
- Real PostgreSQL smoke: not run unless `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was set and the smoke command was actually executed.

## Completed Backend Baseline APIs

Status: documented baseline, not blindly verified current code.

- `GET /api/v1/bootstrap`
- `GET /api/v1/spaces/{spaceId}/map`
- `POST /api/v1/documents`
- `GET /api/v1/documents/{documentId}`
- `PATCH /api/v1/documents/{documentId}`
- `PATCH /api/v1/documents/{documentId}/location`
- `PATCH /api/v1/documents/{documentId}/archive`
- `PATCH /api/v1/documents/{documentId}/restore`
- `DELETE /api/v1/documents/{documentId}`
- `GET /api/v1/documents/{documentId}/context`
- `GET /api/v1/documents/{documentId}/activity`
- `GET /api/v1/search?q=&spaceId=`
- `GET /api/v1/spaces/{spaceId}/export`
- `POST /api/v1/spaces/{spaceId}/import`
- Auth:
  - register
  - login
  - refresh
  - logout
  - me
- Workspace members:
  - list
  - add
  - update
  - delete

## Comments State

- Status: `comment v1 / beta-complete`.
- Backend-backed persistence exists.
- Comment-specific APIs exist in contract.
- Stable block identity implemented.
- Load-time relocation implemented.
- Production hardening implemented for comment v1.
- Frontend comment regression suite passed with 73 tests at version definition time.

Explicitly not included in comment v1:

- cross-revision anchor rewriting
- backend anchor rewriting
- collaboration/CRDT/OT relocation
- image/table/node comments
- mentions
- notifications
- fine-grained permissions
- audit timeline
- edit/delete comments
- advanced overlap picking UI
- automated browser E2E harness

## Permission State

- Status: `V1 complete / ship with documented deferred items`.
- Permission system V1 is frozen by `docs/agent/reports/permission-system-v1-doc-freeze.md`.
- Future agents must not reopen permission V1 scope unless the user explicitly starts a permission V2 or targeted follow-up task.
- Permission module phases are separate from backend mainline phases.
- Public-link architecture decision approved:
  - public links are read-only token-scoped capabilities;
  - public document links are supported;
  - public collection links are supported as summary-only;
  - public links are created only through share-link APIs;
  - generic policy patch still rejects direct `linkMode = public`;
  - public share-link creation may internally set resource policy `LinkMode = public`;
  - anonymous public access is limited to dedicated `/api/v1/public/share-links/...` endpoints;
  - protected APIs must not be widened by public links.
- Implemented according to contract:
  - workspace RBAC
  - scoped document/collection permissions
  - groups
  - access requests
  - notifications
  - temporary grants
  - share links
  - IAM sync foundations
  - external authenticated links
  - email invites
  - public document links
  - public collection links / public link passwords where documented
  - notification preferences / watched-muted persistence
  - share-link and invite in-app notification fan-out
  - group grant notification fan-out
  - workspace-scoped SCIM endpoint skeleton
  - MFA/recent-auth backend state foundation
  - real IdP login boundary
  - production invite delivery provider boundary
  - dedicated SCIM bearer-token validation
  - minimal SCIM User/Group provisioning V1
  - SCIM Provisioning Compatibility Hardening V1.1
  - secure invite outbox/retry delivery
  - frontend permission mutation workflow V1 for document resource grants and policy settings
  - frontend permission admin surfaces V1 for workspace members and SCIM management
  - frontend public-link interaction hardening V1 for document public links
  - backend TOTP MFA provider/enrollment/verification/disable flow
  - backend step-up enforcement for high-risk permission mutations
- Conflict status:
  - public collection links and public `linkMode` source conflicts are resolved by approved architecture decision in `docs/agent/reports/public-link-architecture-decision.md`;
  - conflict history remains preserved in `docs/agent/02-conflict-register.md`.
- Latest release-readiness validation was recorded in `docs/agent/reports/permission-system-v1-release-readiness.md`:
  - `dotnet restore services/api/Northstar.sln`: passed;
  - `dotnet build services/api/Northstar.sln`: passed;
  - `dotnet test services/api/Northstar.sln`: passed;
  - `npm run build`: passed;
  - `npm test`: passed.
- Validation caveats:
  - PostgreSQL smoke was not run by agent because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was not visible;
  - user previously reported PostgreSQL smoke passed manually after setting the connection string;
  - Browser QA was not run because local Node runtime is too old for Browser Use.
- Deferred non-V1 permission work:
  - SCIM bulk operations, complex filter grammar, enterprise extension, delete/deactivate behavior, and broader compatibility beyond V1.1
  - full OIDC/SAML provider redirect/callback and secret management
  - WebAuthn/passkeys
  - SMS/email MFA providers
  - MFA recovery codes
  - MFA recovery/reset/admin reset flows
  - PostgreSQL smoke from the agent process when the env var is visible
  - Browser QA / frontend public-link browser acceptance after Node runtime upgrade

## Files State

- Status: Phase 6 target.
- Completion status: not assumed complete unless verified in code.
- Required flow:
  ```text
  upload_sessions -> files -> document_attachments
  ```
- Upload session is source of truth.
- `files` are created only after finalize.
- Finalize is idempotent.
- File URL is not a permanent file field.
- File access goes through API permission checks.
