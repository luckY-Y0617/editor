# Source Map

This file maps existing project documentation to the new agent-control documentation system.

It is an inventory, not a cleanup action.
Do not delete, move, or rewrite source docs based on this file alone.

## Classification Labels

- `canonical`: Authoritative source for a domain or contract.
- `current-phase`: Defines current or near-term phase execution state.
- `domain-contract`: Defines strict behavior, API, persistence, or UI rules for a domain.
- `agent-control`: New control-layer docs for future agents.
- `historical`: Older planning, prompt, spike, or phase material retained for context.
- `superseded`: Older material partly replaced by later docs, but not safe to delete.
- `conflict-prone`: Contains or contributes to documented ambiguities/conflicts.
- `duplicate`: Exact or near duplicate of another source doc.
- `delete-candidate`: Candidate for later deletion only after verification and approval.
- `do-not-delete-yet`: Must remain until facts, conflicts, and unique details are migrated.
- `needs-review`: Cannot be confidently classified without human or code-context review.

## Old Root Doc Coverage Labels

Round 11 adds coverage labels for old root docs directly under `docs/*.md`.
These labels describe documentation coverage only. They do not prove current code implementation.

- `fully covered`: Durable facts appear to be represented in agent-control or contract docs.
- `mostly covered`: Most durable facts are represented; source remains useful for traceability or detail.
- `partially covered`: Important facts are represented, but route/workflow/detail coverage still depends on the old source.
- `weakly covered`: Only broad summary coverage exists.
- `not covered`: No reliable replacement coverage was found during inspection.
- `duplicate`: Source is an exact or near duplicate candidate, subject to re-verification.
- `conflict source`: Source contributes to an unresolved conflict.
- `do not delete yet`: Source must remain available.
- `archive candidate after extraction`: Source may be archived only in a later explicit archive task after extraction/review.
- `delete candidate after verification`: Source may be deleted only in a later explicit delete task after immediate verification and approval.

## Old Root Docs Coverage Status

This table covers only old markdown files directly under `docs/*.md`. It excludes `docs/agent/**`, `docs/contracts/**`, and frontend docs under `apps/web/**`.

