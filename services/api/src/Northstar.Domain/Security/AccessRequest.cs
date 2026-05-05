using Northstar.Domain.Shared;

namespace Northstar.Domain.Security;

public sealed class AccessRequest
{
    private AccessRequest()
    {
        ResourceType = ResourceTypes.Document;
        SubjectType = SubjectTypes.User;
        RequestedRole = PermissionRole.Viewer;
        Status = AccessRequestStatus.Pending;
    }

    public AccessRequest(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid requesterId,
        string subjectType,
        Guid subjectId,
        string requestedRole,
        string? reason = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        ResourceType = ValidResourceType(resourceType);
        ResourceId = resourceId;
        RequesterId = requesterId;
        SubjectType = ValidSubjectType(subjectType);
        SubjectId = subjectId;
        RequestedRole = ValidRole(requestedRole);
        Reason = NormalizeOptional(reason);
        Status = AccessRequestStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public Guid RequesterId { get; private set; }
    public string SubjectType { get; private set; }
    public Guid SubjectId { get; private set; }
    public string RequestedRole { get; private set; }
    public string? Reason { get; private set; }
    public string Status { get; private set; }
    public Guid? DecidedBy { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? DecisionReason { get; private set; }
    public Guid? ResultingGrantId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Approve(Guid decidedBy, Guid resultingGrantId, string? decisionReason)
    {
        EnsurePending();
        Status = AccessRequestStatus.Approved;
        DecidedBy = decidedBy;
        DecidedAt = DateTimeOffset.UtcNow;
        DecisionReason = NormalizeOptional(decisionReason);
        ResultingGrantId = resultingGrantId;
        UpdatedAt = DecidedAt.Value;
    }

    public void Deny(Guid decidedBy, string? decisionReason)
    {
        EnsurePending();
        Status = AccessRequestStatus.Denied;
        DecidedBy = decidedBy;
        DecidedAt = DateTimeOffset.UtcNow;
        DecisionReason = NormalizeOptional(decisionReason);
        UpdatedAt = DecidedAt.Value;
    }

    public void Cancel(Guid cancelledBy, string? decisionReason)
    {
        EnsurePending();
        Status = AccessRequestStatus.Cancelled;
        DecidedBy = cancelledBy;
        DecidedAt = DateTimeOffset.UtcNow;
        DecisionReason = NormalizeOptional(decisionReason);
        UpdatedAt = DecidedAt.Value;
    }

    private void EnsurePending()
    {
        if (Status != AccessRequestStatus.Pending)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "access request is not pending.");
        }
    }

    private static string ValidResourceType(string resourceType)
    {
        var normalized = resourceType.Trim().ToLowerInvariant();
        return ResourceTypes.IsScopedResource(normalized)
            ? normalized
            : throw new DomainException(DomainErrorCodes.ValidationError, "access request resource type is invalid.");
    }

    private static string ValidSubjectType(string subjectType)
    {
        var normalized = subjectType.Trim().ToLowerInvariant();
        return SubjectTypes.IsSupported(normalized)
            ? normalized
            : throw new DomainException(DomainErrorCodes.ValidationError, "access request subject type is invalid.");
    }

    private static string ValidRole(string role)
    {
        var normalized = role.Trim().ToLowerInvariant();
        return ScopedPermissionRoles.IsSupported(normalized)
            ? normalized
            : throw new DomainException(DomainErrorCodes.ValidationError, "access request role is invalid.");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
