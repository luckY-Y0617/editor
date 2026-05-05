# Old Doc Coverage Improvement

## Summary

- Improved governance coverage for old root `docs/*.md` files without moving, deleting, renaming, or archiving any old docs.
- Added an old-root-doc coverage matrix to `docs/agent/00-source-map.md`.
- Added explicit archive coverage thresholds to `docs/agent/doc-cleanup-plan.md`.
- Strengthened backend phase and data-model governance with small durable facts extracted from old root docs.
- No application code was changed, and no code implementation status was changed.

## Scope

- Documentation-governance only.
- Old docs reviewed were markdown files directly under `docs/*.md`.
- Excluded from old-doc review: `docs/agent/**`, `docs/contracts/**`, and `apps/web/*.md`.
- Code was not inspected in this round, so all implementation status remains documentation-only unless already separately code-verified in another task.

## Old Docs Reviewed

- `docs/BACKEND_ARCHITECTURE_V1.md`
- `docs/BACKEND_DATA_MODEL_V1.md`
- `docs/BACKEND_REFACTOR_RULES.md`
- `docs/BACKEND_PHASE1_PROMPT.md`
- `docs/BACKEND_PHASE1_PROMPT (copy).md` expected by task, but path was not found during inspection.
- `docs/BACKEND_PHASE2_PROMPT.md`
- `docs/BACKEND_PHASE3_PROMPT.md`
- `docs/BACKEND_PHASE4_PROMPT.md`
- `docs/BACKEND_PHASE5_PROMPT.md`
- `docs/BACKEND_PHASE6_PROMPT.md`
- `docs/PERMISSION_SYSTEM_CONTRACT.md`
- `docs/COMMENT_V1_BETA_COMPLETE.md`
- `docs/COMMENT_PERSISTENCE_CONTRACT.md`
- `docs/BLOCK_IDENTITY_SCHEMA.md`
- `docs/COMMENT_ANCHOR_RELOCATION_POLICY.md`
- `docs/COMMENT_PRODUCTION_HARDENING_RULES.md`
- `docs/COMMENT_UX_SPIKE_QA.md`
- `docs/FRONTEND_COMMENT_UX_SPIKE.md`

## Coverage Matrix

| Old Doc | Coverage Status | Unique Facts Found | Action Taken | Remaining Risk |
|---|---|---|---|---|
| `docs/BACKEND_ARCHITECTURE_V1.md` | mostly covered; do not delete yet | Architecture, dependency direction, runtime shape, old-service boundaries, files flow, outbox. | Added coverage row in source map. | Still canonical detailed architecture source. |
| `docs/BACKEND_DATA_MODEL_V1.md` | partially covered; conflict source; do not delete yet | Detailed DDL, workspace-scoped relationship guidance, V1/V2 timing, search alternatives. | Added coverage row and strengthened data-model skill. | Detailed schema facts still depend on old source. |
| `docs/BACKEND_REFACTOR_RULES.md` | mostly covered; conflict source; do not delete yet | No ABP/SqlSugar, old-project migration rules, old Phase 5 mismatch. | Added coverage row. | Phase map remains conflict-prone. |
| `docs/BACKEND_PHASE1_PROMPT.md` | mostly covered; archive candidate after extraction | Skeleton-only scope, old-project read-only rules, `/api/v1`. | Added phase ledger entry and coverage row. | Historical validation/source trace remains useful. |
| `docs/BACKEND_PHASE1_PROMPT (copy).md` | duplicate; delete candidate after verification | Expected path not found in this round; no unique facts inspectable. | Source map and cleanup plan now require path/hash re-check. | Cannot delete because file was not present to verify. |
| `docs/BACKEND_PHASE2_PROMPT.md` | partially covered; archive candidate after extraction | Seed data, bootstrap/map/document APIs, revision conflict, Phase 2 stubs. | Added phase ledger entry and coverage row. | Detailed seed/API/test facts still mostly in source. |
| `docs/BACKEND_PHASE3_PROMPT.md` | partially covered; conflict source; archive candidate after extraction | Context/activity/search, derived-data writer, link extraction, search ambiguity. | Added phase ledger entry and coverage row. | Detailed workflows still source-dependent. |
| `docs/BACKEND_PHASE4_PROMPT.md` | partially covered; conflict source; archive candidate after extraction | No-op PATCH cleanup, auth choice, workspace member rules, smoke strategy. | Added phase ledger entry and coverage row. | Auth/smoke conflict remains unresolved. |
| `docs/BACKEND_PHASE5_PROMPT.md` | mostly covered; conflict source; do not delete yet | Logout cleanup, lifecycle, import/export, visibility defaults, historical validation. | Added phase ledger entry and coverage row. | Current baseline facts remain source-trace sensitive. |
| `docs/BACKEND_PHASE6_PROMPT.md` | mostly covered; current-phase; do not delete yet | Files/upload APIs, data model, outbox, storage, Tiptap validation, permissions. | Added phase ledger entry and coverage row. | Current Phase 6 source remains needed until code readiness is verified. |
| `docs/PERMISSION_SYSTEM_CONTRACT.md` | partially covered; conflict source; do not delete yet | Permission phases through Phase 11, public-link conflicts, tokens, audit, deferred Phase 12. | Added coverage row. | Contract is too large to replace without a reviewed permission contract. |
| `docs/COMMENT_V1_BETA_COMPLETE.md` | mostly covered; do not delete yet | Comment v1 scope, exclusions, clean JSON, 73-test baseline. | Added coverage row. | Keep version marker until reviewed replacement exists. |
| `docs/COMMENT_PERSISTENCE_CONTRACT.md` | mostly covered; do not delete yet | Comment-specific APIs, no document PATCH piggybacking, runtime-only data. | Added coverage row. | Keep as canonical comment persistence source. |
| `docs/BLOCK_IDENTITY_SCHEMA.md` | mostly covered; conflict source; do not delete yet | Textblock-only `blockId`, generation/repair, migration, anchor integration. | Added coverage row. | Detailed schema still source-dependent. |
| `docs/COMMENT_ANCHOR_RELOCATION_POLICY.md` | mostly covered; do not delete yet | Runtime-only relocation, candidate priority, ambiguity, backend boundary. | Added coverage row. | Keep until reviewed relocation replacement exists. |
| `docs/COMMENT_PRODUCTION_HARDENING_RULES.md` | mostly covered; do not delete yet | Comment race/failure/UI/performance/accessibility rules. | Added coverage row. | Detailed hardening QA remains source-dependent. |
| `docs/COMMENT_UX_SPIKE_QA.md` | weakly covered; do not delete yet | Detailed automated/manual comment QA flows and JSON cleanliness checks. | Added coverage row. | QA details are not fully consolidated. |
| `docs/FRONTEND_COMMENT_UX_SPIKE.md` | partially covered; conflict source; archive candidate after extraction | No-comment-mark rationale, anchor contract, DecorationSet runtime mapping, blockId sequencing conflict. | Added coverage row. | Older rationale still useful for preventing route drift. |

