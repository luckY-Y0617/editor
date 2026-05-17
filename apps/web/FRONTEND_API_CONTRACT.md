# Northstar Atlas Library Frontend API Contract

本文档定义当前 React + Tiptap 编辑器前端进入后端阶段所需的第一版接口契约。目标是先支撑现有产品壳层和编辑体验，不包含真实协作、评论、上传、权限管理、版本发布流程等复杂能力。

## 约定

- Base URL: `/api/v1`
- Content-Type: `application/json`
- 时间格式: ISO 8601 字符串，例如 `2024-05-14T10:24:00.000Z`
- 文档正文格式: Tiptap `JSONContent`
- ID 格式由后端决定，前端只要求稳定字符串
- 认证方式暂不在本文档定义，可先由后端通过 session/cookie 或 bearer token 注入
- 前端当前可继续本地计算 outline、word count、reading time；后端不必在第一版持久化 outline

## 通用响应

成功响应直接返回业务对象：

```ts
type ApiSuccess<T> = T;
```

错误响应：

```ts
type ApiError = {
  error: {
    code: string;
    message: string;
    details?: unknown;
  };
};
```

建议错误码：

- `VALIDATION_ERROR`
- `NOT_FOUND`
- `CONFLICT`
- `UNAUTHORIZED`
- `FORBIDDEN`
- `INTERNAL_ERROR`

## 数据模型

```ts
type Workspace = {
  id: string;
  name: string; // "Northstar"
  currentSpaceId: string;
};

type Space = {
  id: string;
  name: string; // "Atlas Library"
};

type KnowledgeFolder = {
  id: string;
  title: string; // "01. Foundations"
  sortOrder: number;
  documentCount: number;
};

type KnowledgeDocumentSummary = {
  id: string;
  folderId: string;
  title: string;
  status: "draft" | "published" | "archived";
  updatedAt: string;
  tags: string[];
  sortOrder: number;
};

type KnowledgeDocument = KnowledgeDocumentSummary & {
  owner: {
    id: string;
    name: string;
  };
  version: string; // "3.2"
  content: JSONContent;
  revision: number; // 用于自动保存冲突检测
};

type RelatedDocument = {
  id: string;
  code: string; // "1.2"
  title: string;
};

type VersionTrailItem = {
  id: string;
  version: string;
  date: string;
  author: string;
  status: "published" | "draft";
};

type BacklinkItem = {
  id: string;
  code: string;
  title: string;
  excerpt: string;
};

type ActivityTimelineItem = {
  id: string;
  title: string;
  date: string;
  detail: string;
};
```

## 页面初始化

### GET `/bootstrap`

用于首次加载页面，减少前端串行请求。返回顶部导航、左侧树和默认打开文档。

Response:

```ts
type BootstrapResponse = {
  workspace: Workspace;
  spaces: Space[];
  activeSpaceId: string;
  folders: KnowledgeFolder[];
  documents: KnowledgeDocumentSummary[];
  activeDocumentId: string;
};
```

说明：
- `documents` 是左侧 Knowledge Map 所需的轻量列表，不包含正文。
- 前端拿到 `activeDocumentId` 后调用 `GET /documents/:documentId` 拉取正文。

## Knowledge Map

### GET `/spaces/:spaceId/map`

获取左侧 Knowledge Map。

Response:

```ts
type KnowledgeMapResponse = {
  folders: KnowledgeFolder[];
  documents: KnowledgeDocumentSummary[];
};
```

### POST `/documents`

新建文档。用于左侧 `+`。

Request:

```ts
type CreateDocumentRequest = {
  folderId: string;
  title?: string; // 默认 "Untitled Field Note"
};
```

Response:

```ts
type CreateDocumentResponse = {
  document: KnowledgeDocument;
  map: KnowledgeMapResponse;
};
```

说明：
- 前端会自动选中新建文档。
- 后端应返回最新 map，避免前端自己推断排序和计数。

## 文档读写

### GET `/documents/:documentId`

获取文档详情和正文。

Response:

```ts
type GetDocumentResponse = {
  document: KnowledgeDocument;
};
```

### PATCH `/documents/:documentId`

自动保存标题、正文、tags 等可编辑字段。

Request:

```ts
type UpdateDocumentRequest = {
  baseRevision: number;
  title?: string;
  content?: JSONContent;
  tags?: string[];
};
```

Response:

```ts
type UpdateDocumentResponse = {
  document: KnowledgeDocument;
};
```

冲突：
- 如果 `baseRevision` 低于服务端当前 revision，返回 `409 CONFLICT`。
- 第一版前端可以提示用户刷新，不需要实现复杂 merge。

### PATCH `/documents/:documentId/location`

移动文档到其他 collection，第一版 UI 暂时不一定使用，但后端模型建议预留。

Request:

```ts
type MoveDocumentRequest = {
  folderId: string;
  sortOrder?: number;
};
```

Response:

```ts
type MoveDocumentResponse = {
  document: KnowledgeDocumentSummary;
  map: KnowledgeMapResponse;
};
```

## 右侧 Overview Panel

### GET `/documents/:documentId/context`

获取右侧 Overview 所需数据。

Response:

```ts
type DocumentContextResponse = {
  relatedDocuments: RelatedDocument[];
  versionTrail: VersionTrailItem[];
  backlinks: BacklinkItem[];
};
```

说明：
- `Document Map` 第一版由前端从 Tiptap content 中解析，不需要后端返回。
- 如果后续要做全文搜索或服务端目录，可新增 `outline` 字段。

### GET `/documents/:documentId/activity`

获取 Activity tab 的时间线。

Response:

```ts
type DocumentActivityResponse = {
  items: ActivityTimelineItem[];
};
```

