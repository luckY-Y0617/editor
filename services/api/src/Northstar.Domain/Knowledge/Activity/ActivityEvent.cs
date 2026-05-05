using Northstar.Domain.Shared;

namespace Northstar.Domain.Knowledge.Activity;

public sealed class ActivityEvent
{
    private ActivityEvent()
    {
        EntityType = string.Empty;
        Action = string.Empty;
        Summary = string.Empty;
        Metadata = "{}";
    }

    public ActivityEvent(
        Guid workspaceId,
        Guid? actorId,
        string entityType,
        Guid entityId,
        string action,
        string summary,
        string? metadata = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        ActorId = actorId;
        EntityType = Required(entityType, nameof(entityType));
        EntityId = entityId;
        Action = Required(action, nameof(action));
        Summary = Required(summary, nameof(summary));
        Metadata = string.IsNullOrWhiteSpace(metadata) ? "{}" : metadata;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid? ActorId { get; private set; }
    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string Action { get; private set; }
    public string Summary { get; private set; }
    public string Metadata { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, $"{parameterName} is required.");
        }

        return value.Trim();
    }
}

