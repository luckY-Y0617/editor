# Skill: Backend Phase Execution

## When To Use

- Use for tasks involving backend phase prompts.
- Use for Phase 5 or Phase 6 work.
- Use for files/upload implementation.
- Use when comments or permissions intersect backend phase work.
- Use when deciding whether a feature is `implemented`, `planned`, `deferred`, `conflict-marked`, or `not verified`.

## Read First

- `AGENTS.md`
- `docs/agent/00-project-state.md`
- `docs/agent/02-conflict-register.md`
- `docs/agent/03-required-reading-order.md`
- Relevant phase prompt/docs.
- Current code for the touched feature.
- Related tests/migrations.
- Do not guess paths.

If exact docs are unknown, search exact terms:

- `Phase 5`
- `Phase 6`
- `files`
- `upload_sessions`
- `document_attachments`
- `file_outbox`
- `comment v1`
- `permission contract`
- `Phase 11`

## Current State Assumptions

- Backend mainline is documented as Phase 5 completed.
- Current/next backend target is Phase 6.
- Phase 6 target is files, upload sessions, files, attachments, private access, file outbox, and Tiptap file reference validation.
- Comments are documented as `comment v1 / beta-complete`.
- Permission contract states implementation through Phase 11.
- Permission phases are separate from backend mainline phases.
- Public collection links and public `linkMode` are conflict-marked.
- Documentation state is baseline, not proof of current code.

## Historical Phase Ledger

This ledger is extracted from old root phase prompts. It preserves route and workflow boundaries, but it is not code verification.

- Phase 1: created the `services/api` skeleton, `Northstar.*` projects, `/api/v1` routing, global error shape, and old-project read-only boundary; it did not authorize broad feature implementation.
- Phase 2: introduced bootstrap, space map, document create/read/update/location, seed workspace/space/collections/documents, Tiptap JSON content, and document revision conflict behavior; context/activity/search could be temporary Phase 2 stubs.
- Phase 3: replaced context/activity/search stubs with real query paths backed by `document_versions`, `document_links`, `activity_events`, and `document_search_index`; search strategy remains conflict-marked between prepared PostgreSQL `tsvector` and lightweight title/body contains.
- Phase 4: added auth/current-user/workspace-member permission work plus QA cleanup; no-op document PATCH must not increment revision or write activity, while stale no-op PATCH still returns conflict. Register vs seed-only auth choice remains conflict-marked by later baseline.
- Phase 5: defined logout by refresh-token revocation, document archive/restore/delete, default exclusion of archived/deleted documents from bootstrap/map/search/context, append-only space import, and space export/import boundaries. Phase 5 prompt validation counts are historical snapshots only.
- Phase 6: defines the current files/upload target: `upload_sessions -> files -> document_attachments`, idempotent finalize, file outbox, object storage boundary, file access through API permission checks, Tiptap file reference validation, and import/export file boundaries.

## Must Preserve

- Backend mainline and permission module phases are separate tracks.
- Do not infer global backend completion from permission Phase 11.
- Do not treat V1/V2 labels as automatic blockers if later explicit phase/contract requires the feature.
- Preserve documented conflicts.
- Preserve `not verified` status when code has not been inspected.

## Allowed Work

- Implement explicitly documented current phase target.
- Verify code against documented baseline.
- Mark status as `not verified` when not inspected.
- Report phase mismatch.
- Preserve current behavior when safe.
- Add tests required by current phase work.

## Forbidden Work

- Move deferred features into current phase without explicit instruction.
- Silently decide public collection link behavior.
- Silently decide public `linkMode`.
- Treat comments/collaboration as Phase 5 when later phase prompts define Phase 5 differently.
- Claim PostgreSQL smoke passed unless actually run.
- Claim code completion from docs alone.
- Use old broad planning docs over later current phase docs when they conflict.

## Implementation Rules

- Before coding, classify touched feature as:
  - `implemented`
  - `planned`
  - `deferred`
  - `conflict-marked`
  - `not verified`
- Inspect current code before changing implementation.
- If current task touches conflict-marked behavior, follow `docs/agent/02-conflict-register.md`.
- If docs conflict and task cannot proceed safely, hard stop.
- If docs conflict but task can proceed without choosing conflict behavior, proceed with safest default and report.

## Validation

- Run validation relevant to files changed.
- For backend changes, use backend validation protocol.
- For migration changes, use migration validation protocol.
- Report PostgreSQL smoke gap exactly if env var is absent:
  - `PostgreSQL smoke not run: NORTHSTAR_POSTGRES_SMOKE_CONNECTION not set.`
- Do not weaken production behavior for tests.

## Final Report Notes

- Include phase affected.
- Include feature status classification.
- State whether code was verified.
- List conflict-marked areas touched or `None`.
- List validation run/not run.