| Old root doc | Coverage status | Unique facts or coverage note | Cleanup precondition |
|---|---|---|---|
| `docs/BACKEND_ARCHITECTURE_V1.md` | mostly covered; do not delete yet | Clean Architecture, dependency direction, old-service boundaries, `/api/v1`, files flow, outbox, and PostgreSQL runtime shape are covered in agent rules/skills, but this remains the most detailed architecture source. | Keep until a reviewed architecture contract replaces it. |
| `docs/BACKEND_DATA_MODEL_V1.md` | partially covered; conflict source; do not delete yet | Core schema invariants are covered, but detailed DDL, composite workspace relationship guidance, V1/V2 timing, and search-index alternatives still require source traceability. | Keep until data-model contract is reviewed and V1/V2/search conflicts stay preserved. |
| `docs/BACKEND_REFACTOR_RULES.md` | mostly covered; conflict source; do not delete yet | No-ABP/SqlSugar, old-project boundaries, migration rules, and `/api/v1` are covered; old phase map remains a Phase 5 scope conflict source. | Keep until refactor rules are fully represented and phase conflict remains captured. |
| `docs/BACKEND_PHASE1_PROMPT.md` | mostly covered; archive candidate after extraction | Skeleton, project layout, `/api/v1`, old-project read-only boundaries, and no feature expansion are represented; historical validation details are retained only in source. | Archive only after phase ledger extraction is reviewed. |
| `docs/BACKEND_PHASE1_PROMPT (copy).md` | duplicate; delete candidate after verification | Expected path was not found in Round 11 inspection; existing source-map text previously recorded a byte-identical duplicate hash. No unique facts were inspectable this round. | Re-check path and hash immediately before any separate delete task; do not delete from this inventory. |
| `docs/BACKEND_PHASE2_PROMPT.md` | partially covered; archive candidate after extraction | Phase 2 bootstrap/map/document scope, seed data, revision conflict, and temporary context/activity/search stubs are only summarized in agent docs. | Archive only after backend phase ledger/API facts are reviewed. |
| `docs/BACKEND_PHASE3_PROMPT.md` | partially covered; conflict source; archive candidate after extraction | Context/activity/search, derived-data maintenance, link extraction, and search implementation ambiguity are represented but detailed workflow/testing still lives in source. | Archive only after phase/search facts are reviewed and conflict register remains intact. |
| `docs/BACKEND_PHASE4_PROMPT.md` | partially covered; conflict source; archive candidate after extraction | Auth/current-user/workspace-member rules, no-op PATCH cleanup, auth choice ambiguity, and PostgreSQL smoke strategy are represented; detailed auth and QA scope still lives in source. | Archive only after auth/smoke/no-op facts are reviewed. |
| `docs/BACKEND_PHASE5_PROMPT.md` | mostly covered; conflict source; do not delete yet | Phase 5 completion, lifecycle/import/export, logout cleanup, visibility defaults, and smoke gap are represented at summary level. | Keep until Phase 5 state and validation history are reviewed against code or accepted as documented-only. |
| `docs/BACKEND_PHASE6_PROMPT.md` | mostly covered; current-phase; do not delete yet | Phase 6 files/upload is consolidated in `docs/contracts/files-upload-contract.md`, but source remains the detailed current-phase prompt. | Keep until files/upload contract is reviewed and current code status is verified where needed. |
| `docs/PERMISSION_SYSTEM_CONTRACT.md` | partially covered; conflict source; do not delete yet | Permission phases, central effective permission, token/audit rules, deferred items, public-link conflicts, and testing gates are represented but the contract remains canonical. | Keep until a reviewed permission contract replaces it and conflicts remain preserved. |
| `docs/COMMENT_V1_BETA_COMPLETE.md` | mostly covered; do not delete yet | Comment v1 scope, exclusions, clean JSON, blockId, relocation, and 73-test baseline are represented. | Keep until comment version marker is reviewed or replaced. |
| `docs/COMMENT_PERSISTENCE_CONTRACT.md` | mostly covered; do not delete yet | Comment-specific persistence, repository boundary, no `PATCH /documents` piggybacking, and runtime-only data rules are represented. | Keep until reviewed comment persistence replacement exists. |
| `docs/BLOCK_IDENTITY_SCHEMA.md` | mostly covered; conflict source; do not delete yet | Textblock-only `blockId`, generation/repair, migration, and comment anchor integration are represented; this remains detailed schema source. | Keep until reviewed block identity replacement exists. |
| `docs/COMMENT_ANCHOR_RELOCATION_POLICY.md` | mostly covered; do not delete yet | Runtime-only relocation, candidate priority, ambiguity, and backend boundary are represented. | Keep until reviewed relocation replacement exists. |
| `docs/COMMENT_PRODUCTION_HARDENING_RULES.md` | mostly covered; do not delete yet | Failure, race, UI state, performance, accessibility, and no-scope-expansion rules are represented at skill level. | Keep until reviewed hardening/QA replacement exists. |
| `docs/COMMENT_UX_SPIKE_QA.md` | weakly covered; do not delete yet | Automated and manual browser QA scenarios are only summarized in validation/skill docs; detailed QA flows still live here. | Do not archive until comment QA coverage is intentionally consolidated or retained. |
| `docs/FRONTEND_COMMENT_UX_SPIKE.md` | partially covered; conflict source; archive candidate after extraction | No-comment-mark rationale, anchor model, DecorationSet runtime mapping, thread-vs-anchor lifecycle, and initial blockId sequencing conflict are represented but older rationale remains useful. | Archive only after anchor rationale and conflict source context are reviewed. |

## Canonical Sources

- Path: `docs/BACKEND_ARCHITECTURE_V1.md`
  - Domain: backend architecture
  - Why canonical: Defines ASP.NET Core Modular Monolith + Clean Architecture, project layout, dependency direction, runtime architecture, and old-project boundaries.
  - Should feed into: `docs/agent/01-control-rules.md`, `docs/agent/skills/backend-clean-architecture.md`
  - Safe to delete now: No

- Path: `docs/BACKEND_DATA_MODEL_V1.md`
  - Domain: data model
  - Why canonical: Defines core PostgreSQL schema, workspace scoping, document metadata/body split, revision/version split, and V2 extension boundaries.
  - Should feed into: `docs/agent/00-project-state.md`, `docs/agent/skills/data-model-migrations.md`, `docs/agent/02-conflict-register.md`
  - Safe to delete now: No

- Path: `docs/BACKEND_REFACTOR_RULES.md`
  - Domain: backend refactor rules
  - Why canonical: Defines execution rules for backend rebuild and old-project migration boundaries.
  - Should feed into: `docs/agent/01-control-rules.md`, `docs/agent/skills/backend-clean-architecture.md`, `docs/agent/skills/backend-phase-execution.md`
  - Safe to delete now: No

- Path: `apps/web/FRONTEND_API_CONTRACT.md`
  - Domain: API contracts
  - Why canonical: Defines frontend-facing API contract and later backend contract updates for lifecycle, files, and permissions.
  - Should feed into: `docs/agent/skills/api-contracts.md`, `docs/agent/skills/files-upload.md`, `docs/agent/02-conflict-register.md`
  - Safe to delete now: No

