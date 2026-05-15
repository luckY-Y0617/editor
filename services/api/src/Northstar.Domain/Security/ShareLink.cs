using Northstar.Domain.Shared;

namespace Northstar.Domain.Security;

public sealed class ShareLink
{
    private ShareLink()
    {
        ResourceType = ResourceTypes.Document;
        TokenHash = string.Empty;
        RoleKey = PermissionRole.Viewer;
        Audience = ShareLinkAudiences.Workspace;
    }

    public ShareLink(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string tokenHash,
        string roleKey,
        string audience,
        Guid? createdBy = null,
        DateTimeOffset? expiresAt = null,
        string? subjectEmail = null,
        string? passwordHash = null,
        string? tokenCiphertext = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        ResourceType = ValidResourceType(resourceType);
        ResourceId = resourceId;
        TokenHash = ValidTokenHash(tokenHash);
        RoleKey = ValidRole(roleKey);
        Audience = ValidAudience(audience);
        SubjectEmail = ValidSubjectEmail(Audience, subjectEmail);
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        PasswordHash = NormalizeOptional(passwordHash);
        TokenCiphertext = NormalizeOptional(tokenCiphertext);
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public string TokenHash { get; private set; }
    public string? TokenCiphertext { get; private set; }
    public string RoleKey { get; private set; }
    public string Audience { get; private set; }
    public string? SubjectEmail { get; private set; }
    public string? PasswordHash { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset? PausedAt { get; private set; }
    public Guid? PausedBy { get; private set; }
    public string? PauseReason { get; private set; }
    public bool HasPassword => !string.IsNullOrWhiteSpace(PasswordHash);

    public void Revoke()
    {
        if (RevokedAt.HasValue)
        {
            return;
        }

        RevokedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActive(DateTimeOffset now)
    {
        return RevokedAt is null && PausedAt is null && (ExpiresAt is null || ExpiresAt > now);
    }

    public void UpdateRole(string roleKey)
    {
        RoleKey = ValidRole(roleKey);
    }

    public void UpdateExpiry(DateTimeOffset? expiresAt)
    {
        ExpiresAt = expiresAt;
    }

    public void ReplaceToken(string tokenHash, string tokenCiphertext)
    {
        TokenHash = ValidTokenHash(tokenHash);
        TokenCiphertext = NormalizeOptional(tokenCiphertext)
            ?? throw new DomainException(DomainErrorCodes.ValidationError, "token ciphertext is required.");
    }

    public void Pause(Guid actorId, string? reason)
    {
        if (RevokedAt.HasValue)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "revoked share links cannot be paused.");
        }

        if (PausedAt.HasValue)
        {
            PauseReason = NormalizeOptional(reason);
            return;
        }

        PausedAt = DateTimeOffset.UtcNow;
        PausedBy = actorId;
        PauseReason = NormalizeOptional(reason);
    }

    public void Resume()
    {
        if (RevokedAt.HasValue)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "revoked share links cannot be resumed.");
        }

        PausedAt = null;
        PausedBy = null;
        PauseReason = null;
    }

    private static string ValidResourceType(string resourceType)
    {
        return ResourceTypes.IsScopedResource(resourceType)
            ? resourceType
            : throw new DomainException(DomainErrorCodes.ValidationError, "resource type is invalid.");
    }

    private static string ValidTokenHash(string tokenHash)
    {
        return string.IsNullOrWhiteSpace(tokenHash)
            ? throw new DomainException(DomainErrorCodes.ValidationError, "token hash is required.")
            : tokenHash.Trim();
    }

    private static string ValidRole(string role)
    {
        return ScopedPermissionRoles.IsSupportedLinkRole(role) && role is not null
            ? role
            : throw new DomainException(DomainErrorCodes.ValidationError, "share link role is invalid.");
    }

    private static string ValidAudience(string audience)
    {
        return ShareLinkAudiences.IsSupported(audience)
            ? audience
            : throw new DomainException(DomainErrorCodes.ValidationError, "share link audience is invalid.");
    }

    private static string? ValidSubjectEmail(string audience, string? email)
    {
        if (audience == ShareLinkAudiences.External)
        {
            return string.IsNullOrWhiteSpace(email)
                ? throw new DomainException(DomainErrorCodes.ValidationError, "external share link email is required.")
                : email.Trim().ToLowerInvariant();
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "subject email is only supported for external share links.");
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