## 搜索

### GET `/search?q=:query&spaceId=:spaceId`

顶部 Search Northstar 使用。

Response:

```ts
type SearchResponse = {
  results: Array<{
    id: string;
    type: "document";
    title: string;
    folderId: string;
    excerpt: string;
    updatedAt: string;
  }>;
};
```

第一版可以只搜索标题；后续再扩展正文全文搜索。

## 导入导出

当前前端已有本地 JSON import/export。进入后端后建议保留两种模式：

### GET `/spaces/:spaceId/export`

导出当前 space。

Response:

```ts
type ExportSpaceResponse = {
  exportedAt: string;
  folders: KnowledgeFolder[];
  documents: KnowledgeDocument[];
};
```

### POST `/spaces/:spaceId/import`

导入文档。第一版建议只允许追加，不覆盖已有文档。

Request:

```ts
type ImportSpaceRequest = {
  mode: "append";
  documents: KnowledgeDocument[];
};
```

Response:

```ts
type ImportSpaceResponse = {
  importedDocumentCount: number;
  map: KnowledgeMapResponse;
};
```

## 前端调用流程

### 首次加载

1. `GET /bootstrap`
2. `GET /documents/:activeDocumentId`
3. `GET /documents/:activeDocumentId/context`
4. `GET /documents/:activeDocumentId/activity`

### 切换文档

1. 前端更新 active document id
2. `GET /documents/:documentId`
3. `GET /documents/:documentId/context`
4. `GET /documents/:documentId/activity`

### 自动保存

1. 用户编辑 title 或 content
2. 前端 debounce
3. `PATCH /documents/:documentId`
4. 成功后更新 `revision`、`updatedAt`、保存状态
5. `409 CONFLICT` 时提示刷新或重新加载

### 新建文档

1. `POST /documents`
2. 用 response 的 `map` 更新左侧树
3. 用 response 的 `document` 作为当前文档
4. 保存状态显示 created/saved

## 第一版暂不实现

- 多人协作
- 评论
- 附件上传
- 权限编辑
- 真正的版本发布工作流
- 删除文档
- 复杂 merge
- 服务端 outline 编辑

## 待确认问题

- 文档排序是手动拖拽排序，还是按创建时间/标题排序？
- `version` 是展示字段还是实际发布版本？
- `status` 是否允许前端从 draft 切 published？
- 导入是否需要覆盖模式？
- 搜索第一版是否只做标题搜索？

## Phase 5 Backend Contract Update

### Auth logout cleanup

`POST /auth/logout` is anonymous. It only reads `refreshToken` from the request
body. A valid token is revoked and returns `204 NoContent`; an unknown or invalid
token also returns `204 NoContent`.

### Document lifecycle

```ts
PATCH  /documents/:documentId/archive
PATCH  /documents/:documentId/restore
DELETE /documents/:documentId
```

Archive sets `status: "archived"` and removes the document from default
bootstrap/map/search results. Direct `GET /documents/:documentId` still returns
archived documents.

Restore changes an archived document back to `status: "draft"` and restores
default map/search visibility.

Delete is a soft delete. Deleted documents are excluded from bootstrap, map,
search, context, activity entrypoints, and direct GET returns `404`.

Default visibility rules:

- `GET /bootstrap` excludes archived and deleted documents.
- `GET /spaces/:spaceId/map` excludes archived and deleted documents.
- `GET /search` excludes archived and deleted documents.
- `GET /documents/:documentId/context` excludes archived and deleted related
  documents/backlinks.

### Space export

`GET /spaces/:spaceId/export?includeArchived=true`

Default `includeArchived` is `true`. Deleted documents are never exported.

```ts
type ExportSpaceResponse = {
  schemaVersion: "northstar.space.v1";
  exportedAt: string;
  workspace: { id: string; name: string };
  space: { id: string; name: string };
  collections: Array<{
    id: string;
    title: string;
    sortOrder: number;
  }>;
  documents: Array<{
    id: string;
    folderId: string;
    title: string;
    status: "draft" | "published" | "archived";
    sortOrder: number;
    tags: string[];
    content: JSONContent;
    revision: number;
    createdAt: string;
    updatedAt: string;
  }>;
};
```

Export does not include auth tokens, user credentials, password hashes, or
activity events.

### Space import

`POST /spaces/:spaceId/import`

Phase 5 supports append only. Original document IDs are not reused. Internal
`/documents/:id` links are rewritten when the linked target is also in the same
import batch. Links pointing outside the import batch are preserved in content,
but no invalid backlink row is created.

If a document's `folderId` does not map to an imported collection or an existing
target collection, the document is placed into the first collection in the target
space. If the target space has no collection, the backend creates `Imported`.

```ts
type ImportSpaceRequest = {
  mode: "append";
  collections?: Array<{
    id?: string;
    title: string;
    sortOrder?: number;
  }>;
  documents: Array<{
    id?: string;
    folderId?: string;
    title: string;
    status?: "draft" | "published" | "archived";
    sortOrder?: number;
    tags?: string[];
    content: JSONContent;
  }>;
};

type ImportSpaceResponse = {
  importedCollectionCount: number;
  importedDocumentCount: number;
  map: KnowledgeMapResponse;
};
```

Validation limits:

- Maximum documents per import: 200.
- Maximum content size per document: 2 MB.
- `content` must be a JSON object.
- Validation failures return `400 VALIDATION_ERROR` and roll back the whole
  import.

## Phase 6 Backend Contract Update

### File upload sessions

Phase 6 supports `single` upload only. Multipart is reserved; calling
`POST /files/uploads/sessions/:sessionId/parts/presign` returns
`400 VALIDATION_ERROR` with message `Multipart upload is not enabled in Phase 6.`

