# Comment UX Spike QA

Status: `comment v1 / beta-complete`. The canonical version definition lives in
`docs/COMMENT_V1_BETA_COMPLETE.md`.

Use the local editor route and keep DevTools available for inspecting exported or
saved document JSON. When an API document is loaded, comments should persist
through the comment-specific backend endpoints. Mock/local documents may still
use the in-memory repository fallback for local development and tests.

## Automated Regression Tests

- Run `npm test` in `apps/web`.
- Confirm anchor matching tests cover exact, minor, major, short text, empty
  range, invalid range, and normalizer behavior.
- Confirm decoration plugin tests cover multiple simultaneous highlights,
  selected class behavior, resolved/stale/orphaned rendering, transaction
  mapping, and thread data attributes.
- Confirm composer model and selection UX tests cover empty body prevention,
  submitted body preservation, captured anchor use, composer suppression, and
  existing-highlight overlap blocking.
- Confirm block identity tests cover old JSON migration, id preservation,
  duplicate and invalid id repair, textblock-only assignment, split/paste
  repair, comment anchor `blockId`, and clean content JSON.
- Confirm relocation tests cover blockId/textOffset reconstruction, quote
  fallback, ambiguous matches, deleted blocks, old anchors without blockId,
  cross-textblock anchors, and plugin initialization.
- Confirm production hardening tests cover document-scoped load tokens, stale
  load-result suppression, create failure state, duplicate submit prevention,
  resolve/reopen confirmed-update state, HTTP failure surfacing, add-message
  failure behavior, comment-specific HTTP endpoints, storage/export cleanliness,
  and relocation-not-every-transaction guardrails.
- Run `npm run build` in `apps/web` after tests pass.

## Production Hardening Rules

- Review `docs/COMMENT_PRODUCTION_HARDENING_RULES.md` before implementing
  loading, error, retry, optimistic update, or E2E hardening work.
- Confirm hardening does not change the core architecture: comments remain
  external, runtime ranges remain plugin state, and relocation remains
  runtime-only.
- Confirm user-visible failures have recoverable UI states, not only console
  errors.
- Confirm async request results are document-scoped and stale responses cannot
  overwrite the active document's comments.
- Confirm the Comments panel shows a loading state while comment threads are
  requested for the active document.
- Force a list failure and confirm the panel shows a Retry action and does not
  show comments from another document.
- Retry after a list failure and confirm the retry still loads only the active
  document's threads.
- Confirm switching documents clears the pending composer and selected comment
  thread.
- Force create failure and confirm the composer body, excerpt, and captured
  anchor remain visible with an inline recoverable error.
- Confirm double-clicking Comment while create is in flight creates no duplicate
  thread.
- Force resolve/reopen failure and confirm the prior thread lifecycle state is
  still shown with an inline recoverable error.
- Confirm resolve/reopen success replaces only the returned server thread and
  leaves messages, anchors, document content, runtime ranges, runtime matches,
  and relocation results unchanged.
- If add-message UI is introduced later, apply the same submit disabling,
  draft-preservation, and returned-thread-only update rules.

## Persistence Contract

- Review `docs/COMMENT_PERSISTENCE_CONTRACT.md` before any backend comment
  persistence work.
- Confirm the frontend still uses the `CommentRepository` boundary.
- Confirm backend persistence stores comment threads and anchors outside
  `KnowledgeDocument.content`.
- Confirm runtime mapped ranges, `DecorationSet`, `runtimeMatch`,
  `activeThreadId`, and pending composer state remain session-only.
- Confirm comment APIs do not piggyback on `PATCH /documents/:id`.
- Confirm `blockId` is document structural identity on ProseMirror textblock
  nodes only, not comment metadata.
- Review `docs/COMMENT_ANCHOR_RELOCATION_POLICY.md` before implementing
  relocation.
- Confirm relocation output remains runtime-only and is not persisted even
  though `blockId` now exists.

## Relocation Policy

- Confirm relocation is load-time anchor reconstruction, not a replacement for
  transaction mapping.
- Confirm relocated ranges are initialized into the comment decoration plugin
  and then maintained with `DecorationSet.map`.
- Confirm right panel thread clicks still read current plugin ranges, not
  `anchor.pm.from/to`.
- Confirm relocation tries blockId/textOffset first, then quote search inside
  the block, then conservative document-level quote fallback.
