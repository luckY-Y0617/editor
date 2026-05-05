using Northstar.Domain.Shared;

namespace Northstar.Domain.Security;

public sealed class PermissionNotificationPreference
{
    private PermissionNotificationPreference()
    {
    }

    public PermissionNotificationPreference(
        Guid workspaceId,
        Guid userId,
        string? resourceType,
        Guid? resourceId,
        bool watched,
        bool muted,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        UserId = userId;
        ResourceType = ValidResourceType(resourceType, resourceId);
        ResourceId = ValidResourceId(ResourceType, resourceId);
        SetState(watched, muted);
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string? ResourceType { get; private set; }
    public Guid? ResourceId { get; private set; }
    public bool Watched { get; private set; }
    public bool Muted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void SetState(bool watched, bool muted)
    {
        if (watched && muted)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "notification preference cannot be watched and muted.");
        }

        Watched = watched;
        Muted = muted;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? ValidResourceType(string? resourceType, Guid? resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            if (resourceId.HasValue)
            {
                throw new DomainException(DomainErrorCodes.ValidationError, "resource type is required for resource notification preference.");
            }

            return null;
        }

        if (!resourceId.HasValue)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "resource id is required for resource notification preference.");
        }

        var normalized = resourceType.Trim().ToLowerInvariant();
        return ResourceTypes.IsScopedResource(normalized)
            ? normalized
            : throw new DomainException(DomainErrorCodes.ValidationError, "notification preference resource type is invalid.");
    }

    private static Guid? ValidResourceId(string? resourceType, Guid? resourceId)
    {
        if (resourceType is null)
        {
            return null;
        }

        return resourceId;
    }
}
