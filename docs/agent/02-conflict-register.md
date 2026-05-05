# Conflict Register

This file preserves documented conflicts. Do not resolve them silently.

## Permission Public Collection Links Conflict

Status: resolved by user-approved architecture decision; frozen for permission V1

### Conflict

- Source statement A: Some docs say Phase 11 implemented public anonymous document and collection links.
- Source statement B: Current baseline says public collection links are supported.
- Source statement C: Same contract later says current backend does not yet have public anonymous collection links.
- Source statement D: `share_links` rules say public collection links are rejected/deferred.

### Approved Decision

- User approved `docs/agent/reports/public-link-architecture-decision.md`.
- Permission V1 freeze preserves this approved behavior.
- Public document links are supported.
- Public collection links are supported as collection-summary links.
- Public collection links expose collection metadata and visible child document summaries only, not child document content.
- Public anonymous access remains limited to dedicated public share-link endpoints.
- Protected APIs must not be widened by public links.

### Agent Behavior

- Preserve the approved behavior.
- Do not remove conflict history.
- Do not add child document content to public collection responses unless a later explicit decision changes this boundary.
- Do not broaden public/share-token access into protected APIs.

## Public `linkMode` Conflict

Status: resolved by user-approved architecture decision; frozen for permission V1

### Conflict

- Source statement A: API Surface says `public` link mode remains rejected.
- Source statement B: Later share-link API says public creation is allowed behind `Permissions:PublicShareLinks:Enabled = true`.

### Approved Decision

- User approved `docs/agent/reports/public-link-architecture-decision.md`.
- Permission V1 freeze preserves this approved behavior.
- Generic permission policy patch must continue rejecting direct `linkMode = public`.
- Public links are created only through share-link APIs.
- Public share-link creation may internally set resource policy `LinkMode = public`.

### Agent Behavior

- Do not manually enable general `linkMode = public` policy mutation.
- Preserve feature-flag-gated public creation behavior through dedicated share-link APIs.
- Keep dedicated public share-link workflow separate from generic policy mutation.

## Public Link Contract Source Conflict

Status: resolved by user-approved architecture decision; frozen for permission V1

### Conflict

- Source statement A: `apps/web/FRONTEND_API_CONTRACT.md` contains frontend-facing public anonymous document and collection link contract updates, including public-link create/list/revoke expectations, public password proof, and anonymous public routes.
- Source statement B: `docs/PERMISSION_SYSTEM_CONTRACT.md` contains permission-system rules for public links, public collection links, public `linkMode`, public password handling, and public endpoint isolation.
- Source statement C: Existing docs disagree or are unclear about whether public collection links are currently supported, rejected, deferred, or feature-flagged.
- Source statement D: Existing docs disagree or are unclear about whether general policy `linkMode = public` is rejected, feature-gated, or allowed only through dedicated share-link APIs.
- Source statement E: Frontend/API contract text alone is not enough to enable backend public-link behavior.
- Source statement F: Permission contract text alone is not enough to silently remove frontend public-link contract expectations.

### Approved Decision

- User approved `docs/agent/reports/public-link-architecture-decision.md`.
- Permission V1 freeze preserves this approved behavior.
- Canonical behavior for future work:
  - public links are read-only, token-scoped capabilities;
  - public document links are supported;
  - public collection links are supported as summary-only;
  - public links are created only through authenticated share-link APIs;
  - anonymous public reads use only dedicated `/api/v1/public/share-links/...` endpoints;
  - protected APIs must not be widened.

### Agent Behavior

- If a task touches public document links, public collection links, public `linkMode`, public passwords, or frontend public-link UI/API, read both `apps/web/FRONTEND_API_CONTRACT.md`, `docs/PERMISSION_SYSTEM_CONTRACT.md`, and this conflict entry.
- Do not broaden public/share-token access into bootstrap/map/search/export/list.
- Do not use files/upload, comments, or unrelated API work to resolve public-link conflicts.
- Preserve the approved behavior unless a later explicit user decision changes it.

## Public Link API Surface vs Policy Mutation Conflict

Status: resolved by user-approved architecture decision; frozen for permission V1

### Conflict

- Source statement A: Some docs describe dedicated public/share-link APIs or frontend public-link flows.
- Source statement B: Some docs reject or restrict general policy mutation using `linkMode = public`.
- Source statement C: Dedicated public-link creation may be documented separately from resource policy `linkMode`.
- Source statement D: A dedicated public-share API is not permission to enable general public policy mutation.
- Source statement E: Rejection of general `linkMode = public` is not permission to delete dedicated public-link API or frontend contract behavior.

### Approved Decision

- User approved `docs/agent/reports/public-link-architecture-decision.md`.
- Permission V1 freeze preserves this approved behavior.
- Direct policy patch to `linkMode = public` remains rejected.
- Dedicated public share-link creation may internally set policy `LinkMode = public`.

### Agent Behavior

- Keep dedicated share-link/public-link API behavior separate from resource policy `linkMode` behavior.
- Do not generalize one into the other.
- Before changing either path, identify which path the task touches:
  - dedicated share-link/public-link API
  - resource policy mutation
  - frontend public-link UI
  - public anonymous access endpoint
- Report which path was touched.

## Public Anonymous Access Boundary Conflict

Status: resolved by user-approved architecture decision; frozen for permission V1

### Conflict

- Source statement A: Public anonymous access, when documented, should use dedicated public endpoints or token flows.
- Source statement B: Authenticated effective-permission flow should not silently accept public tokens unless explicitly documented.
- Source statement C: Public/share-token access must not be broadened into bootstrap, map, search, export, list, context, activity, comments, attachments, files, or version endpoints unless explicitly documented.
- Source statement D: Frontend public-link expectations may exist, but backend security boundaries still require explicit endpoint and permission rules.

