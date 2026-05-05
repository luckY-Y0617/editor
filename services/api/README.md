# Northstar API

Northstar modular monolith API.

## Structure

- `src/Northstar.Api`: HTTP entrypoint, OpenAPI, CORS, exception handling, health checks.
- `src/Northstar.Application`: use-case orchestration contracts and application services.
- `src/Northstar.Contracts`: stable API DTOs and response contracts.
- `src/Northstar.Domain`: domain entities and rules. No EF Core or ASP.NET dependencies.
- `src/Northstar.Infrastructure`: EF Core, PostgreSQL, migrations, repositories, external services.

## Local Setup

The default development connection string is:

```text
Host=localhost;Port=5432;Database=northstar;Username=postgres;Password=postgres
```

Override it with `ConnectionStrings:Northstar` or `NORTHSTAR_DATABASE_CONNECTION`.

Development seeds the owner account when `Auth:SeedOwnerPassword` is configured.
The default development login from `appsettings.Development.json` is:

```text
email: owner@northstar.local
password: Northstar.dev.123!
```

Do not use the development signing key or seed password in production. Set
`Auth:Jwt:SigningKey` and omit `Auth:SeedOwnerPassword` outside local/dev test
environments.

## Commands

```powershell
dotnet restore
dotnet build
dotnet test
dotnet ef database update --project src\Northstar.Infrastructure --startup-project src\Northstar.Api
dotnet run --project src\Northstar.Api
dotnet ef migrations script --project src\Northstar.Infrastructure\Northstar.Infrastructure.csproj --startup-project src\Northstar.Api\Northstar.Api.csproj --idempotent
```

OpenAPI is available at `/swagger` in development. Health check is available at `/api/v1/health`.

## Authentication

Phase 5 uses JWT Bearer access tokens plus database-backed refresh tokens. Refresh
tokens are stored as hashes and rotated on each refresh.

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `GET /api/v1/auth/me`

Send protected API requests with:

```text
Authorization: Bearer <access-token>
```

`POST /api/v1/auth/logout` is anonymous in Phase 5. It only uses the refresh
token in the request body; valid and invalid tokens both return `204 NoContent`
so token existence is not leaked.

## Files

Phase 6 adds a local file module behind the API permission boundary. The current
provider is `Local`; multipart and external object storage are reserved for a
future phase.

Default configuration:

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

`LocalRootPath` is intentionally relative by default. File binaries are private:
clients read content through `GET /api/v1/files/{fileId}/content`; no permanent
public URL is stored or returned.

## Endpoints

