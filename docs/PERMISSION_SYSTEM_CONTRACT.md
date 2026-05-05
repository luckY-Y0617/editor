# Permission System Contract

Status: canonical contract for the Northstar permission module; permission
system V1 is frozen as `V1 complete / ship with documented deferred items`.
The V1 freeze is recorded in
`docs/agent/reports/permission-system-v1-doc-freeze.md`; future agents must not
reopen permission V1 scope unless the user explicitly opens a permission V2 task
or targeted follow-up. Implemented V1 surface runs through MFA provider step-up
enforcement V1. Earlier slices include
frontend permission admin surfaces V1, secure invite outbox/retry delivery, SCIM
Provisioning Compatibility Hardening V1.1, the production invite delivery
provider boundary, the real IdP login boundary, the MFA/recent-auth backend
state foundation, the SCIM endpoint skeleton, dedicated SCIM bearer-token
validation, group grant notification fan-out, Phase 13 share-link/invite
in-app notification fan-out, and Phase 11 public anonymous document and
collection links behind a default-off feature flag, endpoint-level rate limiting, link password hashing
for public links, invite delivery status tracking, and a default disabled/noop
invite delivery provider. Earlier phases include
temporary access, expiry notifications, permission boundary hardening,
linkMode-gated internal and external authenticated share-link authorization,
SSO-style external user mapping, IAM-managed groups, SCIM-style sync writes,
email invites, the frontend document permission mutation workflow V1 for
direct grants, group grants, and resource policy settings, and frontend
permission admin surfaces V1 for workspace member management and SCIM
management. SCIM bulk/complex-filter/enterprise/delete-deactivate behavior and
broader compatibility beyond V1.1, full OIDC/SAML provider redirect/callback
and secret management, additional MFA providers/recovery flows, PostgreSQL smoke
from the agent process, and Browser QA/frontend public-link browser acceptance
after Node runtime upgrade remain deferred non-V1 items.

Public-link conflict resolution:

- The public-link architecture in
  `docs/agent/reports/public-link-architecture-decision.md` has been approved
  by the user.
- Public links are read-only, token-scoped capabilities.
- Public document links are supported.
- Public collection links are supported as summary-only public links: anonymous
  collection reads expose collection metadata and visible child document
  summaries only, not child document content.
- Public links are created only through authenticated share-link APIs.
- Generic permission policy patch must continue rejecting direct
  `linkMode = public`.
- Public share-link creation may internally set resource policy
  `LinkMode = public`.
- Anonymous public access is limited to:
  - `GET /api/v1/public/share-links/{token}/resolve`
  - `GET /api/v1/public/share-links/{token}/document`
  - `GET /api/v1/public/share-links/{token}/collection`
- Public links must not broaden bootstrap, map, search, export, context,
  activity, comments, attachments, files, versions, mutations, or permission
  APIs.

This document is the canonical implementation boundary for permissions in the
Northstar knowledge workspace. It defines the role model, access scopes,
effective authorization algorithm, database model, API surface, UI rules,
audit requirements, security requirements, and rollout gates.

When this document conflicts with a mock UI, prototype, or prompt, this
document wins. UI may display deferred capabilities only when they are clearly
marked as non-persistent or unavailable until the backend capability exists.

## Current Baseline

The current backend has workspace membership RBAC:

- `owner`
- `admin`
- `editor`
- `viewer`

Current behavior:

- `viewer`: read bootstrap, map, document, context, activity, and search.
- `editor`: viewer plus create, update, and move documents.
- comments use scoped `document.view` for list and scoped `document.comment`
  for mutation.
- file and attachment operations use scoped file/document/attachment actions.
- `admin`: editor plus workspace member management.
- `owner`: admin plus ownership protection.

Phase 1 through Phase 11 implementation status:

- role keys, contract ranks, and permission action keys are centralized in the
  backend permission catalog;
- workspace view/edit/manage enforcement uses the workspace-only effective
  permission path backed by `workspace_members`;
- `commenter` is still a reserved scoped role and is rejected by
  `workspace_members.role`;
- `resource_access_policies` and `resource_access_grants` exist for
  document/collection scoped permission calculation;
- collection/document effective permission can fall back to workspace
  membership, inherit through collection policy, apply direct active user and
  group grants, and respect `restricted` inheritance mode;
- `GET /api/v1/permissions/effective` exposes the current user's read-only
  effective permissions for workspace, collection, or document resources;
- `GET /api/v1/permissions/resources/{resourceType}/{resourceId}` exposes
  resource policy, user/group grants, effective access, and inherited source
  for `collection` and `document` resources;
- `PATCH /api/v1/permissions/resources/{resourceType}/{resourceId}/policy`
  updates `inherit`/`restricted` policy and allows `linkMode = internal` or
  `external`; `public` link mode is still rejected;
- grant create/update/revoke APIs exist for direct `user` and `group`
  subjects and write immutable permission audit events in the same transaction;
- revoked grants can be re-granted for the same resource/subject because the
  uniqueness rule applies only to active grants;
- workspace group APIs exist for list/detail/create/update/archive and
  add/remove member flows; all group mutations write permission audit events;
- `GET /api/v1/permissions/audit?workspaceId=&resourceType=&resourceId=`
  reads permission audit events for authorized managers;
- `access_requests` and `permission_notifications` exist for
  collection/document access request flows and permission notification reads;
- access request APIs support create, workspace/resource list, approve/deny
  review, and cancel flows; approval creates or upgrades a direct grant and
  writes audit and notifications in the same transaction;
- notification APIs support current-user listing, mark-read operations, and
  current-user notification preference read/upsert;
- share-link and email-invite create/revoke/accept/delivery-failure flows emit
  in-app notifications to relevant resource managers, excluding the actor and
  respecting matching muted notification preferences;
- key document read/write paths now use scoped effective permission, including
  document get/update/move/archive/restore/delete, context, activity, comments,
  attachments, search, export, map, and bootstrap filtering;
- the Share & Permissions page can read the resource permissions API when
  configured with a real document id and can display pending access requests;
- the Updates page can read backend permission notifications and mark them read
  when the API and token are configured;
- temporary direct user/group grants are supported through
  `resource_access_grants.expires_at`; expired and revoked grants are ignored by
  effective permission checks;
- temporary workspace group memberships are supported through
  `workspace_group_members.expires_at`; expired and removed memberships are
  ignored by group effective permission checks;
- grant create/update, group member add, and access request approve reject past
  `expiresAt` with `400 VALIDATION_ERROR`;
- access request review accepts `expiresAt` and approval can create or upgrade a
  temporary direct grant;
- permission expiry notification types and the
  `PermissionExpiryNotificationHostedService` exist for direct user grants and
  group memberships;
- `permission_notifications.dedupe_key` and a filtered unique index provide
  idempotency for expiry notifications;
- `PATCH /api/v1/notifications/read-all` validates `workspaceId` with
  `Guid.TryParse` and returns `400 VALIDATION_ERROR` for invalid UUIDs;
- database unique conflicts, including duplicate pending access requests, are
  mapped to stable `409 CONFLICT` responses;
- access request created notifications include workspace managers, direct user
  grant subjects, and active members of group grants with resource
  `manage_permissions`;
- a workspace-scoped SCIM 2.0 endpoint exists under
  `/api/v1/workspaces/{workspaceId}/scim/v2`; discovery endpoints return
  service-provider, schema, and resource-type shapes for owners/admins or valid
  workspace-scoped SCIM bearer tokens, while minimal User/Group provisioning V1
  and Compatibility Hardening V1.1 require valid workspace-scoped SCIM bearer
  tokens for provisioning mutations;
- dedicated SCIM bearer tokens are managed under
  `/api/v1/workspaces/{workspaceId}/scim/tokens`, are stored hash-only in
  `scim_tokens`, return the raw token only at creation, support revoke/expiry,
  and do not create workspace membership implicitly;
- `GET /api/v1/auth/security-state` exposes backend-derived recent-auth state
  from successful trusted auth/step-up events and reports TOTP MFA enabled and
  verified state from backend-persisted MFA state;
- `POST /api/v1/auth/mfa/totp/enroll`,
  `POST /api/v1/auth/mfa/totp/verify`, and
  `POST /api/v1/auth/mfa/totp/disable` implement backend-backed TOTP
  enrollment, verification, and disable flows with protected secret storage;
- high-risk permission mutations require backend step-up when the current user
  has enabled MFA, including document/collection grant mutation, resource
  policy mutation, share-link create/revoke, email-invite create/revoke,
  workspace member add/update/remove, and SCIM token create/revoke;
- `POST /api/v1/auth/idp/login` provides a default-disabled backend IdP login
  boundary for trusted external identity assertions, maps or binds existing
  users through `external_provider` + `external_subject_id`, issues normal
  Northstar tokens, writes secret-safe auth events, and does not create
  workspace membership implicitly;
- bootstrap/map/search/export filtering uses a batch document effective
  permission helper to avoid per-document policy/grant queries;
- Share & Permissions supports Request access plus approve/deny review actions,
  and Updates follows notification `actionUrl` links. Document grant, policy,
  share-link, invite, and public document-link workflows are implemented through
  the current frontend slices.
- `share_links` exists for `collection` and `document` resources with hashed
  token storage, `audience = workspace`, `viewer`/`commenter` roles, expiry,
  soft revoke, and create/list/revoke/resolve APIs;
- internal share links require authenticated active workspace membership and
  can authorize only the linked resource path for view/comment actions when the
  linked resource policy has `linkMode = internal`;
- share link creation/revocation writes permission audit without storing raw
  tokens, and raw tokens are returned only in the create response;
- document read/context/activity/comment APIs accept a share link token through
  `shareToken` query or `X-Share-Link-Token` header; search/export/list
  filtering does not use share tokens;
- Share & Permissions can create internal viewer/commenter links, display the
  generated URL/token once, list active links, and revoke links.
- users can be mapped to external identities through
  `external_provider` + `external_subject_id` with a filtered unique index;
- workspace groups can be marked IAM-managed through
  `external_provider` + `external_group_id` + `external_synced_at` with a
  workspace-scoped filtered unique index;
- `POST /api/v1/workspaces/{workspaceId}/iam/sync` supports owner/admin
  provider-neutral sync of external users, IAM-managed groups, and group
  memberships;
- IAM sync is idempotent for repeated payloads, soft-removes memberships that
  disappear from the payload, and does not delete local users or local groups;
- IAM-managed groups can receive resource grants and reuse the existing group
  effective permission path;
- normal group update/archive/member mutation APIs reject IAM-managed groups;
- IAM sync writes permission audit events for user mapping, group sync, and
  member add/remove without storing tokens, secrets, or raw credentials;
- Share & Permissions displays external group source metadata and keeps
  IAM-managed group mutation controls disabled.
- external authenticated share links are supported for `document` and
  `collection` resources with `audience = external`, normalized recipient email
  binding, hashed token storage, one-time raw token create response, expiry,
  soft revoke, and `linkMode = external` authorization gating;
- external share links authorize only explicit single-resource token paths for
  view/comment according to `viewer`/`commenter`; they do not create workspace
  membership and do not broaden bootstrap/map/search/export/list filtering;
- `resource_email_invites` implements pending/accepted/revoked/expired email
  invite lifecycle with hashed token storage, normalized email binding, stable
  duplicate pending invite `409 CONFLICT`, accept/revoke APIs, and accepted
  single-resource effective permission;
- email invite create/accept/revoke/explicit expiry state transitions write
  permission audit without raw tokens;
- public anonymous document links are supported only when
  `Permissions:PublicShareLinks:Enabled = true`; they require future expiry,
  enforce the configured max expiry, are viewer-only, reject `subjectEmail`,
  set `resource_access_policies.link_mode = public`, and authorize only the
  dedicated anonymous public resolve/document endpoints;
- public anonymous endpoints are rate-limited by the
  `public-share-links` endpoint policy and never feed bootstrap/map/search/
  export/list/comment/edit/manage/archive/delete paths;
- public collection links are supported through the dedicated anonymous
  collection endpoint and return only minimal collection + child document
  listing metadata; restricted child document policies, archived children, and
  deleted children are excluded and cannot be traversed to document content;
