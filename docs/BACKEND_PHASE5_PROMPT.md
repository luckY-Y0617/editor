# Backend Phase 5 Prompt

本提示词用于在当前 `services/api` 的 Phase 4 成果上继续实现 Phase 5。Phase 5 的主线是文档生命周期和空间级 import/export，并先收口 Phase 4 QA 中发现的 logout 行为细节。

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
GET    /api/v1/documents/{documentId}/context
GET    /api/v1/documents/{documentId}/activity
GET    /api/v1/search?q=&spaceId=
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

Phase 4 新增 migration：

```text
20260428061730_AddAuthWorkspacePermissionsPhase4
```

当前复核结果：

- `dotnet restore .\Northstar.sln` 成功。
- `dotnet build .\Northstar.sln --no-restore` 成功，0 warning，0 error。
- `dotnet test .\Northstar.sln --no-build` 成功，25 tests passed。
- `dotnet ef migrations script ... --idempotent` 成功生成 SQL。
- 仍有本机 `dotnet-ef` 8.0.10 低于 runtime 8.0.11 的提示，不影响当前结果。

架构边界：

- 没有 ABP / SqlSugar / 旧项目引用。
- `Domain` 仍无项目依赖。
- `Api` 没有直接使用 DbContext。
- 权限判断主要在 Application 层。
- PostgreSQL smoke profile 通过 `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` 启用，默认 `dotnet test` 不要求 Docker 或 PostgreSQL。

## 开始前必须阅读

必须阅读并遵守：

- `docs/BACKEND_ARCHITECTURE_V1.md`
- `docs/BACKEND_REFACTOR_RULES.md`
- `docs/BACKEND_DATA_MODEL_V1.md`
- `docs/BACKEND_PHASE4_PROMPT.md`
- `apps/web/FRONTEND_API_CONTRACT.md`
- `services/api/README.md`

旧项目边界：

- `services/api-old` 只读参考，不要修改。
- `E:\ClayMo\services\file-service` 只读参考，不要修改。
- 不要引入 ABP。
- 不要迁移旧的 `ClayMo.*`、`NS.Abp`、`Module.*` 架构。
- 不要把旧 Go/go-zero file-service 当作运行依赖。

## Phase 5 总目标

本阶段完成三件事：

1. 收口 Phase 4 logout 行为。
2. 实现文档归档、删除、恢复的第一版生命周期 API。
3. 实现 space 级 JSON export/import，让当前知识库可以可靠备份、迁移和回灌。

本阶段不要做：

- 文件上传 / upload session
- 评论
- Yjs 协作
- AI
- 外部搜索引擎
- 复杂资源级 ACL
- 复杂导入任务队列
- 跨 workspace 迁移工具

文件模块建议放到 Phase 6。

## Phase 5A: Phase 4 Cleanup

### Logout 允许凭 refresh token 撤销

当前行为：

- `POST /api/v1/auth/logout` 要求 `[Authorize]`。
- 如果 access token 已过期，但 refresh token 仍有效，前端无法直接注销 refresh token，只能先 refresh 再 logout。

要求：

- 将 `POST /api/v1/auth/logout` 改为允许匿名。
- logout 只依赖 request body 中的 refresh token。
- 找到 refresh token hash 后撤销；找不到也返回 `204 NoContent`，避免泄漏 token 是否存在。
- 保留 auth event：
  - 找到 token 时记录 `auth.logout`。
  - 找不到 token 可以不记录，或记录匿名失败事件，但不要返回 401。

补测试：

- 未带 access token 但带有效 refresh token 时，logout 返回 204，随后 refresh 返回 401。
- 无效 refresh token logout 仍返回 204。

## Phase 5B: Document Lifecycle

### 新增 API

实现以下接口：

```text
PATCH  /api/v1/documents/{documentId}/archive
PATCH  /api/v1/documents/{documentId}/restore
DELETE /api/v1/documents/{documentId}
```

建议响应：

```text
PATCH archive  -> MoveDocumentResponse 或 GetDocumentResponse
PATCH restore  -> MoveDocumentResponse 或 GetDocumentResponse
DELETE         -> 204 NoContent
```

选择一个一致风格即可。若返回 map，前端更容易同步左侧树。

### 行为定义

归档：

- 设置 `documents.status = 'archived'`。
- 设置 `documents.archived_at`。
- 保留 `document_drafts`、`document_versions`、`document_links`、`activity_events`。
- 更新 search index，使默认搜索不返回 archived document。
- 写 activity：`document.archived`。
- 需要 editor+ 权限。

恢复：

- 将 `documents.status` 从 `archived` 恢复为 `draft`。
- 清空 `archived_at`。
- 恢复 search index 可见性。
- 写 activity：`document.restored`。
- 需要 editor+ 权限。

删除：