- `GET /api/v1/bootstrap`
- `GET /api/v1/spaces/{spaceId}/map`
- `POST /api/v1/documents`
- `GET /api/v1/documents/{documentId}`
- `PATCH /api/v1/documents/{documentId}`
- `PATCH /api/v1/documents/{documentId}/location`
- `PATCH /api/v1/documents/{documentId}/archive`
- `PATCH /api/v1/documents/{documentId}/restore`
- `DELETE /api/v1/documents/{documentId}`
- `GET /api/v1/documents/{documentId}/context`
- `GET /api/v1/documents/{documentId}/activity`
- `GET /api/v1/documents/{documentId}/comments`
- `POST /api/v1/documents/{documentId}/comments`
- `POST /api/v1/documents/{documentId}/comments/{threadId}/messages`
- `POST /api/v1/documents/{documentId}/comments/{threadId}/resolve`
- `POST /api/v1/documents/{documentId}/comments/{threadId}/reopen`
- `GET /api/v1/search?q=&spaceId=`
- `GET /api/v1/spaces/{spaceId}/export?includeArchived=true`
- `POST /api/v1/spaces/{spaceId}/import`
- `GET /api/v1/workspaces/{workspaceId}/members`
- `POST /api/v1/workspaces/{workspaceId}/members`
- `PATCH /api/v1/workspaces/{workspaceId}/members/{userId}`
- `DELETE /api/v1/workspaces/{workspaceId}/members/{userId}`
- `GET /api/v1/workspaces/{workspaceId}/groups`
- `POST /api/v1/workspaces/{workspaceId}/groups`
- `GET /api/v1/workspaces/{workspaceId}/groups/{groupId}`
- `PATCH /api/v1/workspaces/{workspaceId}/groups/{groupId}`
- `DELETE /api/v1/workspaces/{workspaceId}/groups/{groupId}`
- `POST /api/v1/workspaces/{workspaceId}/groups/{groupId}/members`
- `DELETE /api/v1/workspaces/{workspaceId}/groups/{groupId}/members/{userId}`
- `POST /api/v1/workspaces/{workspaceId}/iam/sync`
- `GET /api/v1/permissions/effective?resourceType=&resourceId=`
- `GET /api/v1/permissions/resources/{resourceType}/{resourceId}`
- `PATCH /api/v1/permissions/resources/{resourceType}/{resourceId}/policy`
- `POST /api/v1/permissions/resources/{resourceType}/{resourceId}/grants`
- `PATCH /api/v1/permissions/resources/{resourceType}/{resourceId}/grants/{grantId}`
- `DELETE /api/v1/permissions/resources/{resourceType}/{resourceId}/grants/{grantId}`
- `GET /api/v1/permissions/resources/{resourceType}/{resourceId}/share-links`
- `POST /api/v1/permissions/resources/{resourceType}/{resourceId}/share-links`
- `DELETE /api/v1/permissions/share-links/{shareLinkId}`
- `GET /api/v1/share-links/{token}/resolve`
- `GET /api/v1/public/share-links/{token}/resolve`
- `GET /api/v1/public/share-links/{token}/document`
- `GET /api/v1/permissions/resources/{resourceType}/{resourceId}/email-invites`
- `POST /api/v1/permissions/resources/{resourceType}/{resourceId}/email-invites`
- `DELETE /api/v1/permissions/email-invites/{inviteId}`
- `GET /api/v1/permissions/email-invites/{token}/resolve`
- `POST /api/v1/permissions/email-invites/{token}/accept`
- `GET /api/v1/permissions/audit?workspaceId=&resourceType=&resourceId=`
- `POST /api/v1/permissions/access-requests`
- `GET /api/v1/permissions/access-requests?workspaceId=&status=`
- `GET /api/v1/permissions/resources/{resourceType}/{resourceId}/access-requests`
- `POST /api/v1/permissions/access-requests/{requestId}/review`
- `POST /api/v1/permissions/access-requests/{requestId}/cancel`
- `GET /api/v1/notifications?workspaceId=&unreadOnly=`
- `PATCH /api/v1/notifications/{notificationId}/read`
- `PATCH /api/v1/notifications/read-all`
- `POST /api/v1/files/uploads/sessions`
- `GET /api/v1/files/uploads/sessions/{sessionId}`
- `PUT /api/v1/files/uploads/sessions/{sessionId}/content`
- `POST /api/v1/files/uploads/sessions/{sessionId}/complete`
- `POST /api/v1/files/uploads/sessions/{sessionId}/finalize`
- `GET /api/v1/files/uploads/sessions/{sessionId}/progress`
- `POST /api/v1/files/uploads/sessions/{sessionId}/abort`
- `POST /api/v1/files/uploads/sessions/{sessionId}/parts/presign` returns
  `400 VALIDATION_ERROR` in Phase 6 because multipart is not enabled.
- `GET /api/v1/files/{fileId}`
- `GET /api/v1/files/{fileId}/content`
- `DELETE /api/v1/files/{fileId}`
- `GET /api/v1/documents/{documentId}/attachments`
- `POST /api/v1/documents/{documentId}/attachments`
- `DELETE /api/v1/documents/{documentId}/attachments/{attachmentId}`

Workspace member permissions are role based:

- `viewer`: read bootstrap/map/document/context/activity/search.
- `editor`: viewer plus create/update/move documents.
- Comment list uses scoped `document.view`. Comment create/add-message/
  resolve/reopen use scoped `document.comment`.
- File upload sessions, file deletion, and document attachment writes use
  scoped file/document/attachment actions.
- `admin`: editor plus member management.
- `owner`: admin plus ownership protection.

Permission Phase 11 is implemented in the backend. Role keys, contract ranks,
and current permission action keys are centralized in the application permission
catalog. Workspace view/edit/manage checks use the central effective permission
path backed by `workspace_members`. The backend has `resource_access_policies`,
`resource_access_grants`, `permission_audit_events`, `workspace_groups`,
`workspace_group_members`, `access_requests`, `share_links`, and
`permission_notifications`, repository support, collection-to-document
inheritance, workspace fallback effective permission calculation, group grant
calculation, access request review, notification reads, internal and external
authenticated share links, default-off public anonymous document and collection
links, public link password hashing, email invites with delivery status
tracking, provider-neutral IAM sync foundations, and scoped enforcement for key
document content paths.

Phase 11 exposes read-only effective permission, resource permission management
for `collection`/`document` resources, direct user/group grant create/update/
revoke, policy mutation for `inherit`/`restricted`, group list/detail/create/
update/archive, group member add/remove, access request create/list/review/
cancel, internal/external share link create/list/revoke/resolve, email invite
create/list/revoke/resolve/accept, notification list/mark-read, owner/admin IAM
sync, anonymous public document/collection share resolve/read endpoints, and
audit reads.
Permission mutation and
audit writes occur in the same transaction. Access request approval creates or
upgrades a direct grant and writes audit + notifications in the same
transaction. Revoked grants can be granted again for the same resource/subject
because uniqueness is scoped to active grants. Expired, non-revoked grants can
be renewed by grant create or access request approval; the renewal writes a
grant-updated audit event. `linkMode = internal` and `linkMode = external` are
allowed through policy mutation. Public document/collection link creation can set
`linkMode = public` only when `Permissions:PublicShareLinks:Enabled` is true;
manual public policy mutation remains deferred.

Temporary access is enforced for:

- `resource_access_grants.expires_at`
- `workspace_group_members.expires_at`

`expiresAt: null` means permanent access or membership. Expired grants,
revoked grants, expired group memberships, and removed group memberships are
ignored by effective permission checks. Past `expiresAt` values return
`400 VALIDATION_ERROR` on:

- `POST /api/v1/permissions/resources/{resourceType}/{resourceId}/grants`
- `PATCH /api/v1/permissions/resources/{resourceType}/{resourceId}/grants/{grantId}`
- `POST /api/v1/permissions/access-requests/{requestId}/review`
- `POST /api/v1/workspaces/{workspaceId}/groups/{groupId}/members`

Access request review accepts `expiresAt`; approving with a future value creates
or upgrades the resulting direct user grant as temporary access. Natural expiry
does not revoke the grant and does not write permission audit.

Grant PATCH treats omitted `expiresAt` as unchanged and explicit
`expiresAt: null` as clearing the expiry.

Phase 6 notification types:

- `permission.grant_expiring`
- `permission.grant_expired`
- `group.member_expiring`
- `group.member_expired`

The lightweight background job is `PermissionExpiryNotificationHostedService`.
It is disabled in the `Testing` environment, sends expiring notifications 24
hours before expiry by default, sends expired notifications for expired records
that have not been notified, and uses `permission_notifications.dedupe_key` for
idempotency. It writes notifications only; it does not write permission audit.

Configuration:

```json
{
  "Permissions": {
    "ExpiryNotifications": {
      "Enabled": true,
      "ScanIntervalMinutes": 60,
      "ExpiringWindowHours": 24
    }
  }
}
```