## Facts Migrated Or Strengthened

- Added per-old-root-doc coverage status and cleanup preconditions in `docs/agent/00-source-map.md`.
- Added archive coverage thresholds in `docs/agent/doc-cleanup-plan.md`.
- Added a compact historical backend phase ledger in `docs/agent/skills/backend-phase-execution.md`.
- Strengthened data-model migration guidance for workspace-isolated relationships and search strategy ambiguity in `docs/agent/skills/data-model-migrations.md`.
- Updated duplicate handling notes for `docs/BACKEND_PHASE1_PROMPT (copy).md` because the expected path was not found in this round.

## Conflicts Preserved

- Public collection links remain conflict-marked.
- Public `linkMode` remains conflict-marked.
- Public-link source conflict remains unresolved.
- Public anonymous access boundary remains unresolved except where already documented.
- Backend mainline phase vs permission-module phase mismatch remains preserved.
- Phase 5 scope mismatch remains preserved.
- V1/V2 data model timing mismatch remains preserved.
- Stable `blockId` sequencing conflict remains preserved.
- Search implementation ambiguity remains preserved.
- Auth implementation choice ambiguity remains preserved.
- README/code verification drift remains preserved.
- PostgreSQL smoke gap remains preserved.

## Docs Still Not Safe To Archive

- All canonical docs: architecture, data model, refactor rules, permission contract, comment contracts, files/current phase sources.
- All current-phase docs: `docs/BACKEND_PHASE5_PROMPT.md`, `docs/BACKEND_PHASE6_PROMPT.md`, `docs/PERMISSION_SYSTEM_CONTRACT.md`, `docs/COMMENT_V1_BETA_COMPLETE.md`.
- All conflict-prone docs until their conflicts remain captured in reviewed replacements.
- `docs/COMMENT_UX_SPIKE_QA.md` because detailed QA flow coverage remains weak.

## Archive Candidates After Extraction

- `docs/BACKEND_PHASE1_PROMPT.md`
- `docs/BACKEND_PHASE2_PROMPT.md`
- `docs/BACKEND_PHASE3_PROMPT.md`
- `docs/BACKEND_PHASE4_PROMPT.md`
- `docs/FRONTEND_COMMENT_UX_SPIKE.md`

These are archive candidates only for a later explicit archive task after extraction/review. They were not archived in this round.

## Delete Candidates After Verification

- `docs/BACKEND_PHASE1_PROMPT (copy).md`

The expected copy path was not found during this round. If it reappears, it remains a delete candidate only after path existence, immediate byte/hash verification against `docs/BACKEND_PHASE1_PROMPT.md`, and explicit user approval in a separate delete task.

## Remaining Coverage Gaps

- Detailed permission contract replacement is not available; `docs/PERMISSION_SYSTEM_CONTRACT.md` remains canonical and conflict-prone.
- Detailed comment QA flows are not fully migrated into a dedicated QA governance doc.
- Detailed data-model DDL is not fully migrated into a reviewed data-model contract.
- `apps/web/*.md` docs were intentionally not reviewed in this round.
- `services/api/README.md` operational drift was intentionally not reviewed or code-verified in this round.
- No code verification was performed, so documented implementation status was not upgraded.

## Recommended Next Governance Step

- Create a focused comment QA consolidation doc or decide to intentionally retain `docs/COMMENT_UX_SPIKE_QA.md` as the canonical QA source.

## Not Done

- No application code was modified.
- No backend, frontend, test, migration, package, or project files were modified.
- No old docs were moved, deleted, renamed, or archived.
- No conflicts were resolved.
- No validation commands were run.
- No code implementation status was changed.
