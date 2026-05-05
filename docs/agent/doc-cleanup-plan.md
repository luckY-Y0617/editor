# Documentation Cleanup Plan

This file defines a safe cleanup plan for old project documentation.

It is a plan, not a cleanup action.
Do not delete, move, rename, archive, or rewrite source docs based on this file alone.

## Cleanup Objectives

- Reduce old doc noise.
- Prevent future agents from loading historical prompts by default.
- Keep source traceability until facts are migrated.
- Preserve current phase state.
- Preserve canonical domain contracts.
- Preserve conflict history.
- Avoid deleting unique facts.
- Enable later archive/delete tasks safely.

## Cleanup Status Legend

- `keep canonical`: Keep as authoritative source until reviewed replacement exists.
- `merge facts`: Extract durable project state or domain facts into agent-control or canonical docs.
- `merge rules`: Extract durable constraints or execution rules into agent-control docs.
- `merge conflict`: Extract conflict statements into `docs/agent/02-conflict-register.md`.
- `needs review`: Requires human or code-context review before action.
- `archive after verification`: May be moved to archive in a later explicit archive task.
- `delete after verification`: May be deleted only in a later explicit delete task.
- `do not delete yet`: Must remain because facts, conflicts, or replacement status are unresolved.

## Global Cleanup Rules

- Do not delete canonical docs until reviewed replacement canonical docs exist.
- Do not delete current phase docs until phase state is captured and verified.
- Do not delete domain contracts until equivalent contract docs exist and are reviewed.
- Do not delete conflict-prone docs until conflicts are fully captured in `docs/agent/02-conflict-register.md`.
- Do not delete docs with unique facts not yet migrated.
- Prefer archive before delete.
- Exact duplicates may be deleted only after byte-level verification and explicit user approval.
- Historical prompts must not be loaded by default after cleanup planning.
- Cleanup must be separated from implementation work.
- Cleanup must not change application behavior.
- Cleanup must not update implementation status without code verification.

## Coverage Threshold Before Archive

Archiving is a future explicit task. Before any old root `docs/*.md` file can be archived, all of the following thresholds must be met and recorded in `docs/agent/00-source-map.md` or a reviewed replacement contract:

- Hard rules covered: architecture, dependency direction, old-service boundaries, data-model invariants, API base path, persistence boundaries, validation rules, and security rules are represented in agent-control or contract docs.
- Conflicts captured: every public-link, phase mismatch, V1/V2 timing, search, auth, README drift, and PostgreSQL smoke conflict contributed by the source is present in `docs/agent/02-conflict-register.md` or explicitly marked as already captured.
- Current phase facts captured: Phase 5 completion, Phase 6 files/upload target, permission-track status, comment v1 status, and validation snapshots remain traceable and are clearly marked documented-only when not code-verified.
- Unique facts handled: each unique fact is either migrated into a reviewed agent/control/contract doc or intentionally retained in the old source with a source-map note.
- Validation facts preserved: historical command results, smoke-profile gaps, and environment requirements remain documented without being reported as current validation unless commands actually ran.
- Cleanup action separated: archive/delete is performed only in a later explicit cleanup task, never as part of implementation work.
- Duplicate deletion re-verified: exact duplicate files require immediate hash/path verification and explicit user approval before deletion.

If any threshold is unmet, the doc remains `do not delete yet`. Archive means moving a source to a traceable archive location; it is not deletion and does not resolve conflicts.

## Cleanup Phases

### Phase A: Stabilize Source Map

Goal:

- Review `docs/agent/00-source-map.md`.
- Confirm classifications.
- Fix missing or inaccurate inventory entries before cleanup.

Allowed actions:

- update source map only
- no move/delete/archive

Exit criteria:

- source map reviewed
- needs-review docs identified
- duplicate groups verified

### Phase B: Fill Consolidation Gaps

Goal:

- Create missing consolidation targets before deleting old docs.

Must address:

- files/upload contract exists at `docs/contracts/files-upload-contract.md`; not fully user-reviewed
- frontend general editor skill exists at `docs/agent/skills/frontend-editor.md`; not fully user-reviewed
- `apps/web/FRONTEND_API_CONTRACT.md` and `docs/PERMISSION_SYSTEM_CONTRACT.md` public-link relationship is captured in `docs/agent/02-conflict-register.md` but unresolved
- `services/api` operational validation reference exists at `docs/contracts/backend-operational-validation.md`; README still needs code/tooling verification before migration

