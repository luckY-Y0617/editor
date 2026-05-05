using Northstar.Domain.Shared;

namespace Northstar.Domain.Users;

public sealed class User
{
    private User()
    {
        DisplayName = string.Empty;
    }

    public User(
        string displayName,
        string? email = null,
        Guid? id = null,
        string? externalProvider = null,
        string? externalSubjectId = null)
    {
        Id = id ?? Guid.NewGuid();
        Email = NormalizeOptional(email);
        DisplayName = Required(displayName, nameof(displayName));
        ExternalProvider = NormalizeOptional(externalProvider);
        ExternalSubjectId = NormalizeOptional(externalSubjectId);
        if ((ExternalProvider is null) != (ExternalSubjectId is null))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "external provider and subject id must be provided together.");
        }

        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public string? Email { get; private set; }
    public string DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? ExternalProvider { get; private set; }
    public string? ExternalSubjectId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public void Rename(string displayName)
    {
        DisplayName = Required(displayName, nameof(displayName));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool ApplyExternalProfile(
        string externalProvider,
        string externalSubjectId,
        string displayName,
        string? email)
    {
        var provider = Required(externalProvider, nameof(externalProvider));
        var subjectId = Required(externalSubjectId, nameof(externalSubjectId));
        if (ExternalProvider is not null &&
            ExternalSubjectId is not null &&
            (ExternalProvider != provider || ExternalSubjectId != subjectId))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "user is already mapped to a different external identity.");
        }

        var normalizedDisplayName = Required(displayName, nameof(displayName));
        var normalizedEmail = NormalizeOptional(email);
        var changed =
            ExternalProvider != provider ||
            ExternalSubjectId != subjectId ||
            DisplayName != normalizedDisplayName ||
            Email != normalizedEmail;

        ExternalProvider = provider;
        ExternalSubjectId = subjectId;
        DisplayName = normalizedDisplayName;
        Email = normalizedEmail;
        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        return changed;
    }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, $"{parameterName} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
