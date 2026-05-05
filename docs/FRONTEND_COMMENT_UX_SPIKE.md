# Frontend Comment UX Spike

Status: design guardrail for the first comment UX spike.

This document records the agreed direction for Northstar editor comments so the
first spike does not drift into a hard-to-migrate implementation.

## Core Principle

Comment threads are external annotations. They are not part of document content.

The Tiptap document JSON stores the body of the document only. Comment threads,
replies, status, permissions, notifications, and audit events are separate
business resources. A comment anchor is a weak binding between a thread and a
location in a specific document revision.

The spike should therefore use this model:

```text
document content -> plain Tiptap JSON
comment threads  -> local/mock external state
comment anchors  -> revision-based annotation data
rendering layer   -> ProseMirror decorations
```

Do not implement comments by writing a `comment` mark into Tiptap content.

## Why Not Comment Marks

A mark-based implementation is not categorically wrong. It can be acceptable for
simple CMS or non-collaborative editors. It is not the right default for
Northstar because comments are not document semantics.

Avoid this as the first implementation:

```json
{
  "type": "text",
  "marks": [{ "type": "comment", "attrs": { "threadId": "thread-1" } }],
  "text": "some text"
}
```

Risks of storing comment IDs in content:

- Pollutes exported and persisted document JSON with annotation metadata.
- Couples resolve/reopen behavior to document autosave and revisioning.
- Makes overlapping comments harder to reason about.
- Makes future permissions, notifications, audit trails, and collaboration
  semantics depend on document patching.
- Creates future migration/cleanup work if the anchor contract changes.

The spike should instead derive highlights from external thread state:

```text
threads -> anchors -> decorations -> inline highlight
```

## Current Repo Fit

The existing frontend is already close to the needed shape:

- `apps/web/src/components/TiptapEditor.tsx` owns the Tiptap editor setup.
- Text selection state already drives the inline bubble menu.
- `isCommentPopoverOpen` is already reserved in editor UI state.
- The right-side `OutlinePanel` can be extended or replaced with a comment
  thread panel.
- Document updates already have `revision/baseRevision` concepts on the backend.

## Spike Goal

Build a local anchored comment annotation spike.

The spike validates comment UX and anchor shape without committing to backend
persistence, stable block IDs, collaboration, or cross-revision relocation.

## Must Have

1. Selecting text shows a comment action in the bubble menu.
2. Clicking the action creates a local/mock comment thread.
3. The selected text is highlighted with ProseMirror decorations.
4. Decoration ranges are maintained during the current editor session with
   transaction mapping.
5. The right-side panel lists comment threads for the active document.
6. Clicking a thread reads the current mapped range from plugin state, then
   focuses, selects, and scrolls to that range in the editor.
7. Resolving a thread hides or weakens its highlight.
8. Reopening a thread restores the active highlight.
9. The spike outputs a `CommentAnchorV1` JSON payload for created threads.
10. Document content JSON remains unchanged by comment creation, resolve, or
    reopen actions.

## Not In Spike

- No backend persistence.
- No document `PATCH /documents/:id` for comments.
- No `comment` mark in Tiptap content.
- No stable `blockId` injection into document nodes.
- No cross-revision anchor relocation.
- No multi-user collaboration semantics.
- No image, table, node, or document-level comments.
- No complex overlapping comment interaction.
- No notification, mention, webhook, or permission model.
- No audit timeline beyond optional local mock events.

## Decoration Mapping Requirement

Static `pm.from/to` rendering is not enough.

This is insufficient because typing before an anchored range immediately makes
the highlight stale:

```ts
Decoration.inline(thread.anchor.pm.from, thread.anchor.pm.to, attrs)
```

The spike should use a keyed ProseMirror plugin with a state field. The plugin
state should hold a `DecorationSet` and the runtime mapped range per thread.

Sketch:

```ts
type RuntimeCommentAnchor = {
  threadId: string;
  from: number;
  to: number;
};

type CommentDecorationPluginState = {
  decorations: DecorationSet;
  rangesByThreadId: Record<string, RuntimeCommentAnchor>;
};
```

The plugin `apply` step must map decorations through each transaction:

```ts
apply(tr, previous) {
  const decorations = previous.decorations.map(tr.mapping, tr.doc, {
    onRemove: (spec) => markAnchorOrphaned(spec.threadId),
  });

  return {
    decorations,
    rangesByThreadId: readRangesFromDecorationSet(decorations),
  };
}
```

Thread panel navigation must not read stale React state ranges. It should query
the plugin state by `threadId` and use the current mapped range.

## Spike Data Model

Local/mock state can start with this shape:

```ts
type CommentThread = {
  id: string;
  documentId: string;
  status: "open" | "resolved";
  anchorStatus: "active" | "stale" | "orphaned";
  anchor: CommentAnchorV1;
  comments: CommentMessage[];
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string | null;
};

type CommentMessage = {
  id: string;
  threadId: string;
  body: string;
  author: {
    id: string;
    name: string;
  };
  createdAt: string;
  updatedAt?: string | null;
  deletedAt?: string | null;
};
```

Keep thread lifecycle separate from anchor lifecycle:

```ts
thread.status = "open" | "resolved";
anchor.status = "active" | "stale" | "orphaned";
```

Do not make `reopened`, `stale`, or `orphaned` long-lived thread statuses.
`reopen` is an event/action that changes `thread.status` back to `open`.
`stale` and `orphaned` describe whether the anchor can still be located.

## Anchor Contract

The first contract should be explicit that ProseMirror positions are not
permanent stable locators. They are a snapshot for a document revision and can be
mapped only during a live editor session.

Recommended request:

```ts
type CreateCommentThreadRequest = {
  anchor: CommentAnchorV1;
  body: string;
};
```

Recommended anchor:

```ts
type CommentAnchorV1 = {
  schema: "northstar.commentAnchor.v1";
  kind: "tiptap.textRange";
  documentId: string;

  baseRevision: number;
  contentHash?: string;

  pm: {
    from: number;
    to: number;
  };

  block: {
    start: AnchorPoint;
    end: AnchorPoint;
  };

  quote: {
    exact: string;
    prefix: string;
    suffix: string;
    normalizedExact: string;
    normalizer: "northstar.plainText.v1";
  };

  display: {
    excerpt: string;
  };
};

type AnchorPoint = {
  blockId?: string;
  path: number[];
  nodeType: string;
  textOffset: number;
};
```

### Field Semantics

Stable or semi-stable fields:

- `schema`: Contract version for future migration.
- `kind`: Anchor kind. First spike supports only `tiptap.textRange`.
- `documentId`: Document the anchor belongs to.
- `baseRevision`: Document revision the anchor was created against.
- `contentHash`: Optional checksum for exact content snapshot validation.
- `quote.normalizer`: Defines text normalization semantics for relocation.

Revision-local fields:

- `pm.from`: Inclusive ProseMirror position in `baseRevision`.
- `pm.to`: Exclusive ProseMirror position in `baseRevision`.

`pm.from/to` are stable only for the exact document revision/content hash. During
an active editing session they may be maintained as runtime ranges through
transaction mapping. They must not be treated as permanent coordinates.

Future stable locator:

- `block.start.blockId` and `block.end.blockId`.

These are optional in the first spike because the current document schema does
not have stable block IDs. They become important for production relocation after
a dedicated stable block identity spike.

Fallback fields:

- `block.path`: Tiptap JSON child index path. Useful as a fallback but unstable
  after block insertion, deletion, or moves.
- `textOffset`: Offset within the containing block text. Define as UTF-16 code
  unit offset unless a future contract says otherwise.
- `quote.exact`: Original selected text.
- `quote.prefix` and `quote.suffix`: Local text context around the selection.
- `quote.normalizedExact`: Normalized selected text according to
  `quote.normalizer`.
- `display.excerpt`: UI-only preview text. Do not use as a precise locator.

## Example Anchor