- public link passwords are supported only for public links, accepted once at
  create time, stored as password hashes, exposed only as `hasPassword`
  metadata, and required as `X-Share-Link-Password` proof on public resolve/read
  endpoints when present;
- email invite creation records delivery status (`disabled`, `sent`, or
  `failed`) through `IEmailInviteDeliveryService`; the default provider is
  disabled/noop, a configuration-driven SMTP provider boundary exists, tests
  can use a fake provider, and raw invite tokens are not stored in DB or audit
  metadata;
- Share & Permissions can create/update/revoke direct user and group document
  grants, update supported resource policy settings, create external
  authenticated links by email, create email invites, show generated URLs/tokens
  only once, list active link/invite metadata, and revoke links/invites.
- Permission Admin surfaces can list/add/update/remove workspace members through
  existing workspace member APIs, show workspace group sources, and manage SCIM
  bearer tokens with one-time raw token display through existing SCIM token
  management APIs.
- Share & Permissions can create public document links through the dedicated
  share-link API only, requires a future expiry, keeps public links viewer-only,
  supports optional create-time password input, shows generated public URL/token
  only in a dismissible one-time panel, and does not expose direct
  `linkMode = public` in generic policy controls.

## V1 Frozen Surface

Permission system V1 is documented as complete and shippable with deferred
items. The V1 surface includes:

- workspace RBAC;
- scoped document/collection permissions;
- effective permission service;
- direct user grants, group grants, and temporary grants;
- access requests;
- permission notifications and notification preferences / watched-muted;
- share links, external authenticated links, and email invites;
- public document links;
- public collection summary links;
- public-link passwords;
- public anonymous access isolated to dedicated public share-link endpoints;
- permission audit;
- SCIM endpoint skeleton, bearer-token validation, provisioning V1, and
  compatibility hardening V1.1;
- real IdP login backend boundary;
- production invite delivery provider boundary;
- secure invite outbox/retry;
- TOTP MFA enrollment, verification, disable, and step-up enforcement for
  high-risk permission mutations;
- frontend document permission mutation workflow;
- frontend permission admin surfaces;
- frontend public-link interaction hardening.

Validation carried forward from
`docs/agent/reports/permission-system-v1-release-readiness.md`:

- `dotnet restore services/api/Northstar.sln`: passed;
- `dotnet build services/api/Northstar.sln`: passed;
- `dotnet test services/api/Northstar.sln`: passed;
- `npm run build`: passed;
- `npm test`: passed;
- PostgreSQL smoke was not run by agent because
  `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was not visible;
- user previously reported PostgreSQL smoke passed manually after setting the
  connection string;
- Browser QA was not run because local Node runtime is too old for Browser Use.

## Deferred Non-V1 Register

The current permission system does not yet have:

- SCIM bulk, complex filter grammar, enterprise extension, delete/deactivate,
  and broader compatibility beyond V1.1;
- full OIDC/SAML provider redirect/callback and secret management;
- WebAuthn/passkeys;
- SMS/email MFA providers;
- MFA recovery codes;
- MFA recovery/reset/admin reset flows;
- advanced SCIM compatibility beyond V1.1;
- PostgreSQL smoke from the agent process while
  `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is not visible;
- Browser QA and frontend public-link browser acceptance while the local Node
  runtime remains too old for Browser Use.

Those capabilities must not be represented as real backend behavior until the
schemas, services, APIs, and tests described here exist.

## Core Model

Northstar uses hierarchical RBAC with scoped grants:

```text
workspace role -> collection grant -> document grant -> share link / invite grant
```

Workspace membership remains the baseline. Collection and document grants can
increase or narrow access for specific resources depending on the resource
policy.

The supported resource scopes are:

- `workspace`
- `collection`
- `document`

The supported subject scopes are:

- `user`
- `group`
- `share_link`
- `email_invite`

Effective access is computed for each `(user, resource, action)` request. The
client never supplies effective permissions as trusted input.

## Non-Negotiable Rules

1. Every protected query and mutation must be authorized server-side.
2. UI checks are convenience only. They are not security boundaries.
3. Never trust role, permission, workspace id, resource id, or subject id values
   from the client without server validation.
4. Every business table participating in authorization must be scoped by
   `workspace_id`.
5. Authorization must be evaluated from persisted server state at request time.
6. Expired grants are ignored even if no cleanup job has processed them.
7. Revoked grants are ignored and retained for audit.
8. The last workspace owner cannot be removed, downgraded, suspended, expired,
   or revoked.
9. A user cannot grant a role higher than their own effective management role
   for that resource.
10. Workspace `owner` and `admin` retain management escape paths unless an
    explicit future legal/retention policy says otherwise.
11. Avoid explicit deny rules in V1. Use `inheritance_mode` instead.
12. Permission mutations and audit writes must occur in the same transaction.
13. Share link tokens must be random, high entropy, hashed at rest, revocable,
    and expiry-aware.
14. Permission checks must not depend on frontend route names or UI state.
15. Search, export, context, activity, comments, attachments, files, and version
    endpoints must all enforce the same effective access rules.
16. New content types must not add ad hoc role checks. They must extend the
    permission catalog.
17. Any capability shown in UI but not persisted must be titled, labelled, or
    otherwise implemented as deferred/non-persistent.

## Phase 11 Threat Model

Phase 9 added external authenticated resource access and email invites without
turning outsiders into implicit workspace members. Phase 10 added default-off
public anonymous document links and invite delivery status. Phase 11 extends
public links to collections with restricted child proof and adds public-link
password hashing without turning anonymous visitors into workspace members.

External authenticated links:

- require an authenticated Northstar user, a valid share token, and an active
  non-revoked, non-expired `audience = external` share link;
- are bound to a normalized recipient email in Phase 9. If future SSO identity
  binding is added, it must compare provider + subject from persisted user
  identity fields and must be documented here before release;
- authorize only the linked resource path and only the link role's allowed
  actions (`viewer` = view, `commenter` = view/comment);
- require the linked resource policy to have `linkMode = external`; missing
  policy, `disabled`, `internal`, or `public` does not authorize external
  links;
- do not create workspace membership, do not make the resource appear in
  bootstrap/map/search/export/list outputs, and do not grant workspace-wide
  visibility.

Email invites:

- create a pending, auditable, revocable, expiring invitation bound to
  workspace, resource type/id, normalized email, role, token hash, and
  inviter;
- return the raw invite token only in the create response and store only the
  token hash;
- can be accepted only by an authenticated user whose normalized email matches
  the invite recipient and while the invite is pending and unexpired;
- after acceptance, authorize only the invited resource path for the accepted
  role and never create workspace membership or global resource listing access;
- must reject duplicate pending invites for the same workspace/resource/email
  with stable `409 CONFLICT`.

Token leakage limits:

- tokens are high entropy and URL-safe; token hashes, not raw tokens, are stored
  and audited;
- revoked, expired, or unknown tokens return stable errors that do not expose
  whether the target resource exists;
- collection external links or invites may authorize child document read/comment
  only through the collection path and only when the collection policy permits
  external link access; a restricted document still blocks inherited collection
  token access unless the document has its own explicit external token/invite
  authorization.

Public anonymous links:

- are default-off and can be created only when
  `Permissions:PublicShareLinks:Enabled = true`;
- are limited to document and collection resources; collection public links
  resolve only through the dedicated public collection endpoint and never grant
  child document content access through legacy share-token paths;
- require a future `expiresAt`, reject expiries beyond
  `Permissions:PublicShareLinks:MaxExpiryDays`, and enforce viewer-only role;
- reject `subjectEmail` and do not bind to an authenticated user;
- require the linked document or collection policy to have `linkMode = public`;
  missing policy, `disabled`, `internal`, and `external` all fail closed;
- are resolved only through `/api/v1/public/share-links/{token}/resolve` and
  read only through `/api/v1/public/share-links/{token}/document` or
  `/api/v1/public/share-links/{token}/collection`;
- may be password-protected; the password is accepted once at create time, only
  a password hash is stored, and public resolve/read requires
  `X-Share-Link-Password` proof when `hasPassword = true`;
- public collection responses include only collection metadata and child
  document listing metadata, exclude restricted child documents, archived
  documents, and deleted documents, and never include child document content;
- must never authorize comment/edit/manage/archive/delete/export/list/search/
  map/bootstrap access or authenticated share-token paths;
- are endpoint-rate-limited and return stable failures for revoked, expired,
  unknown, or policy-mismatched tokens without exposing resource existence.

Invite delivery, audit, and notifications:

- create, accept, revoke, and explicit expire state transitions are audited
  without raw token material;
- invite delivery status is persisted and included in create/list DTOs, but raw
  invite URLs/tokens and token hashes are not written to audit metadata;
- natural expiry is evaluated at authorization time and does not broaden access;
- share-link/invite notification fan-out is implemented as in-app notifications
  only and must not include raw token material, token hashes, accept URLs, or
  password proof material.
- recent-auth state is backend-derived from successful trusted auth events.
- MFA provider/enrollment and step-up enforcement remain deferred; high-risk
  actions must not rely on frontend-only flags.

## Roles

System roles are stable role keys. They may be assigned at workspace,
collection, or document scope as supported by the schema.

| Role | Rank | Purpose |
| --- | ---: | --- |
| `owner` | 500 | Highest authority and ownership protection. |
| `admin` | 400 | Workspace and resource administration without ownership transfer. |
| `editor` | 300 | Content creation and editing. |
| `commenter` | 200 | Comment-only collaboration. |
| `viewer` | 100 | Read-only access. |

### Workspace Role Policy

Current workspace roles are `owner`, `admin`, `editor`, and `viewer`.

`commenter` must not be added to `workspace_members.role` until:

- the domain constants are updated;
- EF check constraints are updated;
- migrations are added;
- role rank logic is updated;
- workspace API DTO validation is updated;
- authorization tests cover commenter behavior.

Until then, `commenter` is valid only as a future scoped grant role for
document/collection collaboration.

### Role Meaning by Scope

| Role | Workspace | Collection | Document |
| --- | --- | --- | --- |
| `owner` | Full control, ownership protection. | Full control. | Full control. |
| `admin` | Manage settings, members, permissions, and content. | Manage collection access and content. | Manage document access and content. |
| `editor` | Create, update, and move workspace content. | Edit collection content. | Edit document content. |
| `commenter` | Deferred for workspace role. | Comment on collection documents. | Comment on document. |
| `viewer` | Read workspace content. | Read collection content. | Read document. |

## Permission Catalog

Permissions are action keys. Backend services should check action keys, not
spread role-name checks throughout the codebase.

Workspace actions:

```text
workspace.view
workspace.manage_settings
workspace.manage_members
workspace.manage_permissions
workspace.view_audit
```

Collection actions:

```text
collection.view
collection.edit
collection.create_document
collection.share
collection.manage_permissions
collection.archive
collection.delete
```

Document actions:

```text
document.view
document.comment
document.edit
document.share
document.manage_permissions
document.archive
document.restore
document.delete
```

Version actions:

```text
version.view
version.create
version.restore
```

File and attachment actions:

```text
file.upload
file.download
file.delete
attachment.view
attachment.create
attachment.delete
```

System actions:

```text
access_request.create
access_request.review
notification.view
notification.manage_preferences
```

The minimum default role mapping is:

| Action class | Owner | Admin | Editor | Commenter | Viewer |
| --- | --- | --- | --- | --- | --- |
| View | yes | yes | yes | yes | yes |
| Comment | yes | yes | yes | yes | no |
| Edit content | yes | yes | yes | no | no |
| Share resource | yes | yes | yes | no | no |
| Manage permissions | yes | yes | no | no | no |
| Manage workspace members | yes | yes | no | no | no |
| Restore/delete/archive | yes | yes | yes | no | no |

If a product decision changes a role meaning, update the catalog and tests
first. Do not special-case a controller.

## Resource Policy

Each collection or document can have a resource policy.

`inheritance_mode`:

- `inherit`: include parent/workspace access when computing effective access.
- `restricted`: require direct grants, group grants, owner/admin escape paths,
  or valid share link grants.

`link_mode`:

- `disabled`: no share link access.
- `internal`: link is usable only by authenticated workspace members.
- `external`: link or accepted email invite is usable only by an authenticated
  user whose normalized email matches the external access record.