```ts
type CreateUploadSessionRequest = {
  idempotencyKey: string;
  originalFilename: string;
  mimeType: string;
  byteSize: number;
  checksumSha256?: string | null;
  bizType?: string | null;
  uploadMode: "single";
  workspaceId?: string | null;
  documentId?: string | null;
};

type UploadTargetDto = {
  type: "local-api";
  method: "PUT";
  url: string;
  headers: Record<string, string>;
};

type CreateUploadSessionResponse = {
  sessionId: string;
  status: "initiated" | "uploading" | "completed" | "aborted" | "expired" | "failed" | "finalized";
  uploadMode: "single";
  uploadTarget: UploadTargetDto;
  expiresAt: string;
};

type UploadSessionDto = {
  id: string;
  workspaceId: string;
  ownerId: string;
  idempotencyKey: string;
  originalFilename: string;
  mimeType: string;
  byteSize: number;
  checksumSha256?: string | null;
  bizType?: string | null;
  status: string;
  uploadMode: string;
  finalizedFileId?: string | null;
  expiresAt: string;
  createdAt: string;
  updatedAt: string;
};
```

Flow:

1. `POST /files/uploads/sessions`
2. `PUT uploadTarget.url` with the raw file body
3. `POST /files/uploads/sessions/:sessionId/complete`
4. `POST /files/uploads/sessions/:sessionId/finalize`

`complete` validates actual byte size and optional SHA-256 checksum. A `files`
row is created only during `finalize`. Repeating `finalize` is idempotent and
returns the existing file without duplicating attachments or outbox events.

### Files and attachments

```ts
type FileDto = {
  id: string;
  workspaceId: string;
  uploadedBy?: string | null;
  originalFilename: string;
  mimeType: string;
  byteSize: number;
  checksumSha256?: string | null;
  width?: number | null;
  height?: number | null;
  metadata: Record<string, unknown>;
  scanStatus: "pending" | "clean" | "blocked" | "failed";
  processingStatus: "pending" | "ready" | "failed";
  createdAt: string;
};

type DocumentAttachmentDto = {
  id: string;
  workspaceId: string;
  documentId: string;
  fileId: string;
  relationType: "attachment" | "inline_image" | "cover";
  metadata: Record<string, unknown>;
  createdAt: string;
  file: FileDto;
};

type FinalizeUploadSessionRequest = {
  documentId?: string | null;
  relationType?: "attachment" | "inline_image" | "cover" | null;
  metadata?: Record<string, unknown> | null;
};

type FinalizeUploadSessionResponse = {
  file: FileDto;
  attachment?: DocumentAttachmentDto | null;
};

type AttachFileToDocumentRequest = {
  fileId: string;
  relationType: "attachment" | "inline_image" | "cover";
  metadata?: Record<string, unknown> | null;
};

type DocumentAttachmentsResponse = {
  attachments: DocumentAttachmentDto[];
};
```

Endpoints:

```text
GET    /files/:fileId
GET    /files/:fileId/content
DELETE /files/:fileId
GET    /documents/:documentId/attachments
POST   /documents/:documentId/attachments
DELETE /documents/:documentId/attachments/:attachmentId
```

File content is private and must be accessed through the API. The backend does
not return permanent public URLs.

### Tiptap file references

When saving `PATCH /documents/:documentId`, backend validation extracts internal
file references from:

- image node `attrs.fileId`
- image node `attrs.src` when it contains `/api/v1/files/:fileId/content`
- attachment/file node `attrs.fileId`
- link mark `attrs.href` when it contains `/api/v1/files/:fileId/content`

Referenced files must exist, must belong to the same workspace, and must not be
deleted. Valid content references auto-create an `inline_image` attachment if
the relation is missing. Missing or cross-workspace file references return
`400 VALIDATION_ERROR`.

### Export/import file boundary

Phase 6 does not export or import file binaries. Space export may preserve
document content file references, but import must not forge local `files` records
from source-environment file IDs. Full file migration needs a future package or
manifest flow.

## Permission System Phase 6/7 Frontend Contract Update

### Request access

`POST /permissions/access-requests`

```ts
type CreateAccessRequestRequest = {
  resourceType: "collection" | "document";
  resourceId: string;
  requestedRole: "viewer" | "commenter" | "editor";
  reason?: string | null;
  subjectType?: "user" | null;
  subjectId?: string | null;
};

type AccessRequestDto = {
  id: string;
  workspaceId: string;
  resourceType: "collection" | "document";
  resourceId: string;
  requesterId: string;
  subjectType: "user" | "group";
  subjectId: string;
  requestedRole: string;
  reason: string | null;
  status: "pending" | "approved" | "denied" | "cancelled";
  decidedBy: string | null;
  decidedAt: string | null;
  decisionReason: string | null;
  resultingGrantId: string | null;
  createdAt: string;
  updatedAt: string;
};
```

The current Share & Permissions UI sends `requestedRole: "viewer"` for the
minimal Request access action. `409 CONFLICT` means a pending request already
exists and should be shown as pending.

### Review access request

`POST /permissions/access-requests/:requestId/review`

```ts
type ReviewAccessRequestRequest = {
  decision: "approve" | "deny";
  roleKey?: "owner" | "admin" | "editor" | "commenter" | "viewer" | null;
  reason?: string | null;
  expiresAt?: string | null;
};
```

Approve defaults to the request's `requestedRole` in the UI. `expiresAt` is
available for temporary access; `null` creates permanent access. Deny may send
`reason: null`.

### Notifications

`PermissionNotificationDto.type` may now include:

```ts
type PermissionNotificationType =
  | "access_request.created"
  | "access_request.approved"
  | "access_request.denied"
  | "permission.grant_created"
  | "permission.grant_updated"
  | "permission.grant_revoked"
  | "permission.grant_expiring"
  | "permission.grant_expired"
  | "group.member_added"
  | "group.member_removed"
  | "group.member_expiring"
  | "group.member_expired";
```

When `actionUrl` is present, Updates should navigate to it. Direct approve/deny
inside Updates remains deferred.

### Share & Permissions scope

Currently supported:

- read resource permissions, direct grants, group grants, effective access, and
  pending access requests;
- submit minimal Request access;
- approve or deny pending requests;
- create, list, and revoke internal workspace-member share links;
- display IAM-managed group provider/source metadata as read-only.

Still deferred:

- public anonymous link interaction UI unless the backend feature flag is known
  enabled;
- production invite email provider UX beyond delivery status display;
- full grant/policy mutation UI;
- bulk access request review;
- notification preference persistence;
- watched/muted persistence;
- group grant fan-out notifications;
- full IAM management UI.

## Permission System Phase 7 Frontend Contract Update

### Internal share links

Phase 7 supports internal workspace-member links for `document` and
`collection` resources. Phase 9 adds external authenticated links and email
invites below. Phase 10 adds default-off public document viewer links at the
backend/API contract level. Phase 11 adds default-off public collection viewer
links and optional public-link password proof. Full frontend public-link
interaction and bulk link management flows remain deferred.

```ts
type ShareLinkDto = {
  id: string;
  workspaceId: string;
  resourceType: "collection" | "document";
  resourceId: string;
  roleKey: "viewer" | "commenter";
  audience: "workspace" | "external" | "public";
  subjectEmail: string | null;
  createdBy: string | null;
  createdAt: string;
  expiresAt: string | null;
  revokedAt: string | null;
  hasPassword: boolean;
  contentProtection?: ShareLinkContentProtectionDto | null;
};

type ShareLinksResponse = {
  links: ShareLinkDto[];
};

type CreateShareLinkRequest = {
  roleKey: "viewer" | "commenter";
  audience?: "workspace" | "external" | "public";
  expiresAt?: string | null;
  subjectEmail?: string | null;
  password?: string | null;
  contentProtection?: ShareLinkContentProtectionDto | null;
};

type CreateShareLinkResponse = {
  link: ShareLinkDto;
  token: string;
  url: string;
};

type ResolveShareLinkResponse = {
  workspaceId: string;
  resourceType: "collection" | "document";
  resourceId: string;
  roleKey: "viewer" | "commenter";
  audience: "workspace" | "external" | "public";
  subjectEmail: string | null;
  expiresAt: string | null;
};
```

Endpoints:

```text
GET    /permissions/resources/:resourceType/:resourceId/share-links
GET    /permissions/share-links/:shareLinkId
POST   /permissions/resources/:resourceType/:resourceId/share-links
DELETE /permissions/share-links/:shareLinkId
GET    /share-links/:token/resolve
```

Create returns the raw token and URL once. The frontend may display them
immediately after creation, but list responses must be treated as metadata only:
they never contain a raw token or token hash. The single-link metadata read
returns the same token-free `ShareLinkDto`, including revoked links when the
caller can manage sharing for the linked resource. Revoke removes the link from
the active list after refresh.

Active link metadata and authorization are separate. The list endpoint may still
return an unexpired, non-revoked link when the resource's `linkMode` is
`disabled`; in that state the link is visible as metadata but cannot authorize
document or collection access. Public controls must stay disabled/read-only
unless the frontend knows `Permissions:PublicShareLinks:Enabled` is true.
External authenticated links are enabled only when created with
`audience: "external"` and `subjectEmail`.

Create examples:

```json
{
  "roleKey": "viewer",
  "audience": "workspace",
  "expiresAt": null
}
```

```json
{
  "roleKey": "commenter",
  "audience": "workspace",
  "expiresAt": "2026-05-01T00:00:00Z"
}
```

Invalid role, invalid audience, and past `expiresAt` return
`400 VALIDATION_ERROR`. Duplicate token values are not client-generated.
`audience: "public"` returns `400 VALIDATION_ERROR` while the backend feature
flag is off. When enabled, create is document-only, viewer-only, requires a
future bounded `expiresAt`, and rejects `subjectEmail`.

### Share-token resource access

The document read/context/activity/comment endpoints accept a share link token
with either:

```text
?shareToken=<token>
X-Share-Link-Token: <token>
```

Supported paths:

```text
GET  /documents/:documentId
GET  /documents/:documentId/context
GET  /documents/:documentId/activity
GET  /documents/:documentId/comments
POST /documents/:documentId/comments
POST /documents/:documentId/comments/:threadId/messages
POST /documents/:documentId/comments/:threadId/resolve
POST /documents/:documentId/comments/:threadId/reopen
```

A `viewer` link allows view-only paths. A `commenter` link allows view and
comment paths. Links do not authorize edit, manage, archive, delete, export,
map, bootstrap, or search. The backend honors a share token only while the
linked resource policy has the matching link mode (`internal` for workspace
links, `external` for external authenticated links); missing policy or
`linkMode = disabled` blocks authorization even if the link is otherwise active.
Collection links used for child documents are gated by the collection
`linkMode`, and restricted document policy still blocks inherited collection
link access.

### Share & Permissions scope

Currently supported in the Share & Permissions UI:

- read resource permissions, direct grants, group grants, effective access, and
  pending access requests;
- submit minimal Request access;
- approve or deny pending requests;
- list active internal links;
- create viewer/commenter internal links;
- create viewer/commenter external authenticated links for a recipient email;
- display the generated link URL/token only once after create;
- revoke active internal/external links;
- show disabled link mode as a state where active link metadata may remain but
  access is paused;