Allowed actions:

- create or update new docs only
- do not delete source docs

Exit criteria:

- gaps either resolved or marked as accepted open gaps

### Phase C: Merge Facts and Rules

Goal:

- Move durable facts/rules from historical docs into agent-control docs or future canonical docs.

Allowed actions:

- update agent docs
- update source map
- update cleanup plan

Not allowed:

- delete docs
- archive docs
- change project facts without source support

Exit criteria:

- each source doc has clear migrated/not-migrated status
- conflict register updated if needed in a separate task
- no source doc contains unique untracked facts before archive/delete

### Phase D: Archive Historical Docs

Goal:

- Move historical/superseded docs to archive after facts are migrated.

Allowed actions:

- archive only in a separate explicit archive task
- preserve path mapping
- update source map after archive

Not allowed in this plan:

- actual file moves

Exit criteria:

- archived docs remain traceable
- agent default reading excludes archive

### Phase E: Delete Verified Duplicates Only

Goal:

- Delete exact duplicates only after explicit approval.

Allowed actions:

- delete byte-identical duplicates in a separate explicit delete task

Exit criteria:

- hash verified immediately before deletion
- user explicitly approved deletion
- preferred source remains

## Source Doc Action Matrix

| Source doc | Current classification | Planned action | Target destination | Risk | Preconditions | Human approval required |
|---|---|---|---|---|---|---|
| `docs/BACKEND_ARCHITECTURE_V1.md` | canonical | keep canonical | future reviewed architecture contract | high | replacement canonical reviewed | Yes |
| `docs/BACKEND_DATA_MODEL_V1.md` | canonical, domain-contract, conflict-prone | keep canonical | future reviewed data-model contract | high | V1/V2 timing conflict captured and schema facts migrated | Yes |
| `docs/BACKEND_REFACTOR_RULES.md` | canonical, historical, conflict-prone | merge rules | `docs/agent/01-control-rules.md` | high | old-project rules and Phase 5 scope conflict captured | Yes |
| `apps/web/FRONTEND_API_CONTRACT.md` | canonical, current-phase, domain-contract, conflict-prone | keep canonical; merge conflict | `docs/agent/skills/api-contracts.md`, `docs/agent/02-conflict-register.md` | high | public-link relationship fully captured | Yes |
| `docs/PERMISSION_SYSTEM_CONTRACT.md` | canonical, current-phase, domain-contract, conflict-prone | keep canonical | future reviewed permission contract | critical | public-link conflicts captured and replacement reviewed | Yes |
| `docs/COMMENT_V1_BETA_COMPLETE.md` | canonical, current-phase | keep canonical | future reviewed comment version marker | high | comment v1 facts migrated and replacement reviewed | Yes |
| `docs/COMMENT_PERSISTENCE_CONTRACT.md` | canonical, domain-contract | keep canonical | future reviewed comment contract | high | equivalent contract reviewed | Yes |
| `docs/BLOCK_IDENTITY_SCHEMA.md` | canonical, domain-contract, conflict-prone | keep canonical | future reviewed block identity contract | high | blockId conflict captured and replacement reviewed | Yes |
| `docs/COMMENT_ANCHOR_RELOCATION_POLICY.md` | canonical, domain-contract | keep canonical | future reviewed relocation contract | high | equivalent contract reviewed | Yes |
| `docs/COMMENT_PRODUCTION_HARDENING_RULES.md` | canonical, domain-contract | keep canonical | future reviewed comment hardening contract | high | equivalent contract reviewed | Yes |
| `docs/COMMENT_UX_SPIKE_QA.md` | canonical, domain-contract | merge facts | `docs/agent/05-validation-protocol.md` or future comment QA doc | high | validation facts migrated | Yes |
| `services/api/README.md` | canonical, current-phase, needs-review, conflict-prone | needs review | `docs/contracts/backend-operational-validation.md` plus code/tooling verification before replacement | high | code/tooling verification completed; README facts confirmed or migrated; replacement reviewed | Yes |
| `docs/BACKEND_PHASE1_PROMPT.md` | historical | archive after verification | `docs/archive/` | medium | unique Phase 1 facts migrated | Yes |
| `docs/BACKEND_PHASE1_PROMPT (copy).md` | duplicate, delete-candidate | delete after verification | preferred source `docs/BACKEND_PHASE1_PROMPT.md` | low | file path exists, hash re-verified, and approval granted | Yes |
| `docs/BACKEND_PHASE2_PROMPT.md` | historical | merge facts; archive after verification | `docs/agent/skills/backend-phase-execution.md` | medium | seed/API/revision facts migrated | Yes |
| `docs/BACKEND_PHASE3_PROMPT.md` | historical, conflict-prone | merge facts; merge conflict; archive after verification | `docs/agent/skills/backend-phase-execution.md`, `docs/agent/02-conflict-register.md` | high | search ambiguity and context/activity facts migrated | Yes |
| `docs/BACKEND_PHASE4_PROMPT.md` | historical, conflict-prone | merge facts; archive after verification | `docs/agent/skills/permissions.md`, `docs/agent/05-validation-protocol.md` | high | auth choice and smoke facts migrated | Yes |
| `docs/BACKEND_PHASE5_PROMPT.md` | current-phase, conflict-prone | merge facts; do not delete yet | `docs/agent/00-project-state.md`, `docs/agent/skills/backend-phase-execution.md` | high | Phase 5 state captured and verified | Yes |
| `docs/BACKEND_PHASE6_PROMPT.md` | current-phase, domain-contract, conflict-prone | keep canonical | future standalone files/upload contract | high | Phase 6 facts migrated into reviewed file contract | Yes |
| `docs/FRONTEND_COMMENT_UX_SPIKE.md` | historical, superseded, conflict-prone | merge facts; merge conflict; archive after verification | `docs/agent/skills/comments.md`, `docs/agent/skills/frontend-tiptap-comments.md`, `docs/agent/02-conflict-register.md` | high | anchor rationale and blockId sequencing facts migrated | Yes |
| `apps/web/AI_EDITOR_GUARDRAILS.md` | historical, needs-review | needs review | future frontend editor skill/guardrail | high | frontend editor facts reviewed and migrated | Yes |
| `apps/web/EDITOR_QA_CHECKLIST.md` | historical, needs-review | needs review | validation docs or future frontend editor skill | high | QA facts reviewed and migrated | Yes |
| `AGENTS.md` | agent-control | do not delete yet | active root agent entry | high | replacement entry point reviewed | Yes |
| `docs/agent/00-source-map.md` | agent-control | do not delete yet | active inventory | high | cleanup completed and replacement inventory reviewed | Yes |
| `docs/agent/00-project-state.md` | agent-control | do not delete yet | active project fact layer | high | replacement project-state doc reviewed | Yes |
| `docs/agent/01-control-rules.md` | agent-control | do not delete yet | active control layer | high | replacement control layer reviewed | Yes |
| `docs/agent/02-conflict-register.md` | agent-control | do not delete yet | active conflict register | critical | conflicts fully resolved or replacement reviewed | Yes |
| `docs/agent/03-required-reading-order.md` | agent-control | do not delete yet | active reading rules | high | replacement reading rules reviewed | Yes |
| `docs/agent/04-implementation-protocol.md` | agent-control | do not delete yet | active implementation protocol | high | replacement implementation protocol reviewed | Yes |
| `docs/agent/05-validation-protocol.md` | agent-control | do not delete yet | active validation protocol | high | replacement validation protocol reviewed | Yes |
| `docs/agent/06-final-report-format.md` | agent-control | do not delete yet | active report format | medium | replacement report format reviewed | Yes |
| `docs/agent/skills/backend-clean-architecture.md` | agent-control | do not delete yet | active backend architecture skill | high | replacement skill reviewed | Yes |
| `docs/agent/skills/backend-phase-execution.md` | agent-control | do not delete yet | active phase execution skill | high | replacement skill reviewed | Yes |
| `docs/agent/skills/data-model-migrations.md` | agent-control | do not delete yet | active data/migration skill | high | replacement skill reviewed | Yes |
| `docs/agent/skills/files-upload.md` | agent-control | do not delete yet | active files/upload skill | high | replacement skill reviewed | Yes |
| `docs/agent/skills/api-contracts.md` | agent-control | do not delete yet | active API contracts skill | high | replacement skill reviewed | Yes |
| `docs/agent/skills/permissions.md` | agent-control | do not delete yet | active permissions skill | critical | replacement skill reviewed | Yes |
| `docs/agent/skills/comments.md` | agent-control | do not delete yet | active comments skill | high | replacement skill reviewed | Yes |
| `docs/agent/skills/frontend-tiptap-comments.md` | agent-control | do not delete yet | active frontend Tiptap comments skill | high | replacement skill reviewed | Yes |

