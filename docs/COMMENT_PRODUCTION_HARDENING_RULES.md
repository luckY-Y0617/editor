# Comment Production Hardening Rules

Status: implemented for `comment v1 / beta-complete` in the web editor
runtime. See `docs/COMMENT_V1_BETA_COMPLETE.md` for the version definition.

This document defines reliability, failure-state, and QA expectations for
taking the current comment system from beta architecture to production-ready
behavior. It does not introduce new comment anchoring architecture.

## Core Principle

Harden the existing system. Do not expand the product surface unless it directly
supports reliability, recovery, or verification.

Comments remain:

- external annotation resources;
- rendered through ProseMirror decorations;
- persisted through comment-specific APIs;
- relocated only as runtime load-time reconstruction;
- mapped during the current session by `DecorationSet.map`.

## Hard Boundaries

Do not change these boundaries in the hardening spike:

- no comment marks;
- no comment nodes;
- no `threadId` in Tiptap document JSON;
- no runtime ranges in document JSON;
- no `runtimeMatch` or relocation result in document JSON;
- no comment state in frontend storage/export payloads;
- no comment persistence through `PATCH /documents/:id`;
- no backend anchor rewriting;
- no cross-user collaboration semantics;
- no mentions, notifications, or permission model expansion;
- no image/table/node comments.

## Operation Policy

Prefer predictable recovery over aggressive optimism.

Implementation details:

- Comment list loading is represented by a document-scoped runtime load state
  (`idle`, `loading`, `loaded`, `error`) in `KnowledgeEditorPage`.
- Each list request receives a monotonically increasing request id. A response
  may update visible state only when it is still the latest request for the
  active document.
- Create-thread state remains attached to the pending in-memory composer. The
  composer stores only transient `body`, `isSubmitting`, and `error` fields and
  is never written to document content, local storage, export payloads, or
  comment APIs.
- Resolve/reopen use confirmed-update behavior. The thread is replaced only
  after the repository returns the canonical thread. Failures keep the prior
  thread unchanged and show an inline error.
- The current UI does not expose add-message composition. Repository-level
  add-message behavior is still tested: failures do not mutate the thread or
  anchor, and successes append only the returned server thread/message.

### List Threads

- Loading is scoped by `documentId`.
- Only the latest request for the active document may update visible state.
- Switching documents must clear pending composer and active thread UI state.
- A failed load should show a retry affordance in the Comments panel.
- A failed load must not show comments from another document.

### Create Thread

- Do not create an optimistic thread before the server returns a canonical
  thread id.
- Disable duplicate submit while the request is in flight.
- Keep the composer body and captured anchor if creation fails.
- Show an inline, recoverable error near the composer.
- On success, add exactly the returned thread, clear composer state, select the
  thread, and keep the Comments tab open.

### Add Message

- Disable duplicate submit while the request is in flight.
- Keep the draft body if add-message fails.
- Append only the returned server message/thread on success.
- Do not modify the anchor while adding messages.

### Resolve And Reopen

- Resolve/reopen may be optimistic only if the previous thread state is kept and
  can be restored on failure.
- Disable repeated lifecycle action clicks while the request is in flight.
- On failure, roll back to the previous thread state and show a recoverable
  inline error.
- Resolve/reopen must not modify comment messages, anchors, document content, or
  runtime ranges.

## Request Race Rules

Every async comment operation must be guarded against stale results:

- document-scoped loads need a request token or equivalent stale-result guard;
- stale responses must not overwrite newer document state;
- retry actions must preserve document isolation;
- failed requests must not clear unrelated successful data.

## UI State Requirements

The Comments panel needs explicit states:

- loading comments;
- load failed with retry;
- composer submitting;
- create failed with retry/edit preserved;
- message submitting;
- message failed with draft preserved;
- resolve/reopen pending;
- lifecycle action failed and rolled back;
- empty state when no comments exist;
- stale/orphaned anchor states from runtime plugin state.

Do not use only console errors for user-visible failures.

## E2E QA Requirements

Add browser-level regression coverage where practical for:

- create comment, reload, persisted thread appears;
- create comment, reload, relocation initializes the highlight;
- insert text before a comment, save/reload, relocation keeps it attached;
- delete the anchored block, save/reload, thread becomes orphaned;
- resolve/reopen, reload, lifecycle state persists;
- create failure keeps composer body and anchor;
- resolve/reopen failure rolls back UI;
- document switching never leaks comments across documents;
- storage/export payloads remain comment-free;
- document content JSON contains structural `blockId` but no comment metadata.

There is currently no Playwright or equivalent browser E2E dependency in
`apps/web`. Automated coverage is provided by the local comment regression
runner plus TypeScript/Vite build validation. The browser-level flows above are
documented as manual QA in `docs/COMMENT_UX_SPIKE_QA.md` until an E2E harness is
added.

## Performance Guardrails

Relocation must remain load-time only.

Hardening should verify:

- relocation does not run on every editor transaction;
- large documents with many comments do not noticeably block typing;
- decoration rendering still shows all non-orphaned highlights;
- request retries do not duplicate threads or messages;
- panel debug JSON does not force expensive recomputation on each keystroke.

Implemented guardrails:

- `knowledge-comment-decorations` continues to call relocation only when
  `setThreads` initializes a thread without a mapped runtime range. Normal
  editor transactions keep using `DecorationSet.map`.
- Regression coverage verifies that a relocated range is later maintained by
  transaction mapping while the original relocation result object is reused.
- Panel debug JSON is memoized per thread from the current anchor,
  runtime-match, and relocation result inputs instead of being recomputed for
  unrelated renders.

## Accessibility And Interaction

Minimum interaction checks:

- composer textarea receives focus reliably;
- submit/cancel/retry buttons are keyboard reachable;
- pending buttons expose disabled state;
- error text is visible near the failed action;
- Escape still cancels only the active composer;
- selecting/clicking highlights does not open composer;
- right-panel thread navigation still uses plugin runtime ranges.

Implemented interaction states:

- Comments panel shows loading, load failure with Retry, and empty states.
- Composer submit disables duplicate submit, exposes `aria-busy`, keeps body
  and captured anchor on failure, and reports an inline `role="alert"` error.
- Resolve/reopen buttons disable while the request is in flight, expose
  `aria-busy`, and show inline recoverable failures.
- Escape still cancels only the active composer when it is not submitting.

## Testing Requirements

Add or extend tests for:

- repository/http failure handling;
- stale document-load result suppression;
- create success/failure state transitions;
- add-message success/failure state transitions;
- resolve/reopen optimistic rollback or confirmed-update behavior;
- no duplicate submits;
- document isolation under concurrent loads;
- storage/export cleanliness;
- relocation not persisted;
- existing `npm test` comment regression suite remains green.

When practical, add browser/E2E tests for the critical user flows listed above.

## Documentation Requirements

Update:

- `docs/COMMENT_UX_SPIKE_QA.md`
- `docs/COMMENT_PERSISTENCE_CONTRACT.md` only if persistence boundaries change
  in wording, not behavior
- this document with implementation details discovered during hardening

## Out Of Scope

- Editing existing messages.
- Deleting messages or threads.
- Mentions.
- Notifications.
- Fine-grained comment permissions.
- Audit timeline.
- Collaboration conflict semantics.
- Image/table/node comments.
- Backend anchor rewriting.
