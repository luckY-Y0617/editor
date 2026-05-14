# Northstar System Completion Plan v1

## Status

This is the working completion plan for Northstar. It is a product and architecture delivery-control document, not an implementation change.

Use this document to choose the next implementation slice, decide whether a feature is complete enough, and prevent adjacent surfaces from expanding without a clear finish line.

## Scope

- Product: Northstar / Northstar Atlas Library.
- User-facing knowledge model: Workspace -> Library -> Folder/Document.
- Backend model remains: Organization -> Workspace -> Space -> Collection -> Document.
- `Space` remains the backend entity behind user-facing Library.
- `Collection` remains the backend entity behind user-facing Folder.
- This plan does not change backend architecture, database schema, API routes, permission semantics, public-link behavior, comments persistence, files behavior, or Tiptap document JSON rules.

## Completion Principle

Northstar should become a reliable daily knowledge workspace before it becomes a broad enterprise platform.

Prioritize:

1. Stable document work.
2. Clear Library / Folder / Document organization.
3. Trustworthy sharing and permissions.
4. Honest backend-backed Settings and Updates.
5. Files and attachments as part of editing.
6. Activity, notifications, audit, and presence as separate systems.
7. User-facing task placement that matches normal user expectations, not only internal product categories.

Defer:

- Full realtime collaboration.
- Full notification aggregation backend.
- Full i18n.
- Organization provisioning and global member mutation.
- Broad enterprise IAM/SSO expansion beyond existing contracts.
- New Library backend entity.

## Product Lessons Applied

Notion-like systems fail when every surface becomes a dashboard and every event becomes a notification. Obsidian-like systems fail when the folder/document model becomes unstable or hidden behind abstract workspace concepts.

Northstar should keep:

- A stable content path: Library -> Folder -> Document.
- A stable work path: Home -> Library -> Editor -> Share / Comments / Activity.
- A stable admin path: Settings -> Members / Permissions / Security / Integrations.
- A stable notification path: Updates -> access / sharing / permission notifications.

Northstar should avoid:

- Treating ordinary autosave as notification inbox work.
- Hiding broken backend capability behind polished frontend cards.
- Making Settings a second version of every product surface.
- Mixing Organization administration into normal workspace usage.
- Putting a feature on an adjacent surface just because the data is available there.
- Leaving a clickable affordance without a clear result, route, feedback state, or disabled reason.
- Putting comments, files, permissions, activity, or notification state into Tiptap JSON.

Every user-facing implementation slice should include a small product-placement pass while the code is already being touched. This is not a request for separate UX cleanup rounds. If a function belongs to Library, Share Drawer, Editor, Updates, or Settings, keep it there and remove or weaken confusing duplicate entry points when that can be done safely inside the current slice.

## Completion Status Legend

- `Complete for current phase`: enough for the current product phase; future enhancements are optional.
- `Usable but incomplete`: main path works or is mostly wired, but there are known gaps.
- `Frontend-only`: UI or display behavior exists without final backend architecture.
- `Backend contract exists; UI incomplete`: backend appears available or documented, but product UI is not complete.
- `Design only`: architecture or ADR exists; implementation is deferred.
- `Not verified`: status must be confirmed in current code before implementation.
- `Deferred`: intentionally not part of the current completion path.

## System Completion Matrix

