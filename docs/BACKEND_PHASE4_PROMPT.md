# Backend Phase 4 Prompt

本提示词用于在当前 `services/api` 的 Phase 3 成果上继续实现 Phase 4。Phase 4 的主线是认证与 workspace member 权限，但开始前必须先做一轮 Phase 3 QA cleanup，避免把已有保存、activity、search 的隐患带进权限层。

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
GET   /api/v1/bootstrap
GET   /api/v1/spaces/{spaceId}/map
POST  /api/v1/documents
GET   /api/v1/documents/{documentId}
PATCH /api/v1/documents/{documentId}
PATCH /api/v1/documents/{documentId}/location
GET   /api/v1/documents/{documentId}/context
GET   /api/v1/documents/{documentId}/activity
GET   /api/v1/search?q=&spaceId=
```

Phase 3 新增 migration：

```text
20260428053422_AddKnowledgeContextActivitySearchPhase3
```

当前重新验证结果：

```powershell
dotnet restore .\Northstar.sln
dotnet build .\Northstar.sln --no-restore
dotnet test .\Northstar.sln --no-build
dotnet ef migrations script --project .\src\Northstar.Infrastructure\Northstar.Infrastructure.csproj --startup-project .\src\Northstar.Api\Northstar.Api.csproj --idempotent
```

结果：

- restore 成功。
- build 成功，0 warning，0 error。
- test 成功，13 个测试全部通过。
- idempotent migration SQL 可生成。
- 本机 `dotnet-ef` 8.0.10 比 runtime 8.0.11 旧，有工具版本提示，不影响当前结果，但后续可顺手统一。

## 开始前必须阅读

必须阅读并遵守：

- `docs/BACKEND_ARCHITECTURE_V1.md`
- `docs/BACKEND_REFACTOR_RULES.md`
- `docs/BACKEND_DATA_MODEL_V1.md`
- `docs/BACKEND_PHASE3_PROMPT.md`
- `apps/web/FRONTEND_API_CONTRACT.md`

旧项目边界：

- `services/api-old` 只读参考，不要修改。
- `E:\ClayMo\services\file-service` 只读参考，不要修改。
- 不要引入 ABP。
- 不要迁移旧的 `ClayMo.*`、`NS.Abp`、`Module.*` 架构。
- 不要把旧 Go/go-zero file-service 当作运行依赖。

## Phase 4 总目标

本阶段完成两件事：

1. 先收口 Phase 3 QA 问题，保证保存流、activity、search、migration 验证更可靠。
2. 实现第一版认证与 workspace member 权限，让 API 从默认 seed user 过渡到真实 current user / current workspace 上下文。

本阶段不要做：

- 文件上传 / upload session
- 评论
- Yjs 协作
- AI
- 外部搜索引擎
- 复杂资源级 ACL
- 复杂组织/团队/邀请流
- 多租户动态数据库

## Phase 4A: QA Cleanup

### 1. 修复 no-op PATCH 噪声

当前风险：

- `PATCH /api/v1/documents/{id}` 只要请求进来就会递增 `revision`。
- 即使 title/content/tags 没有真实变化，也会写 `activity_events`。
- Phase 3 后这会造成无意义 revision 增长、activity 噪声、search index 更新时间漂移。

要求：

- 对 `UpdateDocumentRequest` 做真实变更判断。
- 只有发生真实变更时才：
  - `documents.revision + 1`
  - 更新 `updated_at`
  - 更新 draft/tags
  - 重建 links
  - 更新 search index
  - 写 activity
- 没有真实变更时：
  - 不递增 revision
  - 不写 activity
  - 不更新 search index
  - 不重建 links
  - 返回当前 document DTO

变更判断建议：

- title：trim 后与当前 title 比较。
- content：用 `DocumentContentAnalyzer` 生成 `ContentHash`，与当前 `DocumentDraft.ContentHash` 比较。
- tags：规范化后与当前 document tags 按 slug 或 case-insensitive name 集合比较。

注意：

- `baseRevision` 校验仍然必须执行。即使 no-op 请求，如果 baseRevision 过期，也应返回 `409 CONFLICT`。
- 不要为了 no-op 判断把大量 EF 查询写进 Controller。
- 建议在 Application service 或 repository query 中补最小需要的当前 tag set。

需要补测试：

- no-op PATCH 不递增 revision。
- no-op PATCH 不新增 activity。
- stale revision 的 no-op PATCH 仍返回 409。

### 2. 补 PostgreSQL smoke 测试

当前大部分 API 测试使用 EF InMemory，能验证用例流程，但不能验证：

- PostgreSQL `jsonb`
- check constraints
- foreign keys
- index/migration SQL
- Npgsql 查询翻译

要求二选一：

1. 优先使用 Testcontainers PostgreSQL，新增 integration/smoke 测试。
2. 如果当前环境暂不适合 Testcontainers，至少新增一个可运行的 PostgreSQL smoke profile，并在 README 中写清楚命令。

建议覆盖：

- 从空 PostgreSQL 执行 migration。
- seed 幂等运行两次。
- `GET /api/v1/bootstrap`
- `GET /api/v1/documents/{id}/context`
- `GET /api/v1/documents/{id}/activity`
- `GET /api/v1/search?q=&spaceId=`
- `PATCH /api/v1/documents/{id}` 后 search/context/activity 派生数据仍正确。

注意：

- 不要把生产代码为了 InMemory 测试写坏。
- 如果引入 Testcontainers，保持测试数量少，作为 smoke，不要拖慢普通单元测试太多。

### 3. 搜索策略记录

当前搜索基于 `document_search_index.title/text_content` contains 查询，可以保留。

本阶段不要升级到外部搜索引擎。

需要做的只是：

- 在代码或文档中明确这是 Phase 3/4 的数据库内轻量搜索。
- 给后续 PostgreSQL `tsvector` / trigram / 外部搜索预留 issue/TODO 文档说明。
- 不要在 Phase 4 里引入 Meilisearch/OpenSearch/Elasticsearch。

## Phase 4B: 认证与 Current User

### 目标

实现第一版可用认证，让 API 不再只能依赖 seed/default owner。

建议优先实现简单、可维护的本地账号认证：

```text
POST /api/v1/auth/register
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me
```

如果当前产品阶段不需要开放注册，可以改为：

```text
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me
```

并由 seed 创建默认账号。

### 认证方式

推荐：

- access token: JWT Bearer，短有效期。
- refresh token: 存数据库，只存 hash，不存明文。
- refresh token rotation：每次 refresh 后旧 token 作废。

也可以使用 cookie，但请在实现前明确选择并保持一致。不要同时实现 Bearer 和 Cookie 两套主流程。

### 新增数据表

在现有 `users` 基础上补认证相关表。不要把 refresh token 存在 users 表 JSON 字段里。

建议新增：

```text
user_credentials
refresh_tokens
login_audit_events 或 auth_events
```

最小字段建议：

`user_credentials`

- `user_id`
- `password_hash`
- `password_hash_algorithm`
- `password_updated_at`
- `created_at`
- `updated_at`

`refresh_tokens`

- `id`
- `user_id`
- `token_hash`
- `family_id`
- `created_at`
- `expires_at`
- `rotated_at`
- `revoked_at`
- `replaced_by_token_id`
- `created_by_ip`
- `user_agent`

规则：

- 密码使用 ASP.NET Core PasswordHasher 或同级成熟实现。
- refresh token 只返回明文一次，数据库只存 hash。
- logout 撤销当前 refresh token。
- refresh token reuse 检测到已撤销 token 再使用时，应撤销同 family 后续 token。

### Current User 抽象

新增 Application 可依赖的当前用户上下文接口，例如：

```csharp
public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
}
```

规则：

- `Application` 依赖接口，不依赖 `HttpContext`。
- `Api` 或 Infrastructure 提供从 claims 读取的实现。
- 当前 document create/update/move 的 actorId 应从 `ICurrentUser` 获取。
- Testing 环境可以提供 test current user 或继续使用 seed user fallback，但 fallback 逻辑必须集中，不要散落在业务服务里。

## Phase 4C: Workspace Member 权限

### 目标

实现 workspace member 级别的最小权限，不做复杂 ACL。

当前已有：

- `users`
- `workspaces`
- `workspace_members`
- `spaces`
- `collections`
- `documents`

本阶段用 `workspace_members.role` 即可：

```text
owner
admin
editor
viewer
```

### 权限规则

建议第一版：

- `viewer`: 可读 bootstrap/map/document/context/activity/search。
- `editor`: viewer 权限 + create/update/move document。
- `admin`: editor 权限 + 管理 workspace member 的预留能力。
- `owner`: admin 权限 + workspace 所有权。

本阶段不做：

- 单文档分享权限
- collection 继承权限
- group ACL
- public link

### Application 权限服务

新增 Application 接口，例如：

```csharp
public interface IWorkspaceAccessService
{
    Task EnsureCanViewWorkspaceAsync(Guid workspaceId, CancellationToken ct);
    Task EnsureCanEditWorkspaceAsync(Guid workspaceId, CancellationToken ct);
    Task EnsureCanManageWorkspaceAsync(Guid workspaceId, CancellationToken ct);
}
```

规则：

- 权限判断放 Application 层。
- Controller 不直接查 member。
- Repository 不隐式吞掉权限问题。
- 不允许跨 workspace 通过猜 UUID 读写数据。

### API 应用权限

至少覆盖：

读权限：

```text
GET /api/v1/bootstrap
GET /api/v1/spaces/{spaceId}/map
GET /api/v1/documents/{documentId}
GET /api/v1/documents/{documentId}/context
GET /api/v1/documents/{documentId}/activity
GET /api/v1/search
```

写权限：

```text
POST  /api/v1/documents
PATCH /api/v1/documents/{documentId}
PATCH /api/v1/documents/{documentId}/location
```

未登录：

- 需要认证的接口返回 `401 UNAUTHORIZED`。

已登录但不是 workspace member：

- 返回 `403 FORBIDDEN`，不要返回数据。

资源不存在：

- 可以继续返回 `404 NOT_FOUND`。
- 对敏感资源可以统一返回 404，但本阶段先保持错误语义清晰。

## Phase 4D: Workspace Member 管理 API

本阶段可以只做最小闭环：

```text
GET   /api/v1/workspaces/{workspaceId}/members
POST  /api/v1/workspaces/{workspaceId}/members
PATCH /api/v1/workspaces/{workspaceId}/members/{userId}
DELETE /api/v1/workspaces/{workspaceId}/members/{userId}
```

规则：

- 只有 `owner/admin` 能管理成员。
- 不能移除 workspace 最后一个 owner。
- 不能把最后一个 owner 降级。
- 不做邮件邀请；`POST members` 可以先按 email 查找已有 user，不存在则返回 validation error。
- 后续邀请流再单独做。

如果实现成本偏大，本阶段可以只实现：

```text
GET /api/v1/workspaces/{workspaceId}/members
```

并把增删改留到 Phase 4.1，但权限检查必须先接入文档 API。

## Contracts DTO

新增 DTO 放在 `Northstar.Contracts`，建议分组：

```text
Contracts/Auth/AuthDtos.cs
Contracts/Workspaces/WorkspaceMemberDtos.cs
```

不要把 EF entity 作为响应体返回。

建议 DTO：

```csharp
RegisterRequest
LoginRequest
AuthTokenResponse
RefreshTokenRequest
MeResponse
WorkspaceMemberDto
WorkspaceMembersResponse
AddWorkspaceMemberRequest
UpdateWorkspaceMemberRequest
```

字段按前端需要保持简洁：

- user id
- email
- display name
- role
- status
- joined at

## Seed 要求

继续幂等 seed：

- 默认 workspace: `Northstar`
- 默认 space: `Atlas Library`
- 默认 owner user: `owner@northstar.local`
- 默认 owner credential 可用于本地开发

注意：

- 不要在生产环境硬编码默认密码。
- Development/Testing 可以通过配置或环境变量启用 seed credential。
- README 必须说明本地默认登录方式。

## 测试要求

至少补：

### Auth tests

- login 成功返回 access token 和 refresh token。
- 密码错误返回 401。
- refresh 成功并轮换 token。
- logout 后 refresh token 不可再用。

### Permission tests

- 未登录访问 protected API 返回 401。
- 非 workspace member 返回 403。
- viewer 可读不可写。
- editor 可写文档。
- 最后一个 owner 不能被移除或降级。

### QA cleanup tests

- no-op PATCH 不递增 revision。
- no-op PATCH 不新增 activity。
- stale revision 的 no-op PATCH 返回 409。

### PostgreSQL smoke

至少有一个真实 PostgreSQL 路径验证 migration + seed + 核心 API。可以用 Testcontainers，也可以先提供独立 smoke profile。

## 验证命令

完成后必须运行：

```powershell
dotnet restore .\Northstar.sln
dotnet build .\Northstar.sln --no-restore
dotnet test .\Northstar.sln --no-build
dotnet ef migrations script --project .\src\Northstar.Infrastructure\Northstar.Infrastructure.csproj --startup-project .\src\Northstar.Api\Northstar.Api.csproj --idempotent
```

如果新增 Testcontainers PostgreSQL 测试，请同时汇报：

- 是否需要 Docker Desktop。
- 测试是否默认随 `dotnet test` 运行。
- 如果不是，单独运行命令是什么。

## 完成后汇报

请汇报：

- QA cleanup 修了哪些问题。
- no-op PATCH 的新行为。
- 新增了哪些认证 API。
- 选择了 Bearer 还是 Cookie，为什么。
- 新增了哪些实体、表、migration。
- workspace member 权限覆盖了哪些 API。
- 新增了哪些测试。
- restore/build/test/migration script 结果。
- Phase 5 建议做什么。

Phase 5 初步建议：

1. 文档删除/归档与 import/export。
2. 文件 upload session / files / document attachments。
3. 评论线程。
4. Yjs 协作继续后置。
