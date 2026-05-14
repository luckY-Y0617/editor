# Phase 6 Files/Upload Readiness Investigation

This report inspects the current codebase for Phase 6 files/upload readiness.

It is an investigation report, not an implementation task.
It does not change code or mark undocumented behavior as implemented.

## Summary

- Overall readiness: Phase 6 files/upload is substantially implemented in code, including upload sessions, local upload content, complete/finalize, file metadata/content/delete, document attachments, file outbox persistence, and Tiptap file reference sync.
- Main implemented areas: API controllers and DTOs, Application services, Domain entities/state transitions, EF configurations/migration, local object storage, S3-compatible object storage provider, and focused API tests exist.
- Main missing or partial areas: file outbox processing is local/in-process rather than an external MQ/cloud dispatcher.
- Main risks: S3-compatible provider has presigned-target test coverage but no live object-storage integration pass; PostgreSQL smoke still depends on `NORTHSTAR_POSTGRES_SMOKE_CONNECTION`.
- Code modified: No application code changed. Only this investigation report was created.

## Scope

- Investigation only.
- No application, backend, frontend, test, migration, package, or project code changed.
- Backend root inspected: `services/api`.
- Phase: Phase 6 files/upload.
- Implementation status is based on code inspection only where inspected.
- Validation commands were not run.

## Docs Read

- `AGENTS.md`
- `docs/agent/00-project-state.md`
- `docs/agent/01-control-rules.md`
- `docs/agent/02-conflict-register.md`
- `docs/agent/03-required-reading-order.md`
- `docs/agent/04-implementation-protocol.md`
- `docs/agent/05-validation-protocol.md`
- `docs/agent/06-final-report-format.md`
- `docs/agent/skills/backend-clean-architecture.md`
- `docs/agent/skills/backend-phase-execution.md`
- `docs/agent/skills/data-model-migrations.md`
- `docs/agent/skills/files-upload.md`
- `docs/agent/skills/api-contracts.md`
- `docs/agent/skills/permissions.md`
- `docs/contracts/files-upload-contract.md`
- `docs/contracts/backend-operational-validation.md`

## Code Areas Inspected

- `services/api/src/Northstar.Api`
- `services/api/src/Northstar.Api/Controllers/FilesController.cs`
- `services/api/src/Northstar.Api/Controllers/DocumentsController.cs`
- `services/api/src/Northstar.Application`
- `services/api/src/Northstar.Application/Files`
- `services/api/src/Northstar.Application/Knowledge/DocumentService.cs`
- `services/api/src/Northstar.Application/Security`
- `services/api/src/Northstar.Contracts`
- `services/api/src/Northstar.Contracts/Files/FileDtos.cs`
- `services/api/src/Northstar.Domain`
- `services/api/src/Northstar.Domain/Files`
- `services/api/src/Northstar.Infrastructure`
- `services/api/src/Northstar.Infrastructure/Files`
- `services/api/src/Northstar.Infrastructure/Persistence`
- `services/api/src/Northstar.Infrastructure/Persistence/Configurations`
- `services/api/src/Northstar.Infrastructure/Persistence/Migrations`
- `services/api/tests`
- `services/api/tests/Northstar.Api.Tests/KnowledgeApiTests.cs`
- `services/api/tests/Northstar.Api.Tests/PostgreSqlSmokeTests.cs`
- `services/api/tests/Northstar.Application.Tests`
- `rg` was attempted but failed with `Access is denied`; PowerShell `Get-ChildItem` and `Select-String` were used instead.

## Component Status Matrix