- display IAM-managed group provider/source metadata and keep those group
  mutation controls disabled;
- create/list/revoke email invites;
- show public anonymous link controls as disabled/read-only unless the backend
  feature flag is enabled.

Still deferred:

- full public/anonymous share-link interaction UI;
- production invite delivery provider configuration UI;
- MFA/recent-auth enforcement UI;
- share-link/invite notification fan-out;
- full grant/policy mutation UI;
- bulk access request review;
- bulk link management;
- notification preference persistence;
- watched/muted persistence;
- group grant fan-out notifications;
- full IAM management UI.

### Document Share drawer enterprise semantics

The editor Share drawer is the normal surface for daily document sharing. It is
not the global permission administration page and it is not a live presence
panel.

Required sections:

- Invite people: create direct user grants for known workspace users and email
  invites for external recipients.
- Share link: create or configure link access using the dedicated share-link
  APIs only.
- Who can access: summarize access entries grouped by source.

"Who can access" rows must be source-aware:

- Owner: the document owner or effective owner/manager source, when available.
- People: direct user grants and pending/accepted email invites.
- Groups: direct group grants, including IAM-managed groups as read-only where
  appropriate.
- Links: active, paused, expired, or revoked share-link metadata, displayed as
  links, not as libraries or folders.
- Inherited access: workspace or collection inheritance shown once per source,
  with the effective inherited role and inheritance mode.

The UI must not use "Current access" as the primary label for this list unless
copy clearly explains that it is a permissions summary. "Current access" can be
confused with live viewers. Live viewers/presence is a separate collaboration
feature and must not be inferred from permission rows.

Share modes map to API behavior:

- Invitation only: do not create a link; rely on direct grants, invites, and
  inherited access allowed by policy.
- Internal: create `audience = "workspace"` links. These require active
  workspace membership and backend `linkMode = internal`.
- External authenticated: create `audience = "external"` links with
  `subjectEmail`; they require authenticated email match and
  `linkMode = external`.
- Public: create `audience = "public"` links only when the app knows the
  backend feature is enabled. Public links are viewer-only, require future
  bounded expiry, may carry a create-time password, and use only
  `/public/share-links/...` anonymous routes for reads.

Role controls must distinguish resource grants from link roles:

- Direct user/group grants may show the resource roles returned by
  `availableRoles`.
- Share-link role controls may show only `viewer` and `commenter`.
- Public-link controls must force `viewer`.
- Editor/admin/owner link options must not appear enabled unless a future
  contract explicitly adds editable or administrative links.

Copy behavior must follow the backend token-handling design:

- Create responses may show and copy the raw token-bearing URL.
- Token-free list/detail rows may show metadata and link IDs.
- Existing-link "copy full URL" is allowed only if the backend exposes an
  approved audited copy endpoint that can return a usable URL without exposing
  token hashes, passwords, or other secrets.
- If that endpoint is absent or disabled, existing-link rows must show a
  disabled reason instead of reconstructing URLs client-side.

### Link Management UI contract

Status: implemented contract for the current workspace Access & Sharing link
inventory, including audited full-URL copy, pause/resume, and access analytics.

Placement rules:

- normal document sharing remains in the document Share drawer.
- resource-scoped advanced permission work remains in Share & Permissions.
- centralized link inventory belongs on a workspace access/security management
  surface, not as the only way to create daily document links.
- visible product copy may say Folder, but API and persistence continue to use
  `collection`.

Centralized link inventory target:

- filters: status, audience, role, expiry window, risk state, resource type,
  creator, and search text.
- columns: resource, token-free link id, audience, role, creator, status,
  expiry, last access, access count, risk state, and actions.
- detail drawer: token-free link id, resource, creator, created time, expiry,
  role, audience, status, last access, access count, access trend, source
  distribution, and recent access events.
- actions: revoke where authorized; pause/resume per-link state; update
  role/expiry only inside backend constraints; audited full-URL copy only
  through the protected copy endpoint.

Copy and reveal rules:

- token-bearing URLs are copyable immediately after create while the create
  response exists in client state.
- existing-link full URL copy is allowed only through the authenticated audited
  `POST /permissions/share-links/:shareLinkId/copy` endpoint. The frontend must
  convert public API URLs from that response into the frontend public reader
  route before showing or copying them.
- list/detail responses never contain raw tokens or token hashes and must not
  be used to reconstruct token-bearing URLs.
- existing link rows may still copy the token-free link id for support/audit
  workflows when a full URL copy is not requested.
- `copy-events` records copy actions for values already held by the client; it
  must not return token material.
- reveal-link UI is not supported as a persistent token display. Existing-link
  URL access is a deliberate audited copy action only.

Risk and audit display target:

- risk indicators may use access count spikes, unexpected IP/source patterns,
  frequent external/public access, expired/revoked access attempts, and failed
  known-link access events.
- Share Governance Hardening V1 shows token-safe governance context in Access
  & Sharing: scope type, Low/Medium/High risk label, reason summary, total
  accesses, tree views, document views, denied attempts, password failures,
  last access, latest event category, and recent event category/scope/result.
- Risk labels are UI governance signals, not enforced policy. Frontend-derived
  thresholds such as expiring soon, long expiry, repeated password failures, or
  repeated scope denied events must be worded as signals/hints unless the
  backend exposes a policy-enforced state.
- Collection and library public links are always viewer-only, scope-bound,
  rendered from public-safe DTOs, and copied only through audited copy.
  Collection public links without a password are high risk; collection scope is
  at least medium risk. Library public links are high risk by default and are
  enabled only when policy and current library context allow.
- unknown-token failures must not reveal token existence and may be summarized
  from rate-limit/security logs rather than application tables.