```json
{
  "schema": "northstar.commentAnchor.v1",
  "kind": "tiptap.textRange",
  "documentId": "doc-editor-experience",
  "baseRevision": 12,
  "contentHash": "optional-sha256-content-hash",
  "pm": {
    "from": 128,
    "to": 176
  },
  "block": {
    "start": {
      "path": [3, 0],
      "nodeType": "paragraph",
      "textOffset": 8
    },
    "end": {
      "path": [3, 0],
      "nodeType": "paragraph",
      "textOffset": 56
    }
  },
  "quote": {
    "exact": "clarity in our communication, our designs, and our decisions",
    "prefix": "We choose ",
    "suffix": ". Simple is not simplistic.",
    "normalizedExact": "clarity in our communication, our designs, and our decisions",
    "normalizer": "northstar.plainText.v1"
  },
  "display": {
    "excerpt": "clarity in our communication, our designs, and our decisions"
  }
}
```

## Relocation Order

Production relocation is out of scope for the first spike, but the contract
should support this order:

1. If current revision/hash matches `baseRevision/contentHash`, use
   `pm.from/to`.
2. If `blockId` exists, locate the block and validate with
   `textOffset + quote`.
3. Try `quote.exact + prefix/suffix` and require a unique match.
4. Try `path + textOffset` as best-effort fallback.
5. If no reliable match exists, mark `anchor.status = "orphaned"`.

If a match is found but confidence is weak, mark `anchor.status = "stale"` and
let the UI show the thread while warning that the anchor was relocated
approximately.

## Stable Block Identity Is A Separate Spike

Do not add `blockId` to all document nodes as part of the first comment UX
spike. Stable block identity affects document JSON and requires its own design.

Future questions:

- How is `blockId` generated for a new block?
- On split, does the first or second block keep the original ID?
- On merge, which block ID survives?
- On paste from external sources, when are new IDs assigned?
- On copy/paste within Northstar, are IDs preserved or regenerated?
- How are old documents lazily migrated?
- How are client-generated IDs de-duplicated in future collaboration?

## Future API Shape

Comments should be independent resources. They should not be persisted through
`PATCH /documents/:documentId`.

Candidate endpoints:

```text
GET    /documents/{documentId}/comment-threads
POST   /documents/{documentId}/comment-threads
POST   /comment-threads/{threadId}/comments
PATCH  /comment-threads/{threadId}/resolve
PATCH  /comment-threads/{threadId}/reopen
DELETE /comment-threads/{threadId}
```

The backend may store `base_revision` separately for querying, but the API
contract should keep revision semantics inside `anchor`.

## Future Production Concepts

The first spike does not implement these, but the design should leave room for
them.

Thread event timeline:

```ts
type ThreadEvent =
  | "thread.created"
  | "comment.added"
  | "comment.edited"
  | "comment.deleted"
  | "thread.resolved"
  | "thread.reopened"
  | "anchor.relocated"
  | "anchor.orphaned";
```

Permissions:

```text
canReadDocument
canComment
canResolveThread
canEditOwnComment
canDeleteOwnComment
canModerateComments
```

Future anchor kinds:

```ts
type CommentAnchorKind =
  | "tiptap.textRange"
  | "tiptap.node"
  | "tiptap.block"
  | "document";
```

Future systems:

- Mentions and notification fanout.
- Webhooks or outbox events.
- Overlapping comment interaction.
- Node, table, image, and document-level comments.
- Collaboration mapping across remote transactions.
- Cross-revision relocation jobs.
- Audit/event history.

## External References

- Google Drive API comments distinguish anchored comments from unanchored
  comments, with anchored comments tied to a specific location in a specific
  document version:
  https://developers.google.com/workspace/drive/api/guides/manage-comments
- Google Drive also notes that anchors are immutable and their relative position
  is not guaranteed between revisions:
  https://developers.google.com/workspace/drive/api/guides/manage-comments
- Tiptap Comments is organized around threads/comments and includes API,
  webhooks, mentions, overlapping comments, node comments, and sidebar comments:
  https://tiptap.dev/docs/comments/getting-started/overview
- ProseMirror plugin state fields can store plugin-owned data:
  https://prosemirror.net/docs/ref/
- ProseMirror `DecorationSet.map(mapping, doc)` maps decorations through document
  changes:
  https://prosemirror.net/docs/ref/
- ProseMirror community discussion covers marks versus decorations for
  range-shaped metadata:
  https://discuss.prosemirror.net/t/how-to-represent-range-shaped-information-marks-or-decorations/1451