| Area | Current Status | Completion Target | Next Required Work |
|---|---|---|---|
| Product IA | Usable but incomplete | Normal users understand Workspace -> Library -> Folder/Document; admin work stays in Settings | Keep removing duplicate legacy entry points and misleading links when found |
| Workspace Home | Usable but incomplete | Home gives daily entry points without pretending to be a full activity inbox | Tighten widgets to backend-backed data and remove unsupported global activity affordances |
| Library / Folder / Document | Complete for current phase | Users can create, open, move, rename, delete, restore, and search content with stable route context | Runtime live-backend QA remains when browser/API QA is allowed |
| Editor | Complete for current phase | Open/edit/save/comment/share/context all work with clear loading/error/retry states | Runtime live-backend QA remains when browser/API QA is allowed |
| Comments | Complete for current phase | List/create/reply/resolve/reopen remain external resources, never stored in document JSON | Future work: edit/delete comments, mentions, browser QA |
| Share Drawer | Complete for current frontend phase | Daily document sharing closes in the drawer; Advanced permissions stays separate | Runtime live-backend QA remains when browser/API QA is allowed |
| Advanced Permissions | Complete for current frontend phase | Document-scoped advanced access management only; not daily sharing | Runtime live-backend QA remains when browser/API QA is allowed |
| Workspace Settings | Usable but incomplete | Members, Permissions, Security, Integrations are the single normal admin entry | Finish visual/copy cleanup and make unsupported capabilities clearly unavailable |
| Updates | Complete for current phase | Access/sharing/permission notification inbox only | Do not add document update/comments/mentions tabs until backend fanout contract exists |
| Activity | Frontend-only | Home/Editor activity previews reduce noise and stay separate from Updates | Later backend event classification before any aggregation table |
| Audit | Design only / partially backend-backed through permissions | Security/admin trail, not user inbox | Do not expose as notification center; design only when backend audit product surface is requested |
| Presence | Deferred | Ephemeral realtime collaboration state | Do not implement until editor stability, comments, files, and sharing are complete |
| Files / Attachments | Complete for local API provider | Upload -> file -> document attachment works through editor with permission-safe access | External object-storage/browser verification remains |
| Search | Complete for current backend phase | Users can find documents/folders and return to correct Library/Editor context | PostgreSQL smoke remains required when `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is configured |
| Organization Admin | Partial / deferred for normal flow | Organization admin exists only in backend/admin context | Do not place organization management in normal knowledge navigation |
| Full i18n | Deferred | Product-wide language quality after UX stabilizes | Do not prioritize until main flows are stable |
| Browser / E2E QA | Incomplete | Repeatable smoke for Home, Library, Editor, Share, Settings, Updates | Add only after flows stop shifting; honor user instruction when browser QA is skipped |

## Whole-System Pending Backlog

This backlog is the source of truth for finishing Northstar beyond the already-tightened frontend main path. Future rounds should choose from this list instead of expanding from chat memory.

Status labels:

- `Now`: should be handled in the current completion path.
- `Next`: important after current reliability/runtime gaps close.
- `Later`: valuable, but should not interrupt the daily knowledge-workspace path.
- `Explicit decision required`: conflict-marked or strategy-dependent; do not implement by assumption.

| Track | Priority | Pending Work | Guardrails |
|---|---|---|---|
| Runtime proof and acceptance | Now | Run live backend acceptance for Home, Library, Editor, Comments, Share Drawer, Advanced permissions, Settings, Updates, Search, and Files/Attachments when allowed. Verify API wiring, auth refresh, object storage/local file provider, and PostgreSQL smoke only when configured. | Do not claim pass from docs or static tests. Browser QA remains skipped when the user asks to skip it. PostgreSQL smoke requires `NORTHSTAR_POSTGRES_SMOKE_CONNECTION`. |
| Files and attachments | Next | External object-storage verification remains if production storage is configured. Local API runtime has passed upload session -> content upload -> complete/finalize -> idempotent finalize -> attachment list -> content read -> active-delete conflict -> remove relation -> delete file, Tiptap file-reference validation, and owner/editor/viewer/outsider permission matrix checks. Public file DTOs no longer expose storage internals. | Follow Phase 6 files contract. Do not store permanent URLs or attachment metadata as source of truth in Tiptap JSON. Do not delete files in a way that silently breaks active attachments. Do not claim S3/MinIO/object-storage integration until configured and tested. |
| Workspace Settings finalization | Now | Keep Settings as the single normal admin entry for Members, Permissions, Security, Integrations. Remove residual legacy/admin affordances, unsupported enabled-looking controls, and duplicate daily sharing paths. | Organization members/provisioning do not belong in normal Workspace Settings. Daily document sharing stays in the Editor Share Drawer. |
| Product IA and placement hygiene | Now | During every feature slice, check whether the action belongs in Home, Library, Editor, Share Drawer, Updates, or Settings. Remove or weaken confusing duplicate links when safe. | Do not create separate cleanup-only rounds unless the mismatch is too large for the current slice. No Space/Collection terminology in normal user paths. |
| Search and knowledge graph | Next | PostgreSQL full-text/trigram strategy is selected and implemented for backend search; document context filters related/backlink metadata through effective access; search index rebuild service exists for repair. | Do not introduce external search, semantic search, vector search, or graph database without a new explicit strategy decision. PostgreSQL smoke still requires `NORTHSTAR_POSTGRES_SMOKE_CONNECTION`. |
| Activity backend classification | Next | Add backend event classification design/implementation for Activity, Notification, Audit, and Presence outputs. Ordinary document updates remain Activity. High-signal events need explicit contracts before entering Updates. | Do not add aggregation tables before event classification is accepted. Do not put ordinary autosave/document updates into Updates. |
| Notification aggregation and preferences | Next | Implement future coalescing for low-signal activity, high-signal notification fanout, per-document follow/mute, frequency preferences, and digest only after contracts exist. | Current frontend grouping is display-only. Do not create `notification_aggregates` or `activity_aggregates` without an approved backend implementation slice. |
| Audit product surface | Next | Define and expose security/admin audit trail for permission, sharing, membership, and high-risk operations where backend data exists. | Audit is not a notification inbox. Do not mix audit into Updates by default. |
| Document versions and publishing | Acceptance polish | Backend exposes protected version list/detail, publish, unpublish, restore, compare, optimistic revision checks, version permissions, and activity events. Editor exposes document-local version history through the top bar and right-side Version Trail. Remaining work is live acceptance and deeper compare UX polish when browser QA is allowed. | Do not conflate `documents.revision`, `document_versions.version_no`, and user-facing version labels. Autosave revision is not a published version. Current version snapshots preserve content/outline/word count, not full title/tag history. |
| Comments v2 collaboration | Next | Add edit/delete comments, richer thread navigation, optional node/image/table comment support, mention contracts, and future comment notifications. | Comments remain external annotation resources. Mentions/notifications require explicit backend contracts. Do not write comments or mention metadata into Tiptap JSON. |
| Templates, import, export, and migration UX | Later | Add usable templates, import/export UI, transfer progress, validation, and recovery states for knowledge-base movement. | Preserve existing import/export contracts. Do not make import/export bypass permission or file rules. |
| Organization admin | Later / explicit decision required | Define organization profile, workspaces inventory, organization members, provisioning, global roles, and admin-only surfaces. | Keep Organization out of normal knowledge navigation. Do not do Organization provisioning/member mutation unless explicitly selected. |
| Enterprise IAM / SCIM / SSO hardening | Later / explicit decision required | Finish OIDC/SAML flows, secret management, SCIM bulk/filter/deactivate behavior, MFA recovery/admin reset, WebAuthn/passkeys, and operational IAM workflows. | Do not present enterprise controls as enabled beyond current backend support. Preserve permission V1 boundaries unless a permission V2 is explicitly started. |
| Presence and realtime collaboration | Later | Add ephemeral viewer/editor presence, realtime cursor/session state, and eventually CRDT/OT only if the product path requires it. | Presence is not Notification, Audit, or persistent Activity by default. Do not implement before editor/comments/files/sharing are stable. |
| Full i18n and localization quality | Later | Complete product-wide English/Chinese language coverage, date/number formatting, missing keys, and copy review after product surfaces stabilize. | Do not use full i18n as a blocker for current core flow completion. Avoid broad copy churn during unrelated slices. |
| Accessibility and responsive polish | Later | Keyboard paths, focus states, screen-reader labels, narrow viewport behavior, overflow handling, and density checks across main surfaces. | Keep changes tied to touched surfaces unless a dedicated accessibility pass is requested. |
| Production operations | Later | Deployment runbooks, observability, structured logs, metrics, error tracking, backups, restore drills, storage lifecycle, and upgrade smoke. | Do not claim production readiness from frontend build/test alone. |

## Backlog Use Rule

Future implementation rounds should:

1. Pick one `Now` or `Next` track unless the user explicitly selects a `Later` track.
2. Define one completion slice from that track.
3. Reuse existing backend contracts before proposing new contracts.
4. Keep frontend placement aligned with normal user expectations.
5. Update this plan when a backlog item changes status.

## Completion Checkpoint 2026-05-09

This checkpoint records the current state after the P0/P1/P2 frontend closure work. It is based on code changes and local validation in `apps/web`; it is not a live backend or browser acceptance result.

### Advanced Areas

| Area | Before | After | Validation |
|---|---|---|---|
| Editor / Comments reliability | Usable but incomplete | Complete for current phase | `npm test`, `npm run build` passed in the closure rounds |
| Comment API adapter | Illegal `fetch` invocation observed | Bound fetch path covered; list/create/reply/resolve/reopen stay external resources | Comment regression suite passed |
| Editor save/load states | Mixed raw errors and limited retry states | User-readable load/save/retry/conflict/error states | Frontend test/build validation |
| Editor attachments | UI incomplete | Image upload, attachment list, relation remove, and generic file upload wired through existing file APIs | Frontend test/build validation |
| Library mutations | Usable but incomplete | Folder/document create/move/rename/delete/archive/restore paths have clearer disabled reasons and mutation feedback | Library model tests passed |
| Live API error boundary | Errors could leak endpoint URLs or raw transport details | Shared frontend formatter for network/unconfigured/401/403/backend messages | API/model tests passed |
| Activity / Updates boundary | Risk of treating document updates as inbox notifications | ADR accepted; Updates remains access/sharing/permission only | Documentation plus frontend model tests |
| Share / permissions trust | Usable but incomplete | Share Drawer now owns daily invite/link/current-access work with explicit disabled reasons; Advanced permissions stays document-scoped | `npm test`, `npm run build` passed |
| Workspace Settings trust | Usable but incomplete | Settings keeps Members / Permissions / Security / Integrations as the normal admin entry, avoids legacy member/permission routes in primary tabs, and shows unsupported SCIM/token states as unavailable instead of half-enabled controls | Frontend test/build validation |
| Search route context | Usable but incomplete | Search preserves requested Library/Folder context in safe hash routes, canonicalizes legacy Discovery to Search, labels backend order honestly, and continues to use the current backend search contract | Frontend model/routing tests passed |
| Workspace Home trust | Usable but incomplete | Home keeps access/sharing widgets separate from document activity, avoids sending ordinary activity into notification-style panels, and links live member summaries to Members instead of a fake leaderboard | Frontend model/build validation |
| Files / Attachments frontend guardrails | Usable but not runtime-verified | Frontend rejects empty/oversized attachment uploads early, keeps local API upload-target support explicit, and leaves file content access permission-scoped through `/files/{fileId}/content` | `npm test`, `npm run build` passed |

### Still Not Verified

- Browser QA was intentionally not run because the user requested no browser QA.
- Live backend runtime acceptance was not run in this checkpoint.
- PostgreSQL smoke was not run because `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` was not set.
- Object storage integration was not run; file upload wiring is frontend/build validated only.
- Backend effective permission enforcement for file content and document attachments is not agent-verified in this checkpoint.

### Current Highest-Risk Items After Checkpoint

1. Live backend and object-storage runtime behavior may still reveal contract gaps in file upload/content access.
2. Workspace Settings still has the most product surface area and should be kept strict: no unsupported capability should look enabled.
3. Search backend strategy is now PostgreSQL full-text/trigram, but PostgreSQL smoke remains unverified until `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is available.
4. Browser/E2E acceptance is the main remaining proof gap, but should only run when the user allows it.

