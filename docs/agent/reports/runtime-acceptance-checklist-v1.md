# Northstar Runtime Acceptance Checklist v1

## Status

Prepared on 2026-05-10. First non-browser runtime acceptance attempt recorded on 2026-05-10.
Files / Attachments command-line runtime acceptance passed against the local API provider on 2026-05-11.
File outbox processing has focused test coverage as of 2026-05-12, but it has not been exercised in a live runtime acceptance pass.
S3-compatible object storage has presigned-target test coverage as of 2026-05-12, and a live endpoint acceptance test exists. The live test has not passed yet because no S3/MinIO endpoint is available from the current agent environment.

This is a manual/live runtime acceptance checklist. It is not proof that the
runtime passed. Browser QA, Playwright, screenshots, PostgreSQL smoke, and
object-storage verification were not run as part of this documentation round.

## Runtime Attempt 2026-05-10

Scope:

- Non-browser runtime acceptance only.
- Used command-line HTTP checks against a running API.
- Created a disposable local PostgreSQL database named `northstar_runtime_acceptance`.
- Did not run browser QA, Playwright, screenshots, or object-storage verification.

Results:

| Area | Status | Evidence |
|---|---|---|
| Backend restore | Pass | `dotnet restore services/api/Northstar.sln` succeeded. |
| Backend build | Pass | `dotnet build services/api/Northstar.sln -c Release --no-restore` succeeded after avoiding the running Debug API lock. |
| Backend tests | Pass | `dotnet test services/api/Northstar.sln -c Release --no-restore --no-build` passed 252 tests. |
| Frontend tests | Pass | `npm test` passed 207 frontend regression tests. |
| Frontend build | Pass | `npm run build` succeeded with the existing Vite large-chunk warning. |
| Existing local API health | Pass | `https://localhost:7036/api/v1/health` returned `200 Healthy`. |
| Acceptance API health | Pass | `http://localhost:5136/api/v1/health` returned `200 Healthy`. |
| Default development database migration | Pass with explicit env | `dotnet ef database update` succeeded only when `NORTHSTAR_DATABASE_CONNECTION` was explicitly set; default config password failed. |
| Disposable acceptance database migration | Pass | All migrations applied to `northstar_runtime_acceptance`. |
| Seed owner login | Fail | `owner@northstar.local` with documented development password returned `401 Unauthorized` in both the existing and disposable runtime databases. |
| Register endpoint | Pass | A new runtime user could be registered and received access/refresh tokens. |
| Registered user workspace access | Blocked | Newly registered users had no workspace membership; `/api/v1/bootstrap` returned `403 Forbidden`. |
| Main app API acceptance | Blocked | Home, Library, Editor, Comments, Share, Settings, Updates, Search, and Files require an authenticated workspace member. No usable workspace-member account was available through documented runtime credentials. |
| PostgreSQL smoke | Not run | `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was not set. |
| Browser QA | Not run | User requested no browser QA. |
| Object storage/file provider content verification | Not run | Blocked behind authenticated workspace-member API flow. |

Runtime blockers:

1. Documented seed owner login is not usable in the current runtime acceptance path.
2. Register creates an authenticated user but does not place that user into a workspace, so protected knowledge-workspace APIs cannot be accepted from that account.
3. `dotnet ef database update` depends on explicit runtime connection-string environment setup; the default connection string can fail against local PostgreSQL.

Next action:

- Fix or document a supported runtime acceptance identity path before continuing live API acceptance. The safest options are either making seed owner credential seeding deterministic for disposable acceptance databases, or adding a documented acceptance setup command that creates a workspace-member test user through approved application code.

## Runtime Attempt 2026-05-11 Files / Attachments

Scope:

- Non-browser runtime acceptance only.
- Used command-line HTTP checks against `http://localhost:5136/api/v1`.
- Used disposable local PostgreSQL database `northstar_runtime_acceptance`.
- Used the development seed owner after current seeding repair in the working tree.
- Exercised the local API file provider path. External object storage was not configured or tested.

