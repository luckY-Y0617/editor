# Agent Docs System Readiness Review

This file audits the current agent-control documentation system.

It is a review report, not an implementation task.
Do not treat this file as permission to edit code, delete docs, or resolve conflicts.

## Overall Readiness

Status: `ready with minor documentation fixes`

- Core control docs, skills, and contracts are present.
- Required skill references in `AGENTS.md` and `03-required-reading-order.md` match existing skill files.
- Files/upload and backend operational validation contracts exist and are linked from the expected control docs.
- Public-link, phase, search, auth, README drift, and PostgreSQL smoke conflicts remain preserved.
- Validation docs still prevent claiming command success without actual execution.
- Minor stale wording remains in cleanup/source-map planning text, but it does not mark old docs safe to delete or resolve conflicts.
- New consolidation docs are not fully user-reviewed or code-verified, so real code work must still inspect current code.

## Files Reviewed

Entry/control docs:

- `AGENTS.md`
- `docs/agent/00-project-state.md`
- `docs/agent/01-control-rules.md`
- `docs/agent/02-conflict-register.md`
- `docs/agent/03-required-reading-order.md`
- `docs/agent/04-implementation-protocol.md`
- `docs/agent/05-validation-protocol.md`
- `docs/agent/06-final-report-format.md`

Skill docs:

- `docs/agent/skills/backend-clean-architecture.md`
- `docs/agent/skills/backend-phase-execution.md`
- `docs/agent/skills/data-model-migrations.md`
- `docs/agent/skills/files-upload.md`
- `docs/agent/skills/api-contracts.md`
- `docs/agent/skills/permissions.md`
- `docs/agent/skills/comments.md`
- `docs/agent/skills/frontend-editor.md`
- `docs/agent/skills/frontend-tiptap-comments.md`

Contract docs:

- `docs/contracts/files-upload-contract.md`
- `docs/contracts/backend-operational-validation.md`

Inventory/cleanup docs:

- `docs/agent/00-source-map.md`
- `docs/agent/doc-cleanup-plan.md`

Missing:

- None.

## Reference Integrity

No reference integrity issues found.

- `AGENTS.md` skill list matches actual skill files under `docs/agent/skills`.
- `docs/agent/03-required-reading-order.md` skill selection entries match actual skill files.
- `docs/agent/skills/files-upload.md` references `docs/contracts/files-upload-contract.md`.
- `docs/agent/05-validation-protocol.md` references `docs/contracts/backend-operational-validation.md`.
- `docs/agent/00-source-map.md` includes `docs/contracts/files-upload-contract.md`, `docs/contracts/backend-operational-validation.md`, and `docs/agent/skills/frontend-editor.md`.
- `docs/agent/doc-cleanup-plan.md` marks files/upload, frontend editor, and backend operational validation targets as created but not fully reviewed/code-verified where appropriate.
- No required review references point to non-existent files.
- No corrective edits were made in this review.

## Skill Coverage

- Domain: backend architecture/layering
  - Skill: `docs/agent/skills/backend-clean-architecture.md`
  - Coverage: sufficient
  - Notes: Covers `services/api`, Clean Architecture layers, dependency direction, old-project boundaries, and validation reporting.
- Domain: backend phase/status
  - Skill: `docs/agent/skills/backend-phase-execution.md`
  - Coverage: sufficient
  - Notes: Covers Phase 5/Phase 6 boundaries, status classification, conflict handling, and documented baseline vs code verification.
- Domain: data model/migrations
  - Skill: `docs/agent/skills/data-model-migrations.md`
  - Coverage: sufficient
  - Notes: Covers EF entities, migrations, schema constraints, PostgreSQL, workspace scoping, and InMemory smoke limitations.
- Domain: files/upload
  - Skill: `docs/agent/skills/files-upload.md`
  - Coverage: sufficient
  - Notes: Backed by `docs/contracts/files-upload-contract.md`; code still must be inspected before implementation.
- Domain: API contracts
  - Skill: `docs/agent/skills/api-contracts.md`
  - Coverage: sufficient
  - Notes: Covers `/api/v1`, DTO ownership, error shape, auth boundaries, and public/private endpoint safety.
- Domain: permissions
  - Skill: `docs/agent/skills/permissions.md`
  - Coverage: sufficient
  - Notes: Covers RBAC, effective permission, share links, invites, tokens, audit, and public-link conflict handling.