## Runtime Acceptance Attempt 2026-05-10

This checkpoint records the first non-browser runtime acceptance attempt. Detailed status is in `docs/agent/reports/runtime-acceptance-checklist-v1.md`.

| Area | Result | Notes |
|---|---|---|
| Backend restore/build/test | Passed | Restore passed; Release build passed; backend tests passed 252 tests. Debug build was blocked by the existing Visual Studio/API process lock, so Release output was used for validation. |
| Frontend test/build | Passed | `npm test` passed 207 tests; `npm run build` passed with the existing Vite large-chunk warning. |
| Local API health | Passed | Existing API at `https://localhost:7036/api/v1/health` and disposable acceptance API at `http://localhost:5136/api/v1/health` returned healthy responses. |
| Migration | Passed with explicit connection | Disposable `northstar_runtime_acceptance` database applied all migrations. Default migration command failed until `NORTHSTAR_DATABASE_CONNECTION` was explicitly set. |
| Auth acceptance identity | Blocked | Documented seed owner login returned 401. Register worked, but registered users had no workspace membership, so `/api/v1/bootstrap` returned 403. |
| Main app live acceptance | Blocked | Home, Library, Editor, Comments, Share, Settings, Updates, Search, and Files could not be accepted without a usable workspace-member account. |

Next required runtime work:

- Establish a documented acceptance identity path before continuing live API acceptance. The path should create or repair a workspace-member user through approved application code, not through ad hoc database edits.
- After the identity path is available, rerun the runtime checklist starting from bootstrap, Library map, document open/save, comments, share, Settings, Updates, Search, and Files/Attachments.

## Files / Attachments Runtime Acceptance 2026-05-11

This checkpoint records the first successful non-browser runtime acceptance pass for the Phase 6 local API file path. Detailed evidence is in `docs/agent/reports/runtime-acceptance-checklist-v1.md`.

| Area | Result | Notes |
|---|---|---|
| Acceptance identity | Passed | Seed owner login with the documented development password succeeded in the disposable runtime database after the current seeding repair in the working tree. |
| Bootstrap | Passed | The runtime returned the seeded workspace and active document needed for file acceptance. |
| Upload session flow | Passed | Create session, upload content, complete, and finalize all passed through `/api/v1/files/uploads/sessions`. |
| Finalize idempotency | Passed | Repeating finalize returned the same file id and attachment id. |
| Document attachment relation | Passed | The finalized file appeared in `GET /api/v1/documents/{documentId}/attachments`. |
| File content access | Passed for owner path | Authenticated owner could read content through `GET /api/v1/files/{fileId}/content`; broader role matrix remains covered by tests, not this runtime pass. |
| Delete safety | Passed | File delete returned `409` while attached, then `204` after attachment removal; content returned `404` after delete. |
| External object storage | Not run | Only the local API file provider was exercised. |
| Browser UI | Not run | User requested no browser QA. |

