# Backend Phase 6 Prompt

本提示词用于在当前 `services/api` 的 Phase 5 成果上继续实现 Phase 6。Phase 6 的主线是文件模块：upload session、文件元数据、文档附件、私有访问入口和 file outbox。旧 Go 文件服务只参考流程，不作为运行依赖，也不迁移 go-zero 架构。

## 当前基线

工作目录：

```text
D:\editor
```

后端项目：

```text
D:\editor\services\api
```

当前已完成：

```text
GET    /api/v1/bootstrap
GET    /api/v1/spaces/{spaceId}/map
POST   /api/v1/documents
GET    /api/v1/documents/{documentId}
PATCH  /api/v1/documents/{documentId}
PATCH  /api/v1/documents/{documentId}/location
PATCH  /api/v1/documents/{documentId}/archive
PATCH  /api/v1/documents/{documentId}/restore
DELETE /api/v1/documents/{documentId}
GET    /api/v1/documents/{documentId}/context
GET    /api/v1/documents/{documentId}/activity
GET    /api/v1/search?q=&spaceId=
GET    /api/v1/spaces/{spaceId}/export
POST   /api/v1/spaces/{spaceId}/import
POST   /api/v1/auth/register
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh
POST   /api/v1/auth/logout
GET    /api/v1/auth/me
GET    /api/v1/workspaces/{workspaceId}/members
POST   /api/v1/workspaces/{workspaceId}/members
PATCH  /api/v1/workspaces/{workspaceId}/members/{userId}
DELETE /api/v1/workspaces/{workspaceId}/members/{userId}
```

Phase 5 验证结果：

- `dotnet restore .\Northstar.sln` 成功。
- `dotnet build .\Northstar.sln --no-restore` 成功，0 warning，0 error。
- `dotnet test .\Northstar.sln --no-build` 成功，共 37 个测试通过。
- `dotnet ef migrations script ... --idempotent` 成功生成 SQL。
- `dotnet ef migrations has-pending-model-changes` 无模型变更。
- PostgreSQL smoke profile 已扩展到 archive/delete/search/export/import，但本机 `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` 未设置，因此真实 PostgreSQL smoke 未运行，filter 测试路径提前返回。

## 开始前必须阅读

必须阅读并遵守：

- `docs/BACKEND_ARCHITECTURE_V1.md`
- `docs/BACKEND_REFACTOR_RULES.md`
- `docs/BACKEND_DATA_MODEL_V1.md`
- `docs/BACKEND_PHASE5_PROMPT.md`
- `apps/web/FRONTEND_API_CONTRACT.md`
- `services/api/README.md`

重点参考：

- `docs/BACKEND_ARCHITECTURE_V1.md` 的“文件模块架构”。
- `docs/BACKEND_DATA_MODEL_V1.md` 的 `files`、`document_attachments`、`upload_sessions`、`file_outbox_events`。
- 旧 `E:\ClayMo\services\file-service` 只读参考上传流程，不修改，不迁移。

旧项目边界：

- `services/api-old` 只读参考，不要修改。
- `E:\ClayMo\services\file-service` 只读参考，不要修改。
- 不要引入 ABP。
- 不要迁移旧 `ClayMo.*`、`NS.Abp`、`Module.*` 架构。
- 不要把旧 Go/go-zero file-service 当作运行依赖。

## Phase 6 总目标

实现第一版文件能力：

1. `upload_sessions -> files -> document_attachments` 的完整主路径。
2. 支持单文件上传 session。
3. 为未来 multipart/presigned/object storage 保留边界。
4. finalize 后才创建 `files`。
5. 私有文件访问统一走 API 权限校验。
6. 文件引用进入 Tiptap content 后，保存文档时做安全校验。
7. 增加 file outbox 表和写入边界，但不要求本阶段接真实 MQ。

本阶段不要做：

- 评论
- Yjs 协作
- AI
- 文件病毒扫描真实集成
- 图片缩略图真实生成
- 外部 CDN
- 复杂异步转码
- 独立 file-service 部署

## 架构原则

继续保持 Clean Architecture：

- Controller 只处理 HTTP 和 DTO。
- Application 编排 use case、权限、事务。
- Domain 放文件实体和状态规则。
- Infrastructure 实现 EF、存储 provider、签名 URL 或本地文件流。
- Contracts 放 API DTO。

