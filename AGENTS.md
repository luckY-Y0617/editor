# Agent Control Entry Point

## Purpose

- This repository uses agent-control documentation to prevent architecture drift, phase confusion, unsafe refactors, and verbose or unbounded agent behavior.
- `AGENTS.md` is an entry point, not a product roadmap.
- Future agents must treat the files in `docs/agent/` as the control layer before implementation work.

## Current Project State Summary

- Project: Northstar / Northstar Atlas Library.
- Backend mainline: Phase 5 completed.
- Current backend target: Phase 6 files, upload sessions, files, document attachments, file outbox, and Tiptap file reference validation.
- Comments: `comment v1 / beta-complete`.
- Permissions: documented through Phase 11, but public-link behavior is conflict-marked.
- Files: Phase 6 target. Do not assume complete unless verified in code.
- PostgreSQL smoke: configured but not run unless `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is set and the smoke command is actually executed.

## Required Reading

Read in this order before code changes:

1. `docs/agent/00-project-state.md`
2. `docs/agent/01-control-rules.md`
3. `docs/agent/02-conflict-register.md`
4. `docs/agent/03-required-reading-order.md`
5. `docs/agent/04-implementation-protocol.md`
6. `docs/agent/05-validation-protocol.md`
7. `docs/agent/06-final-report-format.md`

Read only the agent-control files and domain documentation required by the current task. Do not load unrelated skill, domain, phase, or historical docs. Prefer targeted reading over broad context loading.

## Skills

When a task touches a specific domain, read the matching skill under `docs/agent/skills/`. Read only skills relevant to the task.

Current core skills:

- `docs/agent/skills/backend-clean-architecture.md`
- `docs/agent/skills/backend-phase-execution.md`
- `docs/agent/skills/data-model-migrations.md`
- `docs/agent/skills/files-upload.md`
- `docs/agent/skills/api-contracts.md`
- `docs/agent/skills/permissions.md`
- `docs/agent/skills/comments.md`
- `docs/agent/skills/frontend-editor.md`
- `docs/agent/skills/frontend-tiptap-comments.md`

## Rule Hierarchy

1. Architecture Rules
2. Data Model Rules
3. API Contracts
4. Workflows
5. Suggestions

Higher-priority rules override lower-priority rules.

## Non-Negotiable Rules

- Preserve ASP.NET Core Modular Monolith + Clean Architecture.
- Preserve documented dependency direction.
- Do not use ABP.
- Do not use old SqlSugar/ABP wrappers.
- Do not modify `services/api-old`.
- Do not modify old Go file service at `E:\ClayMo\services\file-service`.
- Do not make old services runtime dependencies.
- Do not change `/api/v1` base path.
- Do not expose EF entities through API responses.
- Do not store comments, tags, files, permissions, or activity inside Tiptap document JSON.
- Do not silently resolve conflict-marked behavior.
- Use the smallest safe diff only.
- No drive-by refactors.
- During browser QA, do not use in-browser / in-app Browser automation; use an allowed local headless browser, CDP, or Playwright-style workflow only when browser QA is requested or allowed, and report the tool used.

## Implementation Boundary

- Inspect current code before changing code.
- Do not infer implementation from docs alone.
- Report unverified implementation status as `not verified`.
- Preserve documented conflicts unless the user explicitly selects a behavior.

## Final Report Requirement

- Use `docs/agent/06-final-report-format.md`.