- Confirm ambiguous matches become stale or orphaned rather than silently active.
- Confirm relocation results are visible in debug UI but are not written to
  document JSON, comment APIs, storage, or export payloads.
- Confirm deleting a relocated/currently mapped range does not resurrect the
  previous relocation result.

## Repository-backed Behavior

- Switch between documents and confirm comments load only for the active
  document.
- Create a comment, resolve it, and reopen it; confirm `KnowledgeDocument.content`
  is unchanged after each action.
- Inspect local storage under `northstar.knowledge-editor.v1` and confirm local
  comment threads are not stored there.
- Export the knowledge editor JSON and confirm comments are not included in the
  export payload.
- Confirm repository data does not include runtime mapped ranges,
  `runtimeMatch`, relocation results, `DecorationSet`, `activeThreadId`, or
  pending composer state.
- Confirm API responses may include `anchorStatus: "active"` only as a
  non-authoritative compatibility default; current anchor status and
  `runtimeMatch` still come from the editor plugin.
- Confirm right panel thread clicks still navigate through the current runtime
  range from the `knowledge-comment-decorations` plugin, not `anchor.pm.from/to`.
- Confirm orphaned thread clicks still select the thread in the panel without
  attempting an invalid editor selection.

## Backend-backed Persistence

- Create a comment on an API-backed document, reload the document, and confirm
  the thread reloads through `GET /api/v1/documents/{documentId}/comments`.
- Add a reply and confirm messages remain ordered by creation time after reload.
- Resolve and reopen a thread, reload, and confirm only lifecycle state changed:
  `status`, `resolvedAt`, and `updatedAt`.
- Confirm create/add-message/resolve/reopen do not change document content,
  drafts, versions, search projection rows, or export payloads.
- Confirm requests and responses do not include document content JSON, runtime
  mapped ranges, `runtimeMatch`, relocation results, `DecorationSet`,
  `activeThreadId`, or pending composer state.
- Confirm comment anchor JSON may naturally include `block.start.blockId` and
  `block.end.blockId`, but no comment API stores runtime mapped ranges or
  treats `blockId` as comment-specific data.
- Confirm `PATCH /api/v1/documents/{documentId}` is not called during comment
  create/add-message/resolve/reopen operations.

## Production Hardening Browser QA

- Create comment -> reload: create a comment on an API-backed document, reload,
  and confirm the persisted thread appears in the Comments panel.
- Create comment -> reload -> relocation: create a comment, edit/save content
  so the original `anchor.pm.from/to` is stale, reload, and confirm the
  highlight initializes from relocation rather than the stale PM snapshot.
- Insert before anchor -> save/reload: insert text before the commented text,
  save/reload, and confirm the thread remains attached.
- Delete anchored block -> save/reload: delete the textblock containing the
  anchor, save/reload, and confirm the thread remains listed as Anchor lost
  without rendering a normal highlight.
- Resolve/reopen -> reload: resolve a thread, reload, confirm it is resolved;
  reopen it, reload, and confirm it is open.
- Create failure: simulate a failed create request and confirm the body and
  captured anchor remain in the composer and no document highlight appears.
- Resolve/reopen failure: simulate a failed lifecycle request and confirm the UI
  rolls back or remains on the previous confirmed state with an inline error.
- Switching documents: start loading comments for document A, switch to document
  B, and confirm a late A response never appears in B's Comments panel.
- Storage/export: after create, failure, resolve/reopen, stale, and orphaned
  flows, inspect storage/export payloads and confirm they remain comment-free.
- Content JSON: confirm document JSON may contain structural `blockId`, but no
  comment marks, comment nodes, `threadId`, runtime ranges, `runtimeMatch`,
  relocation results, or comment metadata.

## Composer Open

- Select non-empty body text that does not overlap an existing comment highlight.
- Click the comment button in the inline BubbleMenu.
- Confirm the right sidebar switches to the Comments tab.
- Confirm a New Comment composer appears at the top of the Comments tab.
- Confirm no thread is created yet and no document highlight appears yet.
- Confirm the composer displays the selected text excerpt.
- Confirm the BubbleMenu closes or no longer visually competes with the composer.

## Composer Submit