| Component | Status | Evidence | Notes |
|---|---|---|---|
| upload sessions | implemented | `UploadSession`, `UploadSessionService`, `IUploadSessionService`, `UploadSessionConfiguration`, `20260428075433_AddFilesUploadSessionsPhase6.cs` | Lifecycle states, idempotency key, owner/workspace scope, object key, complete/finalize fields exist. |
| files table/entity | implemented | `StoredFile`, `StoredFileConfiguration`, `NorthstarDbContext.Files`, `files` migration table | Includes workspace, storage fields, byte size, checksum, metadata, status, created/deleted timestamps. |
| document attachments | implemented | `DocumentAttachment`, `DocumentAttachmentService`, `DocumentAttachmentConfiguration`, document attachment routes in `DocumentsController` | Create/list/remove and relation types exist. |
| file outbox | implemented for local Phase 6 path | `FileOutboxEvent`, `FileOutboxFactory`, `FileOutboxEventConfiguration`, `EfFileRepository.AddOutboxEventAsync`, `FileOutboxProcessor`, `FileOutboxHostedService` | Persistence, due-event processing, retry/failure state, and local object deletion are implemented. External MQ/cloud-storage dispatch is not implemented. |
| object storage abstraction | implemented | `IObjectStorage`, `StoredObjectInfo` | Application depends on abstraction, not a concrete SDK. |
| local object storage provider | implemented | `LocalFileStorage`, `DependencyInjection` selects local `IObjectStorage` by config | Local API upload target and stream-based read/write exist. |
| S3/MinIO provider, if any | implemented, not runtime-verified | `S3ObjectStorage`, `AWSSDK.S3`, `Files:S3` options, focused presigned-target test, `S3CompatibleStorageAcceptance_UploadReadDeleteThroughApi` | Supports S3-compatible presigned PUT targets, API-proxy upload, object info, stream read, and delete through `IObjectStorage`; live S3/MinIO acceptance is blocked until an endpoint is reachable. |
| upload session create | implemented | `FilesController.CreateUploadSession`, `UploadSessionService.CreateAsync`, `CreateUploadSessionRequest` | Supports idempotency key and local upload target. |
| upload content | implemented | `FilesController.UploadContent`, `UploadSessionService.UploadContentAsync`, `LocalFileStorage.WriteUploadContentAsync` | Direct local/API-proxy `PUT` path exists. |
| upload complete | implemented | `FilesController.CompleteUploadSession`, `UploadSessionService.CompleteAsync` | Validates object exists, byte size, and SHA-256 when supplied. |
| upload finalize | implemented | `FilesController.FinalizeUploadSession`, `UploadSessionService.FinalizeAsync` | Creates `StoredFile` only after completed session and writes outbox event. |
| idempotent finalize | implemented | `UploadSessionService.FinalizeAsync`, `UploadSession.Finalize`, `KnowledgeApiTests.UploadSession_IsIdempotent_AndFinalizeCreatesFileOnce` | Repeated finalize returns existing file and does not duplicate file or tested attachment. Tests were inspected, not run. |
| file metadata API | implemented | `FilesController.GetFile`, `FileService.GetAsync`, `FileDto` | Metadata endpoint exists; public DTO no longer exposes storage internals. |
| file content access API | implemented | `FilesController.GetFileContent`, `FileService.OpenContentAsync`, `LocalFileStorage.OpenReadAsync` | Streams content through API with range processing. |
| file delete API | implemented | `FilesController.DeleteFile`, `FileService.DeleteAsync`, `StoredFile.Delete`, `FileOutboxProcessor` | Soft-deletes file, rejects active attachments, writes `file.deleted`, and the local outbox processor deletes the storage object. |
| document attachment create/list/remove | implemented | `DocumentsController.GetAttachments`, `AttachFile`, `DeleteAttachment`; `DocumentAttachmentService` | Uses document-scoped permission service for attachment operations. |
| Tiptap file reference validation | partially implemented | `FileReferenceExtractor`, `FileReferenceService`, `DocumentService.UpdateAsync`, tests at `KnowledgeApiTests` lines around `UpdateDocumentContent_...FileReferences` | Validates referenced files exist and share workspace, then creates inline image attachments. It does not distinguish node/link relation type beyond `inline_image`. |
| permission checks | implemented for Phase 6 local API path | `WorkspaceAccessService`, `ScopedResourceAccessService`, `DocumentAttachmentService`, `FileService`, `UploadSessionService`, file API tests | File metadata/content/delete use file actions and attached-document scoped checks; attachment APIs use document-scoped attachment actions. |
| tests | partially implemented | `KnowledgeApiTests` file/upload methods; `PostgreSqlSmokeTests` file smoke flow | Focused tests exist but were not run; storage-provider integration and full scoped permission matrix are not verified. |

## Data Model Findings

