# Comment Anchor Relocation Policy

Status: implemented frontend runtime policy for load-time comment relocation in
`comment v1 / beta-complete`.

This document defines how persisted comment anchors are relocated when a
document is loaded and the original `pm.from/to` snapshot may no longer point to
the intended text. It builds on backend comment persistence and stable
textblock-level `blockId` support.

## Core Principle

Relocation is load-time anchor reconstruction.

It is not a replacement for current-session mapping. After an initial runtime
range is established, the `knowledge-comment-decorations` plugin must continue
to maintain current positions with `DecorationSet.map(tr.mapping, tr.doc)`.

Relocation results are runtime state. Do not persist them.

Do not write relocation data to:

- Tiptap document JSON
- comment backend tables
- comment anchor JSON
- storage/export payloads

## Inputs And Outputs

Input:

- persisted `CommentAnchorV1`
- current Tiptap/ProseMirror document

Output:

- optional runtime range `{ from, to }`
- anchor status: `active`, `stale`, or `orphaned`
- confidence: `exact`, `high`, `medium`, `low`, or `none`
- reason/debug metadata explaining how the range was chosen

The output is exposed only through plugin/runtime state and UI debug surfaces.

## Relocation Result Shape

Use an explicit result object, for example:

```ts
type AnchorRelocationResult = {
  status: "active" | "stale" | "orphaned";
  confidence: "exact" | "high" | "medium" | "low" | "none";
  reason:
    | "pm_snapshot_valid"
    | "block_offset_match"
    | "block_exact_quote"
    | "block_fuzzy_quote"
    | "document_exact_quote"
    | "document_fuzzy_quote"
    | "ambiguous_match"
    | "missing_block"
    | "invalid_anchor"
    | "empty_quote"
    | "no_match";
  range?: { from: number; to: number };
  similarity?: number;
  editDistance?: number;
  candidates?: number;
};
```

The implementation can refine names, but it must keep the same information:
status, confidence, reason, optional range, and debug metrics.

## Candidate Priority

Relocation must be deterministic and conservative. Prefer a clear stale or
orphaned state over silently attaching a comment to the wrong text.

Evaluate candidates in this priority order:

1. Validate anchor shape and quote.
2. If `block.start.blockId` exists, find the matching textblock.
3. Try `blockId + textOffset` inside that textblock.
4. Try exact quote search inside the matched block.
5. Try normalized/fuzzy quote search inside the matched block.
6. If start/end block ids differ and both textblocks exist, try a conservative
   cross-textblock range reconstruction.
7. Try document-level exact quote search fallback.
8. Try document-level fuzzy quote search fallback.
9. Mark orphaned when no usable deterministic range exists.

Do not run relocation on every editor transaction. It should run when persisted
threads are loaded or when plugin state is initialized without a current mapped
range for a thread.

Implementation notes:

- A still-valid `pm.from/to` snapshot may initialize the runtime range with
  `reason: "pm_snapshot_valid"`.
- If the snapshot is stale and a `blockId` is present, block evidence takes
  priority over the old ProseMirror position.
- If an old anchor has no `blockId`, a valid stale `pm.from/to` snapshot remains
  a last-resort stale runtime range only after quote fallback fails.
- Previous relocation ranges are not reused after current-session edits remove
  a mapped decoration; after initialization, `DecorationSet.map` is the only
  range maintenance mechanism.

## BlockId Rules

`blockId` is the strongest structural signal, but it is not sufficient by
itself.

When a textblock with the anchor's `blockId` is found:

- compute an offset-based candidate from `textOffset`;
- validate the mapped text against `anchor.quote.normalizedExact`;
- if it is exact or high confidence, use it as `active`;
- if it is valid but only medium/low confidence, return a `stale` candidate
  unless a stronger quote match exists in the same block.

When the block is missing:

- do not assume the original `pm.from/to` is still valid;
- try document-level quote fallback;
- if fallback is ambiguous or absent, mark the anchor orphaned.

