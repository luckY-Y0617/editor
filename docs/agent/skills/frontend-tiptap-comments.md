# Skill: Frontend Tiptap Comments

## When To Use

- Use for frontend tasks touching Tiptap editor.
- Use for ProseMirror plugins.
- Use for comment highlights and decorations.
- Use for runtime range mapping.
- Use for blockId assignment/repair.
- Use for comment panel navigation.
- Use for stale/orphaned UI state.
- Use for comment composer state.
- Use for comment load/retry behavior.
- Use for frontend comment regression tests.
- Use for editor serialization/deserialization.

## Read First

- `AGENTS.md`
- `docs/agent/00-project-state.md`
- `docs/agent/01-control-rules.md`
- `docs/agent/02-conflict-register.md`
- `docs/agent/skills/comments.md`
- Comment v1 docs/contracts.
- Block identity docs.
- Frontend comment decoration/plugin code.
- Existing editor serialization code.
- Comment API client code.
- Frontend tests/regression suite.
- Do not guess paths.

If exact docs are unknown, search exact terms:

- `comment v1`
- `blockId`
- `knowledge-comment-decorations`
- `DecorationSet`
- `decorations`
- `runtime ranges`
- `runtimeMatch`
- `orphaned`
- `stale`
- `ProseMirror`
- `Tiptap`
- `comment panel`
- `composer`

## Current State Assumptions

- Comments are documented as `comment v1 / beta-complete`.
- Highlights render via ProseMirror decorations from `knowledge-comment-decorations`.
- Runtime mapped ranges live in plugin state only.
- `blockId` is structural identity on textblock nodes.
- Panel navigation reads plugin runtime ranges, not persisted `anchor.pm.from/to`.
- Frontend comment regression suite passed with 73 tests at version definition time.
- This is documented baseline, not proof current code matches.

## Must Preserve

- Document JSON must remain comment-free.
- No comment marks/nodes in Tiptap JSON.
- No `threadId`, runtime ranges, `runtimeMatch`, relocation output, anchor status, active thread, or composer state in document JSON.
- Decorations are runtime rendering only.
- `DecorationSet` is not persisted.
- Runtime ranges are plugin state only.
- Relocation output is runtime-only.
- `pm.from/to` are not permanent coordinates.
- `blockId` only on textblock nodes.
- `blockId` must not encode document id, position, text, comment id, or thread id.
- Panel navigation uses plugin runtime ranges.
- Serialization/deserialization must not leak runtime comment metadata into document content.

## Allowed Work

- Fix documented frontend comment v1 behavior.
- Update decoration rendering from persisted comment threads.
- Update runtime range mapping through ProseMirror transactions.
- Update panel navigation using plugin runtime ranges.
- Update stale/orphaned display behavior if documented.
- Update blockId assignment/repair only within structural identity rules.
- Add/update focused frontend tests.
- Update API client only when API contract requires it.

## Forbidden Work

- Add comment marks/nodes to document JSON.
- Persist `DecorationSet`.
- Persist runtime mapped ranges.
- Persist relocation results.
- Persist active thread/composer state in document JSON.
- Treat `anchor.pm.from/to` as permanent truth.
- Put `blockId` on wrapper/container/non-textblock nodes.
- Encode comment/thread/document/position/text in `blockId`.
- Implement cross-revision/backend/CRDT/OT relocation unless explicitly moved into scope.
- Add image/table/node comments unless explicitly moved into scope.
- Add mentions, notifications, edit/delete comments, advanced overlap picking, or browser E2E unless explicitly moved into scope.
- Change backend comment contract from frontend task without using API/comment skills.

## Implementation Rules

### Decoration Rendering

- Build comment highlights from persisted comment threads plus runtime relocation/mapping.
- Use ProseMirror decorations for rendering.
- Do not mutate Tiptap JSON to display highlights.
- Non-orphaned runtime ranges may render decorations.
- Stale/orphaned UI state must not become document content.

### Runtime Mapping

- Map ranges on transactions using plugin/runtime state.
- Do not persist mapped positions as permanent anchors.
- Reinitialize runtime ranges on document/comment load according to documented relocation behavior.

### Block Identity

- Maintain `blockId` only as structural textblock identity.
- Repair/generate `blockId` only according to existing documented logic.
- Do not use `blockId` as comment metadata.

### Serialization

- Before saving document content, ensure runtime comment metadata is not serialized.
- Document content save must not include comment marks/nodes/thread ids/runtime ranges.
- Comment persistence must use comment APIs, not document PATCH.

## Validation

- Inspect frontend package scripts before running validation.
- Run focused frontend comment/editor tests when available.
- Run backend/API tests if API contract changes.
- Report Vite large-chunk warning as warning, not failure, if build otherwise passes and docs already note it.
- Do not claim 73-test regression passed unless actually run in this task.
- Report tests not run with reason.

## Final Report Notes

- List frontend comment/editor areas changed.
- State document JSON serialization impact.
- State decoration/runtime range impact.
- State blockId impact.
- State API contract impact or `None`.
- List tests run/not run.
- State deferred comment features touched or `None`.