- Tables/entities found:
  - `upload_sessions`: `UploadSession`, `UploadSessionConfiguration`, `NorthstarDbContext.UploadSessions`.
  - `files`: `StoredFile`, `StoredFileConfiguration`, `NorthstarDbContext.Files`.
  - `document_attachments`: `DocumentAttachment`, `DocumentAttachmentConfiguration`, `NorthstarDbContext.DocumentAttachments`.
  - `file_outbox_events`: `FileOutboxEvent`, `FileOutboxEventConfiguration`, `NorthstarDbContext.FileOutboxEvents`.
- Migration found: `services/api/src/Northstar.Infrastructure/Persistence/Migrations/20260428075433_AddFilesUploadSessionsPhase6.cs`.
- Model snapshot includes file tables in `NorthstarDbContextModelSnapshot.cs`.
- Workspace scoping evidence: all four file tables/entities include `WorkspaceId` mapped to `workspace_id`.
- Soft-delete evidence: `StoredFile.DeletedAt`, `files.deleted_at`, and `FileService.DeleteAsync` soft-delete behavior found.
- Checksum/size evidence: `byte_size` and `checksum_sha256` exist on upload sessions and files; complete validates size and optional SHA-256.
- Attachment relation types: `attachment`, `inline_image`, and `cover` in `DocumentAttachmentRelationType` and DB check constraint.
- Gaps:
  - File outbox processing is an in-process local Phase 6 worker, not an external MQ or cloud-storage dispatcher.
  - `upload_sessions.owner_id` is non-null in migration while FK delete behavior is `SetNull`; deletion behavior was not validated.
  - Object retention beyond soft-delete plus outbox-driven local object deletion is not implemented.

## API Findings

- Base route behavior: `Program.cs` applies `api/v1` via `RoutePrefixConvention`.
- File controller found: `services/api/src/Northstar.Api/Controllers/FilesController.cs`.
- Document attachment routes found in `services/api/src/Northstar.Api/Controllers/DocumentsController.cs`.
- Contracts found in `services/api/src/Northstar.Contracts/Files/FileDtos.cs`.
- Routes found:
  - `POST /api/v1/files/uploads/sessions`
  - `GET /api/v1/files/uploads/sessions/{sessionId}`
  - `PUT /api/v1/files/uploads/sessions/{sessionId}/content`
  - `POST /api/v1/files/uploads/sessions/{sessionId}/complete`
  - `POST /api/v1/files/uploads/sessions/{sessionId}/finalize`
  - `GET /api/v1/files/uploads/sessions/{sessionId}/progress`
  - `POST /api/v1/files/uploads/sessions/{sessionId}/abort`
  - `POST /api/v1/files/uploads/sessions/{sessionId}/parts/presign`
  - `GET /api/v1/files/{fileId}`
  - `GET /api/v1/files/{fileId}/content`
  - `DELETE /api/v1/files/{fileId}`
  - `GET /api/v1/documents/{documentId}/attachments`
  - `POST /api/v1/documents/{documentId}/attachments`
  - `DELETE /api/v1/documents/{documentId}/attachments/{attachmentId}`
  - Tiptap file reference validation is wired into `PATCH /api/v1/documents/{documentId}` through `DocumentService.UpdateAsync`.
- Multipart/presign route exists but returns a Phase 6 validation stub.
- Outbox/internal processing route: not exposed, and no public route is expected by the contract. Processing is handled by an internal application processor and hosted service.

## Application / Domain / Infrastructure Findings

- API:
  - `FilesController` and `DocumentsController` call Application services and appear thin.
  - Controllers use Contracts DTOs, not EF entities.
- Contracts:
  - `FileDtos.cs` defines upload session, upload target, finalize, file, and attachment DTOs.
  - `FileDto` does not expose storage provider, bucket, object key, or permanent URLs.
- Application:
  - `UploadSessionService`, `FileService`, `DocumentAttachmentService`, `FileReferenceService`, and `FileReferenceExtractor` exist.
  - `UploadSessionService` orchestrates create/content/complete/finalize/progress/abort.
  - `FileService` handles metadata/content/delete.
  - `DocumentAttachmentService` handles attachment create/list/remove.
  - `DocumentService.UpdateAsync` invokes file reference validation before saving content.
- Domain:
  - `UploadSession`, `StoredFile`, `DocumentAttachment`, and `FileOutboxEvent` own state and basic invariants.
  - Domain file classes do not reference ASP.NET Core, EF Core, HTTP, or storage SDKs in inspected files.
