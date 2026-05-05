# Skill: Comments

## When To Use

- Use for comment threads.
- Use for comment messages.
- Use for comment APIs/contracts.
- Use for comment repository/persistence.
- Use for comment anchors.
- Use for comment load/retry/resolve/reopen behavior.
- Use for backend comment authorization.
- Use for comment-related document context.
- Use for comment-free storage/export.
- Use for block identity when used for comments.

## Read First

- `AGENTS.md`
- `docs/agent/00-project-state.md`
- `docs/agent/01-control-rules.md`
- `docs/agent/02-conflict-register.md`
- `docs/agent/skills/backend-clean-architecture.md`
- `docs/agent/skills/api-contracts.md`
- `docs/agent/skills/permissions.md`
- Comment v1 docs/contracts.
- Block identity docs.
- Existing comment API/repository code.
- Comment tests/regression notes.
- Do not guess paths.

If exact docs are unknown, search exact terms:

- `comment v1`
- `comment_threads`
- `comments`
- `CommentRepository`
- `blockId`
- `relocation`
- `runtime ranges`
- `anchor`
- `DecorationSet`
- `resolve`
- `reopen`

## Current State Assumptions

- Comments status is `comment v1 / beta-complete`.
- Backend-backed persistence exists according to docs.
- Comment-specific APIs exist in contract.
- Stable block identity implemented according to docs.
- Load-time relocation implemented according to docs.
- Production hardening implemented for comment v1 according to docs.
- Frontend comment regression suite passed with 73 tests at version definition time.
- This is documented baseline, not proof current code matches.
- Comment v1 explicitly excludes advanced/deferred items listed in project state.

## Must Preserve

- Comments are external annotation resources.
- Document JSON remains comment-free.
- Do not persist comments through `PATCH /documents/:id`.
- No comment marks/nodes in Tiptap JSON.
- No `threadId`, runtime ranges, `runtimeMatch`, relocation output, anchor status, active thread, or composer state in document JSON.
- Relocation output runtime-only.
- Runtime mapped ranges plugin/frontend state only.
- `blockId` structural identity only.
- `blockId` only on ProseMirror textblock nodes.
- `blockId` must not encode document id, position, text, comment id, or thread id.
- Comment APIs must enforce effective access.
- Comment-free storage/export.

## Allowed Work

- Implement or fix documented comment v1 behavior.
- Add/update comment DTOs/contracts when documented.
- Add/update comment repository/persistence according to documented schema.
- Add/update load/retry/resolve/reopen behavior if documented.
- Add/update backend authorization for comments.
- Add tests for comment API, persistence, authorization, and comment-free document JSON.
- Update frontend integration only when task requires it and frontend skill is used.

## Forbidden Work

- Store comment marks/nodes/thread ids in Tiptap JSON.
- Use document PATCH to persist comments.
- Persist runtime relocation output.
- Persist `DecorationSet` or runtime mapped ranges.
- Treat `pm.from/to` as permanent coordinates.
- Put `blockId` on non-textblock wrapper/container nodes.
- Use `blockId` as comment metadata.
- Add cross-revision anchor rewriting unless explicitly moved into scope.
- Add backend anchor rewriting unless explicitly moved into scope.
- Add collaboration/CRDT/OT relocation unless explicitly moved into scope.
- Add image/table/node comments unless explicitly moved into scope.
- Add mentions, notifications, audit timeline, edit/delete comments, advanced overlap UI, or browser E2E harness unless explicitly moved into scope.
- Bypass permission checks for comments.
- Store comments inside export document content JSON.

## Implementation Rules

### Backend/API

- Comment APIs use Contracts DTOs.
- Controllers remain thin.
- Application orchestrates comment use cases.
- Infrastructure owns repository/persistence.
- Comment endpoints enforce effective access.
- Do not expose EF entities.

### Persistence

- Comments/thread tables are external resources.
- Document content JSON must remain free of comment metadata.
- Export/import must not turn runtime comment data into document content.
- Preserve comment state separately from document draft body.

### Anchors and Block Identity

- `blockId` is structural textblock identity.
- Anchors may reference structural identity according to contract.
- Runtime relocation result must not be saved into document JSON.
- Permanent coordinates must not be based only on ProseMirror positions.

## Validation

- Run backend validation where applicable:
  - `dotnet restore`
  - `dotnet build`
  - `dotnet test`
- Run focused comment tests/regression suite when available.
- Run frontend tests if frontend comment behavior changes and scripts exist.
- Report if automated browser E2E harness is not available.
- Do not claim 73-test regression passed unless actually run in this task.

## Final Report Notes

- List comment backend/API changes or `None`.
- State document JSON storage impact.
- State block identity impact.
- State relocation/runtime state impact.
- State permission impact.
- List tests run/not run.
- State deferred comment capabilities touched or `None`.