- access event tables are analytics/security data and do not replace immutable
  permission audit events.

## Permission System Phase 9 Frontend Contract Update

### External authenticated links

External links use the same share-link endpoints. They must be clearly shown as
authenticated email-bound links, not public links.

```json
{
  "roleKey": "commenter",
  "audience": "external",
  "expiresAt": "2026-05-07T00:00:00Z",
  "subjectEmail": "person@example.com"
}
```

Frontend rules:

- show `subjectEmail` for external link metadata;
- display generated URL/token only immediately after create;
- list responses never contain raw token or token hash;
- external links authorize only single-resource token paths and only for an
  authenticated user whose email matches `subjectEmail`;
- search, export, map, bootstrap, and list surfaces must not pass or rely on
  share tokens;
- `linkMode = disabled` may leave active metadata visible but pauses
  authorization;
- public anonymous controls remain disabled/read-only unless the backend
  feature flag is enabled.

### Email invites

Email invite endpoints:

```text
GET    /permissions/resources/:resourceType/:resourceId/email-invites
POST   /permissions/resources/:resourceType/:resourceId/email-invites
DELETE /permissions/email-invites/:inviteId
GET    /permissions/email-invites/:token/resolve
POST   /permissions/email-invites/:token/accept
```

```ts
type EmailInviteDto = {
  id: string;
  workspaceId: string;
  resourceType: "collection" | "document";
  resourceId: string;
  email: string;
  roleKey: "viewer" | "commenter";
  status: "pending" | "accepted" | "revoked" | "expired";
  invitedBy: string | null;
  acceptedBy: string | null;
  revokedBy: string | null;
  createdAt: string;
  expiresAt: string;
  acceptedAt: string | null;
  revokedAt: string | null;
  expiredAt: string | null;
  deliveryStatus: "disabled" | "sent" | "failed";
  deliveryProvider: string;
  deliveryAttemptedAt: string | null;
  deliveryErrorCode: string | null;
};

type EmailInvitesResponse = {
  invites: EmailInviteDto[];
};

type CreateEmailInviteRequest = {
  email: string;
  roleKey: "viewer" | "commenter";
  expiresAt: string;
};

type CreateEmailInviteResponse = {
  invite: EmailInviteDto;
  token: string;
  url: string;
  delivery: EmailInviteDeliveryDto;
};

type EmailInviteDeliveryDto = {
  status: "disabled" | "sent" | "failed";
  provider: string;
  attemptedAt: string | null;
  errorCode: string | null;
};

type ResolveEmailInviteResponse = {
  workspaceId: string;
  resourceType: "collection" | "document";
  resourceId: string;
  email: string;
  roleKey: "viewer" | "commenter";
  status: "pending" | "accepted" | "revoked" | "expired";
  expiresAt: string;
};

type AcceptEmailInviteResponse = {
  invite: EmailInviteDto;
};
```

Frontend rules:

- create requires email, role, and future `expiresAt`;
- `409 CONFLICT` on create means an invite is already pending for that email;
- show created invite URL/token only once;
- list rows show email, role, status, and expiry, never token material;
- revoke refreshes or marks the row revoked;
- accept/resolve screens must require auth and should treat `404` for expired,
  revoked, or unknown tokens as non-recoverable;
- accepted invite access is single-resource only and must not be used to unlock
  workspace-wide navigation, search, export, map, bootstrap, or list results.
- show `deliveryStatus` and provider/error metadata as operational status only;
  never show token hash or raw token after the one-time create result.

## Permission System Phase 10/11 Frontend Contract Update

### Public anonymous document and collection links

Public link management uses the existing share-link create/list/revoke
endpoints. Frontend controls must stay disabled/read-only while the backend
feature flag is off.

Document Share Drawer management creates and manages links for the current
document only. Collection and library public links continue to use the same
dedicated share-link API with `collection` or `library` resource routes, but
their creation belongs to Access & Sharing or a clear container context, not
the document-local drawer. The document drawer may show related broader
collection/library links as read-only inherited/broader access context. It does
not use generic permission policy mutation. Workspace public scope remains
unsupported.

```json
{
  "roleKey": "viewer",
  "audience": "public",
  "expiresAt": "2026-05-07T00:00:00Z",
  "subjectEmail": null,
  "password": "optional-create-time-password"
}
```

Rules:

- document, collection, and policy-enabled library resources are supported when
  the backend feature flag is enabled;
- the document Share Drawer can create document-scope public links through
  `/permissions/resources/document/:documentId/share-links`;
- collection-scope and library-scope public links are created from Access &
  Sharing or a container-specific share/publish context through
  `/permissions/resources/:resourceType/:resourceId/share-links`;
- the Libraries page provides the primary container-specific creation context:
  the Library header publish/share action creates `resourceType = "library"`
  links for the active Library, and the folder-list share action creates
  `resourceType = "collection"` links for the selected Folder/Collection;
  neither action offers workspace public scope or generic policy `public` mode;
- Access & Sharing inventory displays document, collection, and library scope metadata,
  including resource title, audience, role, status, expiry, password-protected
  metadata, access counts, last access, and pause/resume/revoke/copy actions;
- `roleKey` must be `viewer`;
- `expiresAt` is required, future, and bounded by backend config;
- `subjectEmail` must be omitted or `null`;
- create returns the raw token and public URL once; list never returns token
  material;
- existing-link copy must use the audited
  `POST /permissions/share-links/:shareLinkId/copy` endpoint and convert
  public API URLs into the frontend route
  `#public/share-links/:token` before display/copy;
- optional `password` is accepted only once at create time; list responses show
  `hasPassword` only and never expose plaintext password or password hash;