Results:

| Area | Status | Evidence |
|---|---|---|
| Runtime API startup | Pass | API started on `http://localhost:5136`; `/api/v1` authenticated endpoints were reachable. |
| Seed owner login | Pass | `POST /api/v1/auth/login` accepted `owner@northstar.local` with documented development password. |
| Bootstrap | Pass | `GET /api/v1/bootstrap` returned workspace `10000000-0000-0000-0000-000000000001` and active document `a826b005-f22c-4821-ab31-f95b5363587e`. |
| Upload session | Pass | `POST /api/v1/files/uploads/sessions` returned session `3a46c416-7964-4ca8-9ef2-31a380d79f85`. |
| Content upload | Pass | `PUT /api/v1/files/uploads/sessions/{sessionId}/content` returned `200`. |
| Complete | Pass | `POST /api/v1/files/uploads/sessions/{sessionId}/complete` completed the session. |
| Finalize | Pass | `POST /api/v1/files/uploads/sessions/{sessionId}/finalize` created file `45b0b97d-2392-4f3e-b829-4b7e761b11ce` and attachment `495de340-a720-4040-9d48-0ea95786715d`. |
| Idempotent finalize | Pass | Repeating finalize returned the same file id and attachment id. |
| Attachment list | Pass | `GET /api/v1/documents/{documentId}/attachments` included the finalized attachment. |
| File content | Pass | `GET /api/v1/files/{fileId}/content` returned the uploaded text content. |
| Delete conflict | Pass | `DELETE /api/v1/files/{fileId}` returned `409` while the document attachment was active. |
| Remove attachment | Pass | `DELETE /api/v1/documents/{documentId}/attachments/{attachmentId}` returned `204`. |
| Delete after detach | Pass | `DELETE /api/v1/files/{fileId}` returned `204` after the attachment relation was removed. |
| Content after delete | Pass | `GET /api/v1/files/{fileId}/content` returned `404` after file deletion. |
| Runtime cleanup | Pass | The runtime-created attachment relation was removed and the runtime-created file was deleted. |
| PostgreSQL smoke | Not run | `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was not set. |
| Browser QA | Not run | User requested no browser QA. |
| External object storage | Not run | Only the local API file provider was exercised. |

Observed follow-up:

- Public file DTOs have since been hardened so ordinary responses no longer expose `storageProvider`, `bucket`, or `objectKey`.
- Remaining Files / Attachments runtime acceptance should cover Tiptap file reference validation and the broader file permission role matrix beyond the owner path.

## File Outbox Processor Test Closure 2026-05-12

Scope:

- Backend focused tests only.
- No browser QA.
- No live runtime worker observation.
- No external object-storage provider.

Results:

| Area | Status | Evidence |
|---|---|---|
| Finalize outbox processing | Pass | Focused API test marked `file.finalized` as `published`. |
| Attachment outbox processing | Pass | Focused API test marked `document_attachment.created` as `published`. |
| Delete outbox processing | Pass | Focused API test called `IObjectStorage.DeleteObjectAsync` and marked `file.deleted` as `published`. |
| Retry/failure state | Pass | Focused API test retried delete failures and marked the event `failed` after max attempts. |
| Hosted service runtime loop | Not run | Implemented, but not observed in live runtime acceptance. |
| External object storage | Not run | No live S3/MinIO endpoint was configured or exercised. |
| Browser QA | Not run | User requested no browser QA. |

## S3-Compatible Object Storage Provider Test Closure 2026-05-12

Scope:

- Backend focused tests only.
- No live S3/MinIO service.
- No browser QA.

Results:

| Area | Status | Evidence |
|---|---|---|
| Provider selection | Pass | `Files:StorageProvider = S3` resolves the S3-compatible storage path. |
| Presigned upload target | Pass | Focused API test returns a presigned `PUT` URL and content-type header. |
| Local provider default | Pass by existing focused tests | Existing upload/finalize/content/delete tests continue to pass with default Local configuration. |
| Live S3/MinIO content upload | Blocked | Acceptance test exists, but no reachable endpoint/credentials were available. Docker CLI exists, but Docker engine was not running in the agent environment. |
| Live S3/MinIO read/delete | Blocked | Acceptance test exists, but no reachable endpoint/credentials were available. Docker CLI exists, but Docker engine was not running in the agent environment. |
| Browser QA | Not run | User requested no browser QA. |

Acceptance test:

```powershell
$env:NORTHSTAR_S3_ACCEPTANCE_ENDPOINT = "http://127.0.0.1:19000"
$env:NORTHSTAR_S3_ACCEPTANCE_ACCESS_KEY = "northstar"
$env:NORTHSTAR_S3_ACCEPTANCE_SECRET_KEY = "northstar123"
$env:NORTHSTAR_S3_ACCEPTANCE_BUCKET = "northstar-acceptance"
dotnet test services/api/tests/Northstar.Api.Tests/Northstar.Api.Tests.csproj --filter "FullyQualifiedName~S3CompatibleStorageAcceptance_UploadReadDeleteThroughApi"
```

## Runtime Attempt 2026-05-11 Files Permission Matrix / Tiptap References

Scope:

- Non-browser runtime acceptance only.
- Used command-line HTTP checks against `http://localhost:5136/api/v1`.
- Used disposable local PostgreSQL database `northstar_runtime_acceptance`.
- Exercised local API file provider and document patch APIs.

