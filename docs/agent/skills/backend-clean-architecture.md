# Skill: Backend Clean Architecture

## When To Use

- Use for tasks touching `services/api`.
- Use for backend controllers/endpoints.
- Use for Application services and use cases.
- Use for Domain entities, value objects, and domain services.
- Use for Infrastructure EF, storage, repositories, and background workers.
- Use for Contracts DTOs.
- Use for dependency direction, backend refactors, and backend feature work.

## Read First

- `AGENTS.md`
- `docs/agent/00-project-state.md`
- `docs/agent/01-control-rules.md`
- `docs/agent/04-implementation-protocol.md`
- Relevant backend code under `services/api`.
- Relevant tests.
- Relevant domain or phase docs.
- Do not guess paths.

If exact docs are unknown, search exact terms:

- `Clean Architecture`
- `Northstar.Api`
- `Northstar.Application`
- `Northstar.Domain`
- `Northstar.Infrastructure`
- `Northstar.Contracts`
- `services/api`
- `api-old`
- `ABP`
- `SqlSugar`

## Current State Assumptions

- Backend is documented as ASP.NET Core Modular Monolith + Clean Architecture.
- Current backend root is `services/api`.
- New backend naming is `Northstar`.
- Old `services/api-old` is read-only reference only.
- Old Go file service at `E:\ClayMo\services\file-service` is read-only reference only.
- Old services are not runtime dependencies.
- Documentation is not proof of current code. Inspect code before editing.

## Must Preserve

- Project layout:
  - `Northstar.Api`
  - `Northstar.Application`
  - `Northstar.Domain`
  - `Northstar.Infrastructure`
  - `Northstar.Contracts`
- Dependency direction:
  - `Api -> Application / Contracts / Infrastructure`
  - `Application -> Domain / Contracts`
  - `Infrastructure -> Application / Domain / Contracts`
  - `Domain -> no project dependency`
- Controllers remain thin.
- Business orchestration stays in Application.
- Invariants and state transitions stay in Domain.
- EF, storage, background workers, and providers stay in Infrastructure.
- DTOs and API contracts stay in Contracts.

## Allowed Work

- Add code inside existing layers.
- Add Application services/interfaces for documented use cases.
- Add Infrastructure implementations behind interfaces.
- Add Contracts DTOs when an API contract requires them.
- Add focused tests.
- Refactor within a layer only when public behavior, schema, contracts, workflows, and dependencies stay unchanged.

## Forbidden Work

- Introduce ABP.
- Use ABP modules, `ApplicationService`, repositories, or UnitOfWork.
- Use old SqlSugar/ABP wrappers.
- Use old `ClayMo.*`, `NS.Abp`, or `Module.*`.
- Recreate old `framework/modules/src`.
- Split into microservices.
- Modify `services/api-old`.
- Modify `E:\ClayMo\services\file-service`.
- Make old file-service a runtime dependency.
- Copy old Go/go-zero structure.
- Move business logic into controllers.
- Move EF/database logic into Domain.
- Make Application depend on `HttpContext` or concrete SDKs.
- Return EF entities from APIs.
- Introduce generic architecture rewrites.
- Do drive-by refactors.

## Implementation Rules

- Inspect existing code before editing.
- Make the smallest safe diff.
- Preserve `/api/v1`.
- Preserve documented error response shape.
- Do not add uncontracted public API behavior.
- Keep layer responsibilities clear.
- Do not add abstractions unless required by existing architecture or explicit docs.

## Validation

- Run backend validation where applicable:
  - `dotnet restore`
  - `dotnet build`
  - `dotnet test`
- Inspect repo for correct solution/project paths.
- Report commands not run with reason.
- Do not claim validation passed unless actually run.

## Final Report Notes

- Mention backend layers touched.
- List files changed.
- List APIs changed or `None`.
- List migrations changed or `None`.
- List tests run.
- List tests not run with reason.
- Include any architecture risks or conflicts.
