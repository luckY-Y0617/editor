# Control Rules

This file is the strict control layer for future agents.

## Rule Hierarchy

1. Architecture Rules
2. Data Model Rules
3. API Contracts
4. Workflows
5. Suggestions

Rules:

- Architecture overrides everything lower.
- Data model overrides API contracts, workflows, and suggestions.
- API contracts override workflows and suggestions.
- Workflows override suggestions.
- Suggestions never override hard rules.
- If a document declares itself canonical for a domain, use it as the highest source inside that domain unless it conflicts with a higher-level rule.

## Hard Constraints

### Architecture

- Use ASP.NET Core Modular Monolith + Clean Architecture.
- Keep `services/api` as new backend root.
- Use `Northstar` naming for new backend projects and namespaces.
- Preserve dependency direction:
  - `Api -> Application / Contracts / Infrastructure`
  - `Application -> Domain / Contracts`
  - `Infrastructure -> Application / Domain / Contracts`
  - `Domain -> no project dependency`
- Domain must not reference ASP.NET Core, EF Core, database implementation, HTTP, cache, file SDKs, or external infrastructure SDKs.
- Application must not directly depend on ASP.NET Controller, `HttpContext`, or concrete object-storage SDKs.
- Controllers must remain thin.
- Business flows belong in Application.
- Business invariants and state transitions belong in Domain.
- EF Core, migrations, repository/query implementations, storage providers, and background workers belong in Infrastructure.
- DTOs and public contracts belong in Contracts.

### Old Project Boundaries

- `services/api-old` is read-only reference only.
- `E:\ClayMo\services\file-service` is read-only reference only.
- Do not modify old projects.
- Do not make old Go file-service a runtime dependency.
- Do not migrate old architecture.

### Forbidden Technology / Structure

- Do not introduce ABP.
- Do not use ABP modules.
- Do not use ABP `ApplicationService`.
- Do not use ABP repositories or UnitOfWork.
- Do not use old SqlSugar/ABP wrappers.
- Do not use old `ClayMo.*`, `NS.Abp`, or `Module.*`.
- Do not recreate old `framework/modules/src` layout.
- Do not split into microservices at the current architecture stage.

### Data Model

- PostgreSQL is the default database.
- EF Core migrations are required for schema changes.
- Core business tables carry `workspace_id`.
- Use one PostgreSQL database with multi-workspace shared tables unless docs explicitly change this.
- Do not use tenant-per-database, dynamic connection strings, or ABP Tenant as main path.
- Tiptap document body is stored as JSON/JSONB.
- Do not store HTML as sole source of truth.
- Separate document metadata and body:
  - `documents`
  - `document_drafts`
- Tags, links, activity, permissions, files, and comments must not be embedded into document content JSON.
- `documents.revision` is autosave optimistic locking only.
- `document_versions` are immutable snapshots.
- Do not conflate `revision`, `version_no`, and user-facing version labels.
- Do not build `document_blocks` in V1 unless docs explicitly change boundary.
- Use `collections` in database even if frontend calls them folders.

### API

- API base path is `/api/v1`.
- Do not mix `/api/app` with `/api/v1`.
- Do not expose EF entities through controllers.
- Do not create temporary endpoints outside API contract.
- Preserve error response shape:
  ```json
  {
    "error": {
      "code": "CONFLICT",
      "message": "Document revision conflict.",
      "details": {}
    }
  }
  ```
- `/api/v1` must keep the Northstar error envelope above. Do not switch `/api/v1` to RFC 9457 Problem Details (`application/problem+json`) unless the user explicitly approves a versioned API contract change.
- Automatic model binding/model validation errors must be mapped into the Northstar error envelope, typically with `code = VALIDATION_ERROR` and field details under `error.details`.
- Problem Details may be reconsidered only for a future versioned public API or explicit media-type negotiation plan; do not mix Problem Details and the Northstar envelope inside `/api/v1`.
- Standard error codes:
  - `VALIDATION_ERROR`
  - `NOT_FOUND`
  - `CONFLICT`
  - `UNAUTHORIZED`
  - `FORBIDDEN`
  - `INTERNAL_ERROR`

### Comments

- Comments are external annotation resources.
- Do not persist comments through `PATCH /documents/:id`.
- Do not store comment marks in Tiptap JSON.
- Do not store comment nodes in Tiptap JSON.
- Do not store `threadId`, runtime ranges, `runtimeMatch`, relocation results, anchor status, active thread, or composer state in document JSON.
- Runtime mapped ranges are plugin state only.
- Relocation output is runtime-only and must not be persisted.
- `blockId` is structural identity only.
- `blockId` may appear only on ProseMirror textblock nodes.
- `blockId` must not encode document id, position, text, comment id, or thread id.

### Files

- Upload flow must follow:
  ```text
  upload_sessions -> files -> document_attachments
  ```
- Upload session is source of truth.
- `files` may be created only after finalize.
- Finalize must be idempotent.
- File URLs are not permanent file fields.
- File access must go through API permission checks.
- Deleting a file must not silently break active attachments.
- Application and Domain must not depend directly on S3/MinIO SDKs.

### Permissions

