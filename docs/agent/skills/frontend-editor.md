# Skill: Frontend Editor

## When To Use

- Use for frontend/editor tasks touching Tiptap editor.
- Use for ProseMirror schema/plugins.
- Use for document editing.
- Use for document save/load.
- Use for document serialization/deserialization.
- Use for editor state.
- Use for selection/cursor behavior.
- Use for marks/nodes/extensions.
- Use for paste/drop/import behavior.
- Use for keyboard shortcuts.
- Use for editor toolbar/menu behavior.
- Use for frontend document API client.
- Use for editor QA.
- Use for non-comment editor behavior.
- For comment highlights, comment decorations, runtime comment ranges, blockId/comment anchors, and comment panel navigation, also read `docs/agent/skills/frontend-tiptap-comments.md`.

## Read First

- `AGENTS.md`
- `docs/agent/00-project-state.md`
- `docs/agent/01-control-rules.md`
- `docs/agent/03-required-reading-order.md`
- `docs/agent/04-implementation-protocol.md`
- `docs/agent/05-validation-protocol.md`
- `docs/agent/06-final-report-format.md`
- `docs/agent/skills/api-contracts.md`
- `docs/agent/skills/frontend-tiptap-comments.md` when comments are touched
- `apps/web/AI_EDITOR_GUARDRAILS.md`
- `apps/web/EDITOR_QA_CHECKLIST.md`
- existing editor code
- existing editor API client code
- existing frontend tests
- Do not guess paths.
- Do not load unrelated historical docs.
- Inspect current code before editing.

If exact docs/code paths are unknown, search exact terms:

- `Tiptap`
- `ProseMirror`
- `editor`
- `document JSON`
- `serialize`
- `deserialize`
- `selection`
- `toolbar`
- `keyboard`
- `paste`
- `drop`
- `schema`
- `extension`
- `autosave`
- `document API`
- `AI_EDITOR_GUARDRAILS`
- `EDITOR_QA_CHECKLIST`

## Current State Assumptions

- This skill is a general frontend/editor guardrail.
- It is derived from documented guardrails and QA docs.
- It is not proof current frontend code matches the docs.
- Existing comment-specific behavior remains governed by `docs/agent/skills/frontend-tiptap-comments.md`.
- Editor QA checklist may contain manual QA items; manual QA is not considered run unless explicitly performed.
- Frontend general editor state still needs review before old guardrail/QA docs can be archived.

## Must Preserve

- Document JSON remains the source of persisted rich-text content.
- Runtime UI/editor state must not be persisted into document JSON unless explicitly part of the document schema.
- Do not store unrelated feature metadata in Tiptap JSON.
- Comments, permissions, files metadata, tags, links, activity, and runtime state remain outside document content JSON unless a specific contract says otherwise.
- Preserve existing document save/load contract.
- Preserve API client alignment with `/api/v1`.
- Preserve backend API contract ownership.
- Preserve existing editor schema unless task explicitly requires schema change.
- Preserve existing serialization/deserialization behavior unless task explicitly targets it.
- Preserve accessibility and keyboard behavior when editing toolbar/menu/editor interactions.
- Preserve existing comment-specific runtime-only boundaries by deferring to `frontend-tiptap-comments.md`.

## Allowed Work

- Fix documented frontend/editor behavior.
- Update editor UI components for documented tasks.
- Update editor commands/extensions when required by documented behavior.
- Update serialization/deserialization only when explicitly required.
- Update API client when API contract explicitly changes.
- Add/update focused frontend tests.
- Update QA docs only when requested or behavior changes.
- Make small refactors inside editor code if public behavior and persistence format remain unchanged.

## Forbidden Work

- Rewrite the editor architecture opportunistically.
- Replace Tiptap/ProseMirror stack without explicit instruction.
- Change document JSON schema casually.
- Persist runtime UI/editor state into document JSON.
- Persist selection/cursor/composer/panel state into document JSON.
- Store comments, permissions, file metadata, tags, activity, or backend authorization state in document JSON.
- Change backend API contract from frontend task without using `api-contracts.md`.
- Change `/api/v1`.
- Add temporary frontend-only API assumptions.
- Bypass backend validation with frontend-only checks.
- Treat manual QA checklist as automatically passed.
- Mark unrun frontend tests as passed.
- Broaden comment behavior without using comment-specific skill.
- Implement high-risk editor features without explicit user approval.
- Perform large UI rewrites or design-system rewrites during unrelated editor tasks.
- Reformat unrelated frontend files.

