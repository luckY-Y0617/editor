# Organization Settings Read-only Contract Design v1

## Scope

This is a contract design note for the first safe Organization Settings slice. It does not implement Organization Settings, add API routes, add migrations, or change permissions.

## Current Code Verification

- `Northstar.Domain.Workspaces.Workspace`, `WorkspaceMember`, and `Northstar.Domain.Users.User` exist.
- `Northstar.Infrastructure.Persistence.NorthstarDbContext` has `Workspaces`, `WorkspaceMembers`, and `Users`.
- No `Organization` or `Tenant` domain entity, EF table, DTO, controller, or application service was found.
- `WorkspacesController` exposes workspace-scoped members/groups/IAM/SCIM-adjacent management under `/api/v1/workspaces/...`.
- `WorkspaceMembersService.GetMembersAsync` is workspace-scoped and currently requires `EnsureCanManageWorkspaceAsync`.
- Bootstrap returns a single accessible workspace shape through `WorkspaceDto`; it is not an organization profile contract.

## Backend Implementation Gate

Result: **deferred**.

Reason:

- Organization profile needs a durable organization read model before a live endpoint can be honest.
- Global members read-only needs a cross-workspace membership projection and organization-level read authorization.
- Reusing workspace members as global members would blur workspace and organization boundaries.
- Adding either live endpoint now would require at least DTO/service/controller design and likely a new domain/table/migration plus permission catalog decision.

## Organization Profile Read-only DTO Draft

```csharp
public sealed record OrganizationProfileResponse(
    OrganizationProfileDto Organization);

public sealed record OrganizationProfileDto(
    string Id,
    string Name,
    string Status,
    IReadOnlyList<OrganizationWorkspaceDto> Workspaces,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record OrganizationWorkspaceDto(
    string Id,
    string Name,
    string Slug,
    string CurrentSpaceId,
    string CurrentUserRole,
    DateTimeOffset CreatedAt);
```

Recommended route draft:

```text
GET /api/v1/organizations/{organizationId}/profile
```

Application service ownership:

```text
Northstar.Application.Organizations.IOrganizationSettingsQueryService
Northstar.Application.Organizations.OrganizationSettingsQueryService
```

## Global Members Read-only DTO Draft

```csharp
public sealed record OrganizationMembersResponse(
    IReadOnlyList<OrganizationMemberDto> Members);

public sealed record OrganizationMemberDto(
    string UserId,
    string? Email,
    string DisplayName,
    string Status,
    IReadOnlyList<OrganizationMemberWorkspaceDto> Workspaces);

public sealed record OrganizationMemberWorkspaceDto(
    string WorkspaceId,
    string WorkspaceName,
    string Role,
    string Status,
    DateTimeOffset? JoinedAt);
```

Recommended route draft:

```text
GET /api/v1/organizations/{organizationId}/members
```

Application service ownership:

```text
Northstar.Application.Organizations.IOrganizationMembersQueryService
Northstar.Application.Organizations.OrganizationMembersQueryService
```

## Domain / Data Model Decision

Recommended minimum model before implementation:

- Add `Organization` entity/table with `id`, `name`, `slug`, `status`, `created_at`, `updated_at`, and optional `deleted_at`.
- Add `organization_id` to `workspaces`, or introduce a workspace-to-organization ownership table if multiple ownership semantics are required later.
- Keep `workspace_members` workspace-scoped. Do not convert it into organization membership.
- Use a query projection for global members by joining organization-owned workspaces to workspace members and users.

Migration: required for a real implementation unless the product explicitly chooses the temporary single-workspace-as-organization compatibility model. That compatibility model is not recommended for live API because it would encode a placeholder as an organization contract.

## Permission / Authorization Decision

Recommended first read authorization:

- New catalog action: `organization.view`.
- Authorize if the current user has active membership in at least one workspace owned by the organization.
- Later organization admin actions should use separate actions, for example `organization.manage_members` and `organization.manage_settings`.
- Do not reuse `workspace.manage_members` for global members read-only; it is too strong and workspace-scoped.

## Why Read-only

- Profile and member discovery define the organization boundary without changing membership, roles, workspace provisioning, domains, SSO, billing, or audit.
- Read-only avoids last-owner, invite, role downgrade, billing ownership, and IdP ownership side effects.
- It gives the frontend a stable live-backed surface before mutations are designed.

## Explicitly Deferred Mutations

- Rename organization.
- Invite/add/remove global member.
- Change organization role.
- Create/archive/delete workspace.
- Domain verification.
- SSO/OIDC/SAML setup.
- Organization-scoped SCIM token issuance.
- Billing/plan changes.
- Audit retention policy changes.
- Data retention delete/export/legal-hold controls.

## Next Minimal Slice

1. Add Organization read model + migration.
2. Add read-only Contracts DTOs.
3. Add Application query services and Infrastructure projection.
4. Add thin controller routes under `/api/v1/organizations/{organizationId}/...`.
5. Add focused backend tests for allowed/forbidden reads and DTO shape.
6. Connect frontend Organization assessment to live read-only data.
