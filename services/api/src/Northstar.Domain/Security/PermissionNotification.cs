using Northstar.Domain.Shared;

namespace Northstar.Domain.Security;

public sealed class PermissionNotification
{
    private PermissionNotification()
    {
        Type = PermissionNotificationTypes.AccessRequestCreated;
        Title = string.Empty;
    }

    public PermissionNotification(
        Guid workspaceId,
        Guid recipientUserId,
        string type,
        string title,
        string? body = null,
        Guid? actorUserId = null,
        string? resourceType = null,
        Guid? resourceId = null,
        Guid? accessRequestId = null,
        Guid? permissionGrantId = null,
        string? actionUrl = null,
        Guid? id = null,
        string? dedupeKey = null)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        RecipientUserId = recipientUserId;
        ActorUserId = actorUserId;
        Type = ValidType(type);
        ResourceType = ValidResourceType(resourceType);
        ResourceId = resourceId;
        AccessRequestId = accessRequestId;
        PermissionGrantId = permissionGrantId;
        Title = ValidTitle(title);
        Body = NormalizeOptional(body);
        ActionUrl = NormalizeOptional(actionUrl);
        DedupeKey = NormalizeOptional(dedupeKey);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string Type { get; private set; }
    public string? ResourceType { get; private set; }
    public Guid? ResourceId { get; private set; }
    public Guid? AccessRequestId { get; private set; }
    public Guid? PermissionGrantId { get; private set; }
    public string Title { get; private set; }
    public string? Body { get; private set; }
    public string? ActionUrl { get; private set; }
    public string? DedupeKey { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void MarkRead()
    {
        ReadAt ??= DateTimeOffset.UtcNow;
    }

    private static string ValidType(string type)
    {
        var normalized = type.Trim();
        return PermissionNotificationTypes.IsSupported(normalized)
            ? normalized
            : throw new DomainException(DomainErrorCodes.ValidationError, "permission notification type is invalid.");
    }

    private static string? ValidResourceType(string? resourceType)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            return null;
        }

        var normalized = resourceType.Trim().ToLowerInvariant();
        return ResourceTypes.IsSupported(normalized)
            ? normalized
            : throw new DomainException(DomainErrorCodes.ValidationError, "permission notification resource type is invalid.");
    }

    private static string ValidTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "permission notification title is required.");
        }

        return title.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
