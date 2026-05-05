namespace Northstar.Contracts.Workspaces;

public sealed record WorkspaceMemberDto(
    string UserId,
    string? Email,
    string DisplayName,
    string Role,
    string Status,
    DateTimeOffset? JoinedAt);

public sealed record WorkspaceMembersResponse(IReadOnlyList<WorkspaceMemberDto> Members);

public sealed record AddWorkspaceMemberRequest(string Email, string Role);

public sealed record UpdateWorkspaceMemberRequest(string Role);