Results:

| Area | Status | Evidence |
|---|---|---|
| Owner upload/finalize | Pass | Owner created upload session, uploaded content, completed, and finalized a document attachment. |
| Editor upload/finalize | Pass | Editor workspace member created upload session, uploaded content, completed, and finalized a document attachment. |
| Viewer file metadata read | Pass | Viewer received `200` from `GET /api/v1/files/{fileId}`. |
| Viewer file content read | Pass | Viewer received `200` from `GET /api/v1/files/{fileId}/content`. |
| Outsider file metadata read | Pass | Non-member received `403` from `GET /api/v1/files/{fileId}`. |
| Outsider file content read | Pass | Non-member received `403` from `GET /api/v1/files/{fileId}/content`. |
| Viewer upload session create | Pass | Viewer received `403` from `POST /api/v1/files/uploads/sessions`. |
| Viewer attach file | Pass | Viewer received `403` from `POST /api/v1/documents/{documentId}/attachments`. |
| Viewer remove attachment | Pass | Viewer received `403` from `DELETE /api/v1/documents/{documentId}/attachments/{attachmentId}`. |
| Tiptap valid file reference | Pass | `PATCH /api/v1/documents/{documentId}` with image `attrs.fileId` and `/api/v1/files/{fileId}/content` returned `200`. |
| Tiptap inline attachment sync | Pass | Valid image file reference created an `inline_image` document attachment. |
| Tiptap missing file reference | Pass | `PATCH /api/v1/documents/{documentId}` with a missing file id returned `400`. |
| Runtime cleanup | Pass | Runtime-created attachments/files were removed; acceptance document was restored to content without file references. |
| PostgreSQL smoke | Not run | `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was not set. |
| Browser QA | Not run | User requested no browser QA. |
| External object storage | Not run | Only the local API file provider was exercised. |

## Purpose

Use this checklist when Northstar is ready for a focused runtime pass across the
daily user path:

```text
Home -> Library -> Editor -> Comments / Share / Activity
Settings -> Members / Permissions / Security / Integrations
Updates -> access / sharing / permission notifications
```

The checklist is intentionally bounded. It verifies whether current frontend
surfaces match existing backend contracts; it does not authorize backend
architecture changes, new entities, route renames, new notification fanout, or a
new Library backend model.

## Environment Prerequisites

### Backend

- Work from `services/api`.
- Verify current solution/project paths before running commands.
- Expected commands from current README:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet ef database update --project src\Northstar.Infrastructure --startup-project src\Northstar.Api
dotnet run --project src\Northstar.Api
```