Phase 6 also hardens invalid `read-all.workspaceId` handling, maps unique
database conflicts to stable `409 CONFLICT` responses, notifies direct resource
managers when access requests are created, and uses a batch document permission
helper for bootstrap/map/search/export filtering.

Share links are implemented for `document` and `collection` resources.
Management requires authenticated active workspace membership plus the resource
share permission (`document.share` or `collection.share`). Link roles are
limited to `viewer` and `commenter`; `editor`, `admin`, and `owner` are
rejected.

Supported audiences:

- `workspace`: internal workspace-member links. Resolve and authorization
  require active workspace membership and `resource_access_policies.link_mode =
  internal`.
- `external`: authenticated external links. Resolve and authorization require
  the current user's normalized email to match `subjectEmail` and
  `link_mode = external`.
- `public`: default-off anonymous document and collection links. Creation requires
  `Permissions:PublicShareLinks:Enabled = true`, `roleKey = viewer`, future
  bounded `expiresAt`, no `subjectEmail`, and a document or collection
  resource. Collection public links expose only minimal collection/document
  listing metadata and exclude restricted, archived, and deleted children.

Create request:

```json
{
  "roleKey": "viewer",
  "audience": "workspace",
  "expiresAt": null,
  "subjectEmail": null,
  "password": null
}
```

External link request example:

```json
{
  "roleKey": "commenter",
  "audience": "external",
  "expiresAt": "2026-05-07T00:00:00Z",
  "subjectEmail": "person@example.com"
}
```

Public document or collection link request example when the feature flag is
enabled:

```json
{
  "roleKey": "viewer",
  "audience": "public",
  "expiresAt": "2026-05-07T00:00:00Z",
  "subjectEmail": null,
  "password": "optional-create-time-password"
}
```

Create response returns the raw token only once:

```json
{
  "link": {
    "id": "3b85e917-7c61-45f8-8f6e-c45ecf04ca4c",
    "workspaceId": "00000000-0000-0000-0000-000000000000",
    "resourceType": "document",
    "resourceId": "11111111-1111-1111-1111-111111111111",
    "roleKey": "viewer",
    "audience": "workspace",
    "subjectEmail": null,
    "createdBy": "22222222-2222-2222-2222-222222222222",
    "createdAt": "2026-04-30T00:00:00Z",
    "expiresAt": null,
    "revokedAt": null,
    "hasPassword": false
  },
  "token": "base64url-token-shown-once",
  "url": "/api/v1/share-links/base64url-token-shown-once/resolve"
}
```

List responses never include the raw token, token hash, plaintext password, or
password hash. Tokens are random, URL-safe, high entropy, and stored only as
SHA-256 hashes. Public link passwords are stored only as password hashes.
Expired or revoked links do not authorize. Natural expiry does not write
permission audit; explicit revoke writes `share_link.revoked`.

Share-link authorization is gated by `resource_access_policies.link_mode`.
An active link authorizes only when the linked document or collection policy has
the matching link mode (`internal` for workspace links, `external` for external
links, `public` for public document/collection links on public endpoints).
Missing policy, `disabled`, and mismatched modes do not authorize share-link
access. Public tokens are ignored by authenticated document `shareToken` paths.
List responses
still return active link metadata
while `linkMode = disabled`; disabling link mode pauses authorization without
deleting or revoking the link.

Resolve:

```text
GET /api/v1/share-links/{token}/resolve
GET /api/v1/public/share-links/{token}/resolve
GET /api/v1/public/share-links/{token}/document
GET /api/v1/public/share-links/{token}/collection
```

Public anonymous endpoint responses are intentionally minimal and read-only.
`/public/share-links/{token}/document` returns link metadata plus document id,
title, status, updatedAt, tags, content, and revision.
`/public/share-links/{token}/collection` returns link metadata plus collection
id, title, updatedAt, sortOrder, and child document listing metadata only. It
excludes restricted child documents, archived documents, and deleted documents.
If `hasPassword = true`, callers must send `X-Share-Link-Password`. Revoked,
expired, unknown, missing-policy, disabled, internal, external-mode,
missing-password, or wrong-password tokens return a stable `404 NOT_FOUND`.