- Every protected query and mutation must be authorized server-side.
- UI checks are never security boundaries.
- Never trust client-supplied role, permission, workspace id, resource id, subject id, or effective access.
- Authorization is evaluated from persisted server state at request time.
- Use central effective permission service.
- Controllers must not reimplement permission ranking or effective permission logic.
- Expired grants are ignored.
- Revoked grants are ignored and retained for audit.
- Last workspace owner cannot be removed, downgraded, suspended, expired, or revoked.
- Permission mutations and audit writes occur in the same transaction.
- Share link and invite tokens are high-entropy and hashed at rest.
- Raw tokens are returned only once at create time.
- Raw tokens, token hashes, passwords, provider secrets, SMTP secrets, SAML/OIDC tokens, and accept URLs must not be written to audit metadata.
- Search, export, context, activity, comments, attachments, files, and version endpoints must enforce the same effective access rules.

## Forbidden Actions

The agent must not:

- Modify project architecture.
- Replace Clean Architecture.
- Introduce microservices.
- Introduce ABP.
- Reuse old ABP/SqlSugar infrastructure.
- Rename new backend away from `Northstar`.
- Move business logic into controllers.
- Move EF/database logic into Domain.
- Make Application depend on HTTP infrastructure.
- Add controller-local authorization logic.
- Add ad hoc role checks outside permission catalog/effective service.
- Modify `services/api-old`.
- Modify `E:\ClayMo\services\file-service`.
- Treat old file-service as runtime dependency.
- Copy Go/go-zero structure into new backend.
- Copy old MySQL schema directly.
- Copy old knowledge-base schema directly.
- Change API base path.
- Add uncontracted public API behavior.
- Return EF entities from API.
- Change DTOs without respecting Contracts ownership.
- Change error response shape.
- Merge `revision` and version semantics.
- Store autosaves as document versions unless workflow says so.
- Store comments, tags, links, activity, permissions, or files inside document JSON.
- Store permanent file URLs.
- Create `files` before upload finalize.
- Make upload finalize non-idempotent.
- Delete files in a way that silently breaks active document attachments.
- Persist runtime comment relocation output.
- Persist ProseMirror `DecorationSet`.
- Persist runtime mapped ranges.
- Treat `pm.from/to` as permanent coordinates.
- Put `blockId` on non-textblock JSON wrapper/container nodes.
- Use `blockId` as comment metadata.
- Broaden public/share-token access into bootstrap/map/search/export/list.
- Let external links or email invites create workspace membership implicitly.
- Expose raw share-link or invite tokens after create.
- Store public-link passwords in plaintext.
- Implement deferred capabilities as if real.
- Mark unavailable UI features as persistent or working.
- Resolve documentation conflicts silently.
- Optimize workflows by reordering, merging, skipping, or simplifying steps.
- Introduce new abstractions unless docs explicitly allow or existing architecture requires them.
- Add schema fields or tables outside documented model without clarification.
- Change permission role meanings without updating catalog and tests.
- Add `commenter` to `workspace_members.role` before all documented prerequisites exist.
- Implement MFA/recent-auth enforcement using frontend-only flags.
- Add public SCIM, production SMTP, notification preferences, or fan-out behavior unless docs explicitly move them out of deferred status.

## Allowed Actions

The agent may:

- Implement behavior explicitly described by docs.
- Add code inside existing Clean Architecture layers.
- Add DTOs under Contracts when API contract requires them.
- Add EF entities, configurations, and migrations when documented data model requires them.
- Add Application services/interfaces when they match documented use cases.
- Add Infrastructure implementations behind existing or documented interfaces.
- Add focused tests required by current phase or contract.
- Fix bugs that violate documented behavior.
- Refactor within a layer only if:
  - public behavior does not change;
  - API contracts do not change;
  - schema does not change;
  - workflows remain intact;
  - dependencies remain valid;
  - no new architecture is introduced.
- Add validation enforcing documented constraints.
- Add comments only to clarify non-obvious documented behavior.
- Update docs/contracts only when implementation changes are explicitly required by source docs.
- Preserve messy or legacy-compatible behavior when docs state it.
- Keep stubs only when a phase explicitly allows them and mark them as phase stubs.

## Hard Stop vs Proceed With Safest Default

### Hard Stop

Stop before changing code when:

- Requested change violates architecture.
- Requested change requires modifying old projects.
- Requested change requires ABP, SqlSugar wrappers, old project naming, or old module layout.
- Requested change changes dependency direction.
- Requested change stores non-document data inside Tiptap content JSON.
- Requested change changes API base path or error response format.
- Requested change requires schema changes not present in documented model.
- Requested change alters permission role meanings.
- Requested change enables capability explicitly marked deferred.
- Requested change touches conflict-marked behavior and correct behavior is not explicitly selected.
- Requested change could expose raw tokens, secrets, passwords, or inaccessible resource metadata.
- Requested change would make tests pass by weakening production behavior.
- Requested change requires external infrastructure not configured in docs.
- Requested change would optimize or simplify a documented workflow.

### Proceed With Safest Default + Report

Proceed only if safe, and report the assumption, when:

- PostgreSQL smoke cannot run because env var is absent.
- Docs and code differ slightly but current task does not require choosing a conflicted behavior.
- A documented feature is stated as baseline but code must still be verified.
- A test cannot run due to missing local dependency.
- A task can be completed without touching conflict-marked areas.
