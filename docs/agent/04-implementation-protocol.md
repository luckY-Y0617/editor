# Implementation Protocol

Purpose: define how future agents must execute implementation tasks.

## Plan Before Patch

Before editing code, produce a short plan containing:

- task type
- touched domains
- files or areas likely to inspect
- conflict-marked areas to check
- product/UX placement check for user-facing changes
- expected validation

The plan must be short. Do not include speculative architecture. Do not include roadmap. Do not propose alternative designs unless the user explicitly asks. If the task is documentation-only, state that no code edits are planned.

## Implementation Protocol

1. Identify task type:
   - bugfix
   - feature
   - refactor
   - docs
   - test
   - migration
   - investigation
2. Identify touched domains:
   - backend
   - files
   - permissions
   - comments
   - frontend
   - API
   - data model
3. Read required docs using `03-required-reading-order.md`.
4. Produce a short plan before code inspection or editing.
5. Inspect existing code before editing.
6. Identify conflict-marked areas before editing.
7. Make the smallest safe diff.
8. Avoid unrelated cleanup.
9. Add or update focused tests when behavior changes.
10. Run validation according to `05-validation-protocol.md`.
11. Report using `06-final-report-format.md`.

## User-Facing Placement Gate

For any user-facing frontend, navigation, settings, editor, sharing, activity, notification, search, or content-management change, agents must check product placement before patching:

- Identify the user's task and the surface where a normal user would expect to perform it.
- Keep the action on its owning surface instead of exposing it in every adjacent surface.
- Do not place workspace administration inside document-local panels unless the entry is explicitly advanced and resource-scoped.
- Do not place daily document sharing inside Settings or Advanced permissions when the Share Drawer is the normal path.
- Do not place content organization actions inside Settings when Library / Folder / Document is the normal path.
- Do not place ordinary document activity inside Updates when Updates is scoped to access / sharing / permission notifications.
- Do not expose backend implementation concepts such as Space or Collection in normal user paths when the product terms are Library and Folder.
- Every visible action must have a clear outcome, route, feedback state, or disabled reason.
- User-facing copy and styling should support scanning and task completion; avoid dashboard/card sprawl when a table, list, form, or status section matches the task better.
- Treat this as part of each implementation slice. Do not defer obvious placement or interaction mismatches to a separate cleanup round unless fixing them would expand the slice beyond a smallest safe diff.

## Smallest Safe Diff

- Change only files required for the task.
- Do not reformat unrelated files.
- Do not rename unrelated types.
- Do not introduce new abstractions unless required by existing architecture or explicit docs.
- Do not collapse or reorder documented workflows.
- Do not opportunistically upgrade dependencies.
- Do not modernize old code during unrelated tasks.
- Do not fix unrelated bugs unless they block the task. If they block, report clearly.

## No Drive-By Refactors

Do not perform:

- controller base-class rewrites
- repository pattern rewrites
- generic abstraction rewrites
- permission model rewrites
- API response normalization changes
- large folder moves
- dependency upgrades
- formatting-only churn
- old-project cleanup

## Code Placement Rules

- API boundary and controllers stay thin.
- Application owns orchestration and use cases.
- Domain owns invariants and state transitions.
- Infrastructure owns EF, storage, background workers, and providers.
- Contracts owns DTOs and API contracts.
- Tests must be focused and aligned with the changed behavior.

## Conflict-Marked Behavior

- Check conflict register before changing permissions, public links, search, auth, comments, or files.
- If task touches conflict-marked behavior, do not choose an interpretation unless the user has explicitly selected one.
- Preserve current behavior where safe.
- Report unresolved conflicts.

## Documentation Updates

- Update docs only when the task explicitly requires documentation updates or behavior/contracts changed.
- Do not rewrite project history.
- Do not mark planned or deferred features as implemented.
- Do not delete conflict notes after unrelated work.