- Default local database connection is documented as:

```text
Host=localhost;Port=5432;Database=northstar;Username=postgres;Password=postgres
```

- Override database connection with `ConnectionStrings:Northstar` or
  `NORTHSTAR_DATABASE_CONNECTION`.
- Development seed owner is documented as:

```text
email: owner@northstar.local
password: Northstar.dev.123!
```

- Health check: `/api/v1/health`.
- Swagger in development: `/swagger`.

### Frontend

- Work from `apps/web`.
- Available scripts:

```powershell
npm test
npm run build
npm run dev
npm run preview
```

- API configuration:
  - `VITE_NORTHSTAR_API_BASE_URL`
  - fallback: `VITE_API_BASE_URL`
  - if unset, the Vite dev proxy can route `/api/v1` to
    `VITE_NORTHSTAR_API_PROXY_TARGET` or default `https://localhost:7036`.
- Workspace override:
  - `VITE_NORTHSTAR_WORKSPACE_ID`
  - or `?workspaceId=<uuid>` in the URL.
- Frontend auth tokens are stored under the Northstar local storage keys used by
  `apiClient.ts`; runtime acceptance should sign in through the normal API flow
  instead of manually forging tokens.

### Files

- Current backend README documents Phase 6 `Files` provider as `Local` by
  default.
- Relevant configuration keys:

```json
{
  "Files": {
    "StorageProvider": "Local",
    "LocalRootPath": "var/files",
    "DefaultBucket": "northstar-local",
    "UploadSessionMinutes": 60,
    "MaxFileBytes": 52428800
  }
}
```

- File binaries must remain private and be read through
  `GET /api/v1/files/{fileId}/content`.
- Do not claim object-storage or file-content acceptance unless the file flow is
  actually exercised against the running backend and configured storage.

### PostgreSQL Smoke

PostgreSQL smoke is accepted only if all are true:

1. `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is set.
2. The smoke command actually runs.
3. The smoke command succeeds.

Documented smoke command:

```powershell
$env:NORTHSTAR_POSTGRES_SMOKE_CONNECTION="Host=localhost;Port=5432;Database=northstar_smoke;Username=postgres;Password=postgres"
dotnet test .\Northstar.sln --filter PostgreSqlSmoke
```

If the variable is absent, report exactly:

```text
PostgreSQL smoke not run: NORTHSTAR_POSTGRES_SMOKE_CONNECTION not set.
```

## Acceptance Result Values

Use one of:

- `Pass`: verified against the running system.
- `Fail`: verified and does not meet the expected behavior.
- `Blocked`: cannot proceed because environment, auth, seed data, or backend
  capability is missing.
- `Not run`: intentionally not executed.

Do not use `Pass` for inferred behavior from code review or docs.

## Global Gates

| Gate | Expected Result | Status |
|---|---|---|
| User-facing hierarchy | Ordinary UI uses Workspace, Library, Folder, Document | Blocked: browser/UI path not run |
| Backend model preservation | No frontend acceptance requires renaming Organization, Workspace, Space, Collection, or Document backend contracts | Not run |
| API truthfulness | UI shows loading, empty, forbidden, error, and API-unconfigured states honestly | Blocked: no workspace-member account |
| Placement | Features live on the surface where a normal user expects them | Blocked: browser/UI path not run |
| Dead affordances | Every visible button/link has an action, route, feedback state, or disabled reason | Blocked: browser/UI path not run |
| Tiptap JSON boundary | Comments, files, permissions, notifications, activity, audit, and presence are not stored in document JSON | Not run |
| Public-link boundary | Public links use dedicated share-link APIs only | Not run |
| Updates boundary | Updates is access/sharing/permission notifications, not document activity | Not run |
| Browser QA | Browser QA is not run unless explicitly allowed by the user | Pass: skipped by user instruction |

## Runtime Order

1. Start backend and confirm `/api/v1/health`.
2. Run or confirm database migration state for the target database.
3. Start frontend with configured API base or dev proxy.
4. Sign in as a seeded or provisioned workspace user.
5. Verify Home, Library, Editor, Share, Settings, Updates.
6. Verify attachments/files only after backend and local storage path are known.
7. Run PostgreSQL smoke only when `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is set.