Public link feature flag example:

```json
{
  "Permissions": {
    "PublicShareLinks": {
      "Enabled": false,
      "RequireExpiry": true,
      "ViewerOnly": true,
      "MaxExpiryDays": 7,
      "RateLimit": {
        "PermitLimit": 60,
        "WindowSeconds": 60,
        "QueueLimit": 0
      }
    }
  }
}
```

Phase 11 public-link behavior:

- feature flag default remains `false`;
- public document and collection links are viewer-only, require bounded future
  expiry, and reject `subjectEmail`;
- public collection links never return child document content and exclude
  restricted, archived, and deleted child documents;
- optional public link passwords are accepted only at create time, stored only
  as password hashes, and proved with `X-Share-Link-Password`;
- public tokens are never accepted by bootstrap, map, search, export, comments,
  edit, archive, delete, management, or authenticated share-token paths.

Protected document read/context/activity/comment endpoints accept a share link
token as either `?shareToken=` or `X-Share-Link-Token`. A viewer link authorizes
view only. A commenter link authorizes view and comment. Share links do not
authorize edit/manage/archive/delete/export, and bootstrap/map/search/export/
list filtering ignores share tokens. Collection links can authorize child
document view/comment only while the collection policy has the matching link
mode and the document is not blocking inherited collection access through its
own restricted policy.

Email invites are also implemented for `document` and `collection` resources.
They are token-based, email-bound, revocable, expiring, and do not create
workspace membership.

```text
GET    /api/v1/permissions/resources/{resourceType}/{resourceId}/email-invites
POST   /api/v1/permissions/resources/{resourceType}/{resourceId}/email-invites
DELETE /api/v1/permissions/email-invites/{inviteId}
GET    /api/v1/permissions/email-invites/{token}/resolve
POST   /api/v1/permissions/email-invites/{token}/accept
```

Create invite example:

```json
{
  "email": "person@example.com",
  "roleKey": "viewer",
  "expiresAt": "2026-05-07T00:00:00Z"
}
```

Create returns the raw invite token only once:

```json
{
  "invite": {
    "id": "44444444-4444-4444-4444-444444444444",
    "workspaceId": "00000000-0000-0000-0000-000000000000",
    "resourceType": "document",
    "resourceId": "11111111-1111-1111-1111-111111111111",
    "email": "person@example.com",
    "roleKey": "viewer",
    "status": "pending",
    "invitedBy": "22222222-2222-2222-2222-222222222222",
    "acceptedBy": null,
    "revokedBy": null,
    "createdAt": "2026-04-30T00:00:00Z",
    "expiresAt": "2026-05-07T00:00:00Z",
    "acceptedAt": null,
    "revokedAt": null,
    "expiredAt": null,
    "deliveryStatus": "disabled",
    "deliveryProvider": "noop",
    "deliveryAttemptedAt": null,
    "deliveryErrorCode": null
  },
  "token": "base64url-token-shown-once",
  "url": "/api/v1/permissions/email-invites/base64url-token-shown-once/accept",
  "delivery": {
    "status": "disabled",
    "provider": "noop",
    "attemptedAt": null,
    "errorCode": null
  }
}
```

Invite rules:

- `expiresAt` is required and must be in the future.
- Duplicate pending invite for the same workspace/resource/email returns
  `409 CONFLICT`.
- Resolve/accept requires an authenticated user whose normalized email matches
  the invite.
- Accepted invites authorize only the bound resource path while unexpired,
  non-revoked, and gated by `linkMode = external`.
- Search/export/map/bootstrap/list ignore invite access.
- List/resolve/accept/audit never return raw tokens or token hashes.
- Create attempts delivery through `IEmailInviteDeliveryService`. The default
  provider is disabled/noop, so no external email is sent unless configured or
  replaced by a provider/fake in tests.