- password-protected public resolve/read calls must send
  `X-Share-Link-Password`;
- public controls must not expose MFA, recent-auth, commenter, editor, admin,
  owner, or subject-email options as functional controls.
- Security Policy Controls V1 may block public-link creation based on
  `Permissions:PublicShareLinks`: enabled state, viewer-only role,
  document/collection/library scope allow flags, required expiry, max expiry,
  library-specific max expiry, required password, collection/library-specific
  required password, enforced public download-disabled policy, and optional
  collection/library watermark requirement. Blocked creates
  return the normal API error envelope with token-safe
  `details.reason` values such as `PUBLIC_SHARE_PASSWORD_REQUIRED` or
  `PUBLIC_SHARE_EXPIRY_TOO_LONG`, `PUBLIC_SHARE_DOWNLOAD_DISABLED_REQUIRED`, or
  `PUBLIC_SHARE_WATERMARK_REQUIRED`.
- Public link create requests may include token-safe content protection
  metadata:
  `contentProtection = { disableDownload, disablePrint, disableCopy,
  watermarkEnabled, watermarkText }`. Defaults are download disabled, print
  allowed, copy allowed, watermark disabled, and `watermarkText = "Public link"`.
  Missing metadata on legacy links is normalized to defaults without rewriting
  or revoking the link.
- Content Protection Policy V1 is not DRM. The public reader hides absent
  download/print controls where applicable, uses no-select/copy event
  deterrence for `disableCopy`, and displays a light token-safe watermark for
  `watermarkEnabled`. It does not add public file download, public edit, public
  comments, or any absolute anti-copy/anti-screenshot guarantee.
- The Share Drawer may use local management-safe policy assumptions when no
  policy discovery endpoint exists, but backend create failure remains the
  source of enforcement. Collection public links can require an access
  password. Library public links are enabled only when `AllowLibraryScope` and
  a current library context are available; policy can require library password,
  watermark, and stricter expiry. Workspace public links remain unsupported.
- Access & Sharing may show advisory policy warnings for legacy links that no
  longer satisfy current policy, including missing required password, too-long
  expiry, no expiry when expiry is required, or collection/library scope
  currently disabled. These warnings do not revoke or pause links; backend
  `policy_paused` remains a distinct state.

Anonymous read endpoints:

```text
GET /public/share-links/:token/resolve
GET /public/share-links/:token/document
GET /public/share-links/:token/collection
GET /public/share-links/:token/tree
GET /public/share-links/:token/documents/:documentId
```

```ts
type ResolvePublicShareLinkResponse = {
  workspaceId: string;
  resourceType: "document" | "collection" | "library";
  resourceId: string;
  roleKey: "viewer";
  audience: "public";
  expiresAt: string;
  hasPassword: boolean;
  contentProtection: ShareLinkContentProtectionDto;
};

type ShareLinkContentProtectionDto = {
  disableDownload: boolean;
  disablePrint: boolean;
  disableCopy: boolean;
  watermarkEnabled: boolean;
  watermarkText: string;
};

type PublicShareDocumentDto = {
  id: string;
  title: string;
  status: "draft" | "published" | "archived";
  updatedAt: string;
  tags: string[];
  content: JSONContent;
  revision: number;
};

type PublicShareDocumentResponse = {
  link: ResolvePublicShareLinkResponse;
  document: PublicShareDocumentDto;
};

type PublicShareCollectionDocumentDto = {
  id: string;
  title: string;
  status: "draft" | "published";
  updatedAt: string;
  tags: string[];
  sortOrder: number;
};

type PublicShareCollectionDto = {
  id: string;
  title: string;
  updatedAt: string;
  sortOrder: number;
  documents: PublicShareCollectionDocumentDto[];
};

type PublicShareCollectionResponse = {
  link: ResolvePublicShareLinkResponse;
  collection: PublicShareCollectionDto;
};

type PublicShareTreeNodeDto = {
  id: string;
  type: "document" | "collection";
  title: string;
  parentId: string | null;
  updatedAt: string;
  hasChildren: boolean;
  sortOrder: number;
};

type PublicShareTreeResponse = {
  shareLinkId: string;
  scopeType: "document" | "collection" | "library";
  title: string;
  contentProtection: ShareLinkContentProtectionDto;
  nodes: PublicShareTreeNodeDto[];
};
```

Public tokens must not be passed to bootstrap, map, search, export, comments,
edit, archive, delete, or management endpoints.

Public reader API calls use `auth: false`. Password-protected public
resolve/tree/document calls send the password only through
`X-Share-Link-Password`; the password must not be placed in query strings,
route hashes, localStorage, or analytics payloads.

Public Knowledge Base Experience V1 keeps the public reader route at
`#public/share-links/:token`, does not enter the editor route or app shell, and
uses the shared readonly document reader surface for document content. Document
links may continue using the legacy single-document endpoint. Collection and
library links call `/tree`, render the backend public-safe tree with a
scope-aware header, breadcrumb, current-document highlight, empty/read-denied
states, mobile-collapsible navigation, and previous/next document controls.
They load document content only through `/documents/:documentId`. The legacy
`/collection` endpoint remains summary-only for compatibility and must not be
used to fetch child document content through protected APIs.

Library public scope uses the backend `library` resource type and public-safe
tree response. The frontend must display a Library scope label, render the
existing tree UI without workspace navigation/sidebar, and rely only on backend
public endpoint responses. It must not synthesize library-wide access from
protected APIs.

Share V1 closeout:

- Closeout status is recorded in
  `docs/agent/reports/share-v1-closeout.md`.