- Domain: comments backend/API/persistence
  - Skill: `docs/agent/skills/comments.md`
  - Coverage: sufficient
  - Notes: Covers comment APIs, persistence boundary, comment-free document JSON, authorization, and deferred comment features.
- Domain: frontend editor general behavior
  - Skill: `docs/agent/skills/frontend-editor.md`
  - Coverage: sufficient
  - Notes: Covers Tiptap/editor schema, serialization, runtime state, API client, editor QA, and manual QA reporting.
- Domain: frontend Tiptap comments
  - Skill: `docs/agent/skills/frontend-tiptap-comments.md`
  - Coverage: sufficient
  - Notes: Covers decorations, runtime ranges, blockId, panel navigation, and comment serialization boundaries.

## Contract Coverage

- Contract area: files/upload
  - Status: exists
  - Risk: low
  - Suggested next step: Use `docs/contracts/files-upload-contract.md` for Phase 6 file work, then inspect code before implementation.
- Contract area: backend operational validation
  - Status: exists
  - Risk: low
  - Suggested next step: Use `docs/contracts/backend-operational-validation.md` before backend validation planning.
- Contract area: general API contract canonical doc
  - Status: source canonical retained
  - Risk: medium
  - Suggested next step: Keep using `apps/web/FRONTEND_API_CONTRACT.md` plus `docs/agent/skills/api-contracts.md`; standalone API contract can be deferred.
- Contract area: permissions canonical replacement contract
  - Status: source canonical retained
  - Risk: medium
  - Suggested next step: Keep `docs/PERMISSION_SYSTEM_CONTRACT.md`; do not replace until reviewed and conflicts are preserved.
- Contract area: frontend editor QA consolidation
  - Status: deferred
  - Risk: low
  - Suggested next step: Keep `apps/web/EDITOR_QA_CHECKLIST.md`; consolidate later if frontend work becomes frequent.
- Contract area: comment QA consolidation
  - Status: deferred
  - Risk: low
  - Suggested next step: Keep `docs/COMMENT_UX_SPIKE_QA.md`; create a focused QA doc later if comment work resumes.

## Conflict Safety

Conflict safety checks passed. Conflicts are preserved, not resolved.

- Public collection links conflict remains conflict-marked.
- Public `linkMode` remains conflict-marked.
- Dedicated public/share-link API is distinguished from policy `linkMode`.
- Public anonymous access boundary is present.
- Backend phase vs permission phase mismatch remains preserved.
- Phase 5 scope mismatch remains preserved.
- V1/V2 timing mismatch remains preserved.
- Stable `blockId` sequencing conflict remains preserved.
- Search implementation ambiguity remains preserved.
- Auth implementation choice ambiguity remains preserved.
- README operational state drift gap is present.
- PostgreSQL smoke gap is present.
- No conflict is marked resolved.

## Validation Safety

Validation safety checks passed.

- `docs/agent/05-validation-protocol.md` requires commands to actually run before claiming pass.
- `docs/contracts/backend-operational-validation.md` says it is not code-verified.
- README is not treated as code verification.
- PostgreSQL smoke requires env var, actual command execution, and success.
- InMemory tests do not replace PostgreSQL smoke.
- Round 9 backend operational validation doc says that round did not run commands.
- Documentation-only tasks are not presented as application validation.

## Cleanup Safety

Cleanup safety checks passed.

- `docs/agent/00-source-map.md` remains an inventory, not a cleanup action.
- `docs/agent/doc-cleanup-plan.md` remains a plan, not a cleanup action.
- Canonical, current-phase, domain-contract, and conflict-prone docs are not marked safe to delete.
- Old docs remain retained until facts/conflicts are migrated and reviewed.
- `services/api/README.md` is not safe to delete.
- `apps/web/AI_EDITOR_GUARDRAILS.md` and `apps/web/EDITOR_QA_CHECKLIST.md` are not safe to delete.
- `docs/BACKEND_PHASE1_PROMPT (copy).md` is delete-after-verification only with hash recheck and explicit approval.
- Newly created consolidation targets are marked not fully reviewed/code-verified where appropriate.

## Stale or Inconsistent Wording

