using Northstar.Domain.Shared;

namespace Northstar.Domain.Security;

public sealed class ResourceEmailInvite
{
    private ResourceEmailInvite()
    {
        ResourceType = ResourceTypes.Document;
        Email = string.Empty;
        TokenHash = string.Empty;
        RoleKey = PermissionRole.Viewer;
        Status = EmailInviteStatuses.Pending;
        DeliveryStatus = EmailInviteDeliveryStatuses.Disabled;
        DeliveryProvider = "noop";
    }

    public ResourceEmailInvite(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string email,
        string tokenHash,
        string roleKey,
        DateTimeOffset expiresAt,
        Guid? invitedBy = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        ResourceType = ValidResourceType(resourceType);
        ResourceId = resourceId;
        Email = ValidEmail(email);
        TokenHash = ValidTokenHash(tokenHash);
        RoleKey = ValidRole(roleKey);
        Status = EmailInviteStatuses.Pending;
        InvitedBy = invitedBy;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        DeliveryStatus = EmailInviteDeliveryStatuses.Disabled;
        DeliveryProvider = "noop";
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public string Email { get; private set; }
    public string TokenHash { get; private set; }
    public string RoleKey { get; private set; }
    public string Status { get; private set; }
    public Guid? InvitedBy { get; private set; }
    public Guid? AcceptedBy { get; private set; }
    public Guid? RevokedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset? ExpiredAt { get; private set; }
    public string DeliveryStatus { get; private set; }
    public string DeliveryProvider { get; private set; }
    public DateTimeOffset? DeliveryAttemptedAt { get; private set; }
    public string? DeliveryErrorCode { get; private set; }

    public bool IsPendingActive(DateTimeOffset now)
    {
        return Status == EmailInviteStatuses.Pending && ExpiresAt > now;
    }

    public bool IsAcceptedActive(DateTimeOffset now)
    {
        return Status == EmailInviteStatuses.Accepted && ExpiresAt > now && RevokedAt is null;
    }

    public void Accept(Guid userId)
    {
        if (Status == EmailInviteStatuses.Accepted && AcceptedBy == userId)
        {
            return;
        }

        if (Status != EmailInviteStatuses.Pending)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "invite cannot be accepted.");
        }

        Status = EmailInviteStatuses.Accepted;
        AcceptedBy = userId;
        AcceptedAt = DateTimeOffset.UtcNow;
    }

    public void Revoke(Guid userId)
    {
        if (Status == EmailInviteStatuses.Revoked)
        {
            return;
        }

        Status = EmailInviteStatuses.Revoked;
        RevokedBy = userId;
        RevokedAt = DateTimeOffset.UtcNow;
    }

    public void Expire()
    {
        if (Status != EmailInviteStatuses.Pending)
        {
            return;
        }

        Status = EmailInviteStatuses.Expired;
        ExpiredAt = DateTimeOffset.UtcNow;
    }

    public void MarkDelivery(
        string status,
        string provider,
        DateTimeOffset? attemptedAt,
        string? errorCode)
    {
        DeliveryStatus = ValidDeliveryStatus(status);
        DeliveryProvider = ValidDeliveryProvider(provider);
        DeliveryAttemptedAt = attemptedAt;
        DeliveryErrorCode = NormalizeOptionalCode(errorCode);
    }

    private static string ValidResourceType(string resourceType)
    {
        return ResourceTypes.IsScopedResource(resourceType)
            ? resourceType
            : throw new DomainException(DomainErrorCodes.ValidationError, "resource type is invalid.");
    }

    private static string ValidEmail(string email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? throw new DomainException(DomainErrorCodes.ValidationError, "email is required.")
            : email.Trim().ToLowerInvariant();
    }

    private static string ValidTokenHash(string tokenHash)
    {
        return string.IsNullOrWhiteSpace(tokenHash)
            ? throw new DomainException(DomainErrorCodes.ValidationError, "token hash is required.")
            : tokenHash.Trim();
    }

    private static string ValidRole(string role)
    {
        return role is PermissionRole.Viewer or PermissionRole.Commenter
            ? role
            : throw new DomainException(DomainErrorCodes.ValidationError, "invite role is invalid.");
    }

    private static string ValidDeliveryStatus(string status)
    {
        return EmailInviteDeliveryStatuses.IsSupported(status)
            ? status
            : throw new DomainException(DomainErrorCodes.ValidationError, "invite delivery status is invalid.");
    }

    private static string ValidDeliveryProvider(string provider)
    {
        return string.IsNullOrWhiteSpace(provider)
            ? "noop"
            : provider.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptionalCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code)
            ? null
            : code.Trim().ToLowerInvariant();
    }
}
