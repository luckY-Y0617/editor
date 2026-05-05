# Comment V1 Beta-Complete Definition

Status: `comment v1 / beta-complete`

This document marks the first comment system version as complete enough to stop
feature expansion and move into bug fixing, manual browser QA, and integration
feedback.

## Included In V1

- Comments are external annotation resources persisted through comment-specific
  APIs.
- Tiptap document JSON remains clean of comment marks, comment nodes,
  `threadId`, runtime ranges, `runtimeMatch`, relocation results, and comment
  metadata.
- ProseMirror textblock nodes may contain structural `attrs.blockId`.
- Stable `blockId` is assigned only to ProseMirror textblock nodes.
- Persisted `CommentAnchorV1` may naturally include start/end textblock
  `blockId`.
- Load-time anchor relocation reconstructs runtime ranges from
  `blockId`/textOffset/quote evidence.
- Current-session edits keep comment ranges mapped with `DecorationSet.map`.
- Runtime anchor status, match results, relocation results, active thread, and
  pending composer state remain frontend runtime state only.
- Comments panel supports loading, load failure retry, empty state, create
  submit pending/failure recovery, and resolve/reopen pending/failure recovery.
- Create thread waits for the canonical server thread id before rendering a new
  thread.
- Resolve/reopen use confirmed-update behavior and do not mutate anchors,
  messages, document content, runtime ranges, `runtimeMatch`, or relocation
  output.
- Storage and export payloads remain comment-free.

## Explicitly Not Included In V1

- Cross-revision anchor rewriting or persisted relocated ranges.
- Backend anchor rewriting.
- Collaboration, CRDT, or OT relocation semantics.
- Comment marks or comment nodes.
- Image, table, media, or generic node comments.
- Mentions, notifications, fine-grained comment permissions, audit timeline,
  edit/delete comments, or advanced overlap picking UI.
- Automated browser E2E harness. Browser-level coverage is documented as manual
  QA in `docs/COMMENT_UX_SPIKE_QA.md`.

## Validation Baseline

The V1 baseline is considered valid when these pass in `apps/web`:

- `npm test`
- `npm run build`

At the time this version was defined, the comment regression suite passed with
73 tests. The build passed with the existing Vite large-chunk warning.

Backend tests are not required for this version marker unless comment API DTOs,
server behavior, or persistence semantics change.

## Follow-Up Policy

Treat new comment work after this point as one of:

- bug fix against the V1 behavior;
- manual/browser QA feedback;
- backend integration issue without changing semantics;
- explicitly scoped V2 work.

Do not silently expand V1 scope.
