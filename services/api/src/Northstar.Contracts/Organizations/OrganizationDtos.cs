namespace Northstar.Contracts.Organizations;

public sealed record UpdateOrganizationProfileRequest(string Name, string Slug);

public sealed record OrganizationProfileResponse(OrganizationProfileDto Organization);

public sealed record OrganizationProfileDto(
    string Id,
    string Name,
    string Slug,
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

public sealed record OrganizationMembersResponse(IReadOnlyList<OrganizationMemberDto> Members);

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