- `public`: usable only by default-off Phase 11 public anonymous document and
  collection links. It is set by public link creation when the feature flag is
  enabled;
  general policy mutation still rejects manual public mode changes unless a
  later UI/API release explicitly enables them.

Current behavior supports `disabled`, `internal`, `external`, and public
document link creation. Creating an internal share link may set
`link_mode = internal`; creating an external share link or email invite may set
`link_mode = external`; creating a public document link may set
`link_mode = public`. Revoking the last active link or invite does not
currently force `link_mode` back to `disabled`.

`link_mode = disabled` is an authorization gate, not a hard delete of link
metadata. Existing unexpired, non-revoked `share_links` rows may still appear in
management list responses, but they do not authorize access until the resource
policy is set back to the matching mode (`internal` for workspace-member links,
`external` for external links/invites). A missing policy is treated as
`disabled` for token/invite authorization.

`restricted` mode must warn users before saving because it can remove access
from workspace viewers or editors who do not have direct grants.

## Effective Permission Algorithm

The backend must use one central service for effective authorization.

Inputs:

- `userId`
- `workspaceId`
- `resourceType`
- `resourceId`
- `actionKey`
- optional `shareLinkToken`
- accepted email invites for the authenticated user's normalized email

Public anonymous document access is intentionally outside this authenticated
effective-permission path. It is evaluated by the dedicated public share-link
service and returns only a minimal read-only document payload.

All expiry comparisons use server UTC time at authorization time.

Algorithm:

1. Require an authenticated user.
2. Load active workspace membership.
3. If no active workspace membership exists, continue only for an explicit
   external share token or an accepted email invite. Internal share links,
   workspace/direct/group grants, and inherited workspace roles require active
   workspace membership. Anonymous requests are denied.
4. Load the target resource and verify it belongs to `workspaceId`.
5. Build the parent chain:
   - document -> collection -> workspace;
   - collection -> workspace;
   - workspace.
6. Load resource policies for each scoped resource.
7. Load active direct user grants for the target and parent resources.
8. Load active group grants for active, unremoved, unexpired groups containing
   the user. IAM-managed groups use this same path; their external source does
   not change grant ranking or action checks.
9. Ignore grants where `revoked_at` is not null.
10. Ignore grants where `expires_at <= now`.
11. If the target policy is `inherit`, include inherited workspace and parent
    roles.
12. If the target policy is `restricted`, ignore lower inherited roles, but
    keep owner/admin escape paths and explicit grants.
13. Include a valid internal share link role only when an explicit
    `shareLinkToken` was supplied for a single resource path, the link is
    active, the audience is `workspace`, the caller is an active workspace
    member, the linked resource policy exists with `linkMode = internal`, and
    the action is allowed by the link role.
14. Include a valid external share link role only when an explicit
    `shareLinkToken` was supplied for a single resource path, the link is
    active, the audience is `external`, the caller is authenticated, the
    caller's normalized email matches `share_links.subject_email`, and the
    linked resource policy exists with `linkMode = external`.
15. Include an accepted email invite role only when the invite is accepted,
    non-revoked, unexpired, bound to the caller's normalized email, and the
    resource policy exists with `linkMode = external`.
16. Public anonymous tokens are ignored by this authenticated share-token path;
    they are valid only on `/api/v1/public/share-links/*`.
17. A missing policy, `linkMode = disabled`, and mismatched link modes do not
    authorize token/invite access. A collection link or invite used for a child
    document must satisfy the collection policy's matching link mode; a
    restricted document still blocks inherited collection token/invite access
    unless a direct document token/invite or other explicit document access
    applies. A `viewer` token/invite can view only; a `commenter` token/invite
    can view and comment. Token/invite access cannot authorize
    edit/manage/archive/delete/export actions and must not broaden
    bootstrap/map/search/export/list filtering.
18. Select the highest ranked effective role.
19. Check `role_permissions` for `actionKey`.
20. Deny if the action is not in the effective role permission set.
21. For management actions, verify the actor can grant/revoke the requested
    role and cannot exceed their own effective management role.

The service returns:

```text
allowed: boolean
effective_role: role key or null
source: workspace | collection | collection_group | collection_share_link |
collection_email_invite | document | document_group | document_share_link |
document_email_invite | owner_escape | admin_escape
reason: machine-readable denial reason when denied
```

No controller should reimplement this algorithm.

## Database Schema

The current `workspace_members` table remains the workspace membership source.

Add these tables when implementing scoped permissions.

### users external identity fields

```sql
alter table users add column external_provider text;
alter table users add column external_subject_id text;

create unique index users_external_provider_subject_key
  on users (external_provider, external_subject_id)
  where external_provider is not null and external_subject_id is not null;
```

Rules:

- `external_provider` is normalized by the sync service.
- `external_subject_id` is the provider's stable subject id for the user.
- both fields are nullable for local users.
- the filtered unique index allows many local users without external identity
  while preventing duplicate provider/subject mappings.
- IAM sync may map an existing local user by email when that user has no
  conflicting external identity.
- IAM sync does not delete local or external users.

### roles

```sql
create table roles (
  id uuid primary key,
  key text not null unique,
  rank integer not null,
  description text not null,
  is_system boolean not null default true,
  created_at timestamptz not null default now()
);
```

Rules:

- `key` must be stable and lowercase.
- system roles cannot be deleted.
- role rank changes require a migration and test updates.

### permissions

```sql
create table permissions (
  key text primary key,
  description text not null
);
```

### role_permissions

```sql
create table role_permissions (
  role_id uuid not null references roles(id) on delete cascade,
  permission_key text not null references permissions(key) on delete cascade,
  primary key (role_id, permission_key)
);
```

### workspace_groups

```sql
create table workspace_groups (
  id uuid primary key,
  workspace_id uuid not null references workspaces(id) on delete cascade,
  name text not null,
  description text,
  type text not null check (type in ('static', 'dynamic')),
  archived_at timestamptz,
  external_provider text,
  external_group_id text,
  external_synced_at timestamptz,
  created_by uuid references users(id),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create unique index workspace_groups_workspace_name_active_key
  on workspace_groups (workspace_id, name)
  where archived_at is null;

create unique index workspace_groups_workspace_external_key
  on workspace_groups (workspace_id, external_provider, external_group_id)
  where external_provider is not null and external_group_id is not null;
```

Rules:

- IAM-managed groups have both `external_provider` and `external_group_id`.
- `external_synced_at` records the latest successful sync write for the group.
- IAM-managed groups are read-only through normal Northstar group
  update/archive/member mutation APIs; only IAM sync can create/update their
  metadata or memberships.
- IAM-managed groups can be granted resource permissions like local groups.
- Archived groups do not produce effective grants.

### workspace_group_members

```sql
create table workspace_group_members (
  id uuid primary key,
  group_id uuid not null references workspace_groups(id) on delete cascade,
  user_id uuid not null references users(id) on delete cascade,
  added_by uuid references users(id),
  added_at timestamptz not null default now(),
  expires_at timestamptz,
  removed_at timestamptz
);

create unique index workspace_group_members_group_user_active_key
  on workspace_group_members (group_id, user_id)
  where removed_at is null;
```

Rules:

- expired group memberships are ignored in effective permission checks.
- removed group memberships are ignored in effective permission checks.
- `expires_at = null` means permanent membership.
- API create/update flows must reject past `expiresAt` values with
  `400 VALIDATION_ERROR`.
- adding a user to a group with an expired, non-removed membership renews the
  existing row's `expires_at` and writes group member audit/notification
  records instead of creating a duplicate row.
- group membership changes must be audited when they affect access.

### resource_access_policies

```sql
create table resource_access_policies (
  id uuid primary key,
  workspace_id uuid not null references workspaces(id) on delete cascade,
  resource_type text not null check (resource_type in ('collection', 'document')),
  resource_id uuid not null,
  inheritance_mode text not null check (inheritance_mode in ('inherit', 'restricted')),
  link_mode text not null default 'disabled'
    check (link_mode in ('disabled', 'internal', 'external', 'public')),
  default_link_role text check (default_link_role in ('viewer', 'commenter')),
  created_by uuid references users(id),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  unique (resource_type, resource_id)
);
```

Rules:

- V1 default is `inherit` and `disabled`.
- `internal` gates workspace-member share links.
- `external` gates external authenticated share links and accepted email
  invites.
- `public` link mode requires a separate implementation and release gate before
  public anonymous access can be enabled.

### resource_access_grants

```sql
create table resource_access_grants (
  id uuid primary key,
  workspace_id uuid not null references workspaces(id) on delete cascade,
  resource_type text not null check (resource_type in ('collection', 'document')),
  resource_id uuid not null,
  subject_type text not null check (subject_type in ('user', 'group')),
  subject_id uuid not null,
  role_key text not null,
  granted_by uuid references users(id),
  granted_at timestamptz not null default now(),
  expires_at timestamptz,
  revoked_at timestamptz,
  revoked_by uuid references users(id),
  reason text
);

create unique index resource_access_grants_resource_subject_key
  on resource_access_grants (resource_type, resource_id, subject_type, subject_id)
  where revoked_at is null;
```

Rules:

- never hard delete grants in normal flows.
- use `revoked_at` and `revoked_by`.
- validate `role_key` against `roles.key`.
- grants cannot outlive a deleted resource.
- `subject_id` is validated in application code against the subject type:
  users must be active workspace members; groups must belong to the workspace
  and not be archived.
- revoked grants remain auditable and can be re-granted because only active
  grants are unique per resource/subject.
- expired grants are ignored in effective permission checks.
- expired non-revoked grants can be renewed by grant create or access request
  approval for the same resource/subject; renewal updates the existing row's
  `role_key`, `expires_at`, and reason and writes `grant.updated` audit.
- `expires_at = null` means permanent access.
- API create/update/review approval flows must reject past `expiresAt` values
  with `400 VALIDATION_ERROR`.
- `PATCH .../grants/{grantId}` treats absent `expiresAt` as no change and
  explicit `expiresAt: null` as clearing the expiry.
- natural expiry does not write revoke audit events.

### share_links

```sql
create table share_links (
  id uuid primary key default gen_random_uuid(),
  workspace_id uuid not null references workspaces(id) on delete cascade,
  resource_type text not null check (resource_type in ('collection', 'document')),
  resource_id uuid not null,
  token_hash text not null unique,
  role_key text not null check (role_key in ('viewer', 'commenter')),
  audience text not null check (audience in ('workspace', 'external', 'public')),
  subject_email citext,
  password_hash text,
  created_by uuid references users(id) on delete set null,
  created_at timestamptz not null default now(),
  expires_at timestamptz,
  revoked_at timestamptz
);

create index idx_share_links_resource
  on share_links (workspace_id, resource_type, resource_id);

create unique index idx_share_links_token_hash
  on share_links (token_hash);

create index idx_share_links_expiry
  on share_links (expires_at)
  where revoked_at is null;

create index idx_share_links_public_active
  on share_links (workspace_id, resource_type, resource_id, expires_at)
  where audience = 'public' and revoked_at is null;
```

Rules:

- generated tokens must be high entropy, URL-safe, and non-guessable.
- store only token hashes, never raw tokens.
- raw tokens are returned only in `POST .../share-links` create responses.
- list and resolve responses never expose the token hash or any reusable raw
  secret.
- `audience = workspace` requires active workspace membership and
  `link_mode = internal`.
- `audience = external` requires authenticated user email binding through
  `subject_email` and `link_mode = external`.
- `audience = public` requires the public feature flag, `resource_type` of
  `document` or `collection`, `role_key = viewer`, no `subject_email`, non-null
  future `expires_at`, expiry within the configured max window, and
  `link_mode = public`.
- `password_hash` is nullable and may be populated only for public links; raw
  public link passwords are never stored, audited, logged, or returned by list
  responses.
- link role cannot be `editor`, `admin`, or `owner`; only `viewer` and
  `commenter` are supported generally; public links are viewer-only.
- expired or revoked links do not authorize.
- active links do not authorize unless the linked resource policy has the
  matching link mode; missing policy, `disabled`, or a mismatched mode blocks
  authorization.
- past `expiresAt` values return `400 VALIDATION_ERROR`.
- natural link expiry does not write permission audit. Revoke writes audit.
- create/list/revoke require authenticated active workspace membership plus
  `document.share` or `collection.share` on the resource.
