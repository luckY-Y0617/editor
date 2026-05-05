using Northstar.Domain.Shared;

namespace Northstar.Domain.Workspaces;

public sealed class WorkspaceMember
{
    private WorkspaceMember()
    {
        Role = WorkspaceMemberRole.Viewer;
        Status = WorkspaceMemberStatus.Active;
    }

    public WorkspaceMember(Guid workspaceId, Guid userId, string role)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        Role = ValidRole(role);
        Status = WorkspaceMemberStatus.Active;
        JoinedAt = DateTimeOffset.UtcNow;
        CreatedAt = JoinedAt.Value;
    }

    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; }
    public string Status { get; private set; }
    public DateTimeOffset? JoinedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void ChangeRole(string role)
    {
        Role = ValidRole(role);
    }

    private static string ValidRole(string role)
    {
        return WorkspaceMemberRole.IsValid(role)
            ? role
            : throw new DomainException(DomainErrorCodes.ValidationError, "workspace member role is invalid.");
    }
}