- Path: `docs/PERMISSION_SYSTEM_CONTRACT.md`
  - Domain: permissions
  - Why canonical: States it is the canonical implementation boundary for permissions, roles, effective authorization, schema, API surface, UI rules, audit, security, and rollout.
  - Should feed into: `docs/agent/00-project-state.md`, `docs/agent/02-conflict-register.md`, `docs/agent/skills/permissions.md`
  - Safe to delete now: No

- Path: `docs/COMMENT_V1_BETA_COMPLETE.md`
  - Domain: comments
  - Why canonical: Defines `comment v1 / beta-complete` scope, included behavior, exclusions, validation baseline, and follow-up policy.
  - Should feed into: `docs/agent/00-project-state.md`, `docs/agent/skills/comments.md`, `docs/agent/skills/frontend-tiptap-comments.md`
  - Safe to delete now: No

- Path: `docs/COMMENT_PERSISTENCE_CONTRACT.md`
  - Domain: comment persistence
  - Why canonical: Defines backend persistence boundary, clean document JSON, runtime reconstruction, backend API boundary, and out-of-scope behavior.
  - Should feed into: `docs/agent/skills/comments.md`, `docs/agent/skills/api-contracts.md`
  - Safe to delete now: No

- Path: `docs/BLOCK_IDENTITY_SCHEMA.md`
  - Domain: block identity
  - Why canonical: Defines implemented stable `blockId` structural identity rules for comment-addressable textblocks.
  - Should feed into: `docs/agent/skills/comments.md`, `docs/agent/skills/frontend-tiptap-comments.md`, `docs/agent/02-conflict-register.md`
  - Safe to delete now: No

- Path: `docs/COMMENT_ANCHOR_RELOCATION_POLICY.md`
  - Domain: comment relocation
  - Why canonical: Defines implemented frontend runtime load-time relocation policy and persistence boundary.
  - Should feed into: `docs/agent/skills/comments.md`, `docs/agent/skills/frontend-tiptap-comments.md`
  - Safe to delete now: No

- Path: `docs/COMMENT_PRODUCTION_HARDENING_RULES.md`
  - Domain: comment production hardening
  - Why canonical: Defines implemented reliability, failure-state, race, UI state, performance, and accessibility requirements for comment v1.
  - Should feed into: `docs/agent/skills/comments.md`, `docs/agent/skills/frontend-tiptap-comments.md`
  - Safe to delete now: No

- Path: `docs/COMMENT_UX_SPIKE_QA.md`
  - Domain: comment validation/testing
  - Why canonical: Defines comment v1 QA checklist, regression test expectations, and manual browser QA flows.
  - Should feed into: `docs/agent/05-validation-protocol.md`, `docs/agent/skills/comments.md`, `docs/agent/skills/frontend-tiptap-comments.md`
  - Safe to delete now: No

- Path: `services/api/README.md`
  - Domain: backend local operations and validation
  - Why canonical: Defines local setup, commands, authentication, files, endpoints, search strategy, and PostgreSQL smoke profile for `services/api`.
  - Should feed into: `docs/agent/05-validation-protocol.md`, `docs/agent/skills/files-upload.md`
  - Safe to delete now: No

- Path: `docs/contracts/files-upload-contract.md`
  - Domain: files/upload
  - Why canonical: Standalone files/upload contract consolidating Phase 6 upload sessions, files, document attachments, access, outbox, Tiptap validation, import/export boundaries, permissions, and object storage boundaries.
  - Should feed into: `docs/agent/skills/files-upload.md`
  - Safe to delete now: No

- Path: `docs/contracts/backend-operational-validation.md`
  - Domain: backend operational validation
  - Why canonical: Consolidates backend command discovery, restore/build/test expectations, EF migration validation, PostgreSQL smoke requirements, files/object-storage validation notes, README drift handling, and validation reporting boundaries.
  - Should feed into: `docs/agent/05-validation-protocol.md`
  - Safe to delete now: No

## Current Phase Sources

- Path: `docs/BACKEND_PHASE5_PROMPT.md`
  - Phase/Track: backend mainline Phase 5
  - Current-state facts found: Phase 4 baseline APIs, Phase 4 migration, 25 tests passed, logout cleanup target, lifecycle/import/export target, PostgreSQL smoke profile behavior.
  - Should feed into: `docs/agent/00-project-state.md`, `docs/agent/skills/backend-phase-execution.md`, `docs/agent/skills/api-contracts.md`
  - Safe to delete now: No