- resolving a workspace-audience link requires authenticated active workspace
  membership in the linked workspace.
- resolving an external-audience link requires authenticated user email match
  against `subject_email`.
- resolving a public-audience link is anonymous only through
  `/api/v1/public/share-links/{token}/resolve`; document reads use
  `/api/v1/public/share-links/{token}/document`.
- public collection reads use `/api/v1/public/share-links/{token}/collection`
  and expose collection metadata plus visible child document summaries only.

### resource_email_invites

```sql
create table resource_email_invites (
  id uuid primary key default gen_random_uuid(),
  workspace_id uuid not null references workspaces(id) on delete cascade,
  resource_type text not null check (resource_type in ('collection', 'document')),
  resource_id uuid not null,
  email citext not null,
  token_hash text not null unique,
  role_key text not null check (role_key in ('viewer', 'commenter')),
  status text not null check (status in ('pending', 'accepted', 'revoked', 'expired')),
  invited_by uuid references users(id) on delete set null,
  accepted_by uuid references users(id) on delete set null,
  revoked_by uuid references users(id) on delete set null,
  created_at timestamptz not null default now(),
  expires_at timestamptz not null,
  accepted_at timestamptz,
  revoked_at timestamptz,
  expired_at timestamptz,
  delivery_status text not null default 'disabled'
    check (delivery_status in ('disabled', 'sent', 'failed')),
  delivery_provider text not null default 'noop',
  delivery_attempted_at timestamptz,
  delivery_error_code text
);

create index idx_resource_email_invites_resource
  on resource_email_invites (workspace_id, resource_type, resource_id);

create unique index idx_resource_email_invites_token_hash
  on resource_email_invites (token_hash);

create unique index resource_email_invites_pending_resource_email_key
  on resource_email_invites (workspace_id, resource_type, resource_id, email)
  where status = 'pending';

create index idx_resource_email_invites_pending_expiry
  on resource_email_invites (expires_at)
  where status = 'pending';

create index idx_resource_email_invites_delivery_status_created
  on resource_email_invites (delivery_status, created_at);
```

Rules:

- invite tokens are high entropy, URL-safe, and stored only as hashes.
- raw invite tokens are returned only in create responses.
- list, resolve, accept, and audit responses never expose token hashes or raw
  tokens.
- `email` is normalized to lowercase and compared against the authenticated
  user's normalized email at resolve/accept/authorization time.
- pending invites have required future `expires_at`; past create values return
  `400 VALIDATION_ERROR`.
- duplicate pending invites for the same workspace/resource/email return stable
  `409 CONFLICT`.
- accepted invites authorize only the bound resource path and only while
  unexpired, non-revoked, and gated by `link_mode = external`.
- accepted invites do not create workspace membership and do not broaden
  bootstrap/map/search/export/list filtering.
- delivery status is observable as `disabled`, `sent`, or `failed`; default
  configuration is disabled/noop and does not send external mail.
- `Permissions:EmailInvites:Delivery:Provider = smtp` enables the SMTP
  provider boundary when delivery is enabled and SMTP configuration is present.
- SMTP provider configuration includes `FromEmail`, optional `FromName`,
  `Smtp:Host`, `Smtp:Port`, `Smtp:UseSsl`, optional `Smtp:Username`, optional
  `Smtp:Password`, and `Smtp:TimeoutSeconds`.
- incomplete SMTP configuration fails closed with `deliveryStatus = failed`
  and a non-secret `configuration_error` code.
- unsupported configured providers fail closed with `deliveryStatus = failed`
  and a non-secret `unsupported_provider` code unless tests or future provider
  registrations explicitly replace the delivery service.
- delivery metadata must not contain raw tokens, token hashes, SMTP secrets, or
  provider credentials.

### access_requests

```sql
create table access_requests (
  id uuid primary key,
  workspace_id uuid not null references workspaces(id) on delete cascade,
  resource_type text not null check (resource_type in ('collection', 'document')),
  resource_id uuid not null,
  requester_id uuid not null references users(id),
  subject_type text not null check (subject_type in ('user', 'group')),
  subject_id uuid not null,
  requested_role text not null check (requested_role in ('owner', 'admin', 'editor', 'commenter', 'viewer')),
  reason text,
  status text not null check (status in ('pending', 'approved', 'denied', 'cancelled')),
  decided_by uuid references users(id),
  decided_at timestamptz,
  decision_reason text,
  resulting_grant_id uuid references resource_access_grants(id),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create unique index access_requests_pending_subject_key
  on access_requests (workspace_id, resource_type, resource_id, subject_type, subject_id)
  where status = 'pending';
```

Rules:

- Phase 5 public API supports `subject_type = user` for the current requester;
  group requests are schema-reserved and not exposed in UI.
- duplicate pending requests for the same workspace/resource/subject are
  rejected by the active pending unique index.
- normal members cannot request `admin` or `owner`; admin/owner requests are
  reserved for explicit admin/owner initiated flows and still require reviewer
  authority.
- approval creates or upgrades a grant and writes audit + notifications in one
  transaction.
- `POST /api/v1/permissions/access-requests/{requestId}/review` accepts
  `expiresAt?: string | null`. When approving, a future value creates or
  upgrades the resulting direct user grant as temporary access. `null` means the
  resulting grant is permanent.
- review approval rejects past `expiresAt` with `400 VALIDATION_ERROR`.
- the reviewer cannot approve a role above their own effective role for the
  resource, and cannot update an existing grant whose current role exceeds their
  own effective role.
- natural grant expiry does not change the access request status; an approved
  request remains `approved` even if the resulting grant later expires.
- denial and cancellation write audit but create no grant.

### permission_notifications

```sql
create table permission_notifications (
  id uuid primary key,
  workspace_id uuid not null references workspaces(id) on delete cascade,
  recipient_user_id uuid not null references users(id) on delete cascade,
  actor_user_id uuid references users(id),
  type text not null check (type in (
    'access_request.created',
    'access_request.approved',
    'access_request.denied',
    'permission.grant_created',
    'permission.grant_updated',
    'permission.grant_revoked',
    'permission.grant_expiring',
    'permission.grant_expired',
    'group.member_added',
    'group.member_removed',
    'group.member_expiring',
    'group.member_expired'
  )),
  resource_type text check (resource_type in ('workspace', 'collection', 'document')),
  resource_id uuid,
  access_request_id uuid references access_requests(id),
  permission_grant_id uuid references resource_access_grants(id),
  title text not null,
  body text,
  action_url text,
  dedupe_key text,
  read_at timestamptz,
  created_at timestamptz not null default now()
);

create unique index permission_notifications_dedupe_key
  on permission_notifications (dedupe_key)
  where dedupe_key is not null;
```

Rules:

- notification rows are addressed to one recipient user.
- notification preference persistence exists separately in
  `permission_notification_preferences`; share-link and email-invite fan-out
  suppresses recipients with matching muted workspace/resource preferences.
- notification reads are current-user scoped. Marking a notification read does
  not write permission audit events.
- `dedupe_key` is internal and nullable. It is required for background expiry
  notifications and share-link/invite fan-out and must be stable per
  `(type, source row, recipient)`.
- the filtered unique index on `dedupe_key` is the idempotency guard for expiry
  scans and concurrent scanner overlap.

### permission_audit_events

```sql
create table permission_audit_events (
  id uuid primary key,
  workspace_id uuid not null references workspaces(id) on delete cascade,
  actor_id uuid references users(id),
  action text not null,
  resource_type text not null,
  resource_id uuid not null,
  subject_type text,
  subject_id uuid,
  before_json jsonb,
  after_json jsonb,
  metadata jsonb not null default '{}',
  created_at timestamptz not null default now()
);
```

Rules:

- audit events are append-only.
- no update or delete endpoint should exist for audit rows.
- user-facing `activity_events` can summarize permission audit events, but
  cannot replace the audit log.

## Required Indexes

Minimum indexes:

```sql
create index idx_grants_workspace_resource
  on resource_access_grants (workspace_id, resource_type, resource_id);

create index idx_grants_subject
  on resource_access_grants (workspace_id, subject_type, subject_id);

create index idx_grants_expiry
  on resource_access_grants (expires_at)
  where revoked_at is null;

create index idx_policies_resource
  on resource_access_policies (resource_type, resource_id);

create index idx_share_links_token_hash
  on share_links (token_hash);

create index idx_share_links_public_document_active
  on share_links (workspace_id, resource_id, expires_at)
  where audience = 'public' and revoked_at is null;

create index idx_resource_email_invites_delivery_status_created
  on resource_email_invites (delivery_status, created_at);

create index idx_access_requests_workspace_status
  on access_requests (workspace_id, status, created_at desc);

create index idx_access_requests_resource_status
  on access_requests (resource_type, resource_id, status);

create index idx_access_requests_requester_status
  on access_requests (requester_id, status);

create index idx_permission_audit_workspace_created
  on permission_audit_events (workspace_id, created_at desc);

create index idx_permission_audit_resource_created
  on permission_audit_events (resource_type, resource_id, created_at desc);

create index idx_permission_notifications_recipient_read_created
  on permission_notifications (recipient_user_id, read_at, created_at desc);

create index idx_permission_notifications_workspace_created
  on permission_notifications (workspace_id, created_at desc);

create index idx_permission_notifications_access_request
  on permission_notifications (access_request_id);

create unique index permission_notifications_dedupe_key
  on permission_notifications (dedupe_key)
  where dedupe_key is not null;
```

## Backend Services

Implement permissions through services, not controller-local logic.

Required services:

- `PermissionCatalogService`
  - loads roles, permissions, role mappings;
  - exposes stable role ranking.
- `EffectivePermissionService`
  - computes effective role and authorizes action keys.
- `ResourceAccessPolicyService`
  - reads and updates inheritance/link policy.
- `ResourceAccessGrantService`
  - grants, updates, expires, and revokes scoped access.
- `GroupService`
  - manages static/dynamic groups and group membership.
- `IamSyncService`
  - syncs provider-neutral external users, IAM-managed groups, and managed
    group memberships for a workspace.
- `AccessRequestService`
  - creates, approves, denies, and cancels access requests.
- `ShareLinkService`
  - creates, validates, expires, and revokes share links;
  - enforces default-off public document link creation rules and anonymous
    public resolve/document reads.
- `EmailInviteService`
  - creates, resolves, accepts, expires, and revokes resource email invites.
- `IEmailInviteDeliveryService`
  - receives the one-time accept URL during invite creation and returns
    delivery status without persisting raw token material.
- `PermissionAuditService`
  - writes immutable audit events.
- `PermissionNotificationService`
  - emits notification/activity records for access changes.
- `PermissionNotificationFanoutService`
  - expands share-link and email-invite notification recipients, applies muted
    notification preferences, and writes in-app notifications without delivery
    transport.
- `PermissionExpiryNotificationHostedService`
  - scans temporary grants and group memberships, emits expiring/expired
    notifications idempotently, and writes no permission audit events.

Existing `WorkspaceAccessService` should remain valid for workspace-only
checks, then delegate collection/document checks to `EffectivePermissionService`
when scoped permissions are introduced.

## API Surface

Current workspace member endpoints remain:

```text
GET    /api/v1/workspaces/{workspaceId}/members
POST   /api/v1/workspaces/{workspaceId}/members
PATCH  /api/v1/workspaces/{workspaceId}/members/{userId}
DELETE /api/v1/workspaces/{workspaceId}/members/{userId}
```

Effective permissions:

```text
GET /api/v1/permissions/effective?resourceType=&resourceId=
```

Resource policies and grants:

```text
GET    /api/v1/permissions/resources/{resourceType}/{resourceId}
PATCH  /api/v1/permissions/resources/{resourceType}/{resourceId}/policy
POST   /api/v1/permissions/resources/{resourceType}/{resourceId}/grants
PATCH  /api/v1/permissions/resources/{resourceType}/{resourceId}/grants/{grantId}
DELETE /api/v1/permissions/resources/{resourceType}/{resourceId}/grants/{grantId}
```

The current implementation supports `resourceType` values `collection` and
`document`, direct `user` and `group` grants, `inherit`/`restricted` policies,
and `linkMode = disabled | internal | external`. `public` link mode remains
rejected.
`POST .../grants` and `PATCH .../grants/{grantId}` accept
`expiresAt?: string | null`; past values return `400 VALIDATION_ERROR`, future
values create temporary access, omitted PATCH fields are unchanged, and
explicit `null` means permanent access.