- 使用软删除，不要 hard delete。
- 设置 `documents.deleted_at`。
- 从默认 map/search/context 中排除。
- 可以保留 tags、versions、activity，用于未来 trash/audit。
- 删除或失效当前文档的 search index。建议物理删除 `document_search_index` 当前行，避免误搜。
- 删除或失效当前文档相关 document_links。建议删除 source/target 任一端为当前文档的 links，避免 backlinks 指向已删除文档。
- 写 activity：`document.deleted`，activity 可以保留。
- 需要 editor+ 权限。

注意：

- Phase 5 不做永久删除。
- Phase 5 不做 trash 列表 UI，但 restore API 要能支持未来 trash。
- 已删除文档 direct GET 返回 `404 NOT_FOUND`。
- 已归档文档 direct GET 可以返回，status 为 `archived`。

### Map/Search 默认规则

更新默认查询：

- `GET /bootstrap` 默认不返回 deleted 或 archived documents。
- `GET /spaces/{spaceId}/map` 默认不返回 deleted 或 archived documents。
- `GET /search` 默认不返回 deleted 或 archived documents。
- `GET /documents/{id}/context` 不返回 deleted 或 archived related/backlink 文档。

可以新增可选参数，但不是必须：

```text
GET /api/v1/spaces/{spaceId}/map?includeArchived=true
GET /api/v1/search?q=&spaceId=&includeArchived=true
```

如果增加参数，必须保持默认行为不影响当前前端。

### Domain/Application 要求

建议给 `Document` 增加领域方法：

```csharp
Archive(Guid? editedBy)
Restore(Guid? editedBy)
Delete(Guid? editedBy)
```

规则：

- 不能归档已删除文档。
- 不能恢复已删除文档，除非后续实现 trash restore；Phase 5 暂不做。
- 重复 archive 可以幂等返回当前状态，不要制造多条 activity。
- 重复 delete 可以返回 204，不要报错。
- restore 只对 archived document 生效。

Application 层继续负责：

- 权限检查。
- 事务边界。
- 派生数据维护。
- 返回 DTO。

不要在 Controller 中写状态规则。

### 派生数据维护

扩展 `IDocumentDerivedDataWriter` 或新增生命周期 writer：

- archive：移除或标记 search index 不可见，写 activity。
- restore：重建 search index，写 activity。
- delete：删除 search index、删除 related/backlink links、写 activity。

如果选择在 `document_search_index` 增加 `is_archived` 或 `is_deleted`，需要新增 migration；如果直接删除 search index 行，可以减少 schema 变化。

## Phase 5C: Space Export

### 新增 API

```text
GET /api/v1/spaces/{spaceId}/export
```

权限：

- viewer+ 可以 export。

默认行为：

- 导出当前 space 中未删除文档。
- 默认包含 archived documents，因为 export 是备份/迁移能力。

可选参数：

```text
includeArchived=true|false
includeDeleted=false
```

建议 Phase 5：

- 支持 `includeArchived`，默认 true。
- 不支持 `includeDeleted` 或固定 false，避免把 trash 语义提前复杂化。

### Export DTO

放在 `Northstar.Contracts`，建议：

```csharp
public sealed record ExportSpaceResponse(
    string SchemaVersion,
    DateTimeOffset ExportedAt,
    WorkspaceExportDto Workspace,
    SpaceExportDto Space,
    IReadOnlyList<CollectionExportDto> Collections,
    IReadOnlyList<DocumentExportDto> Documents);
```

Document export 至少包含：

- `id`
- `folderId`
- `title`
- `status`
- `sortOrder`
- `tags`
- `content`
- `revision`
- `createdAt`
- `updatedAt`

注意：

- `id` 可以导出原 ID，但 import 时不应默认复用原 ID。
- `content` 使用 Tiptap JSON object，不要字符串化给前端。
- 不要导出 password/token/auth 数据。
- 不要导出 activity_events，除非后续有审计迁移需求。
- 不要导出 deleted documents。

## Phase 5D: Space Import

### 新增 API

```text
POST /api/v1/spaces/{spaceId}/import
```

权限：

- editor+ 可以 import。

Phase 5 只支持 append：

```ts
type ImportSpaceRequest = {
  mode: "append";
  documents: KnowledgeDocument[];
};
```

也可以复用 `ExportSpaceResponse` 的 collections/documents 结构，但请求 DTO 应明确，不要让导入直接接受任意完整 export 后无校验落库。

响应：

```csharp
public sealed record ImportSpaceResponse(
    int ImportedCollectionCount,
    int ImportedDocumentCount,
    KnowledgeMapResponse Map);
```

### Import 行为

规则：

- 只向目标 space 追加，不覆盖现有文档。
- 原 document id 不复用，生成新 id。
- 原 folderId 需要映射到目标 collection。
- 如果导入中 collection 不存在，可以创建 collection。
- 如果只导入 documents 而没有 collections，则放入默认/第一个 collection，或要求 request 提供 folderId。请选择一种并在 DTO 中明确。
- title 冲突时允许重名，但 slug 必须保证唯一或可为空。
- tags 复用现有 `tags + document_tags` 逻辑。
- content 必须是合法 JSON object。
- 导入每个 document 时：
  - 创建 document。
  - 创建 draft。
  - 创建初始 document_version，type 建议 `imported`，label `1.0`。
  - 重建 links。跨导入文档的内部链接如果仍指向旧 id，需要尽量映射到新 id。
  - 创建 search index。
  - 写 activity：`document.imported`。