## Quote Matching

Use the existing `northstar.plainText.v1` normalizer and existing lightweight
anchor matching policy.

Exact normalized quote match:

- `active`
- `exact`

Small edit / high similarity:

- `active`
- `high`

Medium or low similarity:

- `stale`
- `medium` or `low`

Empty or invalid mapped text:

- `orphaned`
- `none`

Do not implement complex semantic matching or external search in this spike.

## Ambiguity Rules

Ambiguous relocation must be visible.

If multiple candidates have the same strongest score:

- prefer a candidate in the matching `blockId` textblock;
- otherwise prefer the candidate nearest the original `pm.from/to` snapshot only
  if there is a single nearest candidate;
- if there is still a tie, return `orphaned` with `reason: "ambiguous_match"`.

Document-level fallback should be conservative. A unique exact quote can be
active. Multiple document-level exact matches without blockId support should be
stale or orphaned, not silently active.

## Cross-Textblock Selections

The first relocation spike may support cross-textblock anchors conservatively,
but it must not become a broad range inference system.

Rules:

- If both start and end `blockId` values exist and the corresponding textblocks
  still exist in document order, a range from start textOffset to end textOffset
  may be considered.
- The resulting range must still be validated against the anchor quote.
- If either block is missing, order is inverted, or validation is weak, return
  stale or orphaned.
- Do not implement advanced multi-block diff relocation in this spike.

## Plugin Integration

The comment decoration plugin remains the runtime source of truth.

When threads are set:

- if a current mapped range already exists for a thread, keep mapping behavior;
- if no current mapped range exists, initialize it through relocation;
- build decorations from relocated non-orphaned runtime ranges;
- keep orphaned threads visible in the panel but do not render normal document
  highlights.

After initialization, normal transactions must continue to use
`DecorationSet.map`.

The plugin exposes `relocationResultByThreadId` alongside
`matchResultByThreadId`. Both are runtime-only diagnostic maps.

Right panel navigation must continue to read current mapped ranges from plugin
state. It must not read `anchor.pm.from/to` directly.

## Backend And Persistence Boundary

Do not persist relocation output.

The backend stores:

- thread lifecycle state
- messages
- `CommentAnchorV1`

The backend does not store:

- relocated range
- relocation result
- runtime match
- current anchor status truth
- DecorationSet
- active thread id
- pending composer state

Comment APIs do not change for this spike unless DTOs naturally round-trip
anchor JSON already containing `blockId`.

## UI Expectations

The Comments panel exposes relocation debug information alongside existing
anchor/runtime debug information.

Suggested user-facing status mapping:

- active/exact: Attached
- active/high: Attached - relocated with high confidence
- stale/medium: Needs review
- stale/low: Anchor may be inaccurate
- orphaned/none: Anchor lost

The selected thread behavior, highlight click behavior, resolve/reopen behavior,
and clean document JSON guarantees must remain unchanged.

## Testing Requirements

Regression coverage includes:

- same blockId, text inserted before the anchor, reload relocates correctly;
- same blockId, minor text edit, relocation remains active/high when similarity
  is high;
- same blockId, major text edit, relocation becomes stale;
- missing blockId textblock with unique document-level exact quote fallback;
- missing blockId textblock with ambiguous document-level quote becomes stale or
  orphaned;
- deleted block becomes orphaned when no safe fallback exists;
- old anchors without blockId still use quote fallback;
- cross-textblock anchors are handled conservatively;
- relocated runtime ranges are not persisted;
- document JSON still contains no comment marks, comment nodes, `threadId`,
  runtime ranges, `runtimeMatch`, or comment metadata;
- existing transaction mapping tests still pass after relocation initialization.

## Out Of Scope

- Multiplayer collaboration.
- CRDT/OT-aware relocation.
- Backend anchor rewriting.
- Persisting relocated ranges.
- Image comments.
- Table comments.
- Node comments.
- Semantic/vector matching.
- Background relocation jobs.
