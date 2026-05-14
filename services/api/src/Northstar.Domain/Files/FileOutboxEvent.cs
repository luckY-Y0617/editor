using Northstar.Domain.Shared;

namespace Northstar.Domain.Files;

public sealed class FileOutboxEvent
{
    private FileOutboxEvent()
    {
        AggregateType = string.Empty;
        EventType = string.Empty;
        Payload = "{}";
        Headers = "{}";
        Status = FileOutboxEventStatus.Pending;
    }

    public FileOutboxEvent(
        Guid workspaceId,
        string aggregateType,
        Guid aggregateId,
        string eventType,
        string payload,
        string? headers = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        AggregateType = Required(aggregateType, nameof(aggregateType));
        AggregateId = aggregateId;
        EventType = Required(eventType, nameof(eventType));
        Payload = Required(payload, nameof(payload));
        Headers = string.IsNullOrWhiteSpace(headers) ? "{}" : headers;
        Status = FileOutboxEventStatus.Pending;
        NextRetryAt = DateTimeOffset.UtcNow;
        CreatedAt = NextRetryAt;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string AggregateType { get; private set; }
    public Guid AggregateId { get; private set; }
    public string EventType { get; private set; }
    public string Payload { get; private set; }
    public string Headers { get; private set; }
    public string Status { get; private set; }
    public int RetryCount { get; private set; }
    public DateTimeOffset NextRetryAt { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void MarkPublished(DateTimeOffset now)
    {
        if (Status == FileOutboxEventStatus.Published)
        {
            return;
        }

        Status = FileOutboxEventStatus.Published;
        LastError = null;
        UpdatedAt = now;
    }

    public void MarkFailure(DateTimeOffset now, string error, DateTimeOffset nextRetryAt, int maxAttempts)
    {
        if (Status == FileOutboxEventStatus.Published)
        {
            return;
        }

        RetryCount++;
        LastError = string.IsNullOrWhiteSpace(error) ? "file_outbox_processing_failed" : error.Trim();
        Status = RetryCount >= Math.Max(1, maxAttempts)
            ? FileOutboxEventStatus.Failed
            : FileOutboxEventStatus.Pending;
        NextRetryAt = Status == FileOutboxEventStatus.Pending ? nextRetryAt : now;
        UpdatedAt = now;
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