- Current completed frontend-facing scope includes document public share,
  collection public share, policy-gated library public share, Public Knowledge
  Base reader V1, public-safe tree navigation, scoped readonly document reads,
  document-only Share Drawer creation, container-context collection/library
  creation, Access & Sharing governance, audited copy, pause/resume/revoke,
  governance analytics/risk labels, security policy controls, and content
  protection display behavior.
- Public reader routes stay outside the authenticated editor shell and call
  only anonymous public share-link endpoints with `auth: false`.
- Out of scope for this V1: workspace public scope, public edit, public
  comments, public download/file download, public search, SEO, custom domain,
  theme, and Public Site / Published Knowledge Base product behavior.
- `apps/web/dist` can be regenerated by frontend builds; review generated asset
  churn separately before committing.

## Permission System Phase 8 Frontend Contract Update

### IAM-managed groups

Phase 8 adds provider-neutral IAM sync metadata to workspace groups. The
frontend does not implement a full IAM management page or trigger sync writes
from Share & Permissions.

`GET /workspaces/:workspaceId/groups` returns group metadata with external
source fields:

```ts
type WorkspaceGroupDto = {
  id: string;
  workspaceId: string;
  name: string;
  description: string | null;
  type: "static" | "dynamic";
  isArchived: boolean;
  externalProvider: string | null;
  externalGroupId: string | null;
  externalSyncedAt: string | null;
  membersCount: number;
  createdAt: string;
  updatedAt: string;
};
```

UI rules:

- `externalProvider` or `externalGroupId` means the group is IAM-managed.
- Show the provider/source in Share & Permissions group lists and group grant
  rows.
- Disable edit, archive, member add, and member remove controls for
  IAM-managed groups.
- Local groups keep the existing behavior.
- IAM-managed groups may appear as grant subjects, but full grant mutation UI
  remains deferred.

Workspace members/groups IA discovery:

- Code-level discovery is recorded in
  `docs/agent/reports/workspace-members-groups-ia-discovery-v1.md`.
- `#members` is the workspace-level Members lifecycle surface. `#groups` is the
  workspace-level Groups read/detail surface. Directory Sync and Permissions
  Summary remain available inside the identity administration route.
- Members uses real workspace member APIs for list, add existing user by email,
  role update, and remove. It is not a pending invite workflow and must not be
  labeled as a full invitation lifecycle.
- Member Lifecycle Hardening V1 keeps the add flow labeled as add existing
  user. The frontend exposes only supported workspace roles (`owner`, `admin`,
  `editor`, `viewer`), does not expose owner assignment in the add-existing-user
  form, blocks last-owner demotion/removal when the loaded member list proves
  the target is the last active owner, confirms owner role changes, and blocks
  current-user self removal/role change when `auth/me` identifies the current
  user. Backend validation remains authoritative for stale or incomplete
  frontend state.
- Workspace Settings no longer exposes member management. Legacy
  `#settings?scope=workspace&tab=members` redirects to `#members`; Permissions
  and Integrations Settings tabs remain available for their existing scopes.
- Workspace Settings Permissions and document share surfaces can display
  real workspace group metadata and IAM source metadata. Current frontend group
  management is read/list focused; local group create/update/archive/member
  mutation APIs exist in the backend but are not exposed as editable frontend
- Groups is read/detail-only in V1. Local/static groups are shown as local
  groups, but create/delete/rename/member mutation controls are not exposed. IAM/SCIM/
  external groups are shown as directory-managed and read-only.
- Directory Sync surfaces SCIM discovery and token status. Full SCIM bearer
  token create/revoke management remains in Settings Integrations, where the
  existing one-time raw-token display rules apply. SCIM provisioning UI and IAM
  sync UI remain not exposed.
- Permissions Summary is read-only. It points users to Advanced Permissions for
  resource grants and Access & Sharing for share-link/public exposure; it does
  not edit permissions and does not change Share V1 behavior.
- The identity surfaces do not render global connection/status summary cards.
  API/workspace/member/group/SCIM readiness is surfaced only where it is
  actionable through per-section loading, empty, error, and read-only states.

### IAM sync API visibility

The backend endpoint is:

```text
POST /workspaces/:workspaceId/iam/sync
```

It is owner/admin only. The current frontend does not call this endpoint. If an
admin sync surface is added later, use this shape:

```ts
type IamSyncRequest = {
  provider: string;
  users: Array<{
    externalSubjectId: string;
    email?: string | null;
    displayName: string;
    workspaceRole?: "admin" | "editor" | "viewer" | null;
    workspaceId?: string | null;
  }>;
  groups: Array<{
    externalGroupId: string;
    name: string;
    description?: string | null;
    members: string[];
    workspaceId?: string | null;
  }>;
};

type IamSyncResponse = {
  counts: {
    created: number;
    updated: number;
    removed: number;
    skipped: number;
    usersCreated: number;
    usersUpdated: number;
    workspaceMembersCreated: number;
    groupsCreated: number;
    groupsUpdated: number;
    membersAdded: number;
    membersRemoved: number;
  };
  users: Array<{
    externalSubjectId: string;
    userId: string;
    created: boolean;
    workspaceMemberCreated: boolean;
  }>;
  groups: Array<{
    externalGroupId: string;
    groupId: string;
    created: boolean;
    membersAdded: number;
    membersRemoved: number;
  }>;
};
```

Frontend must not collect or send IdP secrets, bearer tokens, SAML assertions,
OIDC tokens, or raw external credentials through this endpoint.

Still deferred:

- real IdP login UI;
- public SCIM 2.0 server management UI;
- full public anonymous link management UI;
- production invite delivery provider configuration UI;
- MFA/recent-auth enforcement UI;
- share-link/invite notification fan-out UI;
- notification preference persistence;
- watched/muted persistence;
- group grant fan-out notifications;
- full permission mutation UI.
