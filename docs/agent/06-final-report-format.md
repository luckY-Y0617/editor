# Final Report Format

Purpose: control final output verbosity.

## Final Report Format

Future agents must use this structure after code work:

```text
## Summary

- One to three bullets.
- State what changed.
- Do not restate the whole architecture.

## Files Changed

- `path/to/file`: what changed and why.

## APIs Changed

- List API changes.
- If none, write: None.

## Migrations Changed

- List migrations.
- If none, write: None.

## Validation

- Command: result.
- Command: result.

## Not Run / Reason

- Command or validation not run: reason.

## Unresolved Conflicts / Deferred Items

- List conflict-marked or deferred items touched by task.
- If none, write: None.

## Next Safe Step

- One short next step.
```

## Style Rules

- Be concise.
- No long essays.
- No speculative roadmap.
- No alternative architecture proposals.
- No "while I was here" commentary.
- No claims that tests passed unless actually run.
- No hiding skipped validation.
- No burying conflict-marked behavior.
- No recommendations unless explicitly asked.
- Do not include full file contents or large code snippets unless explicitly requested.
- Summarize changed code instead of pasting it.

## Documentation-Only Task Report

For documentation-only tasks, use:

```text
## Summary

- Created/updated agent-control documentation only.
- No application code changed.

## Files Changed

- `AGENTS.md`: ...
- `docs/agent/...`: ...

## Code Changes

None.

## Validation

- Documentation-only change; no build/test required.

## Not Run / Reason

- Backend/frontend tests not run because no application code changed.

## Unresolved Conflicts / Deferred Items

- Preserved documented conflicts in `docs/agent/02-conflict-register.md`.

## Next Safe Step

- Review generated docs before using them for implementation tasks.
```