- Delivery status is persisted as `disabled`, `sent`, or `failed`; audit
  metadata may include delivery status/provider/error code but never raw token,
  accept URL, token hash, SMTP secret, or provider credential.

Invite delivery configuration example:

```json
{
  "Permissions": {
    "EmailInvites": {
      "Delivery": {
        "Enabled": false,
        "Provider": "noop",
        "PublicBaseUrl": "https://northstar.example"
      }
    }
  }
}
```

IAM sync is implemented as provider-neutral, owner/admin-only workspace sync.
It does not implement a real IdP login flow or a public SCIM 2.0 server.

```text
POST /api/v1/workspaces/{workspaceId}/iam/sync
```

Request example:

```json
{
  "provider": "okta",
  "users": [
    {
      "externalSubjectId": "00u123",
      "email": "alex@example.com",
      "displayName": "Alex Kim",
      "workspaceRole": "viewer",
      "workspaceId": "00000000-0000-0000-0000-000000000000"
    }
  ],
  "groups": [
    {
      "externalGroupId": "eng",
      "name": "Engineering",
      "description": "Synced from Okta",
      "members": ["00u123"],
      "workspaceId": "00000000-0000-0000-0000-000000000000"
    }
  ]
}
```

Response summary:

```json
{
  "counts": {
    "created": 3,
    "updated": 0,
    "removed": 0,
    "skipped": 0,
    "usersCreated": 1,
    "usersUpdated": 0,
    "workspaceMembersCreated": 1,
    "groupsCreated": 1,
    "groupsUpdated": 0,
    "membersAdded": 1,
    "membersRemoved": 0
  },
  "users": [
    {
      "externalSubjectId": "00u123",
      "userId": "11111111-1111-1111-1111-111111111111",
      "created": true,
      "workspaceMemberCreated": true
    }
  ],
  "groups": [
    {
      "externalGroupId": "eng",
      "groupId": "22222222-2222-2222-2222-222222222222",
      "created": true,
      "membersAdded": 1,
      "membersRemoved": 0
    }
  ]
}
```

IAM sync behavior:

- `provider` is normalized to lowercase.
- Users are mapped by `(external_provider, external_subject_id)`. If a payload
  user email matches an existing local user without a conflicting external
  identity, sync maps that user instead of creating a duplicate.
- Synced users are ensured as active workspace members. `workspaceRole`
  defaults to `viewer`; `owner` is rejected.
- IAM-managed groups store `externalProvider`, `externalGroupId`, and
  `externalSyncedAt` and use `type = dynamic`.
- Repeating the same payload does not create duplicate users, groups, workspace
  members, or group members.
- Managed group members missing from the next payload are soft-removed.
  Sync does not delete local users, external users, or local groups.
- Normal group update/archive/member add/remove APIs reject IAM-managed groups.
- IAM-managed group grants reuse the existing group grant effective permission
  path for document and collection access.
- Sync writes permission audit actions `iam.user_mapped`, `iam.group_synced`,
  `iam.group_member_added`, and `iam.group_member_removed`. Audit metadata must
  not contain tokens, secrets, SAML assertions, OIDC tokens, SCIM bearer tokens,
  or raw external credentials.

Production SMTP/provider-backed invite delivery, secure invite outbox,
MFA/recent-auth enforcement, notification preference persistence,
watched/muted persistence, share-link/invite notification fan-out,
delivery-failure notifications, group grant fan-out notifications, real IdP
login UI, public SCIM 2.0 server endpoints, group access request UI, bulk link
management, browser acceptance hardening, and full frontend permission mutation
workflows are not implemented. `commenter` remains a scoped grant role only and
is not accepted by `workspace_members.role`.

The long-term permission architecture, scoped grants, audit requirements, and
implementation guardrails are defined in
`../../docs/PERMISSION_SYSTEM_CONTRACT.md`. That contract is authoritative
for future public-link, access-request, SSO/IAM, and permission work.

