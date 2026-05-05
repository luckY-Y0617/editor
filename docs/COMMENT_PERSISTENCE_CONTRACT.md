# Comment Persistence Contract

This contract defines the boundary for backend comment persistence. It preserves
the frontend spike architecture: comments are an external annotation layer over
document content, not part of the Tiptap document itself.

## Current Status

The comment system is defined as `comment v1 / beta-complete`. See
`docs/COMMENT_V1_BETA_COMPLETE.md` for the version boundary and follow-up
policy.

Backend-backed comment persistence is implemented behind the frontend
`CommentRepository` contract. The repository exposes comment-specific
operations:

- `listThreads(documentId)`
- `createThread(documentId, { anchor, body })`
- `addMessage(documentId, threadId, { body })`
- `resolveThread(documentId, threadId)`
- `reopenThread(documentId, threadId)`

This repository boundary must not accept or return Tiptap document content JSON,
runtime mapped ranges, `DecorationSet`, `runtimeMatch`, `activeThreadId`, or
pending composer state.

The web app includes an HTTP-backed repository for API documents and keeps the
local/mock in-memory repository as a temporary local-dev and test fallback. The
fallback remains session-only, keyed by `documentId`, and does not use
`localStorage`, document export payloads, or `PATCH /documents/:id`.

Production hardening adds frontend-only loading, retry, submit, and lifecycle
error state around this repository boundary. Those states are user-interface
recovery state only. They are not part of comment persistence, document content,
storage, export, or backend anchor data.

Backend endpoints implement the same repository semantics: comment threads are
comment resources, create/add-message/resolve/reopen are comment-specific
mutations, and document content saves remain independent.

Stable `blockId` is implemented as document structural identity for
comment-addressable textblocks. Load-time comment anchor relocation is
implemented as runtime reconstruction in the frontend decoration plugin. Block
identity rules live in `docs/BLOCK_IDENTITY_SCHEMA.md`; relocation rules live in
`docs/COMMENT_ANCHOR_RELOCATION_POLICY.md`.

## Architecture Principles

- Comment threads are stored outside `KnowledgeDocument.content`.
- Tiptap document JSON must not contain comment marks, `threadId`, anchor state,
  runtime match state, or UI metadata.
- The editor renders comment highlights from ProseMirror decorations produced by
  the `knowledge-comment-decorations` plugin.
- Plugin state is the source of truth for current mapped ranges during an editor
  session.
- `CommentAnchorV1.pm.from/to` is only a base revision position snapshot. It is
  not the current runtime range after edits.
- Runtime mapped ranges are derived at load and then maintained with transaction
  mapping. They are not persisted.

## Persisted Data

Persist comment thread records in comment-specific backend storage. A persisted
thread includes:

- `id`: stable comment thread id.
- `documentId`: id of the document the thread belongs to.
- `status`: `open` or `resolved`.
- `anchor`: the full `CommentAnchorV1` payload.
- `messages`: ordered comment messages.
- `createdAt`: thread creation timestamp.
- `updatedAt`: thread update timestamp.
- `resolvedAt`: nullable timestamp for resolved threads.

Persist each message with:

- `id`: stable message id.
- `threadId`: owning thread id.
- `body`: message body.
- `author`: author id/name snapshot or backend user reference.
- `createdAt`: message creation timestamp.
- `updatedAt`: nullable update timestamp.
- `deletedAt`: nullable soft-delete timestamp if message deletion is later added.

## Data That Must Not Be Persisted

Do not persist session-only runtime or UI data:

- Current runtime mapped ranges.
- `DecorationSet`.
- `runtimeMatch`.
- `matchResultByThreadId`.
- `anchorStatusByThreadId` as runtime truth.
- `activeThreadId`.
- Pending composer state.
- Editor selection state.
- Sidebar tab state.
- Local-only panel expansion/debug state.
- Comment list loading/error/retry state.
- Composer `isSubmitting` or inline create error state.
- Resolve/reopen pending or inline error state.

Anchor status may be shown in the UI during a session, but the backend should
not treat a runtime `active`, `stale`, or `orphaned` value as durable position
truth. Those values are recomputed from the loaded document and loaded anchors.
The API may return `anchorStatus: "active"` for frontend DTO compatibility, but
that value is a non-authoritative default, not stored runtime truth. Runtime
anchor status still comes from the `knowledge-comment-decorations` plugin and
remains a session-only overlay.

## Clean Document JSON

`KnowledgeDocument.content` remains the canonical document payload. It must stay
portable and comment-free because:

- Document save/export should not silently include review metadata.
- Comments should be lifecycle-managed independently from document edits.
- Comment APIs need independent audit and sync boundaries while reusing existing
  workspace/document permissions.
- Decoration rendering can be rebuilt from persisted comments without mutating
  document schema.

