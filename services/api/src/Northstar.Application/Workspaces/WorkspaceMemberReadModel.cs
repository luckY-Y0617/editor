namespace Northstar.Application.Workspaces;

public sealed record WorkspaceMemberReadModel(
    Guid UserId,
    string? Email,
    string DisplayName,
    string Role,
    string Status,
    DateTimeOffset? JoinedAt);