- Path: `docs/BACKEND_PHASE6_PROMPT.md`
  - Phase/Track: backend mainline Phase 6
  - Current-state facts found: Phase 5 completed, 37 tests passed, migration script generated, no pending model changes, PostgreSQL smoke not run when env var absent, Phase 6 files/upload target.
  - Should feed into: `docs/agent/00-project-state.md`, `docs/agent/skills/backend-phase-execution.md`, `docs/agent/skills/files-upload.md`
  - Safe to delete now: No

- Path: `docs/COMMENT_V1_BETA_COMPLETE.md`
  - Phase/Track: comment v1
  - Current-state facts found: `comment v1 / beta-complete`, included behavior, excluded V1 features, 73 regression tests at definition time, Vite large-chunk warning.
  - Should feed into: `docs/agent/00-project-state.md`, `docs/agent/skills/comments.md`, `docs/agent/skills/frontend-tiptap-comments.md`
  - Safe to delete now: No

- Path: `docs/PERMISSION_SYSTEM_CONTRACT.md`
  - Phase/Track: permission module through Phase 11
  - Current-state facts found: permission module through Phase 11, deferred Phase 12 items, public-link conflicts, separate permission track.
  - Should feed into: `docs/agent/00-project-state.md`, `docs/agent/02-conflict-register.md`, `docs/agent/skills/permissions.md`
  - Safe to delete now: No

- Path: `apps/web/FRONTEND_API_CONTRACT.md`
  - Phase/Track: frontend/API contract updates through backend Phase 6 and permission Phase 10/11
  - Current-state facts found: lifecycle/export/import contract, Phase 6 file API contract, permissions/share-link/frontend contract updates.
  - Should feed into: `docs/agent/skills/api-contracts.md`, `docs/agent/skills/files-upload.md`, `docs/agent/skills/permissions.md`
  - Safe to delete now: No

- Path: `services/api/README.md`
  - Phase/Track: backend local setup/current API operational notes
  - Current-state facts found: commands, endpoints, authentication, files, search strategy, PostgreSQL smoke profile.
  - Should feed into: `docs/agent/05-validation-protocol.md`, `docs/agent/skills/backend-phase-execution.md`
  - Safe to delete now: No

## Domain Contracts

- Path: `docs/PERMISSION_SYSTEM_CONTRACT.md`
  - Domain: permissions
  - Contract facts: role model, effective permission algorithm, permission schema, API surface, share links, invites, audit, notifications, security, rollout.
  - Should feed into: `docs/agent/skills/permissions.md`, `docs/agent/02-conflict-register.md`
  - Safe to delete now: No

- Path: `docs/COMMENT_PERSISTENCE_CONTRACT.md`
  - Domain: comments
  - Contract facts: comment resources external to document content, backend API boundary, persisted and non-persisted data, runtime reconstruction.
  - Should feed into: `docs/agent/skills/comments.md`
  - Safe to delete now: No

- Path: `docs/BLOCK_IDENTITY_SCHEMA.md`
  - Domain: frontend editor/Tiptap comments
  - Contract facts: textblock-only structural `blockId`, generation/repair, migration helper, comment anchor integration.
  - Should feed into: `docs/agent/skills/frontend-tiptap-comments.md`
  - Safe to delete now: No

- Path: `docs/COMMENT_ANCHOR_RELOCATION_POLICY.md`
  - Domain: frontend editor/Tiptap comments
  - Contract facts: runtime-only relocation, candidate priority, blockId rules, quote matching, ambiguity rules, backend boundary.
  - Should feed into: `docs/agent/skills/frontend-tiptap-comments.md`
  - Safe to delete now: No

- Path: `docs/COMMENT_PRODUCTION_HARDENING_RULES.md`
  - Domain: comments/frontend editor
  - Contract facts: loading, retry, failure state, race rules, interaction, performance, accessibility, testing requirements.
  - Should feed into: `docs/agent/skills/comments.md`, `docs/agent/skills/frontend-tiptap-comments.md`
  - Safe to delete now: No

- Path: `apps/web/FRONTEND_API_CONTRACT.md`
  - Domain: API contracts
  - Contract facts: routes, DTO shapes, frontend flows, lifecycle/export/import, Phase 6 files, permissions/share-link contracts.
  - Should feed into: `docs/agent/skills/api-contracts.md`
  - Safe to delete now: No

- Path: `docs/BACKEND_DATA_MODEL_V1.md`
  - Domain: data model
  - Contract facts: V1 tables, DDL skeleton, endpoint-to-table mapping, extension tables for files/comments/permissions/Yjs/AI.
  - Should feed into: `docs/agent/skills/data-model-migrations.md`
  - Safe to delete now: No

