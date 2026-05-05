using Northstar.Domain.Shared;

namespace Northstar.Domain.Users;

public sealed class UserCredential
{
    private UserCredential()
    {
        PasswordHash = string.Empty;
        PasswordHashAlgorithm = string.Empty;
    }

    public UserCredential(Guid userId, string passwordHash, string passwordHashAlgorithm = "aspnet-password-hasher")
    {
        UserId = userId;
        PasswordHash = Required(passwordHash, nameof(passwordHash));
        PasswordHashAlgorithm = Required(passwordHashAlgorithm, nameof(passwordHashAlgorithm));
        PasswordUpdatedAt = DateTimeOffset.UtcNow;
        CreatedAt = PasswordUpdatedAt;
        UpdatedAt = PasswordUpdatedAt;
    }

    public Guid UserId { get; private set; }
    public string PasswordHash { get; private set; }
    public string PasswordHashAlgorithm { get; private set; }
    public DateTimeOffset PasswordUpdatedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdatePassword(string passwordHash, string passwordHashAlgorithm = "aspnet-password-hasher")
    {
        PasswordHash = Required(passwordHash, nameof(passwordHash));
        PasswordHashAlgorithm = Required(passwordHashAlgorithm, nameof(passwordHashAlgorithm));
        PasswordUpdatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = PasswordUpdatedAt;
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