- Type a non-empty comment body.
- Click Comment.
- Confirm exactly one local thread is created.
- Confirm the thread body matches the text you entered, not placeholder text.
- Confirm the thread is selected and the Comments tab remains open.
- Confirm the editor shows the new document highlight.
- Confirm the BubbleMenu does not immediately reopen unless a fresh selection is made.
- Confirm the created anchor uses the selection captured when the composer
  opened, not a later editor selection.
- Expand debug JSON and confirm it includes both `anchor` and `runtimeMatch`.

## Empty Body Prevention

- Open the composer.
- Leave the body empty, or enter only whitespace.
- Confirm the Comment button is disabled or submit is ignored.
- Confirm no thread is created.

## Composer Cancel

- Open the composer.
- Type text in the textarea.
- Click Cancel.
- Confirm the composer closes.
- Confirm no thread is created.

## Escape Cancel

- Open the composer.
- Type text in the textarea.
- Press Escape while the textarea is focused.
- Confirm the composer closes.
- Confirm no thread is created.
- Confirm Escape does not otherwise break editor focus or selection behavior.

## Composer Replacement

- Open a New Comment composer on one text range.
- Before submitting, select another normal uncommented range and click the
  comment button again.
- Confirm no thread is created by replacing the pending composer.
- Confirm the pending composer state is predictable: the previous pending anchor
  is replaced by the latest captured anchor.
- Submit the new composer and confirm the thread uses the latest captured anchor.

## BubbleMenu Empty Selection

- Place the cursor in normal text without selecting any characters.
- Confirm the new comment button is hidden or disabled.
- Confirm no text-range comment can be created from an empty cursor position.

## BubbleMenu Normal Selection

- Select normal uncommented text.
- Confirm the BubbleMenu can show regular formatting actions.
- Confirm the new comment button is visible.
- Click the new comment button and confirm it opens the sidebar composer without
  creating a thread immediately.

## BubbleMenu Existing Comment Selection

- Create a comment.
- Select text fully inside that existing comment highlight.
- Confirm the new comment button is hidden or disabled for this spike.
- Confirm no duplicate thread is created accidentally.
- Select text that partially overlaps the existing highlight.
- Confirm the new comment button is hidden or disabled for this spike.
- Confirm overlapping comment creation is not offered by the default UX.

## BubbleMenu Highlight Click

- Create an open, resolved, or stale rendered comment highlight.
- Click the visible highlight in the document.
- Confirm the corresponding thread is selected in the Comments panel.
- Confirm the selected highlight becomes visually stronger.
- Confirm other non-orphaned highlights remain visible.
- Confirm no New Comment composer opens.
- Confirm no thread is created.
- Confirm the BubbleMenu does not flicker or compete visually with the panel.

## Composer Open State

- Open the New Comment composer.
- Click inside the composer textarea and action buttons.
- Confirm the editor BubbleMenu does not remain visually active in a confusing
  way and does not flicker over the composer.
- Cancel or submit the composer and confirm the BubbleMenu stays closed until a
  fresh editor selection is made.

## Exact Match

- Create a comment and do not edit the highlighted text.
- Confirm the panel status reads Attached.
- Confirm debug JSON includes `runtimeMatch.status: "active"`,
  `confidence: "exact"`, and `reason: "exact_match"`.
- If the comment was initialized after reload, confirm debug JSON may also show
  `relocationResult.reason: "pm_snapshot_valid"` or another load-time
  relocation reason.

## Minor Text Change

- Create a comment on a reasonably long word or phrase.
- Delete or change one character inside the highlighted range.
- Confirm the comment remains attached when similarity stays high.
- Confirm the panel status reads Attached - text changed slightly.
- Confirm debug JSON includes `confidence: "high"` and
  `reason: "minor_text_change"`.

## Major Text Change

- Create a comment on a phrase.
- Replace most of the highlighted text with unrelated text.
- Confirm the range remains highlighted with stale styling.
- Confirm the panel status reads Needs review or Anchor may be inaccurate.
- Confirm debug JSON includes `status: "stale"` and
  `reason: "major_text_change"`.

## Very Short Text Change

- Create a comment on text with length three or less.
- Change one character.
- Confirm the anchor does not automatically remain active.
- Confirm the panel shows a stale review state rather than Attached.

## Deleted Range

