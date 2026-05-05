# Skill: Data Model Migrations

## When To Use

- Use for tasks touching EF entities.
- Use for DbContext and EF configurations.
- Use for migrations.
- Use for repositories/query services.
- Use for PostgreSQL schema.
- Use for document content storage.
- Use for document revisions/versions.
- Use for files/comments/permissions tables.
- Use for search index schema.
- Use for workspace scoping.

## Read First

- `AGENTS.md`
- `docs/agent/00-project-state.md`
- `docs/agent/01-control-rules.md`
- `docs/agent/05-validation-protocol.md`
- Relevant data model docs.
- Existing EF entities/configurations.
- Existing migrations.
- Related tests.
- Do not guess paths.

If exact docs are unknown, search exact terms:

- `data model`
- `workspace_id`
- `documents`
- `document_drafts`
- `document_versions`
- `revision`
- `version_no`
- `JSONB`
- `upload_sessions`
- `files`
- `document_attachments`
- `comment_threads`
- `permissions`
- `share_links`
- `document_search_index`

## Current State Assumptions

- PostgreSQL is default database.
- EF Core migrations are required for schema changes.
- Core business tables carry `workspace_id`.
- Tiptap document body is stored as JSON/JSONB.
- Document metadata and body are separated:
  - `documents`
  - `document_drafts`
- `documents.revision` is autosave optimistic locking.
- `document_versions` are immutable snapshots.
- Files are Phase 6 target.
- Comments and permissions have documented states, but code must be verified.

## Must Preserve

- One PostgreSQL database with multi-workspace shared tables unless docs explicitly change it.
- No tenant-per-database, dynamic connection strings, or ABP Tenant as main path.
- No HTML as sole source of truth.
- No tags, links, activity, permissions, files, or comments embedded in document content JSON.
- Do not conflate `revision`, `version_no`, and user-facing version labels.
- Use `collections` in database even if frontend calls them folders.
- Do not build `document_blocks` in V1 unless docs explicitly change boundary.
- Cross-resource relationships must preserve workspace isolation; prefer workspace-scoped constraints/indexes or explicit validation so a document, file, attachment, permission, link, or activity row cannot silently point across workspaces.
- Search index implementation remains conflict-marked between prepared PostgreSQL `tsvector` and lightweight contains search; do not upgrade search strategy without an explicit task.

## Allowed Work

- Add EF entity/configuration/migration when documented data model requires it.
- Add indexes/constraints required by documented behavior.
- Add repository/query implementation in Infrastructure.
- Update Application interfaces when needed for documented use cases.
- Add tests for schema-related behavior.
- Inspect migration script for unintended model changes.

## Forbidden Work

- Add undocumented tables/fields without clarification.
- Create empty/noise migrations.
- Alter existing applied migrations unless explicitly instructed and safe.
- Store permanent file URLs.
- Create `files` before upload finalize.
- Store comments/files/permissions in Tiptap JSON.
- Merge documents/drafts/versions incorrectly.
- Weaken schema constraints to make tests pass.
- Copy old MySQL schema directly.
- Copy old knowledge-base schema directly.
- Introduce ABP tenancy.
- Treat InMemory tests as replacement for PostgreSQL smoke.

## Implementation Rules

- Schema changes require documented reason.
- Every schema change must be checked against current phase/domain docs.
- Migrations must be minimal and focused.
- Preserve workspace scoping.
- Preserve optimistic locking semantics.
- Preserve immutable version semantics.
- Do not make storage-provider details leak into Domain/Application schema assumptions.
- For files, follow:
  ```text
  upload_sessions -> files -> document_attachments
  ```

## Validation

- For backend/schema changes, run:
  - `dotnet restore`
  - `dotnet build`
  - `dotnet test`
- Inspect/generate migration script where applicable.
- Confirm no unintended model changes where tooling supports it.
- PostgreSQL smoke only if env var is set and command actually runs.
- Report not-run commands with reason.

## Final Report Notes

- List entities/configurations changed.
- List migrations added/changed or `None`.
- List tables/fields/indexes affected.
- State workspace scoping impact.
- State revision/version impact.
- List validation run/not run.
- State PostgreSQL smoke status.
