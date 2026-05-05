using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IPermissionNotificationRepository
{
    Task<IReadOnlyList<PermissionNotification>> GetForRecipientAsync(
        Guid recipientUserId,
        Guid? workspaceId,
        bool unreadOnly,
        CancellationToken cancellationToken = default);

    Task<PermissionNotification?> GetForUpdateAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PermissionNotification notification, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<PermissionNotification> notifications, CancellationToken cancellationToken = default);
}