### Link 映射

导入时要避免内部链接仍指向源环境旧 ID。

建议：

1. 第一遍创建所有 documents，并建立 `oldDocumentId -> newDocumentId` 映射。
2. 第二遍处理 content 中的内部 document links：
   - 如果 href/documentId 指向导入批次中的 old id，则替换为 new id。
   - 如果目标不在导入批次中，保留原 href 但不要创建 document_link，避免伪造不存在 backlinks。
3. 重建 `document_links`。

如果 Phase 5 不想实现 link rewrite，必须在完成汇报中明确限制；但至少不能让保存失败。

### Import 校验

必须限制：

- 单次导入 document 数量，例如最多 200。
- 单篇 content 大小，例如最多 2MB。
- 总 payload 大小可先依赖 ASP.NET 默认限制，但建议在 DTO/use case 层做业务限制。

错误处理：

- 任意文档校验失败时，整个 import 事务回滚。
- 返回 `400 VALIDATION_ERROR`，details 中说明失败文档索引和原因。

## Phase 5E: Contracts / Frontend Contract

更新后端 Contracts：

- document lifecycle request/response DTO。
- export/import DTO。

同时更新：

```text
apps/web/FRONTEND_API_CONTRACT.md
```

要求：

- 增加 archive/restore/delete API。
- 补充 export/import 最终字段。
- 说明 archived/deleted 在 map/search/bootstrap 中的默认可见性。
- 前端可以后续适配，不要求本阶段改前端。

## Testing

至少新增/更新测试：

### Lifecycle tests

- editor 可以 archive document。
- viewer 不能 archive/delete/restore。
- archived document 不出现在 bootstrap/map/search 默认结果中。
- direct GET archived document 返回 status `archived`。
- delete 后 direct GET 返回 404。
- delete 后 search 不再返回该 document。
- delete 后 backlinks/related 不再返回该 document。
- repeated archive/delete 不制造重复 activity。

### Export tests

- viewer 可以 export space。
- export 包含 collections、documents、tags、content。
- export 不包含 auth/token/user credential。
- export 默认包含 archived、不包含 deleted。

### Import tests

- editor 可以 append import。
- viewer import 返回 403。
- import 创建 documents/drafts/tags/versions/search/activity。
- import 后 map 返回新增文档。
- import 内部链接能映射新 document id，或至少不会创建坏 backlinks。
- invalid JSON content 返回 400 且事务回滚。

### Regression tests

- Phase 4 auth 测试仍通过。
- no-op PATCH 仍不递增 revision。
- refresh/logout 行为按 Phase 5A 新规则通过。

### PostgreSQL smoke

更新 PostgreSQL smoke profile，覆盖：

- migration + seed。
- archive/delete/search。
- export/import append。

普通 `dotnet test` 仍然不应强制依赖 PostgreSQL。

## Migration

优先复用已有字段：

- `documents.status`
- `documents.archived_at`
- `documents.deleted_at`

如果可以通过已有字段和删除 search index/link rows 完成，不必新增 migration。

如果新增字段或表，migration 名称建议：

```text
AddDocumentLifecycleImportExportPhase5
```

不要手写临时 SQL 表绕过 EF migration。

## 验证命令

完成后必须运行：

```powershell
dotnet restore .\Northstar.sln
dotnet build .\Northstar.sln --no-restore
dotnet test .\Northstar.sln --no-build
dotnet ef migrations script --project .\src\Northstar.Infrastructure\Northstar.Infrastructure.csproj --startup-project .\src\Northstar.Api\Northstar.Api.csproj --idempotent
```

如果更新 PostgreSQL smoke：

```powershell
$env:NORTHSTAR_POSTGRES_SMOKE_CONNECTION="Host=localhost;Port=5432;Database=northstar_smoke;Username=postgres;Password=postgres"
dotnet test .\Northstar.sln --filter PostgreSqlSmoke
```

如果没有运行 PostgreSQL smoke，必须说明原因。

## 完成后汇报

请汇报：

- logout cleanup 的新行为。
- 新增了哪些 document lifecycle API。
- archive/delete/restore 对 map/search/context/activity 的影响。
- export/import 的 DTO 和行为边界。
- 是否新增 migration，migration 名称是什么。
- 更新了哪些测试。
- restore/build/test/migration script 结果。
- PostgreSQL smoke 是否运行。
- Phase 6 建议做什么。

Phase 6 初步建议：

1. 文件 upload session / files / document attachments。
2. 基于旧 `E:\ClayMo\services\file-service` 只参考流程，不迁移 Go/go-zero 架构。
3. 实现 `upload_sessions -> files -> document_attachments`，保留 outbox。
4. 评论线程继续后置。
5. Yjs 协作继续后置。