- Create a comment on selected text.
- Delete the entire highlighted text.
- Confirm the normal document highlight disappears.
- Confirm the thread remains in the Comments panel and reads Anchor lost.
- Click the orphaned thread and confirm the panel selection changes but the
  editor does not attempt to select an invalid range.

## Runtime Mapping

- Create a comment on a phrase.
- Place the cursor before the highlighted phrase and type several characters.
- Confirm the highlight moves with the original phrase.
- Click the thread in the right panel.
- Confirm the editor selects and scrolls to the moved runtime range.
- Reload with a stale `pm.from/to` snapshot and stable blockId. Confirm the
  initial runtime range is reconstructed before transaction mapping resumes.

## Multiple Comment Highlights

- Create three comments in the same paragraph on three separate non-overlapping
  text ranges.
- Confirm all three highlights are visible in the document at the same time.
- Click each highlight.
- Confirm the corresponding thread is selected and only that highlight is
  visually strengthened.
- Confirm the other highlights remain visible after selecting one thread.
- Click each thread in the right panel.
- Confirm the editor selects and scrolls to the correct mapped range.
- Resolve one thread.
- Confirm only that thread uses the resolved style and the other open thread
  highlights remain visible.
- Reopen the resolved thread.
- Confirm its highlight returns to the open style.
- Expand debug JSON for all three threads and confirm unchanged anchors still
  show `runtimeMatch.status: "active"`, `confidence: "exact"`, and
  `reason: "exact_match"`.

## Resolved Thread Behavior

- Create a comment and confirm it is open and attached.
- Resolve it from the Comments panel.
- Confirm the highlight remains visually weaker according to the current policy.
- Click the resolved highlight and confirm it still selects the thread.
- Edit the highlighted text with a minor or major change.
- Confirm anchor matching still updates while thread status remains resolved.
- Reopen the thread and confirm only thread lifecycle status changes; anchor
  status and runtime match are not reset.

## Stale Thread Behavior

- Create a stale rendered highlight by replacing most of the commented text.
- Click the stale highlight.
- Confirm the matching thread is selected.
- Confirm the BubbleMenu does not treat the stale highlight click as a new
  comment selection.

## Overlapping Comments

- Try selecting text fully inside an existing comment highlight.
- Confirm the new comment button is hidden or disabled.
- Try selecting text that partially overlaps an existing comment highlight.
- Confirm the new comment button is hidden or disabled.
- Known limitation: this spike does not implement advanced overlap picking,
  stacking UI, conflict resolution, or disambiguation.

## Document Switching And Pending State

- Open a New Comment composer on document A.
- Switch to document B before submitting.
- Confirm the pending composer is cleared or no longer submit-capable for
  document A.
- Confirm submitting from document B never creates a comment on document A.
- Create comments on document A and document B.
- Confirm threads are isolated by `documentId`.
- Confirm switching documents shows only the current document's threads,
  runtime matches, and highlights.

## Content JSON Cleanliness

- Create, resolve, reopen, stale, and orphan one or more comments.
- Export or inspect the saved `KnowledgeDocument.content` JSON.
- Confirm document JSON may include `attrs.blockId` on ProseMirror textblock
  nodes.
- Confirm wrapper/container nodes such as blockquote, list items, task items,
  details containers, tables, table cells, images, and media nodes do not
  receive `blockId` unless their compiled ProseMirror node type is a textblock.
- Confirm document JSON does not include comment marks.
- Confirm document JSON does not include `threadId`, `comment`, `anchorStatus`,
  `runtimeMatch`, relocation results, runtime ranges, `data-comment-thread-id`,
  or other comment metadata.

## Storage And Export Cleanliness

- Create comments and leave at least one pending composer or runtime match.
- Inspect local storage under `northstar.knowledge-editor.v1`.
- Export the knowledge editor JSON.
- Confirm storage and export payloads include only `documents` and
  `activeDocumentId` for editor state, plus export metadata for exported files.
- Confirm document content in storage/export may include structural textblock
  `attrs.blockId`.
- Confirm local comment threads, pending composer state, runtime match data, and
  relocation results are not persisted or exported.

## Accessibility Basic Interaction

- Confirm the composer textarea can receive focus.
- Confirm Cancel and Comment buttons are keyboard reachable.
- Confirm Escape cancels only the active composer and creates no thread.
- Click outside the composer.
- Confirm no thread is created accidentally.