## Keep Canonical

- Path: `docs/BACKEND_ARCHITECTURE_V1.md`
  - Reason: Authoritative backend architecture and dependency source.
  - Replacement needed before removal: reviewed backend architecture contract.

- Path: `docs/BACKEND_DATA_MODEL_V1.md`
  - Reason: Authoritative data model and schema source.
  - Replacement needed before removal: reviewed data model contract with conflict history preserved.

- Path: `docs/BACKEND_REFACTOR_RULES.md`
  - Reason: Authoritative rebuild/refactor guardrail source with old-project boundaries.
  - Replacement needed before removal: reviewed refactor/control rules and Phase 5 conflict captured.

- Path: `apps/web/FRONTEND_API_CONTRACT.md`
  - Reason: Frontend-facing API contract and phase contract updates.
  - Replacement needed before removal: reviewed API contract and public-link conflict captured.

- Path: `docs/PERMISSION_SYSTEM_CONTRACT.md`
  - Reason: Canonical permission module contract.
  - Replacement needed before removal: reviewed permission contract with public-link conflicts resolved or preserved.

- Path: `docs/COMMENT_V1_BETA_COMPLETE.md`
  - Reason: Current comment v1 version boundary.
  - Replacement needed before removal: reviewed comment version marker.

- Path: `docs/COMMENT_PERSISTENCE_CONTRACT.md`
  - Reason: Comment persistence boundary.
  - Replacement needed before removal: reviewed comment persistence contract.