Groups:

```text
GET    /api/v1/workspaces/{workspaceId}/groups
POST   /api/v1/workspaces/{workspaceId}/groups
GET    /api/v1/workspaces/{workspaceId}/groups/{groupId}
PATCH  /api/v1/workspaces/{workspaceId}/groups/{groupId}
DELETE /api/v1/workspaces/{workspaceId}/groups/{groupId}
POST   /api/v1/workspaces/{workspaceId}/groups/{groupId}/members
DELETE /api/v1/workspaces/{workspaceId}/groups/{groupId}/members/{userId}
```

`WorkspaceGroupDto` includes IAM source metadata:

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

IAM sync:

```text
POST /api/v1/workspaces/{workspaceId}/iam/sync
```

The endpoint is owner/admin only through `workspace.manage_members`.

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

Rules:

- `provider` is normalized to lowercase.
- `workspaceRole` defaults to `viewer`; `owner` cannot be assigned by IAM sync.
- optional payload `workspaceId` values must match the route workspace id or
  the request returns `400 VALIDATION_ERROR`.
- group member entries reference user `externalSubjectId` values in the same
  payload.
- repeated identical payloads do not create duplicate users, groups, workspace
  members, or group members.
- missing group members in a synced group are soft-removed; local users and
  local groups are not deleted.
- normal group update/archive/member APIs reject IAM-managed groups.

SCIM provisioning V1:

```text
GET /api/v1/workspaces/{workspaceId}/scim/v2/Users
GET /api/v1/workspaces/{workspaceId}/scim/v2/Users/{userId}
POST /api/v1/workspaces/{workspaceId}/scim/v2/Users
PATCH /api/v1/workspaces/{workspaceId}/scim/v2/Users/{userId}
PUT /api/v1/workspaces/{workspaceId}/scim/v2/Users/{userId}
GET /api/v1/workspaces/{workspaceId}/scim/v2/Groups
GET /api/v1/workspaces/{workspaceId}/scim/v2/Groups/{groupId}
POST /api/v1/workspaces/{workspaceId}/scim/v2/Groups
PATCH /api/v1/workspaces/{workspaceId}/scim/v2/Groups/{groupId}
PUT /api/v1/workspaces/{workspaceId}/scim/v2/Groups/{groupId}
```

Rules:

- SCIM provisioning requires a valid dedicated workspace-scoped SCIM bearer
  token for the route workspace.
- SCIM uses `external_provider = scim` plus `external_subject_id` for users and
  `external_group_id` for workspace groups.
- SCIM user create maps by SCIM external id, then may bind an existing local
  email user only when no conflicting external identity exists.
- SCIM-created workspace memberships are viewer-only; owner/admin memberships
  are not created by SCIM.
- SCIM user patch and restricted PUT replace support narrow profile updates,
  preserve the existing SCIM `externalId`, and do not create credentials or
  store passwords/secrets.
- SCIM group create/patch and restricted PUT replace can sync SCIM-managed
  group display name and members; local non-SCIM-managed groups are not mutable
  through SCIM.
- SCIM group member values reference SCIM-managed user ids in the same
  workspace.
- SCIM list filters intentionally support only `userName eq` and
  `externalId eq` for Users, and `displayName eq` and `externalId eq` for
  Groups. Unsupported filters return stable validation errors.
- `startIndex` is normalized to at least `1`; `count` is clamped to the current
  SCIM page-size maximum.
- SCIM delete/deactivate, bulk operations, complex filter grammar, enterprise
  extension, and broader compatibility beyond V1.1 remain deferred.

Share links:

```text
GET    /api/v1/permissions/resources/{resourceType}/{resourceId}/share-links
POST   /api/v1/permissions/resources/{resourceType}/{resourceId}/share-links
DELETE /api/v1/permissions/share-links/{shareLinkId}
GET    /api/v1/share-links/{token}/resolve
GET    /api/v1/public/share-links/{token}/resolve
GET    /api/v1/public/share-links/{token}/document
GET    /api/v1/public/share-links/{token}/collection
```

`CreateShareLinkRequest` is:

```ts
type CreateShareLinkRequest = {
  roleKey: "viewer" | "commenter";
  audience?: "workspace" | "external" | "public";
  expiresAt?: string | null;
  subjectEmail?: string | null;
  password?: string | null;
};
```

`audience = "public"` returns `400 VALIDATION_ERROR` unless
`Permissions:PublicShareLinks:Enabled = true`. When enabled, public creation is
document-or-collection only, viewer-only, requires future `expiresAt`, rejects
`subjectEmail`, and enforces the configured max expiry. Public collection links
return only minimal listing payloads and exclude restricted, archived, and
deleted child documents. `password` is supported only for public links, accepted
once at create time, and stored only as `password_hash`. `audience = "external"`
requires `subjectEmail`.

`CreateShareLinkResponse` returns the raw token only once:

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
};

type CreateShareLinkResponse = {
  link: ShareLinkDto;
  token: string;
  url: string;
};

type ShareLinksResponse = {
  links: ShareLinkDto[];
};
```

List responses never include raw tokens or token hashes.

`ResolveShareLinkResponse` is:

```ts
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

Share-link resolution has no public API side effects and does not create
notifications.

Public anonymous resolve/read responses are:

```ts
type ResolvePublicShareLinkResponse = {
  workspaceId: string;
  resourceType: "document" | "collection";
  resourceId: string;
  roleKey: "viewer";
  audience: "public";
  expiresAt: string;
  hasPassword: boolean;
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
```

Public anonymous endpoints are `[AllowAnonymous]`, endpoint-rate-limited, and
return stable `404 NOT_FOUND` for revoked, expired, unknown, missing-policy, or
wrong-linkMode tokens. If `hasPassword = true`, resolve/read requires
`X-Share-Link-Password`; missing or wrong proof returns the same stable
`404 NOT_FOUND`.

Email invites:

```text
GET    /api/v1/permissions/resources/{resourceType}/{resourceId}/email-invites
POST   /api/v1/permissions/resources/{resourceType}/{resourceId}/email-invites
DELETE /api/v1/permissions/email-invites/{inviteId}
GET    /api/v1/permissions/email-invites/{token}/resolve
POST   /api/v1/permissions/email-invites/{token}/accept
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

type EmailInviteDeliveryDto = {
  status: "disabled" | "sent" | "failed";
  provider: string;
  attemptedAt: string | null;
  errorCode: string | null;
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

Create/list/revoke require resource share permission. Resolve and accept
require an authenticated user whose normalized email matches the invite email.

Access requests:

```text
POST /api/v1/permissions/access-requests
GET  /api/v1/permissions/access-requests?workspaceId=&status=
GET  /api/v1/permissions/resources/{resourceType}/{resourceId}/access-requests
POST /api/v1/permissions/access-requests/{requestId}/review
POST /api/v1/permissions/access-requests/{requestId}/cancel
```

`ReviewAccessRequestRequest` is:

```ts
type ReviewAccessRequestRequest = {
  decision: "approve" | "deny";
  roleKey?: "owner" | "admin" | "editor" | "commenter" | "viewer" | null;
  reason?: string | null;
  expiresAt?: string | null;
};
```

When `decision = "approve"`, `expiresAt` applies to the resulting direct user
grant. When `decision = "deny"`, `expiresAt` is ignored.

Notifications:

```text
GET   /api/v1/notifications?workspaceId=&unreadOnly=
PATCH /api/v1/notifications/{notificationId}/read
PATCH /api/v1/notifications/read-all
```

The permission expiry notification job has no public API. It is configured under
`Permissions:ExpiryNotifications`.

Audit:

```text
GET /api/v1/permissions/audit?workspaceId=&resourceType=&resourceId=
```

Protected endpoints above must use the central effective authorization service.
The public anonymous endpoints must use the dedicated public share-link
validation path instead.

## UI Rules

### Document Share and Permissions

The document permissions UI must show:

- document context;
- inherited workspace access;
- direct user grants;
- group grants;
- effective access source;
- expiration date when present;
- share link status when supported;
- access history.

Required copy:

- if inheriting: "Access is inherited from workspace or collection roles."
- if restricted: "Only explicitly granted users, groups, owners, and admins can
  access this document."
- if capability is not backend-backed: label it as deferred or unavailable.

The UI must not:

- show "Anyone with the link" as active unless the public share feature flag is
  known enabled and a backend-created public link exists;
- allow role mutation without a backend mutation;
- imply document-specific ACL exists before grants are persisted;
- hide the inheritance source from admins.

The Phase 7/9 Share & Permissions UI supports minimal link and invite
workflows:

- list active internal workspace-member links without showing raw tokens;
- list active link metadata even when `linkMode = disabled`; disabled link mode
  means those links cannot authorize access until re-enabled;
- create `viewer` or `commenter` internal links;
- create `viewer` or `commenter` external authenticated links for a normalized
  email address;
- display the generated URL and raw token only immediately after create;
- show link expiry when present;
- revoke an existing link;
- create email invites with recipient email, role, and expiry;
- list invite status, delivery status, and revoke invites;
- show public anonymous link controls as disabled/read-only when the feature
  flag is off;
- when the feature flag is on, backend contract supports public document and
  collection viewer links with required expiry and optional password; until full
  frontend interaction is implemented, public controls may remain disabled or
  read-only but must not expose commenter/editor/admin/owner or subject email
  options.

The Phase 8 Share & Permissions Groups view shows IAM-managed group source
metadata:

- display `externalProvider` and treat `externalGroupId` as metadata, not a raw
  credential;
- disable edit, archive, and member add/remove controls for IAM-managed groups;
- continue to display IAM-managed groups as possible grant subjects when group
  grants already exist;
- keep full grant/policy mutation UI deferred until a backend-backed workflow is
  implemented.

Production invite delivery provider UI, bulk link management, and
MFA/recent-auth management prompts remain deferred.

### Collection Permissions

The collection permissions UI must show:

- inherited workspace access;
- collection grants;
- affected document count;
- document overrides;
- warning before switching to restricted mode.

Text must explain whether new documents inherit collection policy.

### Workspace Permissions

The workspace permissions UI must show:

- workspace members;
- workspace role;
- groups;
- external/IAM-managed group state;
- pending invites/access requests;
- owner protection warning.

Admins can manage members. Editors cannot.

### Access Requests

Restricted resources must offer `Request access` when the user is authenticated
but lacks access.

The Share & Permissions UI provides a minimal request flow:

- request defaults to `viewer`;
- successful request submission displays a pending state;
- duplicate pending responses display an already-pending state;
- failures display a short error and do not break the page.

Admins, owners, and resource managers with `manage_permissions` should see
requests in Share & Permissions with:

- requester;
- resource;
- requested role;
- message;
- approve using the requested role;
- deny with an empty or optional reason.

Approval creates a grant, notification, and audit event in one transaction.
The current UI does not include bulk approve or full grant mutation workflows.

Updates must open a notification's `actionUrl` when present. Direct
approve/deny from Updates remains deferred.

### Warnings

The UI must warn before:

- restricting inherited access;
- revoking the user's own management path;
- expiring access for active collaborators;
- deleting or disabling a share link;
- removing a group grant that affects multiple users;
- changing a role that affects many resources.

## Audit Requirements

Audit these actions:

- workspace role created/updated/removed;
- scoped grant created/updated/revoked;
- group created/updated/archived;
- group member added/removed;
- policy inheritance changed;
- share link created/revoked;
- email invite created/accepted/revoked/expired when the system explicitly
  transitions the invite row to `expired`;
- IAM user mapped;
- IAM-managed group synced;
- IAM-managed group member added/removed by sync;
- access request created/approved/denied/cancelled;
- failed management attempts when useful for security monitoring.

Natural expiry of grants, group memberships, and share links is not a permission
mutation and does not write revoke/expired audit events. The Phase 6 expiry
notification job writes only notifications for grants and group memberships.

Audit event requirements:

- actor id;
- workspace id;
- resource type and id;
- subject type and id when relevant;
- share link id, role key, audience, and `expiresAt` when the action is
  `share_link.created` or `share_link.revoked`;
- email invite id, normalized email, role key, status, and `expiresAt` when the
  action is `email_invite.created`, `email_invite.accepted`,
  `email_invite.revoked`, or `email_invite.expired`;
- email invite delivery status, provider key, attempted timestamp, and sanitized
  error code when invite creation attempts delivery;
- external provider, external subject/group id, group id, and member user id
  when the action is an IAM sync action;
- before/after JSON for changed fields;
- timestamp;
- request metadata when available, such as IP/user agent;
- machine-readable action key.

Audit data must be readable in the Activity Panel, but the immutable security
log remains separate from friendly activity summaries.

Raw share link tokens, raw email invite tokens, accept URLs, token hashes, SMTP
secrets, and provider credentials must never be written to permission audit
metadata.
IAM sync audit metadata must not include tokens, secrets, passwords, SCIM bearer
tokens, SAML assertions, OIDC tokens, or raw external credentials.

## Notifications

Permission notifications are generated for:

- access request created;
- access request approved/denied;
- direct user access granted/updated/revoked;
- direct user grant expiring soon;
- direct user grant expired;
- workspace group member added/removed;
- workspace group member expiring soon;
- workspace group member expired;
- share link created/revoked;
- email invite created/accepted/revoked;
- email invite delivery failed.

Notification type keys:

```text
access_request.created
access_request.approved
access_request.denied
permission.grant_created
permission.grant_updated
permission.grant_revoked
permission.grant_expiring
permission.grant_expired
group.member_added
group.member_removed
group.member_expiring
group.member_expired
share_link.created
share_link.revoked
email_invite.created
email_invite.accepted
email_invite.revoked
email_invite.delivery_failed
```

Recipients:

- `access_request.created`: workspace owners/admins, direct user grant
  subjects, and active members of group grants with resource
  `manage_permissions`.
- `access_request.approved` / `access_request.denied`: requester.
- `permission.grant_created` / `permission.grant_updated` /
  `permission.grant_revoked`: direct user grant subject, or active members of
  the granted group for group-subject grants.
- `permission.grant_expiring` / `permission.grant_expired`: direct user grant
  subject.
- `group.member_added` / `group.member_removed` /
  `group.member_expiring` / `group.member_expired`: member user.
- `share_link.created` / `share_link.revoked`: resource managers and direct
  user grants with resource `share` or `manage_permissions`, excluding the
  actor.
- `email_invite.created` / `email_invite.delivery_failed`: resource managers
  and direct user grants with resource `share` or `manage_permissions`,
  excluding the actor.
- `email_invite.accepted`: resource managers, direct user grants with resource
  `share` or `manage_permissions`, and the inviter when known, excluding the
  actor.
- `email_invite.revoked`: resource managers, direct user grants with resource
  `share` or `manage_permissions`, the inviter when known, and the accepted
  invitee when known, excluding the actor.

Expiry notification rules:

- default expiring window is 24 hours and configurable;
- expired records that have not emitted an expired notification are notified;
- notifications are idempotent through `permission_notifications.dedupe_key`;
- the job does not revoke grants, remove members, or write permission audit.

Still not generated:

- share-link/invite expiry notifications.

Phase 8 IAM sync does not generate user-facing notifications for synced user,
group, or member changes. IAM sync is audit-only in this phase.

Phase 13 generates in-app notifications for share-link and invite create/revoke,
invite accept, and invite delivery failure. It does not implement delivery
transport.

The group grant fan-out slice generates in-app notifications for group-subject
grant create/update/revoke and expands access-request manager notifications
through active group grants with `manage_permissions`.

Notification preferences are persisted by `permission_notification_preferences`
and exposed through `/api/v1/notifications/preferences`. Share-link, invite,
and group grant fan-out suppress matching muted workspace/resource preferences.

## Security Requirements

Authentication:

- sensitive management actions require an authenticated user.
- recent-auth state must come from backend-authored data, not client-supplied
  flags.
- MFA provider/enrollment and step-up enforcement remain deferred; high-risk
  actions must not rely on frontend-only flags.

Authorization:

- all protected read/write endpoints must call the effective permission service.
- list queries must filter inaccessible resources at the database/query layer,
  not after returning full result sets to the client.
- search indexes must not leak inaccessible document titles or excerpts.
- exports must include only resources the actor can access.

Integrity:

- permission mutation and audit write use one transaction.
- stale concurrent updates should use revision or timestamp checks where
  practical.
- all resource ids must be verified inside the same workspace.

Share links:

- raw token is shown only once at creation.
- store hash only.
- revoke by setting `revoked_at`.
- internal links require authenticated active workspace membership and
  `linkMode = internal`.
- external authenticated links require an authenticated matching email and
  `linkMode = external`.
- public anonymous document and collection links require the default-off feature
  flag, future bounded expiry, viewer-only role, no subject email,
  `linkMode = public`, and dedicated rate-limited anonymous endpoints.
- public collection links return only minimal listing metadata; document
  policies with `inheritance_mode = restricted`, archived documents, and
  deleted documents block child listing inheritance.
- public link passwords require `X-Share-Link-Password` proof when configured;
  missing, wrong, revoked, expired, unknown, or policy-mismatched tokens return
  stable failures.
- public tokens must not authorize protected share-token paths or bulk
  surfaces such as bootstrap/map/search/export/list.

Email invites:

- raw invite token is shown only once at creation.
- store hash only.
- accept requires authenticated matching email and pending, unexpired status.
- accepted invite access requires `linkMode = external`.
- accepted invites do not create workspace membership or list/search/export
  visibility.
- invite delivery uses a backend abstraction with default disabled/noop
  behavior and a configuration-driven SMTP provider boundary; delivery status
  is persisted but raw tokens, URLs, token hashes, and provider secrets are not
  persisted or audited.

IAM sync:

- sync entrypoints require owner/admin workspace management authority.
- IAM-managed group metadata and memberships can be changed only by IAM sync.
- sync payloads must not include provider tokens, client secrets, SAML
  assertions, OIDC tokens, or raw credentials.
- optional payload workspace ids must match the route workspace id.
- sync does not automatically delete users or local groups.

Ownership:

- at least one active owner must remain.
- an owner cannot accidentally remove their own last owner access.
- ownership transfer should be explicit.

Data retention:

- revoked grants and audit events are retained.
- hard delete requires separate data retention policy approval.

## Third-Party IAM and SSO

Phase 8 implements provider-neutral sync foundations:

- SSO user identity mapping;
- IAM/IdP group mapping;
- SCIM-style sync writes through an internal owner/admin endpoint.

Implemented fields:

- `users.external_provider`
- `users.external_subject_id`
- `workspace_groups.external_provider`
- `workspace_groups.external_group_id`
- `workspace_groups.external_synced_at`

IdP login boundary:

- `POST /api/v1/auth/idp/login` is an unauthenticated login boundary that is
  default-disabled through configuration and must only be enabled behind a
  trusted internal/test assertion source.
- The request accepts `provider`, `externalSubjectId`, `email`, and
  `displayName`; provider normalization follows IAM sync (`trim` +
  lowercase), and external subject ids are trimmed.
- A matching persisted external identity can issue normal Northstar auth
  tokens.
- If no matching external identity exists, an existing local user with the same
  normalized email may be bound only when that user has no conflicting external
  identity.
- Missing users are rejected; IdP login does not create users or workspace
  memberships implicitly.
- Auth events record only secret-safe metadata such as provider and failure
  reason; provider secrets, SAML/OIDC tokens, raw credentials, and external
  assertions are not persisted.
- Full browser redirect/callback UX, OIDC/SAML provider validation, connector
  configuration, and secret storage remain deferred.

IAM-managed groups:

- are read-only through normal Northstar group APIs and UI controls;
- can be assigned resource grants;
- reuse the existing group effective permission path;
- record sync changes in audit logs;
- show their external source in UI.

Deferred IAM/SSO capabilities:

- full IdP login UI or SSO authentication handshake;
- SCIM bulk, complex filter grammar, enterprise extension, delete/deactivate,
  and broader compatibility beyond V1.1;
- secret/token storage for IdP connectors;
- SSO/IAM-driven workspace owner assignment;
- SCIM endpoint behavior remains unrelated to Phase 10 public document share
  links.

## Testing Gates

No permission feature is complete without tests.

Unit tests:

- role rank mapping;
- role permission catalog;
- effective permission algorithm;
- expired grant ignored;
- revoked grant ignored;
- restricted mode blocks inherited viewer/editor;
- owner/admin escape path behavior;
- cannot grant above actor authority;
- last owner protection.

Integration tests:

- every protected endpoint denies unauthenticated access;
- every protected endpoint denies insufficient role;
- document list/search/export do not leak inaccessible resources;
- grant mutation writes audit event;
- access request approval writes grant + audit + notification;
- expired direct grants do not authorize, future direct grants do authorize,
  and revoked grants do not authorize;
- expired group memberships do not authorize group grants;
- past `expiresAt` on grant create/update, group member add, and access request
  review approval returns `400 VALIDATION_ERROR`;
- access request review with `expiresAt` creates or upgrades a temporary grant;
- expiry notification scans emit expiring/expired notifications idempotently and
  write no permission audit;
- invalid `PATCH /api/v1/notifications/read-all` `workspaceId` returns
  `400 VALIDATION_ERROR`;
- duplicate pending access request database conflicts map to `409 CONFLICT`;
- bootstrap/map/search/export do not leak resources whose only access is
  expired;
- expired non-revoked grants can be renewed and authorize again;
- grant PATCH distinguishes omitted `expiresAt` from explicit `null`;
- internal viewer/commenter share links can be created;
- raw share link tokens are returned only by create and are never returned by
  list;
- invalid share link role, audience, and past expiry are rejected;
- revoked, expired, and unknown share link tokens cannot resolve;
- outsiders cannot resolve internal links, while active workspace members can;
- viewer links allow document view but not comment/edit;
- commenter links allow comment but not edit;
- document share links stop authorizing read/comment when the document policy is
  changed to `linkMode = disabled`, and authorize again if changed back to
  `internal` before expiry/revoke;
- collection share links stop authorizing child document read/comment when the
  collection policy is changed to `linkMode = disabled`, and authorize again if
  changed back to `internal` before expiry/revoke;
- active share links without an internal resource policy do not authorize;
- share link create/revoke writes audit without storing raw tokens;
- feature flag off rejects `audience = public` with `400 VALIDATION_ERROR`;
- feature flag on allows public document viewer links with future bounded
  expiry;
- public link missing expiry, overlong expiry, commenter role, subjectEmail, and
  non-public password requests return `400 VALIDATION_ERROR`;
- public token anonymously reads only the dedicated single-document payload;
- public collection token anonymously reads only the dedicated minimal
  collection/listing payload and excludes restricted, archived, and deleted
  child documents;
- public token cannot comment/edit/export/search/list/map/bootstrap or enter
  authenticated share-token paths;
- public token is rejected when policy is missing, disabled, internal, external,
  revoked, expired, or unknown;
- raw public tokens never enter DB, audit, or list responses;
- public link passwords require missing/wrong/correct proof coverage and store
  only password hashes, never plaintext password material in DB, audit, or list
  responses;
- external authenticated links require matching email, `audience = external`,
  active token state, and `linkMode = external`;
- external links cannot be used by the wrong authenticated user and cannot be
  used without a token;
- external links do not authorize search/export/list/map/bootstrap results;
- revoked, expired, and unknown external tokens reject access without exposing
  resource existence;
- collection external links do not expand access to restricted documents;
- raw external share-link tokens are returned only on create and are never
  stored in database or audit;
- IAM sync creates external user mappings, workspace memberships,
  IAM-managed groups, and group memberships on first run;
- IAM sync is idempotent when repeated with the same payload;
- IAM sync soft-removes managed group members missing from the payload without
  deleting local users or local groups;
- normal group update/archive/member mutation APIs reject IAM-managed groups;
- IAM-managed group grants authorize document/collection access through the
  existing group grant path;
- non-owner/admin users cannot call IAM sync;
- payload workspace ids that cross the route workspace are rejected with
  `400 VALIDATION_ERROR`;
- IAM sync audit events are written and do not contain token/secret material.
- SCIM discovery endpoints require workspace owner/admin authority or a valid
  dedicated SCIM bearer token and return explicit shapes without exposing
  secrets.
- SCIM User/Group provisioning endpoints require valid dedicated SCIM bearer
  tokens for the route workspace.
- SCIM User/Group list/get/create/patch/restricted PUT replace behavior is
  tested for identity mapping, viewer-only membership creation, local group
  mutation rejection, narrow filters, bounded pagination, and no raw-token or
  token-hash exposure.
- email invite create returns a one-time token and list/resolve/accept/audit do
  not expose raw token or token hash;
- duplicate pending email invite returns `409 CONFLICT`;
- invite accept rejects authenticated users whose normalized email does not
  match the invite;
- accepted invites authorize only the bound resource and do not broaden
  search/export/list/map/bootstrap results;
- revoked and expired invites do not authorize;
- invite delivery fake sender receives an accept URL, while DB/audit do not
  contain raw invite tokens or token hashes;
- invite delivery fake failure persists failed status/provider/error metadata
  without raw invite tokens, token hashes, accept URLs, or provider secrets;
- recent-auth/MFA and share-link/invite notification fan-out are either
  implemented with backend-backed tests or explicitly documented as deferred;
- resource share permission is required for external link and email invite
  mutations;

UI tests:

- restricted content shows request access state;
- Share UI displays inherited/direct/group source;
- Share UI request access and approve/deny failures do not break the page;
- Updates follows notification `actionUrl` when present;
- Share UI can create/list/revoke internal links and does not persist raw tokens
  after the one-time create result;
- Share UI can create external authenticated links and email invites, display
  generated URLs/tokens only once, list metadata, and revoke existing rows;
- Share UI keeps public anonymous link controls disabled/read-only when the
  feature flag is off and does not expose MFA/recent-auth controls as
  functional;
- Share UI displays IAM-managed group provider/source metadata and keeps edit,
  archive, and member mutation controls disabled;
- deferred controls do not claim persistence;
- warnings appear before destructive permission changes;
- Updates shows access request notifications.

Migration tests:

- existing workspace members keep current behavior after permission tables are
  introduced;
- default policies are inherit/disabled;
- existing documents remain accessible to current workspace viewers;
- the share link migration defines the `share_links` table, constraints, and
  indexes.
- the IAM sync migration defines user/group external fields and filtered unique
  indexes.
- the Phase 9 migration defines external link fields, check constraints, invite
  table, token hash indexes, and duplicate pending invite index.
- the Phase 10 migration defines invite delivery fields, delivery status
  constraint/index, public share-link viewer/expiry constraint, and public
  active-link index.
- the Phase 11 migration defines public collection-safe share-link constraints,
  public password hash storage, a password/public check constraint, and the
  public active-link index over resource type.

## Rollout Plan

Phase 1: stabilize current workspace RBAC.

- Keep existing `workspace_members`.
- Introduce permission catalog constants/tests. Implemented for role keys,
  ranks, permission action keys, role permissions, and grant hierarchy.
- Add central effective permission service for workspace-only checks.
  Implemented as workspace-only authorization backed by active
  `workspace_members`.
- Keep `commenter` deferred for scoped document/collection collaboration. It is
  not a valid `workspace_members.role`.

Phase 2: add policies and grants.

- Add `resource_access_policies`. Implemented for collection/document
  resources.
- Add `resource_access_grants`. Implemented for direct user grants; group
  subjects remain deferred to Phase 4.
- Default every resource to `inherit`. Implemented when no explicit policy row
  exists.
- Add read-only effective permission endpoint. Implemented at
  `GET /api/v1/permissions/effective`.
- At Phase 2 completion, grant mutation APIs, permission audit writes, groups,
  share links, access requests, temporary access notifications, and
  frontend-backed permission management were still deferred to later phases.

Phase 3: enable document/collection permission management.

- Implemented grant mutation APIs for direct `user` scoped grants on
  `collection` and `document` resources.
- Implemented policy mutation for `inherit`/`restricted`; `internal` and
  `public` link modes still return validation errors.
- Implemented `permission_audit_events` and same-transaction audit writes for
  policy update, grant create, grant update, and grant revoke.
- Implemented read-only audit access at
  `GET /api/v1/permissions/audit?workspaceId=&resourceType=&resourceId=`.
- Implemented collection-to-document inheritance, direct document grant
  precedence, restricted policy behavior, and owner/admin escape paths.
- Implemented scoped enforcement for key document content paths and filtering
  for search/export/map/bootstrap outputs.
- Added minimal Share & Permissions read integration for configured document
  permissions. Public links, groups, access requests, and mutation UI remain
  disabled/deferred.

Phase 4: add groups.

- Implemented `workspace_groups` and `workspace_group_members` with soft
  archive/remove semantics.
- Implemented group list/detail/create/update/archive and add/remove member
  APIs under `/api/v1/workspaces/{workspaceId}/groups`.
- Implemented `resource_access_grants.subject_type = group` and active-grant
  uniqueness, including re-grant after revoke.
- Implemented group grants in effective permission for collections and
  documents, including `collection_group` and `document_group` sources.
- Implemented same-transaction audit writes for group create/update/archive,
  member add/remove, and group grant create/update/revoke.
- Added minimal Share & Permissions Groups tab read integration for workspace
  groups and resource group grants. Public links, email invites, access
  requests, notification workflows, SSO/IAM sync writes, temporary expiry jobs,
  and full frontend mutation UX were still deferred beyond Phase 4.

Phase 5: add access requests and notifications.

- Implemented `access_requests` and `permission_notifications` with
  constraints and indexes.
- Implemented access request create/list/resource-list/review/cancel APIs under
  `/api/v1/permissions`.
- Implemented review approval/denial rules: reviewer must have resource
  manage-permissions, cannot approve above their effective role, approval
  creates or upgrades a direct grant, and denial creates no grant.
- Implemented same-transaction audit writes for request created/approved/
  denied/cancelled and for approval-created or approval-updated grants.
- Implemented default permission notifications for request created,
  approved/denied, direct user grant created/updated/revoked, and workspace
  group member add/remove.
- Implemented current-user notification read APIs and mark-read operations
  under `/api/v1/notifications`.
- Added minimal Updates page read integration for notifications and mark-read,
  plus pending access request display on Share & Permissions.
- At Phase 5 completion, public share links, link expiration jobs, email invite,
  SSO/IAM sync writes, group grant fan-out notifications, temporary expiry jobs,
  notification preference persistence, watched/muted persistence, and full
  frontend permission mutation UX were still deferred. Phase 6 implements
  temporary expiry notifications and the minimal request/review UI described
  below. This backend slice implements notification preference persistence and
  watched/muted persistence.

Phase 6: add temporary access and expiry notifications.

- Implemented temporary direct grants through `resource_access_grants.expires_at`.
- Implemented temporary group memberships through
  `workspace_group_members.expires_at`.
- Implemented future/past expiry validation for grant create/update, group
  member add, and access request approval.
- Implemented `ReviewAccessRequestRequest.expiresAt` so approval can create or
  upgrade temporary direct user grants.
- Implemented `permission.grant_expiring`, `permission.grant_expired`,
  `group.member_expiring`, and `group.member_expired` notification types.
- Implemented `PermissionExpiryNotificationHostedService` with configurable
  enabled flag, scan interval, 24-hour default expiring window, testing
  environment disablement, and `dedupe_key` idempotency.
- Implemented `read-all.workspaceId` validation and stable unique conflict
  mapping for duplicate pending access request races.
- Implemented access request created notification fan-out to direct resource
  managers; group fan-out was deferred at Phase 6 and implemented by the later
  group grant notification fan-out slice.
- Implemented batch document permission filtering for bootstrap/map/search/
  export.
- Implemented minimal frontend Request access, approve/deny review, and Updates
  `actionUrl` navigation.

Phase 7: add share links.

- Implemented `share_links` for document and collection resources with hashed
  token storage, unique token hashes, expiry, and soft revoke.
- At Phase 7 completion, implemented internal workspace-member links only;
  public, anonymous, and external links were still rejected. Phase 9 later
  enabled external authenticated links while keeping public anonymous links
  disabled/deferred.
- Implemented create/list/revoke management APIs under `/api/v1/permissions`
  and resolve under `/api/v1/share-links/{token}/resolve`.
- Implemented share-link-aware effective permission for explicit token access:
  viewer links authorize view, commenter links authorize view/comment, and no
  link authorizes edit/manage/archive/delete/export. Phase 7B hardens this so
  an active link authorizes only while the linked resource policy has
  `linkMode = internal`; missing or disabled policy blocks share-link
authorization without hiding active link metadata.
- Implemented document read/context/activity/comment token support via
  `shareToken` query or `X-Share-Link-Token` header without broadening
  bootstrap/map/search/export/list filtering.
- Implemented `share_link.created` and `share_link.revoked` audit events without
  storing raw tokens.
- Implemented minimal Share & Permissions UI for internal link create, one-time
  token display, active link listing, and revoke.
- Phase 7 entry preconditions were completed by Phase 6: temporary access,
  expiry notifications, and audit/notification consistency are covered by
  tests.

Phase 8: add SSO/IAM/SCIM-style sync foundations.

- Implemented `users.external_provider` and `users.external_subject_id` with a
  filtered unique provider/subject index.
- Implemented `workspace_groups.external_provider`,
  `workspace_groups.external_group_id`, and `external_synced_at` with a
  workspace-scoped filtered unique provider/group index.
- Implemented owner/admin `POST /api/v1/workspaces/{workspaceId}/iam/sync` for
  provider-neutral external user mapping, IAM-managed group sync, and managed
  membership sync.
- Implemented idempotent sync behavior and soft member removal for members
  missing from the payload.
- Implemented read-only enforcement for IAM-managed groups through normal group
  update/archive/member mutation APIs.
- Implemented IAM-managed group grants through the existing group effective
  permission path.
- Implemented IAM sync audit actions for user mapping, group sync, and member
  add/remove without token/secret material.
- Implemented minimal Share & Permissions group-source display and disabled
  mutation controls for IAM-managed groups.

Phase 9: external authenticated share links and email invite security review.

- Implemented the Phase 9 threat model before code changes.
- Implemented external authenticated share links with normalized email binding,
  `audience = external`, one-time raw token create responses, hash-only token
  storage, expiry, revoke, resolve, audit, and `linkMode = external` gating.
- Kept public anonymous links disabled/deferred because the feature flag,
  forced expiry, abuse controls, and public rollout gates are not complete.
- Implemented email invite create/list/revoke/resolve/accept with pending/
  accepted/revoked/expired statuses, normalized email binding, required future
  expiry, one-time raw token create responses, hash-only token storage,
  duplicate pending invite `409 CONFLICT`, audit, and accepted single-resource
  effective permission.
- Implemented external token/invite effective permission without creating
  workspace membership and without broadening bootstrap/map/search/export/list
  filtering.
- Implemented minimal Share & Permissions UI for external authenticated link
  creation and email invite create/list/revoke.

Phase 10: public anonymous links and invite delivery hardening.

- Completed default-off public anonymous document links with forced future
  bounded expiry, viewer-only role, no subject email, policy `linkMode = public`
  gating, stable hash-only token handling, rate-limited anonymous resolve/read
  endpoints, and tests proving no access to protected/bulk paths.
- Completed backend invite delivery boundary with `IEmailInviteDeliveryService`,
  default disabled/noop provider, fake-provider test hook, delivery status DTOs,
  delivery metadata persistence, and token-safe audit metadata.
- Completed Phase 10 migration for public link constraints/indexes and invite
  delivery fields/constraints/indexes.
- Frontend public-link interactions remain limited to contract/read-only or
  disabled states unless the feature flag is enabled.

Production invite delivery provider slice:

- Implemented a configuration-driven SMTP provider boundary for email invite
  delivery while preserving disabled/noop as the default.
- The SMTP provider receives the one-time accept URL in memory for sending.
- Successful provider delivery records `deliveryStatus = sent` and
  `deliveryProvider = smtp`.
- Missing or incomplete SMTP configuration fails closed with
  `deliveryStatus = failed` and `deliveryErrorCode = configuration_error`.
- Provider failures record `provider_error` without storing raw tokens, token
  hashes, accept URLs, SMTP passwords, or provider secrets.
- No background outbox or retry delivery was added in this slice; the later
  secure invite outbox/retry slice adds secret-safe retry state without
  persisting raw accept URLs.

Phase 11: public collection links and link password boundary.

- Completed default-off public collection links with viewer-only role, required
  bounded expiry, no subject email, policy `linkMode = public` gating,
  dedicated anonymous collection endpoint, and restricted/archived/deleted child
  exclusion tests.
- Completed public link password hashing for public links with create-time-only
  password input, `password_hash` storage, `hasPassword` metadata, and
  `X-Share-Link-Password` proof on public resolve/read.
- Completed Phase 11 migration for public collection-safe constraints,
  password hash storage, password/public check constraint, and public active
  index over resource type.
- Hardened invite delivery tests for provider failure status without persisting
  raw invite tokens, token hashes, accept URLs, or provider secrets.
- Kept MFA/recent-auth enforcement explicitly deferred because the current
  auth/session model did not yet provide safe backend-backed boundaries.

Phase 13: share-link and invite notification fan-out.

- Implemented `share_link.created`, `share_link.revoked`,
  `email_invite.created`, `email_invite.accepted`,
  `email_invite.revoked`, and `email_invite.delivery_failed` in-app
  notification types.
- Implemented `PermissionNotificationFanoutService` for share-link and
  email-invite fan-out to resource managers and direct user share/manage grant
  subjects, with actor exclusion.
- Invite accept/revoke fan-out also includes inviter/accepted invitee recipients
  where the current invite row exposes those user ids.
- Matching muted notification preferences suppress fan-out for the recipient and
  resource/workspace.
- Notification rows do not contain raw share-link tokens, raw email invite
  tokens, token hashes, passwords, password hashes, accept URLs, or password
  proofs.
- Production delivery transport, MFA/recent-auth provider/enforcement, full
  SCIM provisioning, and frontend permission mutation workflows remain
  deferred.

Group grant notification fan-out slice:

- Implemented group-subject grant create/update/revoke in-app notifications to
  active group members.
- Group fan-out excludes the actor, removed members, expired members, archived
  groups, and users without active workspace membership.
- Access-request created fan-out now includes active members of group grants
  whose role has resource `manage_permissions`.
- Recipient expansion deduplicates workspace managers, direct user grants,
  group grants, and extra recipients.
- Matching muted notification preferences suppress group grant fan-out.

SCIM endpoint skeleton slice:

- Implemented workspace-scoped SCIM discovery endpoints under
  `/api/v1/workspaces/{workspaceId}/scim/v2`.
- SCIM User/Group provisioning was intentionally deferred from this skeleton
  slice and implemented later by the minimal SCIM provisioning V1 slice.
- Dedicated SCIM bearer-token validation was deferred from this slice and
  implemented by the later dedicated bearer-token validation slice.

Dedicated SCIM bearer-token validation slice:

- Implemented `scim_tokens` for workspace-scoped SCIM bearer tokens with
  hash-only storage, optional expiry, soft revoke, and `last_used_at`.
- Implemented owner/admin token management APIs under
  `/api/v1/workspaces/{workspaceId}/scim/tokens`.
- Raw SCIM tokens are returned only once from token creation and are not exposed
  by token listing, storage, audit metadata, or later responses.
- SCIM discovery accepts valid dedicated SCIM bearer tokens for the route
  workspace.
- Unknown, expired, revoked, and workspace-mismatched SCIM tokens fail with the
  same unauthorized boundary and do not disclose token state.
- SCIM bearer-token requests do not create users, workspace memberships, groups,
  or provisioning state unless they call the later SCIM provisioning V1
  endpoints.

SCIM provisioning V1 slice:

- Implemented minimal SCIM Users list/get/create/patch under
  `/api/v1/workspaces/{workspaceId}/scim/v2/Users`.
- Implemented minimal SCIM Groups list/get/create/patch under
  `/api/v1/workspaces/{workspaceId}/scim/v2/Groups`.
- Provisioning mutations require valid dedicated SCIM bearer-token auth for the
  route workspace; owner/admin JWT fallback remains for discovery only.
- User create maps `externalId` to `users.external_subject_id` with
  `external_provider = scim`, can bind an existing local email user only when no
  conflicting external identity exists, and creates viewer-only workspace
  membership when needed.
- User patch supports narrow profile updates and does not create passwords or
  `UserCredential` rows.
- Group create/patch syncs SCIM-managed workspace group display name and member
  set using SCIM-managed user ids in the same workspace.
- Local non-SCIM-managed groups are rejected by SCIM mutation APIs.
- SCIM delete/deactivate, bulk operations, complex filters, enterprise
  extension, and broader provider compatibility remain deferred.

SCIM Provisioning Compatibility Hardening V1.1 slice:

- Implemented restricted `PUT /api/v1/workspaces/{workspaceId}/scim/v2/Users/{userId}`
  for SCIM-managed users. Replace preserves the existing SCIM `externalId`,
  supports profile fields, rejects deactivation, and does not create
  credentials or passwords.
- Implemented restricted `PUT /api/v1/workspaces/{workspaceId}/scim/v2/Groups/{groupId}`
  for SCIM-managed groups. Replace preserves the existing SCIM `externalId`,
  updates display name, and syncs members when a member list is supplied.
- Local non-SCIM-managed groups remain rejected by SCIM mutation APIs.
- Users support no filter, `userName eq`, and `externalId eq`; Groups support
  no filter, `displayName eq`, and `externalId eq`.
- Unsupported filters use the stable API validation error boundary; non-SCIM
  API error middleware is unchanged.
- `startIndex` and `count` normalization are covered by tests.
- SCIM delete/deactivate, bulk operations, complex filter grammar, enterprise
  extension, and broader compatibility beyond V1.1 remain deferred.

Secure invite outbox/retry delivery slice:

- Implemented `email_invite_delivery_outbox` for backend-owned invite delivery
  retry state.
- Outbox rows store workspace, invite id, recipient email, provider, retry
  status, attempt counts, next/last attempt timestamps, sent/failed timestamps,
  and non-secret last error metadata.
- Outbox rows do not store raw invite tokens, token hashes, raw accept URLs,
  SMTP passwords, provider secrets, or password-like values.
- Invite creation enqueues a delivery outbox item when configured delivery is
  enabled, then performs the first provider attempt with the one-time accept URL
  in memory.
- The processor can retry due outbox items when the caller supplies the
  one-time accept URL in memory. If a due item cannot be processed without
  persisting or reconstructing a secret, it fails closed with
  `missing_accept_url`.
- Retry state tracks `attempt_count`, `max_attempts`, `next_attempt_at`,
  `last_attempt_at`, `sent_at`, `failed_at`, `last_error_code`, and
  `last_error_message`.
- Delivery failure notification fan-out remains secret-safe and reuses the
  existing dedupe key.
- No hosted background worker or durable raw accept URL storage was added.

Frontend permission mutation workflow V1 slice:

- Implemented document-scoped Share & Permissions grant mutation UI for direct
  user grants and group grants using existing resource grant APIs.
- Implemented grant role/expiry update and deliberate revoke actions with
  backend refresh after success.
- Implemented Access Settings UI for supported policy changes:
  `inheritanceMode`, `linkMode = disabled|internal|external`, and
  viewer/commenter default link role.
- Generic policy patch still does not expose direct `linkMode = public`;
  public links remain separated into dedicated share-link APIs.
- Existing share-link and email-invite create/revoke flows now refresh related
  permission data and keep generated raw tokens/URLs as dismissible one-time
  create-response state only.
- No workspace member management UI, SCIM management UI, or full public-link
  browser acceptance flow was added.

Frontend permission admin surfaces V1 slice:

- Implemented workspace member management UI under `#workspace-members` using
  existing workspace member APIs for list, add, role update, and remove.