Follow-up:

- External object-storage verification remains if production storage is configured.
- Browser UI acceptance remains intentionally skipped while the user requests no browser QA.

## Files / Attachments Backend Acceptance Closure 2026-05-12

This checkpoint records the backend Phase 6 local API path after permission-depth closure.

| Area | Result | Notes |
|---|---|---|
| Public file DTOs | Passed | `FileDto` and nested attachment file DTOs do not expose storage provider, bucket, object key, or permanent URL fields. |
| Unattached file access | Passed by code/test path | Metadata/content/delete use workspace-scoped file actions (`file.download`, `file.delete`) instead of generic workspace view/edit. |
| Attached file access | Passed by focused API test | Files attached to restricted documents are no longer readable through workspace membership alone; document-scoped access is required. |
| Upload session document context | Passed by code/test path | `documentId` upload sessions require document-scoped `file.upload`, `attachment.create`, or `document.edit`. |
| Delete safety | Passed | Delete still refuses active attachments with `409 CONFLICT`; users must remove attachment relations first. |
| Tiptap file references | Passed for Phase 6 | Valid references sync `inline_image` attachments outside Tiptap JSON; missing/cross-workspace references are rejected. |
| File outbox persistence | Passed for Phase 6 | `file.finalized`, `document_attachment.created`, and `file.deleted` rows are written. Dispatcher remains out of scope. |
| External object storage | Partially covered | S3-compatible provider is implemented and presigned upload-target generation is tested; no live S3/MinIO endpoint was configured or exercised. |
| PostgreSQL smoke | Not run | Requires `NORTHSTAR_POSTGRES_SMOKE_CONNECTION`. |
| Browser UI | Not run | User requested no browser QA. |

Phase 6 files are acceptable for the local API-backed product path. Remaining work is operational or future-scope: live external object-storage verification, PostgreSQL smoke from the agent environment, and browser UI acceptance only if the user allows browser QA.
- Public file DTOs have been hardened so ordinary responses no longer expose `storageProvider`, `bucket`, or `objectKey`; storage internals remain in Domain/Infrastructure for provider access.

## File Outbox / Retention Closure 2026-05-12

This checkpoint closes the local Phase 6 file outbox gap without changing the public API or database schema.

| Area | Result | Notes |
|---|---|---|
| Outbox processor | Passed by focused API tests | `FileOutboxProcessor` processes due rows for `file.finalized`, `document_attachment.created`, and `file.deleted`. |
| Local object deletion | Passed by focused API tests | `file.deleted` invokes `IObjectStorage.DeleteObjectAsync` after DB soft-delete and marks the outbox row `published` on success. |
| Retry/failure state | Passed by focused API tests | Delete failures keep the event `pending` with retry metadata until max attempts, then mark it `failed`. |
| Hosted processing | Implemented | `FileOutboxHostedService` runs the processor in-process outside the `Testing` environment. |
| Public API surface | Unchanged | No public route was added for outbox processing. |
| Database schema | Unchanged | Existing `file_outbox_events` status/retry fields are reused; no migration was added. |
| External object storage | Not implemented in this checkpoint | No S3/MinIO/cloud provider was added in the outbox checkpoint. A later checkpoint adds S3-compatible provider support but still requires live integration verification. |
| Browser UI | Not run | User requested no browser QA. |

