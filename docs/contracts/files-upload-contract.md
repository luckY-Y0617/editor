# Files / Upload Contract

This file is the canonical contract for Northstar files/upload behavior.

It defines intended behavior and boundaries.
It is not proof that current code implements every item.
Future agents must inspect code before changing implementation.

## Status

- Current status: `documented contract / not code-verified`.
- Backend mainline: Phase 5 completed.
- Files/upload: Phase 6 target.
- Files must not be assumed complete unless verified in code.
- PostgreSQL smoke is not considered passed unless `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is set and the smoke command actually runs.
- Object storage integration must not be claimed passed unless actually tested.

## Source Inputs

| Source | Role | Status |
|---|---|---|
| `docs/BACKEND_PHASE6_PROMPT.md` | Phase 6 files/upload target, workflows, APIs, validation | source read |
| `docs/BACKEND_DATA_MODEL_V1.md` | Files/upload schema tables and data-model boundaries | source read |
| `apps/web/FRONTEND_API_CONTRACT.md` | Frontend-facing Phase 6 file API, DTO, Tiptap, import/export contract | source read |
| `services/api/README.md` | Operational file notes, endpoints, permissions, smoke profile | source read |
| `docs/agent/skills/files-upload.md` | Agent files/upload control rules | source read |
| `docs/agent/skills/data-model-migrations.md` | Agent data model and migration control rules | source read |
| `docs/agent/skills/api-contracts.md` | Agent API contract control rules | source read |
| `docs/agent/skills/permissions.md` | Agent permission control rules | source read |

## Ownership and Scope

- Backend root: `services/api`.
- Architecture: ASP.NET Core Modular Monolith + Clean Architecture.
- API owns request/response boundary.
- Application owns upload/file/attachment orchestration.
- Domain owns invariants/state transitions where applicable.
- Infrastructure owns EF, object storage provider implementation, repositories, outbox persistence/background workers.
- Contracts owns DTOs and public API contracts.
- Frontend may reference file IDs/attachment IDs according to contract, but must not be source of truth for file metadata.
- Old `services/api-old` is read-only reference only.
- Old Go file service at `E:\ClayMo\services\file-service` is read-only reference only.
- Old Go file service is not a runtime dependency.

## Non-Goals

- Do not implement code here.
- Do not define old Go file-service integration.
- Do not store permanent object-storage URLs as file records.
- Do not place file metadata as source of truth in Tiptap JSON.
- Do not define public-link behavior beyond existing conflict register.
- Do not implement production provider details unless separately configured.
- Do not change permission public-link conflicts.
- Do not define unrelated comments/permissions/search behavior except where required for file access.

## Data Model Contract

- Required flow:
  ```text
  upload_sessions -> files -> document_attachments
  ```
- Upload session is upload process source of truth.
- `files` record is created only after finalize succeeds.
- `document_attachments` links documents to finalized files.
- Core business tables must carry `workspace_id`.
- File URL is not a permanent file field.
- Tiptap content may reference file IDs only according to contract.
- Tiptap content is not source of truth for file metadata.
- Files, comments, permissions, tags, links, and activity remain outside document content JSON.
- Schema changes require EF Core migrations.
- PostgreSQL is default database.
- Add only documented tables/fields/indexes.
- Do not copy old MySQL/file-service schema directly.

### Known Tables

- `files`
  - `id`
  - `workspace_id`
  - `uploaded_by`
  - `storage_provider`
  - `bucket`
  - `object_key`
  - `original_filename`
  - `mime_type`
  - `byte_size`
  - `checksum_sha256`
  - `width`
  - `height`
  - `metadata`
  - `created_at`
  - `deleted_at`
  - Phase 6 adds or preserves `scan_status` and `processing_status` semantics.
- `upload_sessions`
  - `id`
  - `workspace_id`
  - `owner_id`
  - `idempotency_key`
  - `original_filename`
  - `mime_type`
  - `byte_size`
  - `checksum_sha256`
  - `biz_type`
  - `storage_provider`
  - `bucket`
  - `object_key`
  - `upload_mode`
  - `multipart_upload_id`
  - `chunk_size`
  - `total_parts`
  - `status`
  - `finalized_file_id`
  - `expires_at`
  - `finalized_at`
  - `created_at`
  - `updated_at`
- `document_attachments`
  - `id`
  - `workspace_id`
  - `document_id`
  - `file_id`
  - `relation_type`
  - `metadata`
  - `created_by`
  - `created_at`
  - `relation_type` values: `attachment`, `inline_image`, `cover`.
- `file_outbox_events`
  - `id`
  - `workspace_id`
  - `aggregate_type`
  - `aggregate_id`
  - `event_type`
  - `payload`
  - `headers`
  - `status`
  - `retry_count`
  - `next_retry_at`
  - `last_error`
  - `created_at`
  - `updated_at`

Exact columns must be verified from data model and current EF code before implementation.

## Upload Session Contract

- Upload session represents upload process.
- Create upload session may use idempotency key where documented.
- Upload content may be direct or via presigned target where documented.
- Session must track enough data to validate finalize.
- Upload session lifecycle must prevent duplicate file creation.
- Repeated create/finalize behavior must be idempotent where documented.
- Incomplete/failed sessions must not create `files`.
- Documented session states include `initiated`, `uploading`, `completed`, `aborted`, `expired`, `failed`, and `finalized`.
- Phase 6 source docs state `single` upload is supported; multipart is reserved or returns documented validation behavior when disabled.

## File Finalize Contract

Workflow:

1. validate upload session exists and belongs to workspace/user/resource context
2. validate object/content exists where applicable
3. validate size and checksum when available
4. validate documented content type/metadata constraints if present
5. create `files` only after validation succeeds
6. mark session finalized
7. optionally create `document_attachments` when requested and authorized
8. write file outbox event where documented
9. repeated finalize returns existing file without duplicate `files`

- Finalize must be transactional where database state changes must stay consistent.
- Finalize must not create permanent public URL.
- Failed finalize must not leave inconsistent file/attachment state.
- Repeated finalize must not duplicate attachments or outbox events where documented.

## Document Attachment Contract

- Attachments link documents and finalized files.
- Attachment creation requires document access permission.
- Attachment listing requires document/file access.
- Deleting/removing attachment must not silently delete underlying file unless explicitly documented.
- Deleting a file must not silently break active attachments.
- Attachment state must not be stored as document JSON source of truth.
- Export/import behavior must follow file import/export boundary.
- Attachment relation types documented for Phase 6 are `attachment`, `inline_image`, and `cover`.

## File Access Contract

- File metadata/content/list/delete endpoints are protected unless explicitly public by a documented public endpoint.
- Access must go through server-side authorization.
- UI checks are not security boundaries.
- Access to file content returns either:
  - short-lived signed URL, or
  - streamed content
  according to contract.
- Generated access URLs are not stored as permanent file fields.
- Raw object storage paths must not be exposed unless explicitly required by contract.
- Endpoint behavior must preserve `/api/v1` and standard error shape.

## Object Storage Boundary

- Application and Domain must not depend directly on S3/MinIO/concrete storage SDKs.
- Application depends on storage abstractions only.
- Infrastructure implements object storage provider.
- Storage provider details must not leak into Domain.
- Object storage integration status must be reported as not tested unless actually tested.
- Missing object storage env/config must be reported, not hidden.
- Documented local configuration uses a relative `LocalRootPath`; production provider details are not defined by this contract.

## Permission Contract

- Every protected file query/mutation must enforce server-side effective access.
- Use central effective permission service.
- Do not implement ad hoc controller-local role checks.
- File access must respect workspace/resource scope.
- List/search/export/context/activity/comments/attachments/files/version endpoints must enforce same effective access rules when relevant.
- Public/share-link behavior must follow `docs/agent/02-conflict-register.md`.
- Do not broaden public/share-token access into bootstrap/map/search/export/list.
- Do not resolve public collection links or public `linkMode` conflicts in file work.

## Tiptap File Reference Contract

- Tiptap JSON may contain documented file references only.
- Tiptap JSON must not store file metadata as source of truth.
- Tiptap file references must be validated against finalized files and authorized attachments according to contract.
- Invalid references must be rejected or reported according to documented workflow.
- Comments/tags/permissions/activity/files metadata must remain outside document JSON.
- File reference validation must not mutate unrelated document content.
- Documented internal references include image `attrs.fileId`, image `attrs.src` containing `/api/v1/files/{fileId}/content`, attachment/file node `attrs.fileId`, and link `attrs.href` containing `/api/v1/files/{fileId}/content`.

## Import / Export Boundary

- Space/document export may include file references according to documented contract.
- Export must not leak private file content or storage paths unless explicitly documented and authorized.
- Import must not reuse old IDs blindly.
- Import must map old file/document/attachment references to new IDs where documented.
- Import should preserve contract boundaries and fail transactionally on validation failure where documented.
- If file binary transfer is out of scope, report it as out of scope rather than inventing behavior.
- Phase 6 sources state file binaries are not packaged in export/import and imports must not forge `files` rows from source-environment file IDs.

## File Outbox Contract

- File-related outbox events are documented Phase 6 target.
- Outbox writes must occur when documented workflow requires them.
- Outbox must not contain raw secrets, raw tokens, permanent public URLs, or provider secrets.
- Outbox event creation must be consistent with finalize/delete/attachment workflows.
- If background processing is not implemented, mark as documented target or not verified.
- Documented event names include `file.finalized`, `document_attachment.created`, and `file.deleted`.

## Delete / Retention Safety

- Deleting a file must not silently break active document attachments.
- Delete behavior must check active attachments.
- Soft delete vs hard delete must follow documented contract or require clarification.
- Object deletion must not occur before database state is safe.
- If retention/garbage collection is not defined, do not invent it.
- Report undefined retention behavior as not specified.

## API Contract Summary

| Category | Expected behavior | Route source | Status |
|---|---|---|---|
| create upload session | Create upload session with documented idempotency and upload target behavior. | `POST /api/v1/files/uploads/sessions` | documented |
| upload content / presigned target | Upload raw content through local/API-proxy path when documented. | `PUT /api/v1/files/uploads/sessions/{sessionId}/content` | documented |
| upload content / presigned target | Multipart/presigned target is optional/reserved and may return validation when disabled. | `POST /api/v1/files/uploads/sessions/{sessionId}/parts/presign` | documented |
| complete upload | Mark uploaded content complete after object, size, and checksum validation where available. | `POST /api/v1/files/uploads/sessions/{sessionId}/complete` | documented |
| finalize upload | Create file record after validation; optionally attach to document; idempotent repeated finalize. | `POST /api/v1/files/uploads/sessions/{sessionId}/finalize` | documented |
| upload session metadata | Read upload session state. | `GET /api/v1/files/uploads/sessions/{sessionId}` | documented |
| upload session progress | Read upload session progress. | `GET /api/v1/files/uploads/sessions/{sessionId}/progress` | documented |
| abort upload session | Abort upload session where documented. | `POST /api/v1/files/uploads/sessions/{sessionId}/abort` | documented |
| file metadata | Read metadata without permanent public URL. | `GET /api/v1/files/{fileId}` | documented |
| file content access | Return stream or short-lived signed URL after authorization. | `GET /api/v1/files/{fileId}/content` | documented |
| file delete | Soft delete according to contract; reject when active attachments would be silently broken. | `DELETE /api/v1/files/{fileId}` | documented |
| document attachment create | Attach finalized file to document. | `POST /api/v1/documents/{documentId}/attachments` | documented |
| document attachment list | List attachments for authorized document/file context. | `GET /api/v1/documents/{documentId}/attachments` | documented |
| document attachment remove | Remove document/file attachment relation. | `DELETE /api/v1/documents/{documentId}/attachments/{attachmentId}` | documented |
| file reference validation | Validate internal Tiptap file references during document save. | `PATCH /api/v1/documents/{documentId}` | documented |
| outbox/internal processing if documented | Persist internal file outbox events; no public route documented. | not found in reviewed sources | documented |

All endpoint rows above are `not code-verified` until current code inspection or tests verify implementation.

## Validation Expectations

- For backend changes:
  - `dotnet restore`
  - `dotnet build`
  - `dotnet test`
- For schema changes:
  - EF migration generation/inspection
  - migration script inspection where applicable
  - no unintended model changes
- PostgreSQL smoke only if `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is set and command actually runs.
- Object storage integration only if configured and actually tested.
- Focused file workflow tests should cover:
  - create upload session
  - complete/finalize
  - idempotent finalize
  - permission checks
  - attachment creation/removal
  - no permanent URL storage
  - outbox write
  - invalid Tiptap file references
- Report all not-run validations with reasons.

## Conflict Boundaries

- Public collection links conflict remains governed by `docs/agent/02-conflict-register.md`.
- Public `linkMode` conflict remains governed by `docs/agent/02-conflict-register.md`.
- File work must not resolve public-link conflicts.
- If frontend API contract and permission contract differ on public file/link behavior, preserve conflict and report it.
- If README operational notes differ from code/tooling, code verification is required before changing this contract.

## Agent Rules

- Read this contract for files/upload work.
- Also read:
  - `docs/agent/skills/files-upload.md`
  - `docs/agent/skills/data-model-migrations.md`
  - `docs/agent/skills/api-contracts.md`
  - `docs/agent/skills/permissions.md`
- Inspect current code before implementation.
- Use smallest safe diff.
- Do not touch old Go file-service.
- Do not add undocumented schema fields/tables.
- Do not change public-link behavior.
- Do not claim files are complete without code verification.
- Do not claim storage/PostgreSQL smoke passed unless actually tested.
- Final report must include:
  - file workflow parts changed
  - APIs changed
  - migrations changed
  - permission checks
  - storage integration status
  - PostgreSQL smoke status
  - object storage integration status
  - public-link conflict touched or None
