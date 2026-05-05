# Block Identity Schema

Status: implemented frontend schema guardrail for stable block identity in
`comment v1 / beta-complete`.

This document defines the rules implemented for stable `blockId` support. The
goal is to add durable structural identity to comment-addressable text blocks
without changing comment persistence, decoration runtime mapping, or
cross-revision relocation behavior yet.

## Core Principle

`blockId` is document structural identity. It is not comment metadata.

It may be stored in Tiptap document JSON as a node attribute, but it must never
encode a comment thread, runtime range, anchor status, or UI state.

Allowed document JSON shape:

```json
{
  "type": "paragraph",
  "attrs": {
    "blockId": "blk_abc123"
  },
  "content": [{ "type": "text", "text": "Example" }]
}
```

Still disallowed in document JSON:

- comment marks
- comment nodes
- `threadId`
- `data-comment-thread-id`
- `anchorStatus`
- `runtimeMatch`
- runtime mapped ranges
- `DecorationSet`
- `activeThreadId`
- pending composer state

## Target Node Rule

Assign `blockId` to ProseMirror textblock nodes only.

The rule is semantic, not visual:

```ts
node.isTextblock === true
```

Do not decide target nodes by enumerating visual wrappers such as list items,
task items, or blockquotes. A wrapper can contain comment-addressable text, but
the text-range anchor should attach to the actual textblock inside the wrapper.

Typical target nodes include:

- `paragraph`
- `heading`
- `codeBlock`
- any custom node whose ProseMirror node type is a textblock

Typical non-target wrapper/container nodes include:

- `blockquote`
- `bulletList`
- `orderedList`
- `listItem`
- `taskList`
- `taskItem`
- `details`
- `detailsContent`
- `table`
- `tableRow`
- `tableCell`
- image/file/media nodes

If a current or future schema makes a wrapper node itself a textblock, the
`node.isTextblock` rule wins and the implementation should handle it as a target
node.

Current frontend textblock node types with `blockId` schema support are:

- `paragraph`
- `heading`
- `codeBlock`
- `detailsSummary`

Runtime repair still uses the compiled ProseMirror `node.isTextblock` check for
assignment. The JSON migration helper uses the current textblock node type list
because raw JSON does not include compiled ProseMirror node type semantics.

## Scope

Block identity is implemented. Cross-revision relocation remains out of scope.

This spike may change document content JSON by adding `attrs.blockId` to
textblock nodes. That change is allowed because it is structural document
identity, not comment data.

This spike must not change:

- comment persistence API behavior
- comment repository semantics
- runtime comment range source of truth
- `knowledge-comment-decorations` mapping behavior
- backend storage of runtime anchor status or runtime match

## Id Format

Use a short, stable, opaque id with a recognizable prefix, for example:

```text
blk_<opaque-id>
```

Rules:

- The id must be unique within a document.
- The id must be stable across saves, exports, imports, and reloads.
- The id must not encode the document id, position, text content, comment id, or
  thread id.
- The id should be safe for JSON and optional DOM `data-*` debugging output.

## Generation And Repair

The implementation must repair textblock identity whenever document content is
loaded or edited.

For every textblock node:

- if `attrs.blockId` is missing, generate a new id;
- if `attrs.blockId` has an invalid format, generate a new id;
- if `attrs.blockId` duplicates an earlier textblock id in the same document,
  keep the first occurrence and regenerate later duplicates.

Repair happens through the `BlockIdentity` Tiptap extension and ProseMirror
plugin. Repairs are consolidated into a single transaction where possible and
are marked with `addToHistory: false`.

Use one consolidated repair transaction when possible. The repair transaction
should not enter normal undo history.

## Load, Import, Paste, Split, And Merge

Required behavior:

- Old documents without `blockId` are migrated on load.
- Imported documents are normalized before use.
- Pasted content without ids receives new ids.
- Pasted content with duplicate ids is repaired.
- Splitting a block may temporarily duplicate an id; repair keeps the first
  textblock and regenerates the later duplicate.
- Merging blocks keeps the surviving textblock id; deleted block ids disappear.

The runtime repair layer remains the final guardrail even when import/load
migration exists.

## Migration Function

The web app includes a pure migration helper:

```ts
migrateDocumentContentBlockIds(content: JSONContent): JSONContent
```

It is used for:

- persisted localStorage load normalization;
- import normalization;
- initial seed cloning if needed;
- tests that validate old document compatibility.

The migration helper must preserve existing valid unique ids and only change
missing, invalid, or duplicate ids.

It also removes `attrs.blockId` from non-textblock JSON wrapper/container nodes
so exported and stored document content only carries structural ids on supported
textblock nodes.

## Comment Anchor Integration

`CommentAnchorV1.block.start/end.blockId` should be populated from the
textblock ancestor used for `path`, `nodeType`, and `textOffset`.

Rules:

- `createAnchorPoint` should find the nearest textblock ancestor.
- It should read `blockId` from that textblock node's attrs.
- Start and end anchors may have different `blockId` values for cross-textblock
  selections.
- `pm.from/to` remains a base revision snapshot.
- Runtime range source of truth remains the comment decoration plugin state.
- Existing persisted comments without `blockId` remain valid and fall back to
  path/textOffset/quote data until future relocation work exists.

This implementation does not alter backend comment API semantics. Newly created
anchor JSON may naturally include `blockId` fields inside `CommentAnchorV1`.

## Testing Requirements

Add regression coverage for:

- old JSON without ids gets textblock ids;
- existing valid unique ids are preserved;
- duplicate ids are repaired deterministically;
- invalid id formats are repaired;
- wrapper/container nodes do not receive ids unless they are textblocks;
- split-block duplicated ids are repaired;
- paste-like inserted content is repaired;
- comment anchor creation includes start/end `blockId` when available;
- document JSON contains structural `blockId`;
- document JSON still does not contain `threadId`, comment marks, comment nodes,
  runtime ranges, `runtimeMatch`, or comment metadata;
- existing comment runtime mapping tests still pass.

## Out Of Scope

- Cross-revision relocation.
- Image comments.
- Table comments.
- Node comments.
- Comment marks.
- Comment-specific block ids.
- Backend relocation logic.
- Storing runtime mapped ranges.
- Persisting `runtimeMatch`.
