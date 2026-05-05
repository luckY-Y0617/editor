# Required Reading Order

Purpose: define what future agents must read before editing.

## Universal Reading Order

For any task:

1. `AGENTS.md`
2. `docs/agent/00-project-state.md`
3. `docs/agent/01-control-rules.md`
4. `docs/agent/02-conflict-register.md`
5. `docs/agent/04-implementation-protocol.md`
6. `docs/agent/05-validation-protocol.md`
7. `docs/agent/06-final-report-format.md`

## Locating Relevant Docs

- Do not guess documentation paths.
- When a referenced phase/domain doc path is unknown, search by exact domain terms first.
- Prefer current phase/domain docs over older broad planning docs.
- If multiple docs conflict, preserve the conflict and apply `docs/agent/02-conflict-register.md`.
- If the correct doc cannot be found, report it as not found instead of inventing content.
- Read only docs relevant to the current task. Do not load unrelated skill/domain docs.

Use exact search terms such as:

- `Phase 6`
- `files`
- `upload_sessions`
- `document_attachments`
- `file_outbox`
- `permission contract`
- `effective permission`
- `public link`
- `comment v1`
- `blockId`
- `Tiptap`

## Skill Selection

- For backend architecture/layering tasks, read `docs/agent/skills/backend-clean-architecture.md`.
- For phase/status/current target tasks, read `docs/agent/skills/backend-phase-execution.md`.
- For schema/entity/migration tasks, read `docs/agent/skills/data-model-migrations.md`.
- For Phase 6 files/upload/attachments/outbox/Tiptap file reference tasks, read `docs/agent/skills/files-upload.md`.
- For API route/DTO/error-contract tasks, read `docs/agent/skills/api-contracts.md`.
- For permission/effective-access/share-link/invite/token/audit tasks, read `docs/agent/skills/permissions.md`.
- For comment backend/API/persistence tasks, read `docs/agent/skills/comments.md`.
- For general frontend/editor/schema/serialization/editor-QA tasks, read `docs/agent/skills/frontend-editor.md`.
- For frontend Tiptap comment decoration/runtime-range/blockId tasks, read `docs/agent/skills/frontend-tiptap-comments.md`.
- Read only the skills relevant to the current task.

## Backend Task Reading Order

For a backend task:

1. Universal reading order.
2. Relevant current phase prompt/docs.
3. Relevant domain contract.
4. Existing implementation code.
5. Related tests.
6. Related migrations/configurations.

## Files / Phase 6 Task Reading Order

For a files/upload/attachments/outbox task:

1. Universal reading order.
2. Phase 6 backend prompt/docs.
3. File/upload docs/contracts.
4. Existing API/Application/Domain/Infrastructure file-related code.
5. Permission access code.
6. Related tests and migrations.

## Permission Task Reading Order

For a permissions task:

1. Universal reading order.
2. Permission system contract.
3. Conflict register.
4. Effective permission service implementation.
5. Permission API/DTO/contracts.
6. Permission tests.
7. Audit/token/share-link handling code.

## Comment Task Reading Order

For a comments task:

1. Universal reading order.
2. Comment v1 docs/contracts.
3. Block identity docs.
4. Decoration/plugin docs for frontend tasks.
5. Existing comment API/repository/plugin code.
6. Comment regression tests.

## Rule

- Agents must inspect current code before changing code.
- Docs define intended behavior, but current code must be verified before editing.
- Do not rely only on project-state documentation as proof of implementation.
