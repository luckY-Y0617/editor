using Northstar.Domain.Shared;

namespace Northstar.Domain.Security;

public sealed class ScimToken
{
    private ScimToken()
    {
        Name = string.Empty;
        TokenHash = string.Empty;
    }

    public ScimToken(
        Guid workspaceId,
        string name,
        string tokenHash,
        Guid? createdBy = null,
        DateTimeOffset? expiresAt = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        Name = Required(name, "SCIM token name");
        TokenHash = Required(tokenHash, "SCIM token hash");
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; }
    public string TokenHash { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }

    public bool IsActive(DateTimeOffset now)
    {
        return RevokedAt is null && (ExpiresAt is null || ExpiresAt > now);
    }

    public void MarkUsed(DateTimeOffset now)
    {
        LastUsedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        if (RevokedAt.HasValue)
        {
            return;
        }

        RevokedAt = now;
    }

    private static string Required(string value, string fieldName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new DomainException(DomainErrorCodes.ValidationError, $"{fieldName} is required.")
            : value.Trim();
    }
}