- Owner assignment is not exposed through the add-member UI; existing owner rows
  remain subject to backend ownership protections for role change/removal.
- Implemented workspace group read surfaces under `#workspace-groups` using the
  existing group list API, including local/IAM-managed source metadata where
  returned by the backend contract.
- Implemented SCIM management UI under `#scim` for discovery status, endpoint
  display, capability summary, and SCIM bearer-token list/create/revoke.
- SCIM raw tokens are shown only in the one-time create-response panel and are
  not shown in long-lived token lists; token hashes are not displayed.
- SCIM provisioning UI, SCIM delete/deactivate, SCIM bulk, complex filters,
  enterprise extension, MFA UI, OIDC/SAML UI, and public-link runtime changes
  were not added.

Frontend public-link interaction hardening V1 slice:

- Implemented document public-link creation in Share & Permissions using only
  the existing authenticated share-link create API.
- Public document link creation sends `audience = public`, viewer role, a
  required future expiry, and optional password only through the share-link API.
- Generic policy mutation still does not expose direct `linkMode = public`;
  public policy changes remain backend-owned side effects of public share-link
  creation.
- Generated public URL/token values are shown only in a dismissible one-time
  create-response panel.
- Active link lists show metadata such as audience, role, expiry, and
  `hasPassword`; they do not show raw tokens, token hashes, passwords, password
  hashes, or password proofs.