- Infrastructure:
  - `EfFileRepository` implements file persistence/query operations.
  - `LocalFileStorage` implements `IObjectStorage`.
  - `S3ObjectStorage` implements `IObjectStorage` for S3-compatible storage when configured.
  - EF configurations and migration exist for file tables.
  - DI registers file repositories, services, and config-selected object storage.
- Layering appears broadly valid in inspected files: API -> Application, Application -> Domain/Contracts, Infrastructure -> Application/Domain/Contracts.

## Permission Findings

- Permission service usage found:
  - `UploadSessionService` uses `IWorkspaceAccessService`.
  - `FileService` uses `IWorkspaceAccessService`.
  - `DocumentAttachmentService` uses `IScopedResourceAccessService`.
  - `DocumentService.UpdateAsync` uses `IScopedResourceAccessService` for document edit before Tiptap file reference validation.
- Protected operations checked:
  - Upload session create requires workspace edit after resolving workspace from `workspaceId` or `documentId`.
  - Upload session read/content/complete/finalize/progress/abort require owner or workspace edit.
  - File metadata/content require workspace view.
  - File delete requires workspace edit and rejects active attachments.
  - Attachment list/create/delete use document scoped access with attachment/document actions.
- Current behavior:
  - File metadata/content use `file.download` for unattached files and attached-document scoped access for attached files.
  - File delete uses `file.delete` for unattached files and attached-document scoped delete/edit checks before returning active-attachment conflict.
  - Upload session creation with `documentId` uses document-scoped `file.upload` / `attachment.create` / `document.edit`.
  - Tiptap file references validate existence and workspace after document edit authorization, then sync attachment rows outside document JSON.
- Public-link conflicts were not resolved.

## Tiptap File Reference Findings

- Validation exists in `FileReferenceExtractor`, `FileReferenceService`, and `DocumentService.UpdateAsync`.
- Extracted references include `attrs.fileId` and file-content URLs in `attrs.src` or `attrs.href` matching `/api/v1/files/{fileId}/content`.
- `FileReferenceService` rejects missing files and cross-workspace file references.
- It creates `DocumentAttachment` rows and outbox events for new references.
- Document JSON remains metadata-safe as inspected behavior stores attachment source of truth outside document JSON; the JSON may contain file IDs/URLs as references.
- Gaps:
  - Extracted references are always represented as `inline_image`; attachment/file nodes or file links are not mapped to distinct relation types during validation.
  - No direct unit test for `FileReferenceExtractor` was found; API-level tests cover valid, missing, and cross-workspace references.

## Outbox Findings

- Outbox entity/table/configuration found:
  - `FileOutboxEvent`
  - `FileOutboxEventConfiguration`
  - `file_outbox_events` table in `20260428075433_AddFilesUploadSessionsPhase6.cs`
- Event names found:
  - `file.finalized`
  - `document_attachment.created`
  - `file.deleted`
- Outbox writes found:
  - finalize writes `file.finalized`
  - attachment creation writes `document_attachment.created`
  - file delete writes `file.deleted`
  - Tiptap reference sync writes `document_attachment.created`
- Processing behavior:
  - `FileOutboxProcessor` reads due `pending` rows and marks `file.finalized` / `document_attachment.created` published.
  - `file.deleted` loads the soft-deleted file and calls `IObjectStorage.DeleteObjectAsync` before marking the event published.
  - Failures remain `pending` with `RetryCount`, `LastError`, and `NextRetryAt` until `MaxAttempts`, then become `failed`.
  - `FileOutboxHostedService` runs the processor in-process and is disabled in the `Testing` environment.
- Remaining gaps:
  - No external MQ/cloud outbox publisher exists.
  - No separate retention policy table or delayed-delete policy exists beyond outbox retry/failure state.

## Test Findings

- Tests found in `services/api/tests/Northstar.Api.Tests/KnowledgeApiTests.cs`:
  - `UploadSession_EditorCanCreate_AndViewerCannotCreate`
  - `UploadSession_IsIdempotent_AndFinalizeCreatesFileOnce`
  - `UploadSession_FinalizeBeforeComplete_AndAbortThenFinalize_ReturnValidationError`
  - `UploadSession_CompleteRejectsChecksumMismatch_AndMultipartPresignIsPhase6Stub`
  - `FileAccess_ViewerCanRead_OutsiderCannotRead_AndDeleteRequiresNoActiveAttachments`
  - `DocumentAttachments_EditorCanAttachAndList_ViewerCannotWrite`
  - `UpdateDocumentContent_ValidatesTiptapFileReferences_AndCreatesInlineImageAttachment`
  - `UpdateDocumentContent_RejectsMissingAndCrossWorkspaceFileReferences`
  - `LocalFileStorage_StreamsContentWithoutReadAllBytes`
