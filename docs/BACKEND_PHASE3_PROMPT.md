# Backend Phase 3 Prompt

本提示词用于在当前 `services/api` 的 Phase 2 成果上继续实现 Phase 3。目标不是重建项目，也不是补旧架构，而是在现有 Clean Architecture 骨架内补齐 context、activity、search 所需的主流持久化模型和应用服务。

## Phase 2 验证结果

当前工作目录是 `D:\editor`，后端项目在：

```text
D:\editor\services\api
```

我已在 2026-04-28 对当前 Phase 2 结果做过验证：

```powershell
dotnet restore .\Northstar.sln
dotnet build .\Northstar.sln --no-restore
dotnet test .\Northstar.sln --no-build
```

结果：

- restore 成功，项目均为最新。
- build 成功，0 warning，0 error。
- test 成功，3 个 test project 共 7 个测试全部通过。

已实现 API：

```text
GET   /api/v1/bootstrap
GET   /api/v1/spaces/{spaceId}/map
POST  /api/v1/documents
GET   /api/v1/documents/{documentId}
PATCH /api/v1/documents/{documentId}
PATCH /api/v1/documents/{documentId}/location
```

当前仍是 stub：

```text
GET /api/v1/documents/{documentId}/context
GET /api/v1/documents/{documentId}/activity
```

尚未实现：

```text
GET /api/v1/search
```

当前 `NorthstarDbContext` 已有：

- `Users`
- `WorkspaceMembers`
- `Spaces`
- `Collections`
- `Documents`
- `DocumentDrafts`
- `Tags`
- `DocumentTags`

当前缺少 Phase 3 所需的核心表：

- `document_versions`
- `document_links`
- `activity_events`
- `document_search_index`

## 开始前必须阅读

请先阅读并遵守：

- `docs/BACKEND_ARCHITECTURE_V1.md`
- `docs/BACKEND_REFACTOR_RULES.md`
- `docs/BACKEND_DATA_MODEL_V1.md`
- `apps/web/FRONTEND_API_CONTRACT.md`

旧项目边界：

- `services/api-old` 只读参考，不要修改。
- `E:\ClayMo\services\file-service` 只读参考，不要修改。
- 不要引入 ABP。
- 不要迁移旧的 `ClayMo.*`、`NS.Abp`、`Module.*` 架构。
- 不要把旧 Go/go-zero file-service 当作运行依赖。

## Phase 3 目标

在现有 `services/api` 上增量实现：

1. 补齐 `document_versions`、`document_links`、`activity_events`、`document_search_index` 的实体、EF 配置、DbContext、migration。
2. 将 `GET /api/v1/documents/{documentId}/context` 从 stub 改为真实查询。
3. 将 `GET /api/v1/documents/{documentId}/activity` 从 stub 改为真实查询。
4. 实现 `GET /api/v1/search?q=&spaceId=`。
5. 在文档 create/update/move 流程中同步维护 activity、links、search index。
6. 保持 Phase 2 已有 API 和测试不破坏。

本阶段不要做：

- 登录认证
- 复杂权限 ACL
- 文件上传
- 评论
- Yjs 协作
- AI
- 外部搜索引擎
- 发布工作流 UI
- 文档删除/归档 API

## 架构要求

继续保持当前分层：

```text
Northstar.Api
  -> Northstar.Application
  -> Northstar.Contracts
  -> Northstar.Infrastructure

Northstar.Application
  -> Northstar.Domain
  -> Northstar.Contracts

Northstar.Infrastructure
  -> Northstar.Application
  -> Northstar.Domain
  -> Northstar.Contracts

Northstar.Domain
  -> no project dependency
```

规则：

- Controller 只接收参数、调用 Application service、返回 DTO。
- 不要在 Controller 里直接使用 DbContext。
- 事务边界仍放在 Application service。
- EF Core 实现放在 Infrastructure。
- DTO 放在 Contracts，不要直接暴露 EF entity。
- search index 可以作为 Infrastructure 读模型，也可以作为轻量 Domain entity，但不要把它泄漏给 API contract。

## 需要实现的 API

### 1. GET `/api/v1/documents/{documentId}/context`

当前 DTO 已存在：