- Path: `docs/BACKEND_PHASE6_PROMPT.md`
  - Domain: files/upload
  - Contract facts: Phase 6 upload session, files, document attachments, storage provider, Tiptap file reference, permission, import/export file boundary.
  - Should feed into: `docs/agent/skills/files-upload.md`
  - Safe to delete now: No

- Path: `services/api/README.md`
  - Domain: validation/testing and files
  - Contract facts: local command expectations, file API operational notes, PostgreSQL smoke profile.
  - Should feed into: `docs/agent/05-validation-protocol.md`, `docs/agent/skills/files-upload.md`
  - Safe to delete now: No

## Agent-Control Docs

- Path or group: `AGENTS.md`
  - Role: Root entry point for future agents.
  - Source dependency: Derived from backend, comment, permission, file, validation, and conflict source docs.
  - Safe to delete now: No

- Path or group:
  - `docs/agent/00-project-state.md`
  - `docs/agent/01-control-rules.md`
  - `docs/agent/02-conflict-register.md`
  - `docs/agent/03-required-reading-order.md`
  - `docs/agent/04-implementation-protocol.md`
  - `docs/agent/05-validation-protocol.md`
  - `docs/agent/06-final-report-format.md`
  - Role: First-round agent-control foundation.
  - Source dependency: Extracted from architecture, data model, backend phase, permission, comment, frontend API, and validation docs.
  - Safe to delete now: No

- Path or group:
  - `docs/agent/skills/backend-clean-architecture.md`
  - `docs/agent/skills/backend-phase-execution.md`
  - `docs/agent/skills/data-model-migrations.md`
  - `docs/agent/skills/files-upload.md`
  - Role: Round 2 backend/data/files skill docs.
  - Source dependency: Extracted from backend architecture, refactor, data model, phase, files, and validation docs.
  - Safe to delete now: No

- Path or group:
  - `docs/agent/skills/api-contracts.md`
  - `docs/agent/skills/permissions.md`
  - `docs/agent/skills/comments.md`
  - `docs/agent/skills/frontend-editor.md`
  - `docs/agent/skills/frontend-tiptap-comments.md`
  - Role: Round 3 API/permissions/comments/frontend comment skill docs plus general frontend editor skill.
  - Source dependency: Extracted from frontend API contract, permission system contract, comment persistence, block identity, relocation, frontend editor guardrail, and QA docs.
  - Safe to delete now: No

## Historical / Superseded Docs

- Path: `docs/BACKEND_PHASE1_PROMPT.md`
  - Reason: Early phase prompt for creating backend skeleton.
  - Important facts not yet migrated: Phase 1 starting constraints, old-project read-only boundaries, exact initial project creation tasks.
  - Suggested action: archive later
  - Safe to delete now: No

- Path: `docs/BACKEND_PHASE1_PROMPT (copy).md`
  - Reason: Previously recorded as an exact byte-identical duplicate of `docs/BACKEND_PHASE1_PROMPT.md`.
  - Round 11 inspection note: expected path was not found under `docs/*.md`; duplicate status must be re-verified before any deletion task.
  - Important facts not yet migrated: None inspectable in Round 11 because the expected path was missing.
  - Suggested action: re-verify path/hash in a later explicit cleanup task
  - Safe to delete now: No

- Path: `docs/BACKEND_PHASE2_PROMPT.md`
  - Reason: Older phase prompt for first core knowledge APIs.
  - Important facts not yet migrated: Phase 1 validation result, Phase 2 API scope, seed data details, revision conflict rules.
  - Suggested action: merge then archive
  - Safe to delete now: No

- Path: `docs/BACKEND_PHASE3_PROMPT.md`
  - Reason: Older phase prompt for context/activity/search.
  - Important facts not yet migrated: Phase 2 validation result, stub removal, link extraction rules, search implementation ambiguity.
  - Suggested action: merge then archive
  - Safe to delete now: No

- Path: `docs/BACKEND_PHASE4_PROMPT.md`
  - Reason: Older phase prompt for auth/workspace member permissions and QA cleanup.
  - Important facts not yet migrated: Phase 3 validation result, no-op PATCH behavior, auth choices, PostgreSQL smoke strategy.
  - Suggested action: merge then archive
  - Safe to delete now: No

- Path: `docs/BACKEND_PHASE5_PROMPT.md`
  - Reason: Current-state historical prompt now superseded by Phase 6 as latest backend target, but still source for Phase 5 completion.
  - Important facts not yet migrated: logout cleanup, lifecycle/import/export details, Phase 4 validation details.
  - Suggested action: keep for now
  - Safe to delete now: No