Do not add comment metadata:

- comment marks
- comment nodes
- `threadId` attrs
- `data-comment-thread-id`
- `anchorStatus`
- `runtimeMatch`

Stable `blockId` is allowed only as document structural identity, governed by
`docs/BLOCK_IDENTITY_SCHEMA.md`. It may appear as `attrs.blockId` on ProseMirror
textblock nodes only, and it must not encode a comment thread or runtime anchor
state.

## Base Snapshot Versus Runtime Range

`CommentAnchorV1.pm.from/to` records where the text range lived in the base
revision used when the thread was created or loaded. During the current editor
session:

- initialize the runtime range from `anchor.pm.from/to` only when loading the
  matching base document revision;
- maintain current positions through `tr.mapping` inside
  `knowledge-comment-decorations`;
- read current ranges from plugin state, not from `anchor.pm.from/to`;
- never write mapped runtime ranges back into document JSON.

## Runtime Reconstruction

On comment load:

1. Load the document content.
2. Load persisted comment threads for the active document.
3. Pass the threads into `knowledge-comment-decorations`.
4. Initialize runtime ranges through the load-time relocation policy. A valid
   `CommentAnchorV1.pm.from/to` snapshot may still be used, but stale snapshots
   are reconstructed with `blockId`, text offsets, and quote evidence.
5. Build ProseMirror decorations for non-orphaned ranges.
6. Maintain ranges with `DecorationSet.map(tr.mapping, tr.doc)` on each editor
   transaction.
7. Recompute `runtimeMatch` and anchor status from the current mapped text.

Right panel thread clicks must continue to read the current runtime range from
plugin state. Orphaned threads must not attempt invalid editor selection.

## Stale And Orphaned Expectations

- `active`: current mapped range is valid and the anchor matching policy says the
  text is still attached.
- `stale`: current mapped range is valid but the mapped text may no longer be the
  intended anchor.
- `orphaned`: range is invalid, empty, out of bounds, deleted, or cannot produce
  usable mapped text.

Load-time relocation is governed by
`docs/COMMENT_ANCHOR_RELOCATION_POLICY.md`. Relocation output remains runtime
state and must not be written to Tiptap document content, comment persistence,
storage, or export payloads.

## Stable Block Identity

Stable `blockId` is implemented as the document schema step used by load-time
comment anchor relocation. The current editor-session runtime mapping model is
unchanged after relocation initialization. Current-session mapping is handled by
ProseMirror transactions and
`DecorationSet.map`.

The stable block identity implementation follows `docs/BLOCK_IDENTITY_SCHEMA.md`.
The key rule is that block identity is assigned to ProseMirror textblock nodes,
not visual wrapper/container nodes. The id lives at `attrs.blockId` on textblock
nodes and is generated or repaired for new blocks, split blocks, pasted content,
imported content, and migrated old documents. Duplicate ids keep the first
occurrence and regenerate later duplicates. `CommentAnchorV1.block.start/end`
now includes `blockId` when the nearest textblock ancestor has one.

`blockId` must not be implemented as a comment mark, comment node, `threadId`, or
comment-specific decoration artifact. It belongs to document structural identity.

Completed and remaining order:

1. Comment backend persistence with comment-specific APIs.
2. Stable block identity design and document schema migration for `blockId`.
3. Load-time comment anchor relocation using `blockId` first, then
   path/textOffset/quote fallback, per
   `docs/COMMENT_ANCHOR_RELOCATION_POLICY.md`.

## Backend API Boundary

Backend comment APIs are comment-specific and nested under documents:

- `GET /api/v1/documents/{documentId}/comments`
- `POST /api/v1/documents/{documentId}/comments`
- `POST /api/v1/documents/{documentId}/comments/{threadId}/messages`
- `POST /api/v1/documents/{documentId}/comments/{threadId}/resolve`
- `POST /api/v1/documents/{documentId}/comments/{threadId}/reopen`

Do not piggyback comment persistence on `PATCH /documents/:id`. Document saves
and comment lifecycle changes are separate domains.

Backend implementations preserve the repository contract:

- list only threads for the requested document;
- create exactly one open thread from the submitted body and supplied
  `CommentAnchorV1`;
- append messages without modifying anchors;
- resolve/reopen only lifecycle status timestamps;
- never store or return runtime mapped ranges, `DecorationSet`, `runtimeMatch`,
  `activeThreadId`, or pending composer state.

## Out Of Scope

- Persisting relocation output or rewriting anchors after relocation.
- Collaboration-aware CRDT/OT relocation.
- Multiplayer collaboration.
- New comment-specific permissions beyond the existing workspace/document
  permission boundary.
- Mentions.
- Notifications.
- Comment-specific blockId injection.
- Image comments.
- Table comments.
- Node comments.