Comment persistence behavior:

- Comments are stored in `comment_threads` and `comment_messages`, outside
  document content, drafts, versions, search projections, exports, and document
  saves.
- `comment_threads.anchor` stores the full `CommentAnchorV1` JSON as PostgreSQL
  `jsonb`.
- The API does not persist runtime mapped ranges, `DecorationSet`,
  `runtimeMatch`, `activeThreadId`, pending composer state, or `blockId`.
- DTOs may return `anchorStatus: "active"` as a frontend compatibility default;
  it is not durable backend anchor truth.
- Comment mutations do not use or modify `PATCH /api/v1/documents/{documentId}`.

Phase 6 file behavior:

- Only `single` upload sessions are supported.
- `files` rows are created only by `finalize`, after `complete` validates byte
  size and optional SHA-256 checksum.
- `workspace_id + idempotency_key` makes upload session creation idempotent.
- Repeated finalize returns the existing file and does not duplicate files,
  attachments, or outbox events.
- `DELETE /files/{fileId}` soft-deletes the file and returns `409 CONFLICT`
  while active document attachments still reference it.
- Saving Tiptap content validates internal file references and auto-creates
  `inline_image` document attachments for valid referenced files.
- Phase 6 writes `file.finalized`, `document_attachment.created`, and
  `file.deleted` rows to `file_outbox_events`; no MQ dispatcher runs yet.

Phase 5 document lifecycle behavior:

- Archive sets `documents.status = archived`, removes search projection, writes
  `document.archived`, and hides the document from bootstrap/map/search.
- Restore changes archived documents back to draft, rebuilds search projection,
  writes `document.restored`, and restores default visibility.
- Delete is soft delete only, removes search projection and document links,
  writes `document.deleted`, and direct GET returns 404.
- Repeated archive/delete calls are idempotent and do not duplicate activity.

Space export/import:

- Export is viewer+ and includes archived documents by default.
- Export never includes deleted documents, auth tokens, credentials, password
  hashes, or activity events.
- Import is editor+ and append-only in Phase 5.
- Import creates documents, drafts, tags, imported initial versions, search
  projections, activity events, and valid internal backlinks.
- Import rewrites internal document links when both source and target are in the
  same import batch. Out-of-batch document links stay in content but do not
  create invalid backlink rows.
- Documents whose folder cannot be mapped are placed into the first target-space
  collection; if none exists, an `Imported` collection is created.
- Phase 6 export/import does not package file binaries. Document content may
  still contain file references and attachment metadata may be exported later,
  but import must not forge `files` rows for source-environment file IDs.

## Search Strategy

Phase 3/4 search is intentionally lightweight and database-local. It queries
`document_search_index.title` and `text_content` with case-insensitive contains
matching and does not use Meilisearch, OpenSearch, Elasticsearch, or an external
indexer.

Future search work should evaluate PostgreSQL `tsvector`, trigram indexes, or an
external search engine behind the existing `document_search_index` projection.

## PostgreSQL Smoke Profile

The normal `dotnet test` path uses EF InMemory for fast API coverage. A real
PostgreSQL smoke path is available when `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is
set to a disposable database connection string. The smoke test deletes and
recreates that database, applies migrations, seeds twice, and exercises bootstrap,
context, activity, search, PATCH-derived data, archive/delete visibility, space
export/import append, upload session, local upload/complete/finalize, document
attachment listing, private file content access, and file delete conflict/success
paths.

```powershell
$env:NORTHSTAR_POSTGRES_SMOKE_CONNECTION="Host=localhost;Port=5432;Database=northstar_smoke;Username=postgres;Password=postgres"
dotnet test .\Northstar.sln --filter PostgreSqlSmoke
```

This profile requires a reachable PostgreSQL server. It does not require Docker
unless PostgreSQL itself is being run through Docker Desktop.
