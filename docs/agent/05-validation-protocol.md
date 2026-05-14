# Validation Protocol

Purpose: define validation expectations.

## General Rule

- Do not claim validation passed unless commands actually ran.
- If a command was not run, report it under `Not Run / Reason`.
- Do not weaken production behavior to satisfy tests.
- Do not replace integration behavior with fake behavior unless test scope explicitly requires it.

## Shell and OS Compatibility

- Use commands compatible with the current shell and operating system.
- This repository may be used from Windows paths such as `D:\editor`.
- For Windows/PowerShell, avoid Unix-only assumptions unless the repo tooling explicitly provides them.
- Do not use destructive shell commands such as `rm -rf` unless explicitly requested and safe.
- Prefer repo-provided scripts or documented commands when available.
- If a command fails because of shell incompatibility, report it and use an equivalent safe command.

## Backend Validation

For backend operational details, also read `docs/contracts/backend-operational-validation.md`.

Expected commands where applicable:

```text
dotnet restore
dotnet build
dotnet test
```

- If actual solution or project paths differ, inspect the repo and use correct paths.
- Use focused validation for narrow changes when full validation is not required or not available.
- Report every command that ran.

## EF / Migration Validation

For schema changes:

- Add EF migration only when schema changes are documented or required.
- Generate or inspect migration script where applicable.
- Confirm no unintended model changes.
- Do not create empty/noise migrations.
- Do not alter existing applied migrations unless explicitly instructed and safe for the repo state.

## PostgreSQL Smoke

- PostgreSQL smoke can only be claimed passed if `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` is set and the smoke profile actually ran.
- If env var is absent, report:
  - "PostgreSQL smoke not run: `NORTHSTAR_POSTGRES_SMOKE_CONNECTION` not set."
- InMemory tests do not replace PostgreSQL smoke.
- Do not weaken production code for InMemory tests.

## Frontend Validation

- Inspect package scripts before running frontend validation.
- Run focused frontend tests for frontend/comment/editor changes when available.
- Report Vite large-chunk warning as warning, not failure, if build otherwise passes and docs already note it.

## Browser QA Fallback

- Run browser QA only when the user requests or allows it.
- Prefer the configured in-app Browser workflow when it is available.
- If the in-app Browser connection times out or cannot initialize, use a local headless browser screenshot for localhost visual QA when a local browser executable is available.
- Report the fallback explicitly, including the screenshot path and why the in-app Browser was not used.
- Do not treat headless screenshot QA as a substitute for interactive workflows that require clicks, keyboard behavior, uploads, or permission prompts unless those interactions were actually performed.

## Required Final Validation Report

Final report must include:

- commands run
- commands passed
- commands failed
- commands not run with reason
- environment gaps
- migration status, if relevant
