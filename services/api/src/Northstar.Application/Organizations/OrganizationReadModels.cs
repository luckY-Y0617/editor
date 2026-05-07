namespace Northstar.Application.Organizations;

public sealed record OrganizationProfileReadModel(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<OrganizationWorkspaceReadModel> Workspaces);

public sealed record OrganizationWorkspaceReadModel(
    Guid Id,
    string Name,
    string Slug,
    Guid? CurrentSpaceId,
    string CurrentUserRole,
    DateTimeOffset CreatedAt);

public sealed record OrganizationMemberFlatReadModel(
    Guid UserId,
    string? Email,
    string DisplayName,
    string WorkspaceId,
    string WorkspaceName,
    string Role,
    string Status,
    DateTimeOffset? JoinedAt);