Phase 6 local files now have a complete backend lifecycle from upload/finalize through attachment safety, soft-delete, outbox processing, and local object cleanup. Production storage/provider work remains a separate operational track.

## S3-Compatible Object Storage Provider 2026-05-12

This checkpoint adds an Infrastructure-only S3-compatible provider behind the existing `IObjectStorage` contract.

| Area | Result | Notes |
|---|---|---|
| Provider boundary | Passed by build | `S3ObjectStorage` is isolated to Infrastructure; Application and Domain still depend only on `IObjectStorage`. |
| Upload target | Passed by focused API test | `Files:StorageProvider = S3` returns a presigned `PUT` target with required content-type header. |
| API contract | Unchanged | Existing upload session, upload content, complete, finalize, file content, and delete routes are unchanged. |
| Local provider | Preserved | Default configuration remains `Local`; local runtime path is not changed. |
| Live S3/MinIO integration | Blocked | Acceptance test exists; Docker CLI is installed but the Docker engine was unavailable, and no external endpoint/credentials were configured. |
| Browser UI | Not run | User requested no browser QA. |

External object storage is now code-supported and has a repeatable live acceptance test, but it is not runtime-accepted until a real S3/MinIO endpoint is reachable and the acceptance test passes.

## Search / Knowledge Graph Backend Closure 2026-05-11

Detailed strategy is recorded in `docs/agent/reports/search-knowledge-graph-backend-strategy-v1.md`.

| Area | Result | Notes |
|---|---|---|
| Search strategy | Complete for current backend phase | Production path uses PostgreSQL full-text search with `pg_trgm` fallback over `document_search_index`; InMemory/test fallback remains contains-based. |
| Search index repair | Complete for current backend phase | Internal `ISearchIndexMaintenanceService` can rebuild missing rows and remove stale archived/deleted rows. |
| Knowledge graph context | Complete for current backend phase | Related documents and backlinks are filtered through effective document access before metadata is returned. |
| PostgreSQL smoke | Not run | Smoke assertions now cover `pg_trgm`, generated `search_vector`, and full-text matches, but smoke still requires `NORTHSTAR_POSTGRES_SMOKE_CONNECTION`. |

Follow-up:

- Run PostgreSQL smoke when `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is available.
- Do not introduce external search, semantic search, vector search, or graph database without a new explicit strategy decision.

## Product Boundaries

### Organization

Organization is a backend and administrative boundary. It should not be shown as a normal knowledge-library navigation layer.

Complete when:

- Organization settings are clearly separate from workspace work.
- Organization members are not mixed into Workspace members.
- Organization provisioning and mutation remain deferred unless explicitly selected.

### Workspace

Workspace is the collaboration, member, permission, security, and notification boundary.

Complete when:

- Left workspace rail contains only Home, Library, Search, Updates, Settings.
- Members are managed through Workspace Settings.
- Permissions, Security, and Integrations are managed through Workspace Settings.
- Updates describes access/sharing/permission notifications only.

### Library

Library is the user-facing name for backend Space.

Complete when:

- Library pages expose Folder/Document organization, not backend Space terminology.
- Library settings are summary and content-organization context, not workspace member management.
- Library/folder/document routes preserve context after create, move, rename, delete, restore, and search.

### Folder

Folder is the user-facing name for backend Collection.

Complete when:

- Users can manage folders without seeing Collection terminology in normal UI.
- Folder operations provide disabled reasons when backend constraints block action.
- Folder links from search/activity/backlinks resolve through safe hash routes.

### Document

Document is the editing, comments, share drawer, document info, and document activity context.

Complete when:

- Document page does not act as a workspace admin hub.
- Document Share opens the drawer by default.
- Advanced permissions are document-scoped.
- Comments and activity are loaded as external resources.
- Tiptap JSON remains document content plus structural identifiers only.

## Workstream Order

### P0: Core Reliability

Goal: make the daily document path dependable.

Status: complete for current frontend phase.

Finish criteria:

- Editor opens real documents and recovers from API loading failures.
- Save states are clear and do not hide backend failures.
- Comments load without illegal `fetch` invocation.
- Comment create/reply/resolve/reopen states are honest.
- Document Share Drawer can complete normal invite/link/grant tasks or explain disabled capability.
- No normal document path links users into workspace admin unless explicitly advanced.

Remaining proof:

- Browser/live backend acceptance when allowed.

### P1: Content Management Closure

Goal: make Library / Folder / Document feel like a real knowledge base.

Status: complete for current frontend phase.

Finish criteria:

- Create folder/document routes focus the new item.
- Move/rename/delete/restore actions preserve Library context.
- Empty/error/forbidden/API-unconfigured states are consistent.
- Search results navigate to Document or Folder with safe hash routes.
- No Space/Collection terminology appears in normal UI.

Remaining proof:

- Browser/live backend acceptance when allowed.

### P2: Files and Attachments Productization

Goal: make file/image attachment part of editing, not a backend-only feature.

Status: usable but not runtime-verified.

Finish criteria:

- Editor upload uses the Phase 6 upload session -> file -> attachment flow.
- Inline image/file references validate through backend and do not store attachment state in Tiptap JSON.
- File access goes through API permission checks.
- Delete behavior does not silently break active attachments.
- UI clearly distinguishes unsupported storage/provider features.

Completed frontend bridge:

- Editor image upload uses the existing upload session -> file -> document attachment flow.
- Editor Info attachments can list current attachments.
- Removing an attachment removes only the document attachment relation.
- Generic file upload is available from the Editor Info attachments panel.

Remaining proof:

- Live backend + object storage verification.
- Backend file permission behavior verification.
- Browser acceptance when allowed.

### P3: Sharing and Permission Trust

Goal: keep daily sharing easy and advanced access safe.

Status: complete for current frontend phase.

Finish criteria:

- Share Drawer remains daily sharing surface.
- Advanced permissions remains document-scoped.
- Workspace Settings remains admin surface for members, groups, access policy, security, and integrations.
- Public links remain dedicated share-link API behavior.
- Generic permission policy does not expose direct public `linkMode` mutation.

Completed frontend trust pass:

- Share Drawer invite/link actions have explicit disabled reasons.
- Email invites expose only viewer/commenter access.
- Workspace member search failures do not show fake user results and still allow email invite.
- Public links require the dedicated share-link API path and a future expiry before creation.
- Advanced permissions copy presents workspace access as an inherited source, not as the daily sharing path.

Remaining proof:

- Browser/live backend acceptance when allowed.

### P4: Settings Closure

Goal: make Settings a reliable management tool, not a set of half-working dashboards.

Finish criteria:

- Tabs are fixed: General, Notifications, Members, Permissions, Security, Integrations.
- Unsupported capabilities are unavailable/deferred with clear reason.
- Members table and mutations are workspace-scoped only.
- Permissions tab contains workspace groups, access requests, grants, and policy summaries without duplicating daily document sharing.
- Integrations/Security do not pretend SSO/SCIM features exist beyond backend support.

### P5: Activity and Notification Backend Preparation

Goal: prepare the backend event model without prematurely building aggregation.

Finish criteria:

- Backend event classification design identifies Activity, Notification, Audit, and Presence outputs.
- Ordinary document updates are classified as Activity.
- High-signal events are explicitly contracted before entering Updates.
- Outbox/coalescing design is ready, but no aggregation table is created until event classification is accepted.

Required source:

- `docs/agent/reports/activity-notification-boundary-decision.md`.

### P6: Search, Context, and Knowledge Graph Quality

Goal: improve retrieval without overbuilding.

Finish criteria:

- Search uses current backend contract honestly.
- Related documents, backlinks, document map, and version trail show real data where available and honest empty/unavailable states otherwise.
- Search improvements do not silently switch engine strategy.

Warning:

- Search implementation is conflict-marked. Do not introduce PostgreSQL `tsvector`, trigram, or external search without explicit selection.

### P7: Production Readiness and QA

Goal: make completed paths repeatably verifiable.

Finish criteria:

- Frontend tests cover model/routing/state boundaries.
- Backend tests cover changed contracts and permission checks.
- Browser/E2E smoke covers Home, Library, Editor, Share Drawer, Settings, Updates when allowed.
- PostgreSQL smoke is reported only when `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is set and the smoke command actually ran.

## Per-Round Selection Rules

Each implementation round should choose one completion slice.

Allowed shape:

- One primary user flow.
- At most two adjacent surfaces.
- One clear completion target.
- Explicit non-goals.
- Validation matched to touched code.

Avoid:

- Combining backend architecture, UI redesign, route migration, and permission semantics in one round.
- Changing data model just to satisfy frontend language.
- Reintroducing legacy routes as visible product structure.
- Implementing unsupported backend capabilities as polished frontend mock behavior.
- Running browser QA when the user explicitly asks not to.

Every round should answer:

1. Which completion plan area is being advanced?
2. What must be true for this slice to be considered done?
3. Which backend contract is being reused?
4. What is explicitly not being changed?
5. Which tests or builds prove the slice?
6. Does each user-facing action live on the surface where a normal user would expect it, with a clear result or disabled reason?

## Non-Negotiable Product Rules

- No new Library backend entity.
- No database table renames from `spaces` or `collections`.
- No backend route rename from Space/Collection solely for frontend semantics.
- No comments, files, permissions, notifications, audit, activity, or presence in Tiptap document JSON.
- No public-link widening into protected APIs.
- No fake all-purpose notification center.
- No ordinary autosave notification fanout.
- No Organization management in the normal document workflow.

## Current Highest-Risk Items

1. File upload/content access still needs live backend and object-storage verification.
2. Settings has improved, but any residual legacy/admin surface can recreate IA confusion.
3. Activity display aggregation is frontend-only and must not be mistaken for backend notification architecture.
4. Search and knowledge graph features can easily expand beyond current contract.
5. Browser/E2E QA remains the largest proof gap while browser QA is intentionally skipped.

## Recommended Next Implementation Order

### Slice 1: Runtime Acceptance Without Browser Automation

Purpose: prove the current code against a running backend without using browser QA.

Status: attempted on 2026-05-10; blocked after backend/frontend validation by missing usable workspace-member acceptance identity.

Scope:

- Start backend and frontend with configured API wiring.
- Verify auth, bootstrap, Library map, document open/save, comments, share drawer, advanced permissions, Settings, Updates, Search, and Files/Attachments through API-backed checks where possible.
- Fill out `docs/agent/reports/runtime-acceptance-checklist-v1.md` with `Pass`, `Fail`, `Blocked`, or `Not run`.

Non-goals:

- No browser QA unless explicitly allowed.
- No backend architecture changes.
- No new product capability.

### Slice 2: Files / Attachments Runtime Closure

Purpose: close the largest remaining backend-backed product risk.

Scope:

- Verify upload sessions, content upload, complete/finalize, document attachment relation, attachment list, remove relation, and file content access.
- Fix frontend/backend contract mismatches discovered in the existing Phase 6 files flow.
- Confirm file permission behavior where tests or runtime checks can verify it.

Non-goals:

- No permanent file URLs.
- No file metadata source of truth in Tiptap JSON.
- No object-storage claim unless actually tested.

### Slice 3: Settings Unsupported-Capability Audit

Purpose: prevent Settings from looking more complete than backend support.

Scope:

- Audit Members, Permissions, Security, and Integrations for enabled-looking controls without real support.
- Keep unsupported SCIM/SSO/security controls unavailable or deferred.
- Remove ordinary UI paths to legacy/admin surfaces if any remain.

Non-goals:

- No Organization provisioning/member mutation.
- No enterprise IAM expansion.

### Slice 4: Search / Knowledge Graph Strategy Decision

Purpose: choose the retrieval direction before adding depth.

Scope:

- Inventory current search contract and frontend knowledge-graph surfaces.
- Decide whether to keep lightweight search or explicitly move to PostgreSQL full text, trigram, or external search.
- Only after selection, improve related documents, backlinks, document map, and version trail.

Non-goals:

- No silent search-engine upgrade.
- No external search dependency without explicit approval.

### Slice 5: Activity / Notification Backend Preparation

Purpose: move from frontend display grouping toward durable event architecture.

Scope:

- Implement or prepare backend event classification for Activity, Notification, Audit, and Presence outputs.
- Keep ordinary document updates in Activity.
- Define high-signal document notification contracts before any fanout.

Non-goals:

- No aggregation table until classification is accepted.
- No ordinary document update notification fanout.

## Update Rule

After any round materially changes completion status, update this plan or add a short follow-up report that states:

- Area advanced.
- Status before.
- Status after.
- Validation run.
- Remaining blockers.

Do not let completion knowledge live only in chat history.