- Path: `docs/BACKEND_REFACTOR_RULES.md`
  - Reason: Older phase map conflicts with later backend phase prompts.
  - Important facts not yet migrated: refactor rules and old-project migration boundaries.
  - Suggested action: merge rules then archive later
  - Safe to delete now: No

- Path: `docs/FRONTEND_COMMENT_UX_SPIKE.md`
  - Reason: Initial comment UX spike superseded by comment v1 beta-complete, block identity, relocation, and persistence docs.
  - Important facts not yet migrated: original anchor contract rationale, "no comment mark" rationale, spike exclusions.
  - Suggested action: merge facts then archive
  - Safe to delete now: No

- Path: `apps/web/AI_EDITOR_GUARDRAILS.md`
  - Reason: Older frontend/editor guardrail and phase plan.
  - Important facts not yet migrated: frontend editor phase rules, high-risk feature approval rules, final experience goals.
  - Suggested action: needs review
  - Safe to delete now: No

- Path: `apps/web/EDITOR_QA_CHECKLIST.md`
  - Reason: Manual editor QA checklist with many frontend/editor behaviors.
  - Important facts not yet migrated: editor QA coverage outside comment-specific QA.
  - Suggested action: needs review
  - Safe to delete now: No

## Conflict-Prone Docs

- Path: `docs/PERMISSION_SYSTEM_CONTRACT.md`
  - Conflict area: public collection links, public `linkMode`, backend phase vs permission phase mismatch
  - Conflict summary: States permissions through Phase 11 and public collection support while also containing statements that public collection links or public link mode are rejected/deferred.
  - Already captured in `02-conflict-register.md`: Yes
  - Safe to delete now: No

- Path: `apps/web/FRONTEND_API_CONTRACT.md`
  - Conflict area: public links and phase-specific contract updates
  - Conflict summary: Contains public anonymous document and collection link contract updates that must be reconciled with permission contract conflict notes.
  - Already captured in `02-conflict-register.md`: Yes
  - Safe to delete now: No

- Path: `docs/BACKEND_REFACTOR_RULES.md`
  - Conflict area: Phase 5 scope mismatch
  - Conflict summary: Phase 5 says files/comments/collaboration, while later prompts define Phase 5 as lifecycle/import/export and Phase 6 as files.
  - Already captured in `02-conflict-register.md`: Yes
  - Safe to delete now: No

- Path: `docs/BACKEND_DATA_MODEL_V1.md`
  - Conflict area: V1/V2 data model timing mismatch, search implementation ambiguity
  - Conflict summary: Labels files/comments/ACL/Yjs as V2 extension tables while later docs treat comments/permissions as implemented and files as Phase 6 target; prepares search vector while later phases allow lightweight contains.
  - Already captured in `02-conflict-register.md`: Yes
  - Safe to delete now: No

- Path: `docs/BACKEND_PHASE3_PROMPT.md`
  - Conflict area: search implementation ambiguity
  - Conflict summary: Allows lightweight title/text contains search while preserving future `tsvector`.
  - Already captured in `02-conflict-register.md`: Yes
  - Safe to delete now: No

- Path: `docs/BACKEND_PHASE4_PROMPT.md`
  - Conflict area: auth implementation choice, PostgreSQL smoke
  - Conflict summary: Allows register or seed-only login and introduces smoke testing choices.
  - Already captured in `02-conflict-register.md`: Yes
  - Safe to delete now: No

- Path: `docs/BACKEND_PHASE5_PROMPT.md`
  - Conflict area: auth implementation choice, PostgreSQL smoke gap
  - Conflict summary: Baseline lists register endpoint and PostgreSQL smoke profile default behavior.
  - Already captured in `02-conflict-register.md`: Yes
  - Safe to delete now: No

- Path: `docs/BACKEND_PHASE6_PROMPT.md`
  - Conflict area: backend phase vs permission phase mismatch, PostgreSQL smoke gap
  - Conflict summary: Latest backend mainline says Phase 6 target and real PostgreSQL smoke not run when env var absent.
  - Already captured in `02-conflict-register.md`: Yes
  - Safe to delete now: No

- Path: `docs/FRONTEND_COMMENT_UX_SPIKE.md`
  - Conflict area: stable `blockId` sequencing
  - Conflict summary: Initial spike says stable `blockId` is a separate spike and not in first comment UX spike.
  - Already captured in `02-conflict-register.md`: Yes
  - Safe to delete now: No

- Path: `docs/BLOCK_IDENTITY_SCHEMA.md`
  - Conflict area: stable `blockId` sequencing
  - Conflict summary: Later doc says stable `blockId` is implemented for `comment v1 / beta-complete`.
  - Already captured in `02-conflict-register.md`: Yes
  - Safe to delete now: No