文件存储必须通过接口隔离，例如：

```csharp
public interface IObjectStorage
{
    Task<UploadTarget> CreateUploadTargetAsync(...);
    Task<StoredObjectInfo?> GetObjectInfoAsync(...);
    Task<Stream> OpenReadAsync(...);
    Task DeleteObjectAsync(...);
}
```

Phase 6 推荐先实现：

- `LocalFileStorage`：开发和测试用，落到本地目录。
- 可选 `S3CompatibleObjectStorage`：如果配置足够明确，可支持 MinIO/S3 presigned URL。

不要让 Application 或 Domain 直接引用 S3/MinIO SDK。

## 数据模型

新增实体和表：

```text
files
document_attachments
upload_sessions
file_outbox_events
```

字段以 `docs/BACKEND_DATA_MODEL_V1.md` 为基线，可按 EF Core 实现微调。

### files

核心字段：

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

规则：

- file 只在 finalize 后创建。
- `object_key` 不使用用户原文件名，必须由后端生成。
- `original_filename` 只作为展示字段。
- `deleted_at` 为软删除。
- URL 不作为永久字段。
- 预留安全/处理状态，避免后续接病毒扫描、缩略图和 metadata extraction 时返工：
  - `scan_status`: `pending | clean | blocked | failed`
  - `processing_status`: `pending | ready | failed`
- Phase 6 如果没有真实扫描服务，可以默认 `scan_status = clean` 或 `pending`，但字段或 metadata 语义必须固定。

### upload_sessions

核心字段：

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

状态：

```text
initiated
uploading
completed
aborted
expired
failed
finalized
```

规则：

- session 是上传过程唯一事实源。
- 相同 `workspace_id + idempotency_key` 必须幂等。
- finalized 后不可再次创建 file。
- 重复 finalize 必须幂等返回已有 finalized file，不要重复创建 `files`、`document_attachments` 或 outbox event。
- expired/aborted/failed 不可 finalize。
- 本阶段可以先支持 `single` upload mode；multipart API 可以先按 contract 保留，或者返回 `400 VALIDATION_ERROR` 明确未启用。

### document_attachments

核心字段：

- `id`
- `workspace_id`
- `document_id`
- `file_id`
- `relation_type`
- `metadata`
- `created_by`
- `created_at`

relation_type：

```text
attachment
inline_image
cover
```

规则：

- attachment 需要 document 所属 workspace 与 file workspace 一致。
- document 软删除后附件记录可以保留，但默认查询不返回 deleted document 的附件。
- file 删除时不要 hard delete attachment；优先软删除 file 或删除 attachment 关系。
- 删除附件关系和删除文件资产必须区分：
  - `DELETE /documents/{documentId}/attachments/{attachmentId}` 删除 document/file 关系。
  - `DELETE /files/{fileId}` 是删除文件资产的软删除请求。
- 如果 file 仍存在 active attachments，`DELETE /files/{fileId}` 应优先返回 `409 CONFLICT`。Phase 6 不要让普通 delete 隐式破坏仍被文档引用的文件。

### file_outbox_events

核心字段：

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

Phase 6 要求：

- finalize file 时写 `file.finalized` outbox event。
- attach file 时写 `document_attachment.created` outbox event。
- delete file 时写 `file.deleted` outbox event。
- 不要求实现真实 MQ dispatcher；可以保留 hosted service skeleton 或只落表。

## API 设计

### Upload Session

实现：

```text
POST /api/v1/files/uploads/sessions
GET  /api/v1/files/uploads/sessions/{sessionId}
POST /api/v1/files/uploads/sessions/{sessionId}/complete
POST /api/v1/files/uploads/sessions/{sessionId}/finalize
GET  /api/v1/files/uploads/sessions/{sessionId}/progress
POST /api/v1/files/uploads/sessions/{sessionId}/abort
```

可选保留：

```text
POST /api/v1/files/uploads/sessions/{sessionId}/parts/presign
```

如果本阶段只做 single upload，`parts/presign` 可以先返回明确错误：

```text
400 VALIDATION_ERROR: Multipart upload is not enabled in Phase 6.
```

#### POST `/files/uploads/sessions`

Request 建议：