- PostgreSQL smoke file flow found in `services/api/tests/Northstar.Api.Tests/PostgreSqlSmokeTests.cs`; it only runs if `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is set.
- Missing or not verified tests:
  - Tests were inspected only and not run.
  - S3-compatible provider has presigned-target focused test coverage and a live acceptance test, but no successful live S3/MinIO/provider run yet.
  - No direct test found for no permanent URL storage or non-exposure of raw object paths.
  - No focused test found for file-specific `file.download`/`file.delete` scoped permission behavior.
  - `FileOutboxProcessor_PublishesFinalizeAttachmentAndDeleteEvents`
  - `FileOutboxProcessor_RetriesThenFailsDeleteObjectErrors`

## Architecture Risks

- Public file DTOs no longer expose `StorageProvider`, `Bucket`, or `ObjectKey`.
- File metadata/content/delete now enforce file-specific or attached-document scoped permission checks.
- Outbox processor risk: processing is in-process and local-provider oriented; production external MQ/cloud-storage dispatch remains future work.
- Object lifecycle risk: file delete soft-deletes DB state and outbox processor removes the local object, but no delayed retention policy is implemented.
- No direct dependency on S3/MinIO SDKs in Application/Domain observed; AWS SDK dependency is isolated to Infrastructure.
- No old Go file service, `services/api-old`, `SqlSugar`, ABP, or `ClayMo` runtime dependency observed in inspected `services/api/src` files.

## Conflict-Marked Areas

- Public-link behavior touched: not touched by this investigation.
- Public `linkMode` touched: not touched by this investigation.
- Public-link source conflicts: not resolved.
- Backend phase vs permission phase mismatch: preserved; file implementation status is based on code inspection, not permission Phase 11 documentation.
- README/code verification drift: README was not used as implementation proof; code was inspected directly.
- PostgreSQL smoke gap: preserved. The smoke test exists, but PostgreSQL smoke was not run. `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was not checked.

## Validation Not Run

- `dotnet restore`: not run; investigation-only task.
- `dotnet build`: not run; investigation-only task.
- `dotnet test`: not run; investigation-only task.
- PostgreSQL smoke: not run; investigation-only task.
- Object storage integration: not run; investigation-only task.
- `NORTHSTAR_POSTGRES_SMOKE_CONNECTION`: not checked; investigation-only task.

## Smallest Safe Next Step

implement or complete permission checks

## Implementation Closure 2026-05-12

- File access permission depth was closed for the Phase 6 local API path.
- `FileService.GetAsync` and `OpenContentAsync` now authorize unattached files with `file.download`; attached files are authorized through the attached document using `file.download`, `attachment.view`, or `document.view`.
- `FileService.DeleteAsync` now authorizes unattached files with `file.delete`; attached files are authorized through the attached document using `file.delete`, `attachment.delete`, or `document.edit`, then still return `409 CONFLICT` while active attachments exist.
- `UploadSessionService.CreateAsync` with `documentId` now checks document-scoped `file.upload`, `attachment.create`, or `document.edit` instead of only workspace edit.
- Focused file tests passed on 2026-05-12, including restricted-document file access behavior.
- Remaining non-acceptance items: live external object storage integration when Docker/S3/MinIO is reachable, PostgreSQL smoke when the environment variable is available, and browser UI QA while the user requests no browser QA.

## Open Questions

- Exact production object storage endpoint/credential configuration is environment-specific and was not runtime-tested.
- Whether file content access should be authorized by workspace view, `file.download`, document attachment visibility, or another scoped rule.
- Whether upload session creation with a document context should require document-scoped attachment/file upload permission.
- Outbox dispatch, retry, retention, and object garbage collection behavior.
- Multipart support beyond the current Phase 6 validation stub.
- PostgreSQL smoke environment and whether `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is available for later validation.