- Path: `docs/BLOCK_IDENTITY_SCHEMA.md`
  - Reason: Block identity rules.
  - Replacement needed before removal: reviewed block identity contract.

- Path: `docs/COMMENT_ANCHOR_RELOCATION_POLICY.md`
  - Reason: Runtime relocation rules.
  - Replacement needed before removal: reviewed relocation contract.

- Path: `docs/COMMENT_PRODUCTION_HARDENING_RULES.md`
  - Reason: Comment v1 hardening rules.
  - Replacement needed before removal: reviewed hardening contract or QA protocol.

- Path: `docs/COMMENT_UX_SPIKE_QA.md`
  - Reason: Comment validation and QA coverage.
  - Replacement needed before removal: reviewed comment QA/validation doc.

- Path: `services/api/README.md`
  - Reason: Backend operational commands and smoke profile source.
  - Replacement needed before removal: code-verified review of `docs/contracts/backend-operational-validation.md` and README/tooling drift.

- Path: `docs/BACKEND_PHASE5_PROMPT.md`
  - Reason: Source for Phase 5 state and baseline details.
  - Replacement needed before removal: verified phase-state capture.

- Path: `docs/BACKEND_PHASE6_PROMPT.md`
  - Reason: Current backend Phase 6 target and files/upload contract source.
  - Replacement needed before removal: standalone reviewed files/upload canonical contract.

## Merge Into Agent-Control Docs

- Path: `docs/BACKEND_REFACTOR_RULES.md`
  - Merge type: rules/conflict
  - Target: `docs/agent/01-control-rules.md`, `docs/agent/02-conflict-register.md`
  - Unique facts to preserve: old-project boundaries, no ABP/SqlSugar, phase map conflict.
  - Preconditions: replacement rules reviewed.

- Path: `docs/BACKEND_PHASE1_PROMPT.md`
  - Merge type: facts/phase
  - Target: `docs/agent/skills/backend-phase-execution.md`
  - Unique facts to preserve: initial skeleton creation constraints and old-project read-only rules.
  - Preconditions: facts migrated and duplicate copy handled separately.

- Path: `docs/BACKEND_PHASE2_PROMPT.md`
  - Merge type: facts/API/phase
  - Target: `docs/agent/skills/backend-phase-execution.md`, `docs/agent/skills/api-contracts.md`
  - Unique facts to preserve: seed data, bootstrap/map/document APIs, revision conflict tests.
  - Preconditions: durable facts migrated.