## Home

| Check | Expected Result | Status |
|---|---|---|
| Home loads | Shows workspace daily entry points or a clear API/auth/unconfigured state | Blocked: no workspace-member account |
| Primary actions | New document, Library, Search, Updates, and Settings routes are clear and canonical | Not run |
| Team activity | Activity is labeled as recent document/workspace activity, not as notification inbox work | Not run |
| Activity grouping | Repeated ordinary document update rows are grouped or reduced in display | Not run |
| Activity links | Activity item actions route to concrete documents or safe hash targets | Not run |
| No workspace admin leakage | Home does not mix Organization administration into normal workspace usage | Not run |

## Library / Folder / Document

| Check | Expected Result | Status |
|---|---|---|
| Library map loads | Library, folders, and documents load from backend or show honest unavailable state | Blocked: no workspace-member account |
| Create folder | Creates a folder or shows permission/API disabled reason | Not run |
| Create document | Creates a document and focuses/opens the new document route | Not run |
| Rename/move/delete/restore | Preserves Library context and does not fake success | Not run |
| Folder navigation | Folder links stay in Library context and do not route to workspace admin | Not run |
| Search from Library | Search results route to Folder/Document targets through safe hashes | Not run |
| Terminology | No ordinary UI exposes Space or Collection as the user-facing model | Not run |

## Editor

| Check | Expected Result | Status |
|---|---|---|
| Document open | `#editor?documentId=...` loads document content and context | Blocked: no workspace-member account |
| Load failure | Shows clear error and return path without breaking the whole shell | Not run |
| Save state | Saved/saving/error/conflict states are accurate; failed saves do not show success | Not run |
| Document context | Overview, Info, Comments, Activity tabs keep current document context | Not run |
| Return paths | Back to Library/Folder routes are safe and preserve context | Not run |
| Local panel boundary | Editor panels do not become workspace member/settings management surfaces | Not run |
| Activity boundary | Editor Activity shows current document activity and does not link to Updates as a full activity inbox | Not run |

## Comments

| Check | Expected Result | Status |
|---|---|---|
| List threads | Loads document comments or shows empty/error/forbidden/unconfigured state | Blocked: no workspace-member account |
| Create comment | Creates a thread with non-empty input and prevents duplicate pending submit | Not run |
| Reply | Adds a message without losing input on failure | Not run |
| Resolve/reopen | Resolves and reopens using comment APIs with feedback | Not run |
| Retry | Load failure can be retried | Not run |
| Document switch | Comments from one document do not appear under another document | Not run |
| Persistence boundary | Comment data, anchors, runtime ranges, and active state are not written into Tiptap JSON | Not run |
| Mention boundary | `@` text does not create unsupported mention notification/model behavior | Not run |

## Share Drawer

| Check | Expected Result | Status |
|---|---|---|
| Entry | Editor Share opens the lightweight Share Drawer, not the advanced page | Blocked: browser/UI path not run |
| Current document | Drawer title and actions stay scoped to the current document | Not run |
| Member grant | Existing workspace members can receive roles only when backend roles allow it | Not run |
| Email invite | Email invite uses existing invite API; edit is not exposed unless backend supports it | Not run |
| Member search fallback | If member search is unavailable/forbidden, UI says so and still allows email invite where supported | Not run |
| Link scope | Invited/internal/public states map to existing share-link behavior | Not run |
| Link role | Share links expose viewer/commenter only unless backend contract explicitly supports more | Not run |
| Public link | Public link creation uses dedicated share-link API and does not mutate generic public policy directly | Not run |
| Password/expiry | Password and expiry controls are enabled only when backend capability supports them | Not run |
| Current access | Owner, direct grants, inherited workspace access, link access, and invites are clearly separated | Not run |
| Advanced link | Advanced permissions opens the document-scoped advanced page | Not run |