- Path: `services/api/README.md`
  - Conflict area: PostgreSQL smoke and operational state drift
  - Conflict summary: README may describe current commands/smoke behavior, but code verification is required before treating it as current implementation proof.
  - Already captured in `02-conflict-register.md`: Yes
  - Safe to delete now: No

## Duplicate / Near-Duplicate Docs

- Group:
  - Path: `docs/BACKEND_PHASE1_PROMPT.md`
  - Path: `docs/BACKEND_PHASE1_PROMPT (copy).md`
- Similarity:
  - previously recorded as exact duplicate; Round 11 inspection found only `docs/BACKEND_PHASE1_PROMPT.md`
- Preferred source: `docs/BACKEND_PHASE1_PROMPT.md`
- Reason: Previous source map recorded byte-identical SHA256 hash `578D68884B9D4D8C47D9D7226BB0B4CFCB508F2E780EE985D35FAC2FBD162E21`; current inspection did not find the copy path, so no current hash comparison was possible.
- Suggested action:
  - keep preferred source
  - if the copy path reappears, delete duplicate only after immediate hash re-verification and explicit approval
- Safe to delete now:
  - No, unless exact byte-identical duplicate and user explicitly approves deletion

## Delete Candidates

- Path: `docs/BACKEND_PHASE1_PROMPT (copy).md`
  - Reason candidate for deletion: Previously recorded as a byte-identical duplicate of `docs/BACKEND_PHASE1_PROMPT.md`.
  - Round 11 inspection note: expected copy path was not found under `docs/*.md`.
  - Preconditions before deletion: User explicitly approves deletion in a separate cleanup task; verify the file exists and hash again immediately before deletion.
  - Replacement location: `docs/BACKEND_PHASE1_PROMPT.md`
  - Safe to delete now: No

## Do Not Delete Yet

- Path or group: canonical sources
  - Reason: Authoritative domain facts still depend on source docs.

- Path or group: current phase sources
  - Reason: Backend Phase 5 completion, Phase 6 target, comment v1, permission Phase 11, and PostgreSQL smoke gap must remain traceable.

- Path or group: domain contracts
  - Reason: Contracts define strict behavior and persistence/API boundaries.

- Path or group: conflict-prone docs
  - Reason: Conflict history must remain available until conflicts are fully captured and reviewed.

- Path or group: agent-control docs
  - Reason: Active control layer for future agents.

- Path or group: historical backend phase prompts
  - Reason: Older prompts contain validation snapshots, exact phase boundaries, and implementation details not fully migrated.

- Path or group: frontend/editor docs under `apps/web/`
  - Reason: They may contain unique editor behavior and QA facts not yet migrated into agent skills.

- Path or group: `services/api/README.md`
  - Reason: May contain operational commands and current API/file/smoke behavior; code verification needed before replacement.

## Needs Review

- Path: `apps/web/AI_EDITOR_GUARDRAILS.md`
  - Why needs review: Large frontend/editor phase guardrail doc; relationship to current frontend state and comment v1 docs is not fully classified.
  - Suggested reviewer action: Extract durable frontend/editor rules into a future frontend/editor skill, then decide archive status.

- Path: `apps/web/EDITOR_QA_CHECKLIST.md`
  - Why needs review: Large QA checklist for editor behavior outside the current comment-specific agent skills.
  - Suggested reviewer action: Extract stable validation requirements into validation docs or a frontend editor skill.

- Path: `services/api/README.md`
  - Why needs review: Operational README may be current or partially code-derived; docs are not proof current code matches.
  - Suggested reviewer action: Compare with `services/api` code and test scripts before treating as canonical replacement material.

## Suggested Consolidation Targets

