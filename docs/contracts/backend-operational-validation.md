# Backend Operational Validation

This file is the operational validation reference for backend agent work.

It records documented validation commands and constraints.
It is not proof that current code or tooling matches.
Future agents must inspect current code and tooling before running or reporting validation.

## Status

- Current status: `documented operational reference / not code-verified`.
- This document consolidates validation guidance; it does not prove commands still work.
- Backend mainline: Phase 5 completed according to documented baseline.
- Current backend target: Phase 6 files/upload.
- Phase 5 documented validation:
  - restore passed
  - build passed
  - test passed
  - 37 tests passed
  - migration script generated
  - no pending model changes
- PostgreSQL smoke profile exists according to docs.
- Real PostgreSQL smoke is not considered passed unless `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is set and the smoke command actually runs.
- This round does not run commands.

## Source Inputs

- `services/api/README.md`
  - Role: backend local setup, commands, files notes, endpoints, search strategy, PostgreSQL smoke profile
  - Status: source read
- `docs/BACKEND_PHASE5_PROMPT.md`
  - Role: Phase 5 validation commands, lifecycle/import/export validation expectations, PostgreSQL smoke guidance
  - Status: source read
- `docs/BACKEND_PHASE6_PROMPT.md`
  - Role: Phase 6 files/upload target, validation commands, migration checks, PostgreSQL smoke guidance
  - Status: source read
- `docs/agent/05-validation-protocol.md`
  - Role: agent validation reporting, shell compatibility, backend/migration/PostgreSQL rules
  - Status: source read
- `docs/agent/02-conflict-register.md`
  - Role: README operational drift gap and PostgreSQL smoke documentation gap
  - Status: source read
- `docs/contracts/files-upload-contract.md`
  - Role: files/upload validation expectations and object storage boundary
  - Status: source read

## Scope

- Applies to backend work under `services/api`.
- Applies to validation planning and reporting for backend changes.
- Applies to EF migration validation.
- Applies to PostgreSQL smoke reporting.
- Applies to Phase 6 files/upload validation when backend/file work is touched.
- Does not run validation by itself.
- Does not override architecture, data model, API, permission, or files/upload contracts.
- Does not prove README matches current code.

## Command Discovery Rules

- Inspect current repo before running commands.
- Verify actual solution/project paths before using `dotnet restore`, `dotnet build`, or `dotnet test`.
- Prefer repo-documented commands when they match current files.
- If README commands and actual repo files differ, report drift.
- Do not invent project paths.
- Do not assume commands from old docs still work.
- Do not treat old phase validation snapshots as current validation.
- Use commands compatible with current shell/OS.
- On Windows/PowerShell, avoid Unix-only assumptions unless repo tooling provides them.

## Backend Validation Commands

- Expected command categories:
  - restore
  - build
  - test
- Documented generic commands:

  ```text
  dotnet restore
  dotnet build
  dotnet test
  ```

- Agents must inspect current solution/project paths before running.
- If README gives specific commands, use them only after path/tooling verification.
- If commands are not run, final report must list them under `Not Run / Reason`.
- Do not claim passed unless command actually ran and succeeded.
- For narrow changes, focused validation is allowed when justified and reported.
- For documentation-only changes, build/test are not required.

## EF / Migration Validation

- Required when schema/model changes occur.
- Schema changes require EF migration only when documented/required.
- Generate or inspect migration script where applicable.
- Confirm no unintended model changes where tooling supports it.
- Do not create empty/noise migrations.
- Do not alter applied migrations unless explicitly instructed and safe for repo state.
- Do not weaken model constraints to pass tests.
- InMemory tests do not replace migration or PostgreSQL validation.
- If migration commands are not run, report reason.

## PostgreSQL Smoke Validation

- PostgreSQL smoke profile exists according to docs.
- Existence of a smoke profile does not mean smoke passed.
- PostgreSQL smoke is passed only if:
  1. `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is set;
  2. the smoke command actually runs;
  3. the smoke command succeeds.
- Required report phrase when env var is absent:

  ```text
  PostgreSQL smoke not run: NORTHSTAR_POSTGRES_SMOKE_CONNECTION not set.
  ```

- README may document smoke commands, but execution must be verified.
- Do not update project-state or final report to say smoke passed based only on README.
- Do not treat InMemory tests as PostgreSQL smoke.
- Do not weaken production behavior for InMemory tests.

## Files / Object Storage Validation

- For Phase 6 files/upload work, also read `docs/contracts/files-upload-contract.md`.
- Object storage integration is passed only if configured and actually tested.
- Missing object storage env/config must be reported, not hidden.
- Do not claim object-storage integration passed based only on README.
- Focused file workflow tests should cover when applicable:
  - create upload session
  - complete/finalize
  - idempotent finalize
  - permission checks
  - attachment creation/removal
  - no permanent URL storage
  - outbox write
  - invalid Tiptap file references
- If object storage or file workflow tests are not run, report reason.

## README Drift Handling

- `services/api/README.md` may be closer to current code than older phase prompts, but it is still documentation.
- README is not code verification.
- If README and phase prompts differ, inspect current code/tooling before updating validation docs or project state.
- README operational details do not override architecture, data model, API, permission, files/upload, or security contracts.
- If README mentions endpoints or behavior not verified in code, report as documented, not verified.
- If README commands fail due to drift, report the drift instead of silently changing commands.
- Do not update implementation status based only on README.

## Shell and OS Compatibility

- Use commands compatible with current shell and OS.
- Repository may be used from Windows paths such as `D:\editor`.
- For Windows/PowerShell, avoid Unix-only shell assumptions unless repo tooling provides them.
- Do not use destructive commands such as `rm -rf` unless explicitly requested and safe.
- Prefer safe PowerShell equivalents or repo-provided scripts when available.
- If a command fails due to shell incompatibility, report it and use an equivalent safe command only in implementation/validation tasks, not in this documentation-only round.

## Reporting Rules

Final reports for backend validation must include:

- commands run
- commands passed
- commands failed
- commands not run with reason
- environment gaps
- migration status, if relevant
- PostgreSQL smoke status
- object storage integration status, if relevant
- whether README/code drift was observed
- whether validation was full or focused

For documentation-only tasks:

- report no build/test required
- report backend/frontend tests not run because no application code changed

## Forbidden Claims

Future agents must not claim:

- restore passed unless restore actually ran and succeeded
- build passed unless build actually ran and succeeded
- tests passed unless tests actually ran and succeeded
- migration is clean unless migration/model check actually ran or was inspected
- PostgreSQL smoke passed unless env var was set and smoke command ran successfully
- object storage integration passed unless configured and actually tested
- README proves current implementation
- old phase prompt validation proves current implementation
- InMemory tests replace PostgreSQL smoke
- documentation-only task validated application behavior

## Agent Rules

- Read this document for backend validation planning.
- Also read:
  - `docs/agent/05-validation-protocol.md`
  - `docs/agent/02-conflict-register.md`
  - `docs/contracts/files-upload-contract.md` when files/upload is touched
- Inspect current repo before running validation.
- Do not run commands during documentation-only tasks unless explicitly requested.
- Do not modify code to make validation easier.
- Do not weaken production behavior for tests.
- Report missing env/config explicitly.
- Use final report format from `docs/agent/06-final-report-format.md`.
- If validation cannot run, explain why.
- If README/tooling drift is found, report it as drift, not as resolved.