- Location: `docs/agent/doc-cleanup-plan.md`, Phase B
  - Current wording: `files/upload has no standalone canonical file contract`
  - Problem: `docs/contracts/files-upload-contract.md` now exists.
  - Suggested wording: `files/upload contract exists at docs/contracts/files-upload-contract.md; not fully user-reviewed`
  - Severity: low
- Location: `docs/agent/doc-cleanup-plan.md`, Phase B
  - Current wording: `frontend general editor has no agent-control skill`
  - Problem: `docs/agent/skills/frontend-editor.md` now exists.
  - Suggested wording: `frontend general editor skill exists at docs/agent/skills/frontend-editor.md; not fully user-reviewed`
  - Severity: low
- Location: `docs/agent/doc-cleanup-plan.md`, Source Doc Action Matrix
  - Current wording: `docs/agent/05-validation-protocol.md and future operational notes`
  - Problem: operational notes now exist at `docs/contracts/backend-operational-validation.md`.
  - Suggested wording: `docs/contracts/backend-operational-validation.md plus code/tooling verification before replacement`
  - Severity: low
- Location: `docs/agent/doc-cleanup-plan.md`, Keep Canonical
  - Current wording: `Replacement needed before removal: code-verified operational validation doc`
  - Problem: an operational validation doc exists but is not code-verified.
  - Suggested wording: `Replacement needed before removal: code-verified review of docs/contracts/backend-operational-validation.md and README/tooling drift`
  - Severity: low
- Location: `docs/agent/00-source-map.md`, Suggested Consolidation Targets
  - Current wording: `apps/web/AI_EDITOR_GUARDRAILS.md` -> `docs/agent/skills/frontend-tiptap-comments.md`; `Broader frontend/editor guardrails may need a future skill.`
  - Problem: general frontend editor skill now exists at `docs/agent/skills/frontend-editor.md`.
  - Suggested wording: point general editor guardrails to `docs/agent/skills/frontend-editor.md`; keep comment-specific material tied to `frontend-tiptap-comments.md`.
  - Severity: low
- Location: `docs/agent/doc-cleanup-plan.md`, Execution Order
  - Current wording: `Update conflict register for partially captured conflicts in a separate task.`
  - Problem: public-link conflicts are now captured but unresolved; wording may imply incomplete capture.
  - Suggested wording: `Update conflict register in a separate task only if a later review finds missing conflict entries.`
  - Severity: low

## Readiness Risks

- New docs are not fully user-reviewed: medium.
- Docs are not proof current code matches: high.
- Old canonical docs are still retained and must remain traceable: medium.
- Real Codex task should be small and low-risk first: medium.
- PostgreSQL/object storage validation may need environment configuration: medium.
- Cleanup/source-map wording has minor stale planning text: low.
- Public-link behavior remains intentionally unresolved: high if a task touches public links; low for unrelated tasks.

## Required Fixes Before Real Code Work

None.

## Deferred Improvements

- Patch stale cleanup/source-map wording listed above.
- Create standalone general API canonical contract.
- Create reviewed permissions replacement contract only if public-link conflicts remain preserved.
- Create comment QA consolidation doc.
- Create frontend editor QA consolidation doc.
- Create `.codex/` prompts later if explicitly requested.
- Add scripts or agent checks later if explicitly requested.
- Archive historical docs later after facts are migrated and explicit approval is given.
- Delete exact duplicate `docs/BACKEND_PHASE1_PROMPT (copy).md` later only after hash recheck and explicit approval.

## Suggested Dry Run

Recommended dry run:
Ask Codex to perform a Phase 6 files/upload implementation readiness investigation without changing code.

Expected behavior:
- read AGENTS.md
- read project-state, conflict-register, implementation protocol, validation protocol
- read files-upload skill
- read files-upload contract
- read backend operational validation
- inspect code paths only if explicitly allowed
- do not modify code
- do not resolve public-link conflicts
- report implementation status as verified / not verified per inspected code

Alternative dry run:
Ask Codex to perform a frontend editor serialization risk review without changing code.

## Final Verdict

Verdict: ready with minor documentation fixes

The system is ready for guarded Codex implementation planning, with minor stale cleanup wording still worth fixing.

Real implementation tasks can begin only after the required reading order and current code inspection are followed.

Recommended first real task type: Phase 6 files/upload codebase investigation and implementation plan, no code changes.
