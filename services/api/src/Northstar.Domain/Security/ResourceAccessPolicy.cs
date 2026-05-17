using Northstar.Domain.Shared;

namespace Northstar.Domain.Security;

public sealed class ResourceAccessPolicy
{
    private ResourceAccessPolicy()
    {
        ResourceType = ResourceTypes.Document;
        InheritanceMode = InheritanceModes.Inherit;
        LinkMode = LinkModes.Disabled;
    }

    public ResourceAccessPolicy(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string inheritanceMode = InheritanceModes.Inherit,
        string linkMode = LinkModes.Disabled,
        string? defaultLinkRole = null,
        Guid? createdBy = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        ResourceType = ValidResourceType(resourceType);
        ResourceId = resourceId;
        InheritanceMode = ValidInheritanceMode(inheritanceMode);
        LinkMode = ValidLinkMode(linkMode);
        DefaultLinkRole = ValidDefaultLinkRole(defaultLinkRole);
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public string InheritanceMode { get; private set; }
    public string LinkMode { get; private set; }
    public string? DefaultLinkRole { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void SetInheritanceMode(string mode)
    {
        InheritanceMode = ValidInheritanceMode(mode);
        Touch();
    }

    public void SetLinkMode(string mode, string? defaultLinkRole)
    {
        LinkMode = ValidLinkMode(mode);
        DefaultLinkRole = ValidDefaultLinkRole(defaultLinkRole);
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string ValidResourceType(string resourceType)
    {
        return ResourceTypes.IsShareableResource(resourceType)
            ? resourceType
            : throw new DomainException(DomainErrorCodes.ValidationError, "resource type is invalid.");
    }

    private static string ValidInheritanceMode(string inheritanceMode)
    {
        return InheritanceModes.IsSupported(inheritanceMode)
            ? inheritanceMode
            : throw new DomainException(DomainErrorCodes.ValidationError, "inheritance mode is invalid.");
    }

    private static string ValidLinkMode(string linkMode)
    {
        return LinkModes.IsSupported(linkMode)
            ? linkMode
            : throw new DomainException(DomainErrorCodes.ValidationError, "link mode is invalid.");
    }

    private static string? ValidDefaultLinkRole(string? role)
    {
        return ScopedPermissionRoles.IsSupportedLinkRole(role)
            ? role
            : throw new DomainException(DomainErrorCodes.ValidationError, "default link role is invalid.");
    }
}