| Source doc | Suggested destination | Migration type | Notes |
|---|---|---|---|
| `docs/BACKEND_ARCHITECTURE_V1.md` | `docs/agent/skills/backend-clean-architecture.md` | keep canonical | Already partially migrated; keep source canonical until reviewed replacement exists. |
| `docs/BACKEND_REFACTOR_RULES.md` | `docs/agent/01-control-rules.md` | merge rules | Preserve old-project boundaries and phase conflict. |
| `docs/BACKEND_DATA_MODEL_V1.md` | `docs/agent/skills/data-model-migrations.md` | keep canonical | Already partially migrated; keep for schema details. |
| `apps/web/FRONTEND_API_CONTRACT.md` | `docs/agent/skills/api-contracts.md` | keep canonical | Keep as frontend-facing API contract. |
| `docs/BACKEND_PHASE1_PROMPT.md` | `docs/archive/` | archive after verification | Historical phase prompt. |
| `docs/BACKEND_PHASE1_PROMPT (copy).md` | `docs/BACKEND_PHASE1_PROMPT.md` | delete after verification | Exact byte-identical duplicate; deletion requires approval. |
| `docs/BACKEND_PHASE2_PROMPT.md` | `docs/agent/skills/backend-phase-execution.md` | merge facts | Preserve seed, bootstrap/map/document, revision conflict details. |
| `docs/BACKEND_PHASE3_PROMPT.md` | `docs/agent/skills/backend-phase-execution.md` | merge conflict | Preserve search ambiguity and context/activity/search details. |
| `docs/BACKEND_PHASE4_PROMPT.md` | `docs/agent/skills/permissions.md` | merge facts | Preserve auth/current-user/workspace member rules and no-op PATCH QA. |
| `docs/BACKEND_PHASE5_PROMPT.md` | `docs/agent/00-project-state.md` | merge facts | Preserve Phase 5 completion and lifecycle/import/export facts. |
| `docs/BACKEND_PHASE6_PROMPT.md` | `docs/agent/skills/files-upload.md` | keep canonical | Current backend target source for files. |
| `docs/PERMISSION_SYSTEM_CONTRACT.md` | `docs/agent/skills/permissions.md` | keep canonical | Canonical permission source; conflict-prone. |
| `docs/COMMENT_V1_BETA_COMPLETE.md` | `docs/agent/00-project-state.md` | keep canonical | Current comment version marker. |
| `docs/COMMENT_PERSISTENCE_CONTRACT.md` | `docs/agent/skills/comments.md` | keep canonical | Comment persistence boundary. |
| `docs/BLOCK_IDENTITY_SCHEMA.md` | `docs/agent/skills/frontend-tiptap-comments.md` | keep canonical | Block identity rules. |
| `docs/COMMENT_ANCHOR_RELOCATION_POLICY.md` | `docs/agent/skills/frontend-tiptap-comments.md` | keep canonical | Runtime relocation rules. |
| `docs/COMMENT_PRODUCTION_HARDENING_RULES.md` | `docs/agent/skills/comments.md` | keep canonical | Comment hardening rules. |
| `docs/COMMENT_UX_SPIKE_QA.md` | `docs/agent/05-validation-protocol.md` | merge facts | Comment validation and manual QA. |
| `docs/FRONTEND_COMMENT_UX_SPIKE.md` | `docs/archive/` | merge conflict | Older spike; preserve anchor rationale and blockId sequencing conflict first. |
| `apps/web/AI_EDITOR_GUARDRAILS.md` | `docs/agent/skills/frontend-editor.md` | needs review | General frontend/editor guardrails now have a consolidation target; source still needs review before archive/delete. |
| `apps/web/EDITOR_QA_CHECKLIST.md` | `docs/agent/05-validation-protocol.md` | needs review | QA checklist may need extraction into validation or frontend skill. |
| `services/api/README.md` | `docs/contracts/backend-operational-validation.md` | needs review | Operational validation notes created; code/tooling verification still required before replacing README. |
| `AGENTS.md` and `docs/agent/*` | Keep in place | already migrated | Active agent-control layer. |

## Cleanup Rules

- Do not delete source docs until facts are migrated or explicitly declared obsolete.
- Do not delete conflict-prone docs until conflicts are captured in `02-conflict-register.md`.
- Do not delete canonical docs until replacement canonical docs exist and are reviewed.
- Do not delete current phase docs until phase state is captured and verified.
- Prefer archive before delete.
- Exact duplicates may be delete candidates only after byte-level verification and explicit user approval.
- Historical prompts should not be loaded by default after source map exists.
- Old docs should be removed from default agent context before deletion.
- Deletion requires a separate cleanup task, not this inventory task.

## Inventory Gaps

- Standalone files/upload canonical contract created at `docs/contracts/files-upload-contract.md`; not fully user-reviewed. Source docs remain retained.
- Backend operational validation reference created at `docs/contracts/backend-operational-validation.md`; not code-verified or fully user-reviewed. `services/api/README.md` remains retained for review.
- Frontend general editor skill created at `docs/agent/skills/frontend-editor.md`; not fully user-reviewed. `apps/web/AI_EDITOR_GUARDRAILS.md` and `apps/web/EDITOR_QA_CHECKLIST.md` still need review.
- Public-link relationship is captured in `docs/agent/02-conflict-register.md` but not resolved.
- `services/api/README.md` may reflect code state, but no code verification was performed in this inventory.
- Round 11 old-root-doc inspection did not find `docs/BACKEND_PHASE1_PROMPT (copy).md`; any duplicate/delete decision must re-check the actual file path and hash in a separate cleanup task.