```csharp
DocumentContextResponse(
    IReadOnlyList<RelatedDocumentDto> RelatedDocuments,
    IReadOnlyList<VersionTrailItemDto> VersionTrail,
    IReadOnlyList<BacklinkItemDto> Backlinks)
```

实现规则：

- document 不存在或已删除时返回 `404 NOT_FOUND`。
- `relatedDocuments` 来自 `document_links` 中当前文档的出站内部文档链接。
- `backlinks` 来自 `document_links` 中指向当前文档的入站内部文档链接。
- `versionTrail` 来自 `document_versions`，并且可以在顶部追加当前 draft 的伪版本项。
- 不要为了 context 在 Controller 内拼查询，放到 Application/Infrastructure query service。

`code` 字段规则：

- 目前数据库没有单独的文档编号字段。
- Phase 3 先用稳定的展示编号生成器，例如按 collection/document 排序生成 `01.001`。
- 不要为了这个字段过早加复杂编号表。

backlink excerpt 规则：

- 优先使用 `document_links.anchor_text`。
- 如果没有 anchor_text，从 source document 的 `document_drafts.text_content` 截取 120-180 字符。
- 不要返回整篇正文。

### 2. GET `/api/v1/documents/{documentId}/activity`

当前 DTO 已存在：

```csharp
DocumentActivityResponse(IReadOnlyList<ActivityTimelineItemDto> Items)
```

实现规则：

- document 不存在或已删除时返回 `404 NOT_FOUND`。
- 读取 `activity_events`，限定：
  - `entity_type = 'document'`
  - `entity_id = documentId`
  - 同 workspace
- 按 `created_at DESC` 返回。
- 每个成功的 create/update/move 最多写一条 activity，不要每个字段一条。
- activity 只做产品内时间线，不要在 Phase 3 做完整审计日志。

建议 action：

```text
document.created
document.updated
document.moved
document.tags_updated
```

如果一次 PATCH 同时修改 title/content/tags，建议合并为一条 `document.updated`，summary/detail 里描述变化范围。

### 3. GET `/api/v1/search`

新增 Contracts DTO，例如：

```csharp
public sealed record SearchResponse(IReadOnlyList<SearchResultDto> Results);

public sealed record SearchResultDto(
    string Id,
    string Type,
    string Title,
    string FolderId,
    string Excerpt,
    DateTimeOffset UpdatedAt);
```

接口：

```text
GET /api/v1/search?q={query}&spaceId={spaceId}
```

实现规则：

- `q` 为空或全空白时返回空 results，不报错。
- `spaceId` 必须是合法 UUID；不存在时返回 `404 NOT_FOUND` 或空结果，优先保持与现有错误风格一致。
- 第一版只返回 `type = "document"`。
- 搜索范围限定在指定 space。
- 排除 `deleted_at IS NOT NULL` 的文档。
- 默认 limit 可用 20 或 30，避免无界返回。
- excerpt 从 `document_search_index.text_content` 生成，不返回整篇正文。

搜索实现建议：

- Phase 3 先用数据库内搜索，不接 Meilisearch/OpenSearch/Elasticsearch。
- 表结构保留 `document_search_index`，字段至少包含：
  - `document_id`
  - `workspace_id`
  - `space_id`
  - `title`
  - `text_content`
  - `updated_at`
- 如果当前 EF/Npgsql 配置能稳定支持 PostgreSQL `tsvector + GIN`，按 `docs/BACKEND_DATA_MODEL_V1.md` 实现 `search_vector`。
- 如果 `tsvector` 会明显拖慢本阶段或破坏 InMemory 测试，可以先用 `title + text_content` 做 case-insensitive contains/ILIKE，并在 migration 中保留后续加 `search_vector` 的位置。不要接外部搜索引擎。

## 数据模型

### DocumentVersion

建议位置：

```text
Northstar.Domain/Knowledge/Versions/DocumentVersion.cs
```

核心字段参考 `docs/BACKEND_DATA_MODEL_V1.md`：

- `Id`
- `WorkspaceId`
- `DocumentId`
- `VersionNo`
- `Label`
- `VersionType`
- `Content`
- `TextContent`
- `Outline`
- `WordCount`
- `CreatedBy`
- `CreatedAt`
- `PublishedAt`

