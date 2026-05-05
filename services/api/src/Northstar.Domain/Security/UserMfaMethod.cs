using Northstar.Domain.Shared;

namespace Northstar.Domain.Security;

public sealed class UserMfaMethod
{
    private UserMfaMethod()
    {
        MethodType = MfaMethodTypes.Totp;
        SecretCiphertext = string.Empty;
        Status = MfaMethodStatuses.Pending;
    }

    public UserMfaMethod(
        Guid userId,
        string methodType,
        string secretCiphertext,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        UserId = userId;
        MethodType = NormalizeMethodType(methodType);
        SecretCiphertext = Required(secretCiphertext, "MFA secret ciphertext");
        Status = MfaMethodStatuses.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string MethodType { get; private set; }
    public string SecretCiphertext { get; private set; }
    public string Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public DateTimeOffset? DisabledAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }

    public bool IsPending => Status == MfaMethodStatuses.Pending;
    public bool IsEnabled => Status == MfaMethodStatuses.Enabled && DisabledAt is null;

    public void MarkVerified(DateTimeOffset verifiedAt)
    {
        if (Status == MfaMethodStatuses.Disabled)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "disabled MFA methods cannot be verified.");
        }

        Status = MfaMethodStatuses.Enabled;
        VerifiedAt ??= verifiedAt;
        LastUsedAt = verifiedAt;
    }

    public void MarkUsed(DateTimeOffset usedAt)
    {
        if (!IsEnabled)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "MFA method is not enabled.");
        }

        LastUsedAt = usedAt;
    }

    public void Disable(DateTimeOffset disabledAt)
    {
        if (Status == MfaMethodStatuses.Disabled)
        {
            return;
        }

        Status = MfaMethodStatuses.Disabled;
        DisabledAt = disabledAt;
    }

    private static string NormalizeMethodType(string methodType)
    {
        var normalized = Required(methodType, "MFA method type").ToLowerInvariant();
        return normalized == MfaMethodTypes.Totp
            ? normalized
            : throw new DomainException(DomainErrorCodes.ValidationError, "MFA method type is invalid.");
    }

    private static string Required(string value, string fieldName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new DomainException(DomainErrorCodes.ValidationError, $"{fieldName} is required.")
            : value.Trim();
    }
}
