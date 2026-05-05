namespace Northstar.Domain.Security;

public sealed class WorkspaceGroupMember
{
    private WorkspaceGroupMember()
    {
    }

    public WorkspaceGroupMember(
        Guid groupId,
        Guid userId,
        Guid? addedBy = null,
        DateTimeOffset? expiresAt = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        GroupId = groupId;
        UserId = userId;
        AddedBy = addedBy;
        AddedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }
    public Guid GroupId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? AddedBy { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? RemovedAt { get; private set; }

    public void ChangeExpiry(DateTimeOffset? expiresAt)
    {
        ExpiresAt = expiresAt;
    }

    public void Remove()
    {
        if (RemovedAt.HasValue)
        {
            return;
        }

        RemovedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActive(DateTimeOffset now)
    {
        return RemovedAt is null && (ExpiresAt is null || ExpiresAt > now);
    }
}
