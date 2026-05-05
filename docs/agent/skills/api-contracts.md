# Skill: API Contracts

## When To Use

- Use for API routes, endpoints, and controllers.
- Use for request/response DTOs.
- Use for `Northstar.Contracts`.
- Use for auth behavior and protected endpoints.
- Use for error handling.
- Use for pagination, enums, and status strings.
- Use for public/private endpoint boundaries.
- Use for API compatibility and frontend API client contract alignment.

## Read First

- `AGENTS.md`
- `docs/agent/00-project-state.md`
- `docs/agent/01-control-rules.md`
- `docs/agent/02-conflict-register.md`
- `docs/agent/04-implementation-protocol.md`
- `docs/agent/skills/backend-clean-architecture.md`
- Relevant API contract docs.
- Existing controllers/endpoints.
- Existing `Northstar.Contracts` DTOs.
- Frontend API client code if task affects frontend contract.
- Related tests.
- Do not guess paths.

If exact docs are unknown, search exact terms:

- `/api/v1`
- `Northstar.Contracts`
- `error response`
- `VALIDATION_ERROR`
- `CONFLICT`
- `FORBIDDEN`
- `bootstrap`
- `documents`
- `search`
- `export`
- `import`
- `auth`
- `workspace members`
- `attachments`
- `comments`
- `permissions`

## Current State Assumptions

- API base path is `/api/v1`.
- Existing documented backend baseline APIs include bootstrap, spaces map, documents CRUD/update/location/archive/restore/delete/context/activity, search, export/import, auth, and workspace members.
- Baseline API list is documented baseline, not blindly verified current code.
- DTOs and public contracts belong in `Northstar.Contracts`.
- Controllers/endpoints must remain thin.
- Code must be inspected before changing API behavior.

## Must Preserve

- `/api/v1` base path.
- No `/api/app`.
- No EF entities in API responses.
- Error response shape:
  ```json
  {
    "error": {
      "code": "CONFLICT",
      "message": "Document revision conflict.",
      "details": {}
    }
  }
  ```
- `/api/v1` keeps the Northstar error envelope above; do not convert it to RFC 9457 Problem Details without an explicit versioned contract decision.
- Automatic ASP.NET Core model validation and model binding failures must also return the Northstar envelope, not default `ProblemDetails` / `ValidationProblemDetails`.
- Recommended validation detail shape is stable field-level data under `error.details`, for example `{ "fields": { "title": ["Title is required."] } }`.
- Problem Details can be evaluated for a future `/api/v2`, public API, or media-type negotiation plan, but must not be mixed into the existing `/api/v1` contract.
- Standard error codes:
  - `VALIDATION_ERROR`
  - `NOT_FOUND`
  - `CONFLICT`
  - `UNAUTHORIZED`
  - `FORBIDDEN`
  - `INTERNAL_ERROR`
- DTO ownership in Contracts.
- Protected endpoints enforce server-side authorization.
- List/search/export/context/activity/comments/attachments/files/version endpoints enforce effective access when relevant.
- Public/private endpoint boundary.
- Raw tokens returned only once when applicable.
- No token hashes, secrets, passwords, or provider secrets in responses or audit metadata.

## Allowed Work

- Add/update DTOs in Contracts when required by documented API contract.
- Add/update thin controllers/endpoints for documented behavior.
- Add Application use-case calls behind API boundary.
- Add validation and error mapping that preserves error shape.
- Add tests for API behavior and authorization.
- Align frontend API client only when API contract explicitly changes.

## Forbidden Work

- Change API base path.
- Mix `/api/app` and `/api/v1`.
- Expose EF entities.
- Create temporary/uncontracted endpoints.
- Add uncontracted public API behavior.
- Change error response shape.
- Introduce Problem Details into `/api/v1` responses without an explicit versioned API contract change.
- Add controller-local business logic.
- Add controller-local authorization ranking logic.
- Return raw token/token hash/password/secret after allowed creation moment.
- Broaden public/share-token access into bootstrap/map/search/export/list.
- Silently change DTO fields used by frontend without contract/test updates.
- Use docs alone as proof endpoint exists.

## Implementation Rules

- Inspect existing controllers, Contracts DTOs, Application use cases, and tests before editing.
- Controllers call Application. Do not embed business flows.
- Map domain/application errors to documented error shape.
- Validate request fields at API/Application boundary according to existing pattern.
- Keep route naming consistent with documented contracts.
- When changing contract, update tests and affected client code.
- For conflict-marked public-link behavior, follow `docs/agent/02-conflict-register.md`.
- Do not create endpoints for deferred capabilities unless explicitly moved into scope.

## Validation

- Run backend validation where applicable:
  - `dotnet restore`
  - `dotnet build`
  - `dotnet test`
- Run focused API tests when available.
- Run frontend/client tests only if client contract is changed and scripts exist.
- Report commands not run with reason.
- Do not claim endpoint behavior verified unless tests or code inspection support it.

## Final Report Notes

- List APIs changed or `None`.
- List DTOs/contracts changed or `None`.
- State auth/authorization impact.
- State error shape impact.
- State frontend client impact or `None`.
- List tests run/not run.
- State conflict-marked public endpoint behavior touched or `None`.