```csharp
public sealed record CreateUploadSessionRequest(
    string IdempotencyKey,
    string OriginalFilename,
    string MimeType,
    long ByteSize,
    string? ChecksumSha256,
    string? BizType,
    string UploadMode);
```

Response 建议：

```csharp
public sealed record CreateUploadSessionResponse(
    string SessionId,
    string Status,
    string UploadMode,
    UploadTargetDto UploadTarget,
    DateTimeOffset ExpiresAt);
```

`UploadTargetDto`：

- local/dev 可以返回 API upload URL。
- S3/MinIO 可以返回 presigned PUT URL。

如果实现 local storage，建议新增：

```text
PUT /api/v1/files/uploads/sessions/{sessionId}/content
```

该接口仅用于 local/dev provider 或 API-proxy upload。若选择 presigned PUT，则不用实现该 PUT API。

local/API-proxy upload 约束：

- 不允许把整个请求体读入内存。
- 必须 stream 写入临时/staging 对象路径。
- 必须限制 request body size，不能只依赖前端。
- 写入完成后 session 可进入 `uploading` 或保持 `initiated`，由 `complete` 做最终校验。
- `complete` 必须校验实际 byte size，若提供 `checksum_sha256` 还必须校验 checksum。
- finalize 前对象必须处于 staging/provisional 路径或等价状态；finalize 成功后才被视为正式 file。

#### POST `/complete`

语义：

- 客户端告知对象已上传完成。
- 后端校验 session 状态、对象存在、大小/checksum 是否匹配。
- 成功后 session -> `completed`。

#### POST `/finalize`

语义：

- 只有 completed session 可 finalize。
- 在事务中创建 `files`。
- session -> `finalized`，写 `finalized_file_id`。
- 可选创建 document attachment。
- 写 file outbox event。
- 如果 session 已经 finalized，直接返回既有 `files` 和 attachment 信息，不重复创建任何记录。

Request 建议：

```csharp
public sealed record FinalizeUploadSessionRequest(
    string? DocumentId,
    string? RelationType,
    object? Metadata);
```

Response：

```csharp
public sealed record FinalizeUploadSessionResponse(
    FileDto File,
    DocumentAttachmentDto? Attachment);
```

如果 `DocumentId` 不为空：

- 要校验当前用户对 document workspace 有 editor+ 权限。
- document 必须未 deleted。
- file workspace 与 document workspace 一致。

### Files

实现：

```text
GET    /api/v1/files/{fileId}
GET    /api/v1/files/{fileId}/content
DELETE /api/v1/files/{fileId}
```

规则：

- `GET /files/{fileId}` 返回 metadata，不返回永久 URL。
- `GET /files/{fileId}/content` 必须做权限校验：
  - 当前用户是 workspace member viewer+。
  - 若 file 附着到 document，可基于 document workspace 判断。
  - 未来有 public link 再单独做。
- content 可返回：
  - 302 redirect 到短期签名 URL。
  - 或直接 stream 文件。
- 本阶段 local storage 建议直接 stream。
- 删除使用软删除，写 `file.deleted` outbox。
- 删除 file 前必须检查 active attachments：
  - 若仍有 active attachments，默认返回 `409 CONFLICT`。
  - 用户应先删除 attachment relation，再删除 file。
  - 未来如需“强制删除并断开所有引用”，单独加明确 API，不要让普通 delete 隐式破坏文档内容。

### Document Attachments

实现：

```text
GET    /api/v1/documents/{documentId}/attachments
POST   /api/v1/documents/{documentId}/attachments
DELETE /api/v1/documents/{documentId}/attachments/{attachmentId}
```

Request：

```csharp
public sealed record AttachFileToDocumentRequest(
    string FileId,
    string RelationType,
    object? Metadata);
```

规则：

- GET 需要 viewer+。
- POST/DELETE 需要 editor+。
- attachment relation 不等于 content 引用，二者都要支持：
  - attachment：普通附件列表。
  - inline_image：Tiptap content 引用的图片。
  - cover：文档封面。

## Tiptap Content 文件引用校验

Phase 6 必须处理“文件已经进入 Tiptap content 后的安全校验”。

在 `PATCH /api/v1/documents/{id}` 保存 content 时：