- Path: `docs/BACKEND_PHASE3_PROMPT.md`
  - Merge type: facts/conflict/API
  - Target: `docs/agent/skills/backend-phase-execution.md`, `docs/agent/02-conflict-register.md`
  - Unique facts to preserve: context/activity/search, link extraction, search ambiguity.
  - Preconditions: search conflict verified in register.

- Path: `docs/BACKEND_PHASE4_PROMPT.md`
  - Merge type: facts/conflict/validation
  - Target: `docs/agent/skills/permissions.md`, `docs/agent/05-validation-protocol.md`
  - Unique facts to preserve: auth choices, no-op PATCH cleanup, PostgreSQL smoke strategy.
  - Preconditions: auth and smoke conflicts fully captured.

- Path: `docs/BACKEND_PHASE5_PROMPT.md`
  - Merge type: facts/API/phase
  - Target: `docs/agent/00-project-state.md`, `docs/agent/skills/backend-phase-execution.md`
  - Unique facts to preserve: lifecycle/import/export, logout cleanup, Phase 5 validation details.
  - Preconditions: phase state verified and captured.

- Path: `docs/COMMENT_UX_SPIKE_QA.md`
  - Merge type: validation
  - Target: `docs/agent/05-validation-protocol.md`, future comment QA target
  - Unique facts to preserve: automated regression categories and manual browser QA flows.
  - Preconditions: comment QA target reviewed.

- Path: `docs/FRONTEND_COMMENT_UX_SPIKE.md`
  - Merge type: facts/conflict
  - Target: `docs/agent/skills/comments.md`, `docs/agent/skills/frontend-tiptap-comments.md`, `docs/agent/02-conflict-register.md`
  - Unique facts to preserve: anchor rationale, no comment mark rationale, original blockId sequencing conflict.
  - Preconditions: conflict fully captured and durable facts migrated.

- Path: `apps/web/FRONTEND_API_CONTRACT.md`
  - Merge type: conflict/API
  - Target: `docs/agent/skills/api-contracts.md`, `docs/agent/02-conflict-register.md`
  - Unique facts to preserve: public-link frontend contract relationship to permission contract.
  - Preconditions: public-link conflicts fully captured.

## Needs Manual Review

- Path: `apps/web/AI_EDITOR_GUARDRAILS.md`
  - Why review is required: Large frontend/editor phase guardrail with possible unique product/editor rules outside current comment skills.
  - Suggested reviewer: frontend/editor owner.
  - Decision needed: extract into future frontend editor skill, archive, or keep canonical.

- Path: `apps/web/EDITOR_QA_CHECKLIST.md`
  - Why review is required: Large editor QA checklist with validation coverage outside current comment QA.
  - Suggested reviewer: frontend QA/editor owner.
  - Decision needed: migrate to validation protocol or future frontend editor QA doc.

- Path: `services/api/README.md`
  - Why review is required: Operational README may reflect current code/tooling, but code verification was not performed.
  - Suggested reviewer: backend owner.
  - Decision needed: keep canonical, migrate commands to validation docs, or split operational docs.

## Archive After Verification

- Path: `docs/BACKEND_PHASE1_PROMPT.md`
  - Archive only after: Phase 1 facts migrated and duplicate copy decision completed.
  - Suggested archive location: `docs/archive/backend-phases/`
  - Traceability requirement: source map records original path and preferred source.

- Path: `docs/BACKEND_PHASE2_PROMPT.md`
  - Archive only after: seed/API/revision facts migrated.
  - Suggested archive location: `docs/archive/backend-phases/`
  - Traceability requirement: source map records original path and migrated targets.

- Path: `docs/BACKEND_PHASE3_PROMPT.md`
  - Archive only after: context/activity/search facts and search conflict migrated.
  - Suggested archive location: `docs/archive/backend-phases/`
  - Traceability requirement: conflict register points to archived source.

- Path: `docs/BACKEND_PHASE4_PROMPT.md`
  - Archive only after: auth/no-op PATCH/smoke facts migrated.
  - Suggested archive location: `docs/archive/backend-phases/`
  - Traceability requirement: conflict register and source map updated.

- Path: `docs/FRONTEND_COMMENT_UX_SPIKE.md`
  - Archive only after: anchor rationale and blockId sequencing conflict migrated.
  - Suggested archive location: `docs/archive/comment-spikes/`
  - Traceability requirement: comment skill and conflict register cite archived source.

## Delete After Verification

