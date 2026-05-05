# Backend Refactor Rules

本文档是后端重建阶段的执行规则。`services/api-old` 只作为旧 API 项目参考，不在其上继续修补式重构。旧文件服务位于 `E:\ClayMo\services\file-service`，同样只作为文件上传/对象存储设计参考。

## 1. 总原则

1. 新后端以业务目标和数据库模型为准，不以前端现有 mock 和旧 ABP 项目结构为准。
2. `services/api-old` 保持只读参考，除非明确要做对照实验，不在旧项目里修 bug、补引用、改业务。
3. `E:\ClayMo\services\file-service` 保持只读参考，不迁移 Go/go-zero 工程结构到新 .NET 后端。
4. 新项目必须可从空数据库启动，迁移、种子数据、配置和本地运行步骤要明确。
5. 每次迁移旧代码，都必须判断“复用业务规则”还是“复用实现代码”。默认只复用规则，不复用旧框架粘连代码。
6. 后端 API 契约可以适配前端，但数据模型不能被前端当前 UI 限死。

后续实现必须同时遵守:

- `docs/BACKEND_ARCHITECTURE_V1.md`: 整体架构和项目布局基准。
- `docs/BACKEND_DATA_MODEL_V1.md`: 数据库模型基准。
- `apps/web/FRONTEND_API_CONTRACT.md`: 第一版前端 API 对接契约。

## 2. 技术边界

### 不再引入

- 不再使用 ABP。
- 不再使用 ABP Module、`ApplicationService`、conventional controller、ABP repository、ABP UnitOfWork。
- 不迁移旧项目中的 `ClayMo.*` 命名和历史目录结构。
- 不迁移旧的自研 ABP/SqlSugar 适配层作为基础设施。

### 默认选择

除非后续明确改技术栈，新后端默认使用:

- ASP.NET Core Web API
- PostgreSQL
- EF Core migrations
- Cookie 或 Bearer token 认证，具体在认证方案文档里定
- OpenAPI/Swagger
- FluentValidation 或原生 endpoint validation
- 结构化日志

选择 PostgreSQL + EF Core 的原因:

- 当前表结构强关系、强约束、版本和权限表较多。
- EF Core 迁移、索引、外键、事务边界更利于长期维护。
- 去 ABP 后不需要继续背旧 SqlSugar 封装。

## 3. 新项目目录规则

建议新后端建在:

```text
services/api
```

旧后端保留:

```text
services/api-old
```

旧文件服务外部参考路径:

```text
E:\ClayMo\services\file-service
```

新项目初始结构建议:

```text
services/api
├─ Northstar.sln
├─ src
│  ├─ Northstar.Api
│  ├─ Northstar.Application
│  ├─ Northstar.Contracts
│  ├─ Northstar.Domain
│  └─ Northstar.Infrastructure
└─ tests
   ├─ Northstar.Domain.Tests
   ├─ Northstar.Application.Tests
   └─ Northstar.Api.Tests
```

职责:

- `Northstar.Api`: HTTP 入口、认证中间件、OpenAPI、Controller/Endpoint。
- `Northstar.Application`: 用例服务、DTO、事务脚本、权限检查编排。
- `Northstar.Domain`: 实体、值对象、领域规则、领域错误码。不引用 EF Core、不引用 ASP.NET。
- `Northstar.Infrastructure`: EF Core DbContext、Repository、外部服务、缓存、文件存储、邮件等。
- `Northstar.Tests`: 单元测试和集成测试。
- 具体依赖方向和模块布局以 `docs/BACKEND_ARCHITECTURE_V1.md` 为准。

依赖方向:

```text
Api -> Application -> Domain
Api -> Infrastructure
Infrastructure -> Application/Domain
Domain -> no project dependency
```

## 4. 命名规则

统一使用 `Northstar` 作为新后端命名空间和项目名前缀。

禁止继续扩散:

- `ClayMo`
- `NS.Abp`
- `Module.*` 作为新项目主结构
- `Abp` 相关命名

数据库表名使用 snake_case，和 `docs/BACKEND_DATA_MODEL_V1.md` 保持一致。

API 路径使用:

```text
/api/v1/...
```

不要沿用旧项目的:

```text
/api/app/...
```

## 5. 数据库规则

数据库设计以 `docs/BACKEND_DATA_MODEL_V1.md` 为基准。

第一阶段必须覆盖:

- `users`
- `workspaces`
- `workspace_members`
- `spaces`
- `collections`
- `documents`
- `document_drafts`
- `document_versions`
- `tags`
- `document_tags`
- `document_links`
- `activity_events`
- `document_search_index`

关键约束:

- 所有核心业务表必须带 `workspace_id`。
- 文档元数据和正文分离。
- `revision` 只用于自动保存乐观锁，不能和用户可见版本号混用。
- Tiptap 正文存 JSON/JSONB，不存 HTML 作为唯一真相源。
- 标签、引用、活动、权限不能塞进文档正文 JSON。
- 迁移必须可重复在空库执行。

## 6. API 规则

第一版 API 对齐 `apps/web/FRONTEND_API_CONTRACT.md`，但允许为了后端模型做字段映射。

优先实现:

1. `GET /api/v1/bootstrap`
2. `GET /api/v1/spaces/{spaceId}/map`
3. `POST /api/v1/documents`
4. `GET /api/v1/documents/{documentId}`
5. `PATCH /api/v1/documents/{documentId}`
6. `PATCH /api/v1/documents/{documentId}/location`
7. `GET /api/v1/documents/{documentId}/context`
8. `GET /api/v1/documents/{documentId}/activity`
9. `GET /api/v1/search`

响应规则:

- 成功响应直接返回业务对象。
- 错误响应统一为:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Human readable message",
    "details": {}
  }
}
```

错误码第一版:

- `VALIDATION_ERROR`
- `NOT_FOUND`
- `CONFLICT`
- `UNAUTHORIZED`
- `FORBIDDEN`
- `INTERNAL_ERROR`

## 7. 从旧项目迁移的规则

`services/api-old` 可以参考这些内容:

- refresh token rotation 思路
- 密码哈希策略
- 登录日志/审计字段思路
- 权限码组织方式
- 租户初始化经验
- 知识库评论、版本、引用的业务经验

`E:\ClayMo\services\file-service` 可以参考这些内容:

- 上传会话生命周期: create session -> presign parts -> complete -> finalize。
- 设计原则: 上传过程唯一事实源是 upload session，file 只在 finalize 后创建。
- 私有桶稳定访问入口: `/files/{fileId}/content` 返回短期签名 URL 的 redirect/stream。
- 单文件和分片上传模式选择。
- 幂等 key、hash dedup、abort、progress 的接口经验。
- 文件 metadata、outbox、upload session 的建模经验。

不要直接迁移:

- ABP 模块代码
- ABP Service/Repository/UoW 代码
- 旧 `SqlSugarRepository` 封装
- 旧 conventional controller 配置
- 旧 `ClayMo.*` 项目引用和命名
- 旧知识库表结构本身
- 旧 `file-service` 的 Go/go-zero 工程结构和生成代码
- 旧文件服务对 MySQL、Kafka、Redis 的直接配置方式

迁移任何旧代码前必须做三件事:

1. 说明它解决什么业务问题。
2. 去掉 ABP/旧基础设施依赖。
3. 用新项目命名、错误处理、DTO 和事务边界重写。

## 8. 分阶段执行

### Phase 0: 冻结旧项目

- `services/api-old` 只读。
- 不再清理旧项目引用。
- 不再尝试让旧项目 build 作为主线目标。

### Phase 1: 新项目骨架

- 创建 `services/api`。
- 建 solution 和基础项目。
- 配置本地运行、OpenAPI、健康检查。
- 接入 PostgreSQL 配置。
- 建第一批 EF Core migrations。

### Phase 2: 核心知识库 API

- seed 一个 workspace、space、collections、documents。
- 实现 bootstrap/map/document read/write。
- 实现 `revision` 冲突检测。
- 前端从 localStorage/mock 切到 API。

### Phase 3: 搜索、引用、活动

- 实现 tags。
- 实现 document_links。
- 实现 activity_events。
- 实现标题搜索，预留全文搜索。

### Phase 4: 认证和权限

- 实现本地用户。
- 实现登录、refresh、logout。
- 实现 workspace member 权限。
- 再考虑资源级 ACL。

### Phase 5: 文件、评论、协作

- 文件上传和对象存储。
- 评论线程。
- Yjs 协作存储。

文件上传实现时，优先参考旧文件服务的 API 流程，但在新后端中按 `files`、`upload_sessions`、`document_attachments` 等表重新建模；不要把旧服务作为独立运行依赖。

## 9. 验收标准

每个阶段结束必须满足:

- `dotnet restore` 成功。
- `dotnet build` 成功。
- 至少有基础测试或可执行的 smoke test。
- OpenAPI 能打开。
- 本地配置不依赖私密信息。
- 文档同步更新。

不得接受:

- 通过硬编码 mock 数据冒充后端完成。
- 把大量业务逻辑写进 Controller。
- 无迁移的手写临时表。
- 为了赶进度绕过 `workspace_id`、revision 或权限边界。