版本策略：

- `documents.revision` 仍只用于自动保存乐观锁。
- 不要把每次 PATCH 自动保存都写成一个 `document_versions`。
- `document_versions` 只表示不可变快照，例如 system/imported/manual/published。
- Phase 3 可在 `POST /documents` 时创建一条 `system` 初始版本 `1.0`。
- Seeder 中每个 seed document 必须有一条幂等的初始版本。
- 未来的发布/手动快照 endpoint 不在 Phase 3 做。

### DocumentLink

建议位置：

```text
Northstar.Domain/Knowledge/Links/DocumentLink.cs
```

核心字段：

- `Id`
- `WorkspaceId`
- `SourceDocumentId`
- `TargetDocumentId`
- `TargetUrl`
- `LinkType`
- `AnchorText`
- `SourceAnchor`
- `TargetAnchor`
- `CreatedBy`
- `CreatedAt`

实现规则：

- 当前阶段至少支持内部文档链接。
- 从 Tiptap JSONContent 中递归提取链接。
- 支持以下常见形态：
  - link mark 的 `attrs.href` 包含 `/documents/{uuid}`。
  - link mark 的 `attrs.href` 直接是 UUID。
  - node 或 mark 的 `attrs.documentId` / `attrs.targetDocumentId`。
- 无效 UUID、跨 workspace、目标文档不存在、自引用都应忽略，不要让保存失败。
- 每次 content 成功保存后，重建当前文档的出站 links。
- 提取到的内部链接默认 `link_type = 'reference'`。
- `relatedDocuments` 可先返回 `reference` 和 `related` 两类内部出站链接。

### ActivityEvent

建议位置：

```text
Northstar.Domain/Knowledge/Activity/ActivityEvent.cs
```

核心字段：

- `Id`
- `WorkspaceId`
- `ActorId`
- `EntityType`
- `EntityId`
- `Action`
- `Summary`
- `Metadata`
- `CreatedAt`

实现规则：

- Phase 3 只写 document 相关 activity。
- `Metadata` 用 json/jsonb 存结构化补充信息，例如 old/new folder id、changedFields。
- 不要把 activity 当完整审计日志，不需要记录敏感字段。
- Seeder 应幂等创建基础 activity，方便 activity endpoint 不再返回空。

### DocumentSearchIndex

建议位置：

```text
Northstar.Infrastructure/Search/DocumentSearchIndex.cs
```

或放在 Domain 的轻量实体中，但不要给它业务行为。

核心字段：

- `DocumentId`
- `WorkspaceId`
- `SpaceId`
- `Title`
- `TextContent`
- `UpdatedAt`
- 可选 `SearchVector`

实现规则：

- `POST /documents` 后创建 search index。
- `PATCH /documents/{id}` 修改 title/content 后更新 search index。
- `PATCH /documents/{id}/location` 修改 space/folder 时同步更新 index 的 space/folder 相关字段；当前 Phase 2 不允许跨 space 移动，仍要保持代码边界清晰。
- Seeder 必须幂等补齐所有 seed documents 的 search index。
- 可以提供一个内部 rebuild 方法，便于以后后台任务重建索引。

## Application 服务建议

可以新增或调整以下接口，命名可按现有风格微调：

```text
IDocumentContextService
IDocumentActivityService
ISearchService
IDocumentDerivedDataWriter
IDocumentLinkExtractor
```

职责建议：

- `DocumentService` 继续负责 create/update/move 用例。
- `IDocumentDerivedDataWriter` 负责在事务内维护 links、activity、search index。
- `IDocumentLinkExtractor` 只负责从 Tiptap JSON 提取候选链接，不访问数据库。
- `IDocumentContextService` 负责 context endpoint 的用例编排。
- `IDocumentActivityService` 负责 activity endpoint 的用例编排。
- `ISearchService` 负责 search endpoint。

不要让 `DocumentService` 变成所有查询和投影逻辑的大杂烩。

## 更新现有流程

### Create document

成功创建文档时，同一事务内：

1. 创建 `documents`
2. 创建 `document_drafts`
3. 创建初始 `document_versions`，label 建议 `1.0`，type 建议 `system`
4. 创建 `activity_events: document.created`
5. 创建 `document_search_index`
6. 返回当前 document 和 map