- No collection public-link content UI, public anonymous route expansion,
  backend public-link change, or protected API widening was added.

MFA/recent-auth backend state foundation slice:

- Implemented `GET /api/v1/auth/security-state` for authenticated users.
- Recent-auth state is derived from successful backend `auth.login`,
  `auth.register`, and `auth.idp_login` events; refresh token rotation does not
  replace recent-auth.
- MFA is reported as not enabled/not verified until a real backend provider and
  enrollment flow exists.
- No existing permission mutation is blocked by step-up enforcement in this
  slice.

MFA provider step-up enforcement V1 slice:

- Implemented backend-backed TOTP enrollment, verification, and disable APIs
  under `/api/v1/auth/mfa/totp`.
- TOTP enrollment creates pending MFA state and returns the authenticator secret
  only in the enrollment response.
- TOTP secrets are stored as protected ciphertext in `user_mfa_methods`; the
  table does not store plaintext secrets or one-time codes.
- Successful TOTP verification marks MFA enabled, writes a secret-safe
  `auth.mfa_verified` event, and creates a short backend step-up window.
- `GET /api/v1/auth/security-state` reports MFA enabled/verified state,
  recent-auth/step-up timestamps, and the supported `totp` step-up method.
- Disabling TOTP MFA requires backend step-up and marks the active method
  disabled.
- High-risk permission mutations require step-up when MFA is enabled, including
  resource grant create/update/revoke, resource policy update, share-link
  create/revoke, email invite create/revoke, workspace member add/update/remove,
  and SCIM token create/revoke.
- WebAuthn/passkeys, SMS/email MFA, recovery codes, user self-service recovery,
  and administrator reset flows remain deferred.

Real IdP login boundary slice:

- Implemented `POST /api/v1/auth/idp/login` as a default-disabled trusted
  assertion boundary for external identity login.
- Existing external identities issue normal Northstar tokens.
- Existing local email users can be bound to an external identity only when no
  conflicting external identity exists.
- Missing users are rejected and no workspace membership is created implicitly.
- `auth.idp_login` contributes to backend recent-auth state.
- Auth event metadata omits external subject ids, emails, tokens, passwords,
  provider secrets, raw assertions, and callback URLs.
- Full OIDC/SAML provider redirect/callback UX, provider validation,
  connector configuration, and secret storage remain deferred.

Remaining deferred:

- SCIM bulk operations, complex filter grammar, enterprise extension,
  delete/deactivate behavior, and broader compatibility beyond V1.1;
- full OIDC/SAML provider integration, IdP connector secret storage, and bulk
  link management;
- WebAuthn/passkeys, SMS/email MFA, recovery codes, and MFA recovery/reset
  flows.

## Implementation Checklist

Before adding or changing permission behavior, verify:

- [ ] the role/action mapping is defined in the permission catalog;
- [ ] the controller calls the central effective permission service;
- [ ] list/search/export queries cannot leak inaccessible rows;
- [ ] mutation writes an immutable audit event;
- [ ] expired and revoked grants are ignored;
- [ ] tests cover allowed and denied cases;
- [ ] UI labels deferred capabilities honestly;
- [ ] ownership protection is not bypassed;
- [ ] no client-supplied role/effective access is trusted;
- [ ] no controller-local ad hoc role ranking was introduced.

If any item is unchecked, the change is not ready.
