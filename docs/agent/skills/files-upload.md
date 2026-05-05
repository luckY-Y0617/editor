# Skill: Files Upload

## When To Use

- Use for Phase 6 files work.
- Use for upload sessions.
- Use for file metadata.
- Use for document attachments.
- Use for file content access.
- Use for object storage.
- Use for signed URLs/streaming.
- Use for file outbox events.
- Use for Tiptap file reference validation.
- Use for file permission checks.
- Use for file delete/finalize behavior.

## Read First

- `AGENTS.md`
- `docs/contracts/files-upload-contract.md`
- `docs/agent/00-project-state.md`
- `docs/agent/01-control-rules.md`
- `docs/agent/02-conflict-register.md`
- `docs/agent/03-required-reading-order.md`
- `docs/agent/skills/backend-clean-architecture.md`
- `docs/agent/skills/backend-phase-execution.md`
- `docs/agent/skills/data-model-migrations.md`
- Phase 6 backend prompt/docs.
- File/upload docs/contracts.
- Existing API/Application/Domain/Infrastructure file-related code.
- Permission access code.
- Related tests and migrations.
- Do not guess paths.

If exact docs are unknown, search exact terms:

- `Phase 6`
- `files`
- `upload_sessions`
- `document_attachments`
- `file_outbox`
- `Tiptap file reference`
- `IObjectStorage`
- `signed URL`
- `attachment`
- `finalize`
- `idempotent`

## Current State Assumptions

- Files are Phase 6 target.
- Files must not be assumed complete unless verified in code.
- Required flow:
  ```text
  upload_sessions -> files -> document_attachments
  ```
- Upload session is the upload process source of truth.
- `files` are created only after finalize.
- Finalize must be idempotent.
- File URL is not a permanent file field.
- File access goes through API permission checks.
- Application/Domain must not depend directly on S3/MinIO/concrete storage SDKs.
- Permission phases are separate from backend mainline phases.
- Public-link conflicts must not be resolved as part of file work.

## Must Preserve

- Upload session lifecycle as source of truth.
- Finalize creates file record only after validation.
- Repeated finalize returns existing file without duplicates.
- File access requires server-side authorization.
- URLs are short-lived access outputs, not permanent file fields.
- Active attachments must not be silently broken by file deletion.
- Tiptap document JSON must not store file metadata beyond documented file references.
- Files, comments, permissions, tags, and activity must remain outside document content JSON.
- Storage provider details stay in Infrastructure.
- Application depends on abstractions, not concrete SDKs.
- Domain does not depend on storage SDKs.

## Allowed Work

- Implement documented upload-session APIs/use cases.
- Implement documented file metadata/content/delete behavior.
- Implement document attachment behavior.
- Implement file outbox behavior.
- Implement Tiptap file reference validation.
- Add documented tables/entities/configurations/migrations.
- Add Application interfaces and Infrastructure providers.
- Add focused tests for:
  - create upload session
  - complete/finalize
  - idempotent finalize
  - permission checks
  - attachment creation/removal
  - no permanent URL storage
  - outbox write
  - invalid Tiptap file references

## Forbidden Work

- Create `files` before finalize.
- Make finalize non-idempotent.
- Store permanent object-storage URLs.
- Expose raw storage paths when not required by contract.
- Bypass permission checks for file access.
- Make file access rely only on frontend/UI checks.
- Delete files in a way that silently breaks active attachments.
- Store file metadata in Tiptap JSON as source of truth.
- Introduce direct S3/MinIO SDK dependency into Domain/Application.
- Make old Go file service a runtime dependency.
- Modify `E:\ClayMo\services\file-service`.
- Broaden public/share-token access into bootstrap/map/search/export/list.
- Resolve public-link conflicts while implementing files.
- Add undocumented schema fields/tables without clarification.
- Add uncontracted public API behavior.

## Implementation Rules

### Layering

- API:
  - expose endpoints/DTOs only through Contracts
  - keep controllers thin
  - do not return EF entities
- Application:
  - orchestrate upload/file/attachment use cases
  - enforce documented workflows
  - depend on storage abstractions only
  - invoke permission checks before protected access
- Domain:
  - enforce invariants/state transitions where applicable
  - no EF/HTTP/storage SDK references
- Infrastructure:
  - EF entities/configuration/migrations
  - object storage provider implementation
  - repositories/query services
  - outbox persistence/background workers

### Upload Session Workflow

1. Create upload session with idempotency key when documented.
2. Upload content directly or through presigned target where documented.
3. Complete validates object, size, and checksum when available.
4. Finalize creates `files`.
5. Finalize marks session finalized.
6. Finalize optionally creates document attachment.
7. Finalize writes file outbox event where documented.
8. Repeated finalize returns existing file without duplicates.

### Access Rules

- File metadata/content/list/delete/attachment endpoints must enforce effective access.
- Access to file content must produce short-lived signed URL or stream according to contract.
- Do not store generated access URL as permanent file record.
- List/search/export/context/activity/comments/attachments/files/version endpoints must enforce same effective access rules when relevant.

### Tiptap File References

- Tiptap content may reference files only according to documented contract.
- Tiptap content must not become source of truth for file metadata.
- Invalid file references must be rejected or reported according to documented workflow.
- Comments, tags, permissions, activity, and files metadata must remain outside document JSON.

## Validation

- Run backend validation:
  - `dotnet restore`
  - `dotnet build`
  - `dotnet test`
- Run focused tests for file workflows if available.
- For schema changes, run migration validation from `docs/agent/skills/data-model-migrations.md`.
- PostgreSQL smoke only if env var is set and command actually runs.
- Report if storage provider env/config is missing.
- Do not claim object-storage integration passed unless actually tested.
- Do not weaken production behavior for InMemory tests.

## Final Report Notes

- State phase affected: Phase 6 files.
- List file workflow parts changed:
  - upload sessions
  - files
  - attachments
  - access
  - outbox
  - Tiptap validation
- List APIs changed or `None`.
- List migrations changed or `None`.
- State permission checks added/verified or `not verified`.
- State storage provider behavior changed or `None`.
- List tests run/not run.
- State PostgreSQL smoke status.
- State object storage integration status.
- State conflict-marked public-link behavior touched or `None`.