### Update document

成功 PATCH 时，同一事务内：

1. 校验 `baseRevision`
2. 更新 title/content/tags
3. `documents.revision + 1`
4. 如果 content 变更，重建出站 `document_links`
5. 更新 `document_search_index`
6. 写一条 activity
7. 返回最新 document

注意：

- 不要每次 PATCH 都创建 `document_versions`。
- 如果 content 是 JSON object，继续用现有 `DocumentContentAnalyzer` 生成 `text_content`、`outline`、`word_count`、`content_hash`。
- 如果 request 只改 tags，也可以更新 activity，但不必重建 links。

### Move document

成功移动时，同一事务内：

1. 更新 document location/sort order
2. 写一条 `document.moved` activity
3. 必要时同步 search index 的 space/folder 相关字段
4. 返回 document summary 和 map

## Seeder 要求

更新 `NorthstarDataSeeder`，保持幂等：

- 仍然创建 Northstar workspace、Atlas Library space、7 个 collections、3 个 documents。
- 每个 seed document 都要有：
  - draft
  - tags
  - 初始 document_version
  - search index
  - created activity
- 至少增加一两个内部文档链接，便于 context/backlinks 能被测试覆盖。

建议：

- `Mission & Vision` 链接到 `Our Principles`。
- `Operating System` 链接到 `Mission & Vision`。

Seed 不要使用随机 ID。继续使用稳定 seed IDs 或补充新的稳定 seed IDs。

## EF / Migration 要求

新增 EF migration，名称建议：

```text
AddKnowledgeContextActivitySearchPhase3
```

要求：

- PostgreSQL 表名保持 snake_case。
- `workspace_id` 必须出现在核心业务表和投影表中。
- 配置必要索引：
  - `document_versions(workspace_id, document_id, version_no desc)`
  - `document_links(workspace_id, source_document_id, link_type)`
  - `document_links(workspace_id, target_document_id)`
  - `activity_events(workspace_id, entity_type, entity_id, created_at desc)`
  - `document_search_index(workspace_id, space_id)`
  - 如果实现 `search_vector`，加 GIN index。
- JSON 字段使用 jsonb。
- 不要手写临时 SQL 表绕过 EF migration。

## 测试要求

在现有 7 个测试基础上补测试。至少覆盖：

1. `GET /api/v1/documents/{id}/context`
   - seed document 能返回 versionTrail。
   - 被其他文档引用时能返回 backlinks。
2. `GET /api/v1/documents/{id}/activity`
   - seed document 或新建 document 能返回 activity items。
3. `GET /api/v1/search`
   - 按 title 能搜到文档。
   - 按正文 text_content 能搜到文档。
   - 空 q 返回空 results。
4. `PATCH /api/v1/documents/{id}`
   - 更新正文后 search index 可搜到新正文。
   - 更新正文中内部链接后 context/backlinks 会变化。
   - revision conflict 旧测试仍然通过。
5. Link extractor 单元测试：
   - 能从 Tiptap link mark href 中提取 document id。
   - 忽略非法 UUID。

如果 InMemory provider 不适合测试 PostgreSQL full text search，不要为了测试把生产代码写坏。可以：

- 将搜索匹配逻辑放在 query service 中做可测试分支。
- 或把 search API 测试限定在不依赖 PostgreSQL 特有函数的路径。
- 或引入 Testcontainers PostgreSQL，但不要让 Phase 3 被测试基础设施拖成大重构。

## 验证命令

完成后必须运行：

```powershell
dotnet restore .\Northstar.sln
dotnet build .\Northstar.sln --no-restore
dotnet test .\Northstar.sln --no-build
```

如果新增 migration，请汇报 migration 名称。

## 完成后汇报

请汇报：

- 新增/修改了哪些 API。
- 哪些 stub 已经移除。
- 新增了哪些实体、表和 migration。
- create/update/move 如何维护 links、activity、search index。
- `restore/build/test` 结果。
- Phase 4 建议做什么。

Phase 4 建议优先级：

1. 认证和 workspace member 权限。
2. 文档删除/归档和 import/export。
3. 文件 upload session 模块。
4. 评论和协作再往后放。
