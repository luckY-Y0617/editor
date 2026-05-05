using Northstar.Domain.Shared;

namespace Northstar.Domain.Security;

public sealed class ResourceAccessGrant
{
    private ResourceAccessGrant()
    {
        ResourceType = ResourceTypes.Document;
        SubjectType = SubjectTypes.User;
        RoleKey = PermissionRole.Viewer;
    }

    public ResourceAccessGrant(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string subjectType,
        Guid subjectId,
        string roleKey,
        Guid? grantedBy = null,
        DateTimeOffset? expiresAt = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        ResourceType = ValidResourceType(resourceType);
        ResourceId = resourceId;
        SubjectType = ValidSubjectType(subjectType);
        SubjectId = subjectId;
        RoleKey = ValidRole(roleKey);
        GrantedBy = grantedBy;
        GrantedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public string SubjectType { get; private set; }
    public Guid SubjectId { get; private set; }
    public string RoleKey { get; private set; }
    public Guid? GrantedBy { get; private set; }
    public DateTimeOffset GrantedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedBy { get; private set; }
    public string? Reason { get; private set; }

    public void Revoke(Guid? revokedBy, string? reason)
    {
        if (RevokedAt.HasValue)
        {
            return;
        }

        RevokedAt = DateTimeOffset.UtcNow;
        RevokedBy = revokedBy;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public void ChangeRole(string roleKey)
    {
        RoleKey = ValidRole(roleKey);
    }

    public void ChangeExpiry(DateTimeOffset? expiresAt)
    {
        ExpiresAt = expiresAt;
    }

    public void ChangeReason(string? reason)
    {
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public bool IsActive(DateTimeOffset now)
    {
        return RevokedAt is null && (ExpiresAt is null || ExpiresAt > now);
    }

    private static string ValidResourceType(string resourceType)
    {
        return ResourceTypes.IsScopedResource(resourceType)
            ? resourceType
            : throw new DomainException(DomainErrorCodes.ValidationError, "resource type is invalid.");
    }

    private static string ValidSubjectType(string subjectType)
    {
        return SubjectTypes.IsSupported(subjectType)
            ? subjectType
            : throw new DomainException(DomainErrorCodes.ValidationError, "subject type is invalid.");
    }

    private static string ValidRole(string role)
    {
        return ScopedPermissionRoles.IsSupported(role)
            ? role
            : throw new DomainException(DomainErrorCodes.ValidationError, "resource access role is invalid.");
    }
}
