using Northstar.Contracts.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IPermissionNotificationService
{
    Task AddAsync(PermissionNotification notification, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<PermissionNotification> notifications, CancellationToken cancellationToken = default);
    Task<PermissionNotificationsResponse> GetNotificationsAsync(Guid? workspaceId, bool unreadOnly, CancellationToken cancellationToken = default);
    Task<PermissionNotificationDto> MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(Guid? workspaceId, CancellationToken cancellationToken = default);
}
