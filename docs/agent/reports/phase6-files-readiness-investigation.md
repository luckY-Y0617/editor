# Phase 6 Files/Upload Readiness Investigation

This report inspects the current codebase for Phase 6 files/upload readiness.

It is an investigation report, not an implementation task.
It does not change code or mark undocumented behavior as implemented.

## Summary

- Overall readiness: Phase 6 files/upload is substantially implemented in code, including upload sessions, local upload content, complete/finalize, file metadata/content/delete, document attachments, file outbox persistence, and Tiptap file reference sync.
- Main implemented areas: API controllers and DTOs, Application services, Domain entities/state transitions, EF configurations/migration, local object storage, and focused API tests exist.
- Main missing or partial areas: S3/MinIO provider is missing; file outbox dispatch/background processing was not found; permission checks exist but are not consistently file-action or document-scope specific.
- Main risks: `FileDto` exposes `StorageProvider`, `Bucket`, and `ObjectKey`; file metadata/content/delete use workspace-level checks instead of `file.download`/`file.delete` or attachment/document resource checks.
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
| file outbox | partially implemented | `FileOutboxEvent`, `FileOutboxFactory`, `FileOutboxEventConfiguration`, `EfFileRepository.AddOutboxEventAsync` | Persistence and event writes exist; no dispatcher/background processor found during inspection. |
| object storage abstraction | implemented | `IObjectStorage`, `StoredObjectInfo` | Application depends on abstraction, not a concrete SDK. |
| local object storage provider | implemented | `LocalFileStorage`, `DependencyInjection` registers `IObjectStorage, LocalFileStorage` | Local API upload target and stream-based read/write exist. |
| S3/MinIO provider, if any | missing | not found during inspection; no `AWSSDK`, `MinIO`, `Minio`, `S3`, or `Amazon` package/reference found | Production object storage provider not present. |
| upload session create | implemented | `FilesController.CreateUploadSession`, `UploadSessionService.CreateAsync`, `CreateUploadSessionRequest` | Supports idempotency key and local upload target. |
| upload content | implemented | `FilesController.UploadContent`, `UploadSessionService.UploadContentAsync`, `LocalFileStorage.WriteUploadContentAsync` | Direct local/API-proxy `PUT` path exists. |
| upload complete | implemented | `FilesController.CompleteUploadSession`, `UploadSessionService.CompleteAsync` | Validates object exists, byte size, and SHA-256 when supplied. |
| upload finalize | implemented | `FilesController.FinalizeUploadSession`, `UploadSessionService.FinalizeAsync` | Creates `StoredFile` only after completed session and writes outbox event. |
| idempotent finalize | implemented | `UploadSessionService.FinalizeAsync`, `UploadSession.Finalize`, `KnowledgeApiTests.UploadSession_IsIdempotent_AndFinalizeCreatesFileOnce` | Repeated finalize returns existing file and does not duplicate file or tested attachment. Tests were inspected, not run. |
| file metadata API | implemented | `FilesController.GetFile`, `FileService.GetAsync`, `FileDto` | Metadata endpoint exists; DTO exposes storage internals. |
| file content access API | implemented | `FilesController.GetFileContent`, `FileService.OpenContentAsync`, `LocalFileStorage.OpenReadAsync` | Streams content through API with range processing. |
| file delete API | implemented | `FilesController.DeleteFile`, `FileService.DeleteAsync`, `StoredFile.Delete` | Soft-deletes file, rejects active attachments, writes `file.deleted`; object deletion/retention worker not found. |
| document attachment create/list/remove | implemented | `DocumentsController.GetAttachments`, `AttachFile`, `DeleteAttachment`; `DocumentAttachmentService` | Uses document-scoped permission service for attachment operations. |
| Tiptap file reference validation | partially implemented | `FileReferenceExtractor`, `FileReferenceService`, `DocumentService.UpdateAsync`, tests at `KnowledgeApiTests` lines around `UpdateDocumentContent_...FileReferences` | Validates referenced files exist and share workspace, then creates inline image attachments. It does not distinguish node/link relation type beyond `inline_image`. |
| permission checks | partially implemented | `WorkspaceAccessService`, `ScopedResourceAccessService`, `DocumentAttachmentService`, `FileService`, `UploadSessionService` | Server-side checks exist. File metadata/content/delete use workspace-level checks; attachment APIs use scoped document attachment actions. |
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
  - No separate file outbox dispatcher/worker was found.
  - `upload_sessions.owner_id` is non-null in migration while FK delete behavior is `SetNull`; deletion behavior was not validated.
  - Object retention/garbage collection behavior was not found.

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
- Outbox/internal processing route: not found during inspection, and no public route is expected by the contract.

## Application / Domain / Infrastructure Findings

- API:
  - `FilesController` and `DocumentsController` call Application services and appear thin.
  - Controllers use Contracts DTOs, not EF entities.
- Contracts:
  - `FileDtos.cs` defines upload session, upload target, finalize, file, and attachment DTOs.
  - `FileDto` includes storage provider, bucket, and object key.
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
  - EF configurations and migration exist for file tables.
  - DI registers file repositories, services, and local storage.
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
- Gaps:
  - File metadata/content/delete do not appear to check `PermissionActions.FileDownload` or `PermissionActions.FileDelete` directly.
  - File content access is workspace-level, not clearly tied to document attachment access or scoped document permission.
  - Upload creation with `DocumentId` resolves workspace but does not appear to check document-scoped attachment/file upload permission for that document.
  - Tiptap file references validate existence and workspace, not whether the actor has file-specific access beyond the document edit path.
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
- Gaps:
  - No file outbox dispatcher, publisher, hosted service, or retry processor was found during inspection.
  - No object deletion worker/retention processor was found.

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
  - No S3/MinIO/provider integration tests found.
  - No direct test found for no permanent URL storage or non-exposure of raw object paths.
  - No focused test found for file-specific `file.download`/`file.delete` scoped permission behavior.
  - No file outbox dispatcher tests found.

## Architecture Risks

- Potential API/security boundary risk: `FileDto` exposes `StorageProvider`, `Bucket`, and `ObjectKey` through file metadata and attachment responses, which may expose raw storage internals.
- Permission depth risk: file metadata/content/delete use workspace-level authorization rather than clearly enforcing file-specific or document/attachment scoped actions.
- Outbox completeness risk: outbox rows are written but no dispatcher/worker was found, so downstream processing is partial.
- Object lifecycle risk: file delete soft-deletes DB state and writes outbox, but no object deletion or retention processor was found.
- No direct dependency on S3/MinIO SDKs in Application/Domain observed.
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

## Open Questions

- Exact production object storage provider choice: local only is implemented; S3/MinIO provider was not found.
- Whether file metadata responses are intended to expose storage provider, bucket, and object key.
- Whether file content access should be authorized by workspace view, `file.download`, document attachment visibility, or another scoped rule.
- Whether upload session creation with a document context should require document-scoped attachment/file upload permission.
- Outbox dispatch, retry, retention, and object garbage collection behavior.
- Multipart support beyond the current Phase 6 validation stub.
- PostgreSQL smoke environment and whether `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is available for later validation.