## Document Advanced Permissions

| Check | Expected Result | Status |
|---|---|---|
| Page purpose | Page is labeled as Advanced permissions, not daily sharing | Blocked: browser/UI path not run |
| Scope | Page is document-scoped and does not become workspace member management | Not run |
| Direct grants | User grants create/update/revoke only when backend and role allow | Not run |
| Group grants | Group access is shown only if backed by current permission APIs | Not run |
| Access policy | Inherit/restricted policy is clear and scoped to the document resource | Not run |
| Access requests | Document access requests show real state or honest empty/unavailable state | Not run |
| Inherited access | Workspace access is shown as inherited source with a Settings link, not as local document editing UI | Not run |
| Share summary | Share drawer summary is read-only/contextual; daily link/invite work stays in the drawer | Not run |
| Back to document | Return path goes back to the document without losing context | Not run |

## Settings

| Check | Expected Result | Status |
|---|---|---|
| Main tabs | General, Notifications, Members, Permissions, Security, Integrations are the ordinary workspace settings tabs | Blocked: browser/UI path not run |
| Legacy concepts | Access & Identity appears only as internal/legacy compatibility, not ordinary UI | Not run |
| Members | Workspace members are managed only in Settings Members | Not run |
| Members scope | Organization members are not mixed into workspace members | Not run |
| Permissions | Groups, access requests, direct grants, and policy summaries live under Settings Permissions | Not run |
| Daily sharing boundary | Settings does not duplicate the Editor Share Drawer as the daily document sharing surface | Not run |
| Security | Security shows only supported policy/auth capability or honest unavailable/deferred state | Not run |
| Integrations | SCIM/SSO/integration controls are enabled only when backend capability and role allow | Not run |
| Unsupported actions | Unsupported buttons have disabled reasons or are not shown | Not run |
| Legacy routes | `#members`, `#workspace-members`, `#permission-admin`, `#workspace-groups`, `#groups`, and `#scim` hard-canonicalize to Settings | Not run |

## Updates

| Check | Expected Result | Status |
|---|---|---|
| Page purpose | Updates is access, sharing, permission, group, invite, and expiry notifications | Blocked: no workspace-member account |
| Tabs | Only All, Unread, Access requests, Grants & groups, Sharing links & invites, Expiry are primary tabs | Not run |
| Unsupported categories | Comments, Mentions, Document changes, Versions, System alerts are not primary capabilities | Not run |
| Notification rows | Rows show human-readable action, title/body, created time, read/unread, and action target | Not run |
| Actor fallback | Unknown actor/title fallback is honest and does not forge names or documents | Not run |
| Action URL | Notification `actionUrl` uses safe hash normalization and canonical routes | Not run |
| Preferences | Preferences match backend resource watch/mute support; no unsupported category toggles | Not run |
| Boundary | Ordinary `document.updated` and autosave are not reintroduced into Updates | Not run |

## Search

| Check | Expected Result | Status |
|---|---|---|
| Query | Search executes against current backend contract or shows API-unconfigured state | Blocked: no workspace-member account |
| Results | Results distinguish documents/folders where current data allows | Not run |
| Routes | Result actions route to safe document/folder hashes | Not run |
| Scope | Search does not become a workspace member, organization admin, or permissions surface | Not run |
| Strategy boundary | No runtime claim assumes external search, PostgreSQL full text, or trigram unless explicitly implemented | Not run |

## Files / Attachments

Files and attachments are not accepted until exercised against the running
backend and configured storage.

