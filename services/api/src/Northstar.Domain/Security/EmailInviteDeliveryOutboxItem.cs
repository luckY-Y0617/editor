using Northstar.Domain.Shared;

namespace Northstar.Domain.Security;

public sealed class EmailInviteDeliveryOutboxItem
{
    private EmailInviteDeliveryOutboxItem()
    {
        RecipientEmail = string.Empty;
        Provider = "noop";
        Status = EmailInviteDeliveryOutboxStatuses.Pending;
    }

    public EmailInviteDeliveryOutboxItem(
        Guid workspaceId,
        Guid inviteId,
        string recipientEmail,
        string provider,
        int maxAttempts,
        DateTimeOffset nextAttemptAt,
        Guid? id = null)
    {
        if (maxAttempts <= 0)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "max attempts must be positive.");
        }

        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        InviteId = inviteId;
        RecipientEmail = ValidEmail(recipientEmail);
        Provider = ValidProvider(provider);
        Status = EmailInviteDeliveryOutboxStatuses.Pending;
        AttemptCount = 0;
        MaxAttempts = maxAttempts;
        NextAttemptAt = nextAttemptAt;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid InviteId { get; private set; }
    public string RecipientEmail { get; private set; }
    public string Provider { get; private set; }
    public string Status { get; private set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public string? LastErrorCode { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsDue(DateTimeOffset now)
    {
        return (Status == EmailInviteDeliveryOutboxStatuses.Pending ||
                Status == EmailInviteDeliveryOutboxStatuses.RetryScheduled) &&
            NextAttemptAt <= now;
    }

    public void MarkSent(string provider, DateTimeOffset attemptedAt)
    {
        Provider = ValidProvider(provider);
        AttemptCount++;
        LastAttemptAt = attemptedAt;
        SentAt = attemptedAt;
        FailedAt = null;
        NextAttemptAt = null;
        LastErrorCode = null;
        LastErrorMessage = null;
        Status = EmailInviteDeliveryOutboxStatuses.Sent;
        UpdatedAt = attemptedAt;
    }

    public void MarkFailure(
        string provider,
        DateTimeOffset attemptedAt,
        string? errorCode,
        string? errorMessage,
        DateTimeOffset nextAttemptAt)
    {
        Provider = ValidProvider(provider);
        AttemptCount++;
        LastAttemptAt = attemptedAt;
        LastErrorCode = NormalizeOptional(errorCode);
        LastErrorMessage = NormalizeOptional(errorMessage);
        SentAt = null;
        if (AttemptCount >= MaxAttempts)
        {
            Status = EmailInviteDeliveryOutboxStatuses.Failed;
            FailedAt = attemptedAt;
            NextAttemptAt = null;
        }
        else
        {
            Status = EmailInviteDeliveryOutboxStatuses.RetryScheduled;
            FailedAt = null;
            NextAttemptAt = nextAttemptAt;
        }

        UpdatedAt = attemptedAt;
    }

    public void MarkTerminalFailure(
        string provider,
        DateTimeOffset failedAt,
        string errorCode,
        string? errorMessage)
    {
        Provider = ValidProvider(provider);
        Status = EmailInviteDeliveryOutboxStatuses.Failed;
        LastAttemptAt ??= failedAt;
        FailedAt = failedAt;
        NextAttemptAt = null;
        LastErrorCode = NormalizeOptional(errorCode);
        LastErrorMessage = NormalizeOptional(errorMessage);
        UpdatedAt = failedAt;
    }

    private static string ValidEmail(string email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? throw new DomainException(DomainErrorCodes.ValidationError, "recipient email is required.")
            : email.Trim().ToLowerInvariant();
    }

    private static string ValidProvider(string provider)
    {
        return string.IsNullOrWhiteSpace(provider)
            ? "noop"
            : provider.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