1. 从 Tiptap JSON 中提取文件引用。
2. 支持常见形态：
   - image node attrs: `src`, `fileId`
   - attachment/file node attrs: `fileId`
   - link href: `/api/v1/files/{fileId}/content`
3. 如果引用的是内部 fileId：
   - file 必须存在。
   - file workspace 必须等于 document workspace。
   - file 未 deleted。
   - 当前用户必须有 document editor+ 权限。
4. 如果 content 引用 file 但没有 attachment 关系：
   - 可以自动创建 `document_attachments` relation_type=`inline_image`。
   - 或返回 validation error。推荐自动创建，减少前端双写负担。
5. 如果 attachment 已不再被 content 引用：
   - 不要自动删除普通 attachment。
   - inline_image 可以保留关系，后续清理任务再做。

新增 `IFileReferenceExtractor`，不要把提取逻辑写在 Controller。

## 权限规则

复用 Phase 4 workspace member 权限：

- create upload session: editor+，需要明确 workspaceId 或 documentId。
- complete/finalize/abort session: session owner 或 workspace editor+。
- file metadata/content: viewer+。
- delete file: editor+。
- document attachments write: editor+。

Create upload session 的 workspace 定位二选一：

1. Request 显式传 `workspaceId`。
2. Request 传 `documentId`，后端从 document 解析 workspace。

建议 Phase 6 支持两者之一即可，但 DTO 必须明确。若为了文档编辑器上传图片，优先支持 `documentId`。

## Storage Provider

配置建议：

```json
{
  "Files": {
    "StorageProvider": "Local",
    "LocalRootPath": "var/files",
    "DefaultBucket": "northstar-local",
    "UploadSessionMinutes": 60,
    "MaxFileBytes": 52428800,
    "AllowedMimeTypes": [
      "image/png",
      "image/jpeg",
      "image/webp",
      "application/pdf",
      "text/plain"
    ]
  }
}
```

规则：

- local root path 不要写死绝对路径。
- object key 使用 workspace/date/guid 分层，例如：
  `workspaces/{workspaceId}/files/{yyyy}/{MM}/{fileId}`。
- 校验 byte size 和 MIME type。
- MIME type 不能只信客户端，local provider 可先做轻量扩展名/魔数校验，后续再接真实检测。

## Import/Export 的文件边界

Phase 5 已有 space export/import。

Phase 6 不要求把文件二进制打包进 export。

但需要明确：

- export 可以包含 attachment metadata 和 file ids 吗？
- 推荐 Phase 6 暂不导出二进制，只导出 document content 中的 file references 和 attachment metadata。
- import 遇到源环境 fileId 时，不要伪造本环境 file 记录。
- 后续如需完整迁移文件，单独做 export package/manifest。

更新 `apps/web/FRONTEND_API_CONTRACT.md`，说明 Phase 6 文件导入导出边界。

## Contracts

新增 DTO 放在 `Northstar.Contracts`，建议目录：

```text
Contracts/Files/FileDtos.cs
```

至少包含：

```csharp
CreateUploadSessionRequest
CreateUploadSessionResponse
UploadTargetDto
UploadSessionDto
CompleteUploadSessionRequest
FinalizeUploadSessionRequest
FinalizeUploadSessionResponse
FileDto
DocumentAttachmentDto
AttachFileToDocumentRequest
DocumentAttachmentsResponse
```

不要暴露 EF entity。

## Application 服务建议

新增：

```text
Files
  IUploadSessionService
  IFileService
  IDocumentAttachmentService
  IFileReferenceExtractor
  IFileOutboxWriter
```

Infrastructure：

```text
Files
  EfUploadSessionRepository
  EfFileRepository
  EfDocumentAttachmentRepository
  LocalFileStorage
```

Domain：

```text
Files
  File
  UploadSession
  DocumentAttachment
  FileOutboxEvent
```

如果 `File` 与 `System.IO.File` 命名冲突，可以命名为：

```text
StoredFile
```

表名仍用 `files`。

## Migration

新增 migration，建议名称：

```text
AddFilesUploadSessionsPhase6
```

要求：

- 表名 snake_case。
- 所有核心表有 `workspace_id`。
- `files(storage_provider, bucket, object_key)` 唯一。
- `upload_sessions(workspace_id, idempotency_key)` 唯一。
- `upload_sessions(status, expires_at)` 索引。
- `file_outbox_events(status, next_retry_at, created_at)` 索引。
- JSON 字段使用 jsonb。

