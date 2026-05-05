using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IPermissionNotificationFanoutService
{
    Task AddAccessRequestCreatedAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid accessRequestId,
        Guid actorId,
        string requestedRole,
        CancellationToken cancellationToken = default);

    Task AddGrantNotificationAsync(
        ResourceAccessGrant grant,
        Guid actorId,
        string type,
        string title,
        string body,
        CancellationToken cancellationToken = default);

    Task AddShareLinkCreatedAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid shareLinkId,
        Guid actorId,
        CancellationToken cancellationToken = default);

    Task AddShareLinkRevokedAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid shareLinkId,
        Guid actorId,
        CancellationToken cancellationToken = default);

    Task AddEmailInviteCreatedAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid inviteId,
        Guid actorId,
        CancellationToken cancellationToken = default);

    Task AddEmailInviteAcceptedAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid inviteId,
        Guid actorId,
        Guid? invitedBy,
        CancellationToken cancellationToken = default);

    Task AddEmailInviteRevokedAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid inviteId,
        Guid actorId,
        Guid? invitedBy,
        Guid? acceptedBy,
        CancellationToken cancellationToken = default);

    Task AddEmailInviteDeliveryFailedAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid inviteId,
        Guid actorId,
        CancellationToken cancellationToken = default);
}
