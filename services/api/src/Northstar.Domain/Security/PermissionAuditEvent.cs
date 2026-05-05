using Northstar.Domain.Shared;

namespace Northstar.Domain.Security;

public sealed class PermissionAuditEvent
{
    private PermissionAuditEvent()
    {
        Action = string.Empty;
        ResourceType = ResourceTypes.Document;
        Metadata = "{}";
    }

    public PermissionAuditEvent(
        Guid workspaceId,
        Guid? actorId,
        string action,
        string resourceType,
        Guid resourceId,
        string? subjectType = null,
        Guid? subjectId = null,
        string? beforeJson = null,
        string? afterJson = null,
        string? metadata = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        ActorId = actorId;
        Action = Required(action, "action");
        ResourceType = ResourceTypes.IsSupported(resourceType)
            ? resourceType
            : throw new DomainException(DomainErrorCodes.ValidationError, "resource type is invalid.");
        ResourceId = resourceId;
        SubjectType = subjectType;
        SubjectId = subjectId;
        BeforeJson = beforeJson;
        AfterJson = afterJson;
        Metadata = string.IsNullOrWhiteSpace(metadata) ? "{}" : metadata;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid? ActorId { get; private set; }
    public string Action { get; private set; }
    public string ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public string? SubjectType { get; private set; }
    public Guid? SubjectId { get; private set; }
    public string? BeforeJson { get; private set; }
    public string? AfterJson { get; private set; }
    public string Metadata { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private static string Required(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, $"{fieldName} is required.");
        }

        return value.Trim();
    }
}