## Implementation Rules

### Scope Control

- Identify whether task is:
  - editor UI
  - editor schema
  - serialization/deserialization
  - API client
  - QA/test
  - comment-related
- If comment-related, read and apply `frontend-tiptap-comments.md`.
- If API contract changes, read and apply `api-contracts.md`.
- Use smallest safe diff.
- Avoid unrelated cleanup.

### Editor Schema

- Treat schema/extensions as high-risk.
- Do not add/remove marks or nodes without explicit task scope.
- Do not change persisted node/mark shape without contract/test updates.
- Existing documents must remain loadable unless a migration strategy is explicitly provided.

### Runtime State

- Keep transient editor/UI state out of persisted document JSON.
- Keep selection, focus, hover, menu, panel, composer, loading, retry, and error-display state in frontend runtime state.
- Do not encode backend permissions or derived access state into document content.

### Save / Load

- Preserve save/load semantics.
- Validate outgoing document content according to existing contract.
- Do not mutate unrelated document content during save.
- Do not silently strip unknown content unless documented.
- Do not rely only on frontend checks for backend invariants.

### UX Behavior

- Preserve existing keyboard shortcuts unless explicitly changed.
- Preserve accessibility affordances for toolbar/menu/dialog/editor interactions.
- Avoid large UX rewrites during bugfixes.
- Keep UI changes focused on task scope.

## Serialization and Persistence Rules

- Document JSON is persisted rich-text content.
- Runtime state is not persisted content.
- Serialization must not leak:
  - selection/cursor
  - hover/focus/menu state
  - panel/composer state
  - loading/retry/error display state
  - comment runtime ranges
  - permissions/effective access
  - file metadata as source of truth
  - raw tokens/secrets
- Deserialization must not infer permissions or backend state from document JSON.
- File references and comment anchors must follow their domain contracts.
- Any persisted schema change requires explicit scope, tests, and compatibility/migration plan.

## API Client Rules

- Frontend API client must align with documented `/api/v1` contracts.
- Do not invent backend fields/routes from UI needs.
- Do not change DTO assumptions without checking `api-contracts.md`.
- Do not use `/api/app`.
- Do not treat frontend-only validation as security.
- For protected data, rely on backend authorization.
- If API response shape changes, update tests and affected UI code.
- If backend route is missing, report it instead of inventing frontend mocks as production behavior.

## High-Risk Editor Changes

Hard stop unless explicitly requested:

- Tiptap schema changes
- persisted document JSON shape changes
- editor serialization/deserialization rewrite
- replacing Tiptap/ProseMirror
- autosave conflict behavior changes
- document import/export behavior changes
- frontend API contract changes
- security/permission behavior changes
- comment persistence/rendering model changes
- file reference persistence model changes
- large toolbar/menu/editor layout rewrite
- migration of existing documents

Agent behavior:

- Stop and ask for explicit scope before high-risk changes.
- If task can proceed without high-risk change, avoid it and report.
- Do not hide high-risk side effects in a small bugfix.

## Validation

- Inspect `apps/web/package.json` or relevant package scripts before choosing commands.
- Use commands compatible with current shell/OS.
- Run focused frontend/editor tests when available.
- Run build/typecheck/lint only if scripts exist and task scope warrants it.
- Manual QA checklist items are not considered completed unless actually performed.
- Report manual QA as `not run` when not performed.
- Report Vite large-chunk warning as warning, not failure, if build otherwise passes and existing docs note it.
- If API client changes, run affected client/frontend tests if available.
- If comment editor behavior changes, run comment/editor tests and apply `frontend-tiptap-comments.md`.
- Do not claim `EDITOR_QA_CHECKLIST.md` passed unless explicitly run/performed.

## Final Report Notes

- List frontend/editor areas changed.
- State whether schema changed.
- State whether serialization/deserialization changed.
- State document JSON persistence impact.
- State runtime state impact.
- State API client impact.
- List tests run/not run.
- State manual QA performed/not performed.
- State high-risk editor changes touched or `None`.
- State comment-specific behavior touched or `None`.
