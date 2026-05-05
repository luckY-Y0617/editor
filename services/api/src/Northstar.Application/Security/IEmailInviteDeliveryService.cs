namespace Northstar.Application.Security;

public interface IEmailInviteDeliveryService
{
    Task<EmailInviteDeliveryResult> SendAsync(
        EmailInviteDeliveryMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record EmailInviteDeliveryMessage(
    Guid InviteId,
    Guid WorkspaceId,
    string ResourceType,
    Guid ResourceId,
    string Email,
    string RoleKey,
    DateTimeOffset ExpiresAt,
    string AcceptUrl);

public sealed record EmailInviteDeliveryResult(
    string Status,
    string Provider,
    DateTimeOffset? AttemptedAt,
    string? ErrorCode = null);
