namespace Northstar.Domain.Users;

public sealed class AuthEvent
{
    private AuthEvent()
    {
        Action = string.Empty;
        Metadata = "{}";
    }

    public AuthEvent(
        Guid? userId,
        string action,
        bool succeeded,
        string? ipAddress = null,
        string? userAgent = null,
        string? metadata = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        UserId = userId;
        Action = action.Trim();
        Succeeded = succeeded;
        IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
        UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
        Metadata = string.IsNullOrWhiteSpace(metadata) ? "{}" : metadata;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string Action { get; private set; }
    public bool Succeeded { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string Metadata { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