## Tests

至少新增：

### Upload session tests

- editor 可以创建 upload session。
- viewer 创建 upload session 返回 403。
- 相同 idempotency key 返回同一 session 或等价 session。
- complete 前 finalize 返回 validation error。
- completed session 可以 finalize，并创建 file。
- finalized session 再 finalize 幂等返回已有 file，不重复创建。
- abort 后不可 finalize。
- local upload 使用 stream 写入，不把大文件完整读入内存；至少用测试或代码结构证明不是 `ReadAllBytes` 或整包 buffering。
- checksum 不匹配时 complete 返回 validation error。

### File access tests

- workspace viewer 可以 GET file metadata/content。
- 非 workspace member 访问 file 返回 403 或 404，保持当前权限语义一致。
- deleted file metadata/content 不可访问。
- delete file 写 outbox event。
- file 仍有 active attachments 时，删除 file 返回 409。
- 删除 attachment relation 后，再删除 file 可以成功软删除。

### Attachment tests

- editor 可以 attach file to document。
- 跨 workspace file/document attach 返回 validation error 或 forbidden。
- viewer 不能 attach/delete attachment。
- document attachments list 返回 attachment。

### Tiptap reference tests

- 保存 document content 引用同 workspace file 成功。
- 保存 content 引用不存在 file 返回 400。
- 保存 content 引用跨 workspace file 返回 400 或 403。
- 保存 image file reference 后自动创建 inline_image attachment，或按你选择的策略返回明确错误。

### Regression tests

- Phase 5 archive/delete/import/export 仍通过。
- Auth/permissions 仍通过。
- no-op PATCH 仍不递增 revision。

### PostgreSQL smoke

扩展 smoke profile：

- migration + seed。
- create upload session。
- local upload/complete/finalize。
- attach to document。
- file content access。
- delete file。

普通 `dotnet test` 仍不强制依赖 PostgreSQL。

## README / Contract 更新

更新：

```text
services/api/README.md
apps/web/FRONTEND_API_CONTRACT.md
```

说明：

- 文件 API。
- local storage 配置。
- 默认最大文件大小和允许 MIME type。
- 本阶段只支持 single upload 或说明 multipart 未启用。
- 文件访问必须走 API，不提供永久 URL。
- PostgreSQL smoke 如何启用文件测试。

## 验证命令

完成后必须运行：

```powershell
dotnet restore .\Northstar.sln
dotnet build .\Northstar.sln --no-restore
dotnet test .\Northstar.sln --no-build
dotnet ef migrations script --project .\src\Northstar.Infrastructure\Northstar.Infrastructure.csproj --startup-project .\src\Northstar.Api\Northstar.Api.csproj --idempotent
dotnet ef migrations has-pending-model-changes --project .\src\Northstar.Infrastructure\Northstar.Infrastructure.csproj --startup-project .\src\Northstar.Api\Northstar.Api.csproj
```

如果可用，运行 PostgreSQL smoke：

```powershell
$env:NORTHSTAR_POSTGRES_SMOKE_CONNECTION="Host=localhost;Port=5432;Database=northstar_smoke;Username=postgres;Password=postgres"
dotnet test .\Northstar.sln --filter PostgreSqlSmoke
```

如果真实 PostgreSQL smoke 未运行，必须说明原因。

## 完成后汇报

请汇报：

- 新增了哪些文件 API。
- 本阶段支持 single upload 还是 multipart。
- 使用了 Local storage 还是 S3/MinIO provider。
- 新增了哪些实体、表、migration。
- upload session 状态机如何保证 finalize 后才创建 file。
- document attachment 和 Tiptap file reference 如何校验。
- file outbox 写了哪些事件。
- 更新了哪些测试。
- restore/build/test/migration script/has-pending-model-changes 结果。
- PostgreSQL smoke 是否运行。
- Phase 7 建议做什么。

Phase 7 初步建议：

1. 评论线程 `comment_threads/comments`。
2. 文件缩略图、metadata extraction、virus scan 可以作为 Phase 6.1 或 Phase 7 前置增强。
3. Yjs 协作继续后置，等文件/评论边界稳定后再做。