### Approved Decision

- User approved `docs/agent/reports/public-link-architecture-decision.md`.
- Permission V1 freeze preserves this approved behavior.
- Anonymous public access is allowed only under:
  - `GET /api/v1/public/share-links/{token}/resolve`
  - `GET /api/v1/public/share-links/{token}/document`
  - `GET /api/v1/public/share-links/{token}/collection`
- Public-link failures must not reveal whether a token exists, password failed, policy mismatched, or link expired/revoked.

### Agent Behavior

- Do not add anonymous access to authenticated endpoints.
- Do not let public tokens bypass effective permission checks in protected endpoints.
- Do not expose resource metadata through list/search/bootstrap/map/export because a public link exists.
- Keep public anonymous access isolated to documented public routes.
- If public file access is requested, apply this conflict and `docs/contracts/files-upload-contract.md`.

## Backend Phase vs Permission Phase Mismatch

Status: conflict-marked

### Conflict

- Source statement A: Backend phase prompts place backend at Phase 6 target after Phase 5 completion.
- Source statement B: Permission contract says permission module is implemented through Phase 11.

### Agent Behavior

- Do not silently resolve.
- Treat backend product phases and permission-module phases as separate tracks.
- Do not infer all backend Phase 6+ features exist from permission Phase 11.
- Verify local code before acting.
- If task depends on cross-track status, ask for clarification or report assumption.

## Phase 5 Scope Mismatch

Status: conflict-marked

### Conflict

- Source statement A: Refactor rules say Phase 5 includes files, comments, collaboration.
- Source statement B: Later backend prompts define Phase 5 as lifecycle/import/export and Phase 6 as files.

### Agent Behavior

- Do not silently resolve.
- Use later phase prompts for phase execution boundaries.
- Preserve conflict in reports.
- Do not move comments/collaboration into Phase 5 unless explicitly requested.

## V1/V2 Data Model Timing Mismatch

Status: conflict-marked

### Conflict

- Source statement A: Data model labels files/comments/ACL/Yjs as V2 extension tables.
- Source statement B: Later docs describe permissions/comments as implemented and files as Phase 6 target.

### Agent Behavior

- Do not silently resolve.
- Do not assume V2 labels mean never implement.
- Do not implement V2-labeled areas unless later explicit phase/contract requires them.
- For files, follow Phase 6 only.
- For comments, follow comment v1 contracts.
- For permissions, follow permission contract.

## Stable `blockId` Sequencing Conflict

Status: conflict-marked

### Conflict

- Source statement A: Initial comment spike says stable `blockId` is separate spike and not in first spike.
- Source statement B: Later block identity doc says stable `blockId` is implemented for comment v1 beta-complete.

### Agent Behavior

- Do not silently resolve.
- For current comment v1 behavior, treat `blockId` as implemented structural identity.
- Do not reinterpret original spike scope as current state.
- Do not expand `blockId` beyond textblock structural identity.

## Search Implementation Ambiguity

Status: conflict-marked

### Conflict

- Source statement A: Data model prepares PostgreSQL `tsvector`.
- Source statement B: Phase 3 allows lightweight title/text contains search.
- Source statement C: Phase 4 records current search as lightweight database contains.

### Agent Behavior

- Do not silently resolve.
- Do not upgrade search engine or introduce external search.
- Preserve current lightweight search unless explicitly tasked otherwise.
- If asked to improve search, ask whether to use PostgreSQL `tsvector`, trigram, or external search.

## Auth Implementation Choice Ambiguity

Status: conflict-marked

### Conflict

- Source statement A: Phase 4 allowed register or seed-only login.
- Source statement B: Phase 5 baseline lists register as implemented.

### Agent Behavior

- Do not silently resolve.
- Treat register endpoint as part of current stated baseline.
- Do not remove it.
- If asked to change auth mode, ask for clarification.

## README Operational State Drift Gap

Status: not verified

### Gap

- Source statement A: `services/api/README.md` may describe local setup, endpoints, auth, files, search, permissions, public links, and PostgreSQL smoke behavior.
- Source statement B: README may be closer to current code than older phase prompts, but documentation is not proof current code matches.
- Source statement C: Backend phase prompts contain historical validation snapshots and smoke gaps.
- Source statement D: If README and phase prompts differ, code/tooling verification is required before changing validation docs or project-state docs.
- Source statement E: README operational details do not override explicit architecture, data model, API, or security rules.

### Agent Behavior

- Do not treat README as code verification.
- Do not update project implementation status based only on README.
- Before changing validation commands or smoke status, inspect actual project files/scripts.
- Before claiming PostgreSQL smoke passed, verify `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is set and the smoke command actually ran.
- If README conflicts with phase prompts and code is not inspected, report `operational state not verified`.
- Do not weaken production behavior to match README or InMemory test convenience.

## PostgreSQL Smoke Documentation Gap

Status: not verified

### Gap

- Source statement A: Smoke profile exists.
- Source statement B: Latest baseline says real PostgreSQL smoke did not run because env var was unset.
- Source statement C: README may document how to run smoke.
- Source statement D: Existence of smoke profile does not mean smoke passed.
- Source statement E: InMemory tests do not replace PostgreSQL smoke.

### Agent Behavior

- Do not claim PostgreSQL smoke passed unless actually run.
- Report `PostgreSQL smoke not run: NORTHSTAR_POSTGRES_SMOKE_CONNECTION not set.` when env var is absent.
- If README lists smoke commands, still verify execution.
- Do not update validation status to passed based only on documentation.
- Do not weaken production code for InMemory tests.
