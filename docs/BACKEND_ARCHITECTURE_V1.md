# Backend Architecture V1

本文档定义新后端的整体架构布局。旧项目 `services/api-old` 和 `E:\ClayMo\services\file-service` 只用于参考业务经验和接口流程，不参考其整体架构。

## 1. 架构结论

新后端采用 **ASP.NET Core Modular Monolith + Clean Architecture**。

不采用:

- ABP 模块体系
- 旧项目的 `framework/modules/src` 三段式结构
- 旧项目的自研 SqlSugar/ABP 适配层
- 一开始就拆微服务
- 旧 Go `file-service` 作为运行依赖

选择 Modular Monolith 的原因:

- 当前业务还处于核心模型重建阶段，过早拆微服务会增加接口、部署、事务和排障成本。
- 知识库、文件、搜索、活动、权限之间关系紧密，第一阶段单体更容易保证一致性。
- 模块边界可以先在代码和数据库层清晰划分，等某个模块有真实性能或团队边界需求时再拆服务。
- Clean Architecture 可以让后续替换数据库、搜索、文件存储、认证方式时不污染业务层。

## 2. 顶层目录

```text
services/api
├─ Northstar.sln
├─ src
│  ├─ Northstar.Api
│  ├─ Northstar.Application
│  ├─ Northstar.Domain
│  ├─ Northstar.Infrastructure
│  └─ Northstar.Contracts
└─ tests
   ├─ Northstar.Domain.Tests
   ├─ Northstar.Application.Tests
   └─ Northstar.Api.Tests
```

说明:

- `Northstar.Contracts` 放 API DTO、request/response、错误码、分页对象等跨层契约。它不能引用 Infrastructure。
- 如果后续觉得 Contracts 太早，可以先合并到 Application；但 API DTO 不应散落在 Controller 里。

## 3. 项目依赖方向

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

禁止:

- `Domain` 引用 ASP.NET Core、EF Core、数据库、缓存、HTTP、文件 SDK。
- `Application` 直接引用 ASP.NET Controller、HttpContext 或具体对象存储 SDK。
- `Api` 直接写复杂业务规则。
- `Infrastructure` 反向调用 Controller。

## 4. 层职责

### Northstar.Api

负责 HTTP 边界:

- Controller 或 Minimal API endpoint
- 认证和授权中间件
- OpenAPI/Swagger
- 全局异常处理
- 请求日志、trace id、correlation id
- CORS
- health check

规则:

- Controller 保持薄层，只做参数接收、调用 Application、返回响应。
- 不在 Controller 里写数据库查询。
- 不在 Controller 里拼业务流程。

### Northstar.Application

负责用例编排:

- workspace/bootstrap
- document create/update/move/read
- autosave revision conflict
- import/export
- tag/link/activity/search 编排
- 文件上传 session 编排
- 权限检查
- 事务边界

规则:

- 一个 public method 对应一个清晰用例。
- 事务在 Application 层开启，由 Infrastructure 提供实现。
- 不直接依赖 EF Core DbContext，优先依赖接口。
- 可接受轻量查询服务，例如 `IDocumentQueryService`，避免复杂读模型挤进 Domain。

### Northstar.Domain

负责业务核心:

- Entity
- Value Object
- Domain Service
- 领域错误码
- 状态转换规则
- revision/version 规则
- workspace membership 权限概念

规则:

- Domain 不关心 HTTP。
- Domain 不关心数据库实现。
- Domain 不负责 DTO mapping。
- 如果一个规则脱离 Web/API 仍成立，应放 Domain。

### Northstar.Infrastructure

负责技术实现:

- EF Core DbContext
- entity configurations
- migrations
- repository/query service implementation
- PostgreSQL full text index support
- object storage implementation
- email/SMS provider
- cache provider
- outbox dispatcher
- background worker

规则:

- Infrastructure 可以依赖外部 SDK。
- 所有数据库配置集中在 Infrastructure。
- migrations 不散落到其他项目。
- 外部服务配置通过 options 注入，不硬编码。

### Northstar.Contracts

负责对外契约:

- API request/response DTO
- pagination DTO
- error response
- enum/string contract
- OpenAPI 稳定模型

规则:

- Contract DTO 不直接暴露 EF entity。
- Contract DTO 字段以 API 稳定性为先，不被数据库字段名绑死。
- 前端依赖 Contracts，而不是 Domain entity。

## 5. 模块布局

新项目采用“层内按业务模块分组”，不要恢复旧 ABP 的每模块五项目结构。

推荐:

```text
Northstar.Domain
├─ Users
├─ Workspaces
├─ Knowledge
│  ├─ Spaces
│  ├─ Collections
│  ├─ Documents
│  ├─ Versions
│  ├─ Tags
│  ├─ Links
│  └─ Activity
├─ Files
└─ Shared

Northstar.Application
├─ Bootstrap
├─ Workspaces
├─ Knowledge
├─ Files
├─ Search
└─ Security

Northstar.Infrastructure
├─ Persistence
│  ├─ Configurations
│  ├─ Migrations
│  └─ Repositories
├─ Files
├─ Search
├─ Security
├─ BackgroundJobs
└─ Outbox
```

不要这样做:

```text
modules/kb/Domain
modules/kb/Application
modules/kb/SqlSugar
modules/identity/Domain
modules/identity/Application
...
```

原因: 当前团队和业务阶段不需要复制 ABP 模块工程数量。先保持模块边界清楚，项目数量少，迁移速度更快。

## 6. 运行时架构

第一阶段运行时只有一个主 API:

```text
Frontend
  -> Northstar.Api
     -> PostgreSQL
     -> Object Storage
     -> Redis optional
```

文件上传不单独部署旧 Go 服务。新 API 内部提供文件模块:

```text
CreateUploadSession
PresignUpload
CompleteUpload
FinalizeUpload
GetFileContent
```

未来只有在以下条件满足时才拆独立 `file-service`:

- 文件流量明显高于主业务 API
- 需要独立扩缩容
- 需要独立安全边界
- 需要独立团队维护

拆服务前，数据库表和 API 流程仍按本文档设计，避免二次返工。

## 7. 数据架构

数据库以 `docs/BACKEND_DATA_MODEL_V1.md` 为准。

第一阶段采用单 PostgreSQL 数据库，多 workspace 共享表:

```text
workspace_id everywhere
```

暂不采用:

- 每租户一个数据库
- 动态连接字符串
- ABP Tenant
- 旧项目的 tenant provisioning 作为主路径

原因:

- 当前产品核心是 workspace/space 知识库，不需要一开始引入复杂租户数据库编排。
- 单库多 workspace 更适合早期迭代、搜索、跨空间统计和运维。
- 后续如果出现企业私有化或强隔离需求，再升级为租户级数据库隔离。

事务规则:

- 文档保存、tag 更新、activity、search index 同步可以在一个事务内完成或用 outbox 分离。
- 文件 finalize 创建 `files` 和 `document_attachments` 应在事务中完成。
- outbox 用于可靠异步事件，不用直接在业务事务里调用外部 MQ。

## 8. API 架构

API 路径统一:

```text
/api/v1
```

Controller 分组:

```text
/api/v1/bootstrap
/api/v1/workspaces
/api/v1/spaces
/api/v1/collections
/api/v1/documents
/api/v1/files
/api/v1/search
```

错误响应统一:

```json
{
  "error": {
    "code": "CONFLICT",
    "message": "Document revision conflict.",
    "details": {}
  }
}
```

禁止:

- 混用 `/api/app` 和 `/api/v1`
- 同一个资源同时存在 REST 风格和 RPC 风格的重复接口
- Controller 返回 EF entity
- 临时接口不写入 API contract

## 9. 认证和权限

第一阶段先实现清晰、够用的认证权限，不复刻 ABP 权限体系。

建议:

- `users`
- `workspaces`
- `workspace_members`
- 后续再加 `resource_permissions`

权限判断优先在 Application 层完成:

```text
CanViewDocument
CanEditDocument
CanManageWorkspace
```

避免:

- 一开始做复杂 permission provider
- 过早做细粒度 ACL
- 权限逻辑散落在 Controller 和 Repository

## 10. 文件模块架构

旧 Go 文件服务只参考流程。新文件模块按以下模型落地:

```text
upload_sessions -> files -> document_attachments
```

核心原则:

- 上传前创建 upload session。
- session 负责幂等、分片、状态、过期、complete。
- file 只在 finalize 后创建。
- URL 不是 file 的永久字段。
- 文件访问通过稳定 API 入口生成短期签名 URL 或 stream。

推荐 API:

```text
POST /api/v1/files/uploads/sessions
POST /api/v1/files/uploads/sessions/{sessionId}/parts/presign
POST /api/v1/files/uploads/sessions/{sessionId}/complete
POST /api/v1/files/uploads/sessions/{sessionId}/finalize
GET  /api/v1/files/uploads/sessions/{sessionId}/progress
POST /api/v1/files/uploads/sessions/{sessionId}/abort
GET  /api/v1/files/{fileId}
GET  /api/v1/files/{fileId}/content
DELETE /api/v1/files/{fileId}
```

## 11. 后台任务和事件

第一阶段使用 HostedService 或轻量 job runner，不急着引入 Hangfire。

可以有:

- outbox dispatcher
- expired upload session cleanup
- search index rebuild
- file metadata extraction

规则:

- 后台任务必须幂等。
- 外部副作用通过 outbox 或显式状态机保护。
- 不在 HTTP 请求里做长耗时文件处理。

## 12. 测试策略

最低要求:

- Domain unit tests: 领域规则、状态转换、revision/version。
- Application integration tests: 用真实 PostgreSQL 或 Testcontainers。
- API smoke tests: bootstrap、document read/write、conflict。

不接受:

- 只有手工测试。
- 没有数据库迁移验证。
- 关键保存流程没有冲突测试。

## 13. 旧项目参考边界

`services/api-old`:

- 参考认证、refresh token、权限码、租户经验、知识库旧业务规则。
- 不参考整体架构。
- 不迁移 ABP 依赖。
- 不迁移 SqlSugar/ABP 封装。

`E:\ClayMo\services\file-service`:

- 参考上传 session、预签名、finalize、私有桶稳定访问、outbox。
- 不参考 Go/go-zero 分层。
- 不作为运行依赖。
- 不迁移 MySQL schema 原样设计。