- Path: `docs/BACKEND_PHASE1_PROMPT (copy).md`
  - Delete only after: user explicitly approves deletion in a separate cleanup task.
  - Verification required: file path exists; SHA256 hash verified immediately before deletion and matches `docs/BACKEND_PHASE1_PROMPT.md`.
  - Preferred source: `docs/BACKEND_PHASE1_PROMPT.md`
  - Round 11 note: expected copy path was not found during old root doc inspection.

## Conflict Follow-Up

- Conflict area: public-link frontend/API contract vs permission contract
  - Source docs: `apps/web/FRONTEND_API_CONTRACT.md`, `docs/PERMISSION_SYSTEM_CONTRACT.md`
  - Status: captured in `docs/agent/02-conflict-register.md`; not resolved.
  - Required follow-up: preserve unresolved public-link behavior until an explicit clarification task selects behavior.

- Conflict area: operational state drift and PostgreSQL smoke
  - Source docs: `services/api/README.md`, `docs/BACKEND_PHASE5_PROMPT.md`, `docs/BACKEND_PHASE6_PROMPT.md`
  - Status: captured in `docs/agent/02-conflict-register.md`; not resolved.
  - Required follow-up: compare README commands/smoke profile to current code/tooling before updating validation docs or project-state docs.

## Missing Consolidation Targets

- Target: standalone files/upload canonical contract
  - Status: created at `docs/contracts/files-upload-contract.md`; not fully user-reviewed.
  - Why needed: files/upload facts are spread across several docs.
  - Source docs: `docs/BACKEND_PHASE6_PROMPT.md`, `docs/BACKEND_DATA_MODEL_V1.md`, `apps/web/FRONTEND_API_CONTRACT.md`, `services/api/README.md`
  - Required before: archiving Phase 6 prompt or relying only on agent skills.

- Target: frontend general editor skill or guardrail doc
  - Status: created at `docs/agent/skills/frontend-editor.md`; not fully user-reviewed.
  - Why needed: broad editor rules are not covered by current comment-only frontend skill.
  - Source docs: `apps/web/AI_EDITOR_GUARDRAILS.md`
  - Required before: archiving frontend editor guardrails.

- Target: editor QA validation consolidation
  - Why needed: editor QA checklist is large and outside current validation protocol.
  - Source docs: `apps/web/EDITOR_QA_CHECKLIST.md`
  - Required before: archiving or reducing QA checklist.

- Target: `services/api` operational validation/code verification notes
  - Status: created at `docs/contracts/backend-operational-validation.md`; not code-verified or fully user-reviewed.
  - Why needed: README may contain code-derived commands and smoke behavior.
  - Source docs: `services/api/README.md`
  - Required before: migrating or archiving README content.

- Target: public-link conflict clarification or stronger conflict-register entry
  - Status: captured in `docs/agent/02-conflict-register.md`; not resolved.
  - Why needed: frontend API contract and permission contract public-link behavior remains conflict-marked.
  - Source docs: `apps/web/FRONTEND_API_CONTRACT.md`, `docs/PERMISSION_SYSTEM_CONTRACT.md`
  - Required before: archiving either public-link source.

## Execution Order

1. Review source map.
2. Create missing consolidation targets.
3. Merge durable facts/rules from historical docs.
4. Update conflict register in a separate task only if a later review finds missing conflict entries.
5. Update source map and cleanup plan after merges.
6. Archive historical docs in a separate explicit task.
7. Delete exact duplicates in a separate explicit task after hash verification and user approval.

## Hard Stops

- A doc is canonical and no reviewed replacement exists.
- A doc is current-phase and phase state is not fully captured.
- A doc is conflict-prone and conflict is not fully captured.
- A doc has unique facts not migrated.
- A doc is needs-review.
- Code verification is required but not performed.
- User has not explicitly approved delete/archive task.
- The cleanup task tries to mix doc cleanup with implementation work.

## Final Pre-Deletion Checklist

- Is this doc canonical?
- Is this doc current-phase?
- Is this doc conflict-prone?
- Does this doc contain unique facts?
- Were facts migrated?
- Were conflicts captured?
- Is there a reviewed replacement?
- Was code verification required?
- Was code verification completed?
- Is this an exact duplicate?
- Was hash verified immediately before deletion?
- Did the user explicitly approve deletion?
- Is the preferred source retained?

If any risky answer remains unresolved, deletion must not proceed.