| Check | Expected Result | Status |
|---|---|---|
| Upload session | Creates upload session through `/api/v1/files/uploads/sessions` | Pass: command-line API runtime acceptance on 2026-05-11 |
| Content upload | Uploads content through the documented local provider flow | Pass: local API provider content upload returned `200` |
| Complete | Completes session with validation feedback | Pass: complete endpoint accepted uploaded content |
| Finalize | Finalize creates exactly one file record and is idempotent | Pass: repeated finalize returned the same file and attachment ids |
| Attach to document | Finalize or attachment endpoint creates document attachment relation | Pass: finalize created a document attachment relation |
| Attachment list | Editor Info lists real document attachments | Pass for API: attachment list included finalized attachment; browser UI not run |
| Remove attachment | Removing attachment removes relation without deleting active file unexpectedly | Pass: relation delete returned `204`, file delete succeeded only afterward |
| File content | Content reads through `GET /api/v1/files/{fileId}/content` and permission checks | Pass for authenticated owner runtime path; focused API tests now cover viewer, outsider, and restricted attached-document behavior |
| No permanent URL | API/UI do not store or rely on permanent public file URLs | Pass for backend DTO contract: file metadata responses do not expose storage internals or permanent URL fields |
| Tiptap file reference | Tiptap JSON stores only documented file references, not file metadata source of truth | Pass: valid reference patch created an `inline_image` attachment; missing file reference returned `400` |
| Delete conflicts | File delete conflict/success paths match backend rules and do not silently break active attachments | Pass: delete returned `409` while attached and `204` after detach |
| Object storage | Storage provider behavior is actually tested before claiming pass | Not run for external object storage; local API provider passed |

## Backend Runtime Checks

| Check | Expected Result | Status |
|---|---|---|
| Restore | `dotnet restore` succeeds | Pass |
| Build | `dotnet build` succeeds | Pass: Release build; Debug build was blocked by a running API process lock |
| Tests | `dotnet test` succeeds | Pass |
| Migration | Database update applies current migrations to target database | Pass with explicit runtime connection string |
| Health | `/api/v1/health` returns success | Pass |
| Auth | Seed owner login succeeds and returns tokens | Pass on 2026-05-11 after current seeding repair in working tree; failed in first 2026-05-10 attempt |
| PostgreSQL smoke | Smoke only passes if env var is set and smoke test succeeds | Not run: env var not set |

## Frontend Runtime Checks

| Check | Expected Result | Status |
|---|---|---|
| Tests | `npm test` succeeds | Pass |
| Build | `npm run build` succeeds | Pass |
| Dev server | `npm run dev` serves the app | Not run |
| API wiring | Frontend reaches API through configured base URL or Vite proxy | Not run: browser/frontend runtime path skipped |
| Auth refresh | 401 refresh path does not mask real auth failures | Not run |
| Error formatting | API/network/forbidden/unconfigured errors are user-readable | Not run |

## Known Non-Acceptance Items

- Browser QA is not run in this checklist unless the user explicitly allows it.
- PostgreSQL smoke is not passed unless `NORTHSTAR_POSTGRES_SMOKE_CONNECTION`
  is set and the smoke command succeeds.
- Object storage integration is not passed unless the file flow is exercised
  against configured storage.
- Frontend display aggregation for Activity is not backend notification
  aggregation.
- Updates is not a full activity inbox.
- Files Phase 6 cannot be treated as complete based only on frontend build or
  docs.

## Completion Criteria

Runtime acceptance for the current phase is complete only when:

1. Home, Library, Editor, Comments, Share Drawer, Advanced permissions,
   Settings, Updates, Search, and Files/Attachments have `Pass` or documented
   `Blocked` status with owner and next action.
2. No ordinary user flow exposes Space/Collection as the main user-facing model.
3. No document-local page becomes a workspace admin surface.
4. No unsupported backend capability appears enabled.
5. No low-signal document edit appears in Updates as notification inbox work.
6. PostgreSQL and storage claims are backed by actual executed validation.
7. Browser QA status is explicitly reported according to the user's instruction.
