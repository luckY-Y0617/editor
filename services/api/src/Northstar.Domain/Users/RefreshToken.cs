using Northstar.Domain.Shared;

namespace Northstar.Domain.Users;

public sealed class RefreshToken
{
    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    public RefreshToken(
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTimeOffset expiresAt,
        string? createdByIp = null,
        string? userAgent = null,
        Guid? id = null)
    {
        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "expiresAt must be in the future.");
        }

        Id = id ?? Guid.NewGuid();
        UserId = userId;
        TokenHash = Required(tokenHash, nameof(tokenHash));
        FamilyId = familyId;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        CreatedByIp = string.IsNullOrWhiteSpace(createdByIp) ? null : createdByIp.Trim();
        UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public Guid FamilyId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RotatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? UserAgent { get; private set; }

    public bool IsUsable(DateTimeOffset now)
    {
        return RevokedAt is null && RotatedAt is null && ExpiresAt > now;
    }

    public void Rotate(Guid replacementTokenId)
    {
        RotatedAt = DateTimeOffset.UtcNow;
        ReplacedByTokenId = replacementTokenId;
    }

    public void Revoke()
    {
        RevokedAt ??= DateTimeOffset.UtcNow;
    }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, $"{parameterName} is required.");
        }

        return value.Trim();
    }
}
