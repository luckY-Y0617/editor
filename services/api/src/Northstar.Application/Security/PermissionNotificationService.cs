using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Contracts.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class PermissionNotificationService : IPermissionNotificationService
{
    private readonly IPermissionNotificationRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public PermissionNotificationService(
        IPermissionNotificationRepository repository,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public Task AddAsync(PermissionNotification notification, CancellationToken cancellationToken = default)
    {
        return _repository.AddAsync(notification, cancellationToken);
    }

    public Task AddRangeAsync(IEnumerable<PermissionNotification> notifications, CancellationToken cancellationToken = default)
    {
        return _repository.AddRangeAsync(notifications, cancellationToken);
    }

    public async Task<PermissionNotificationsResponse> GetNotificationsAsync(
        Guid? workspaceId,
        bool unreadOnly,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var notifications = await _repository.GetForRecipientAsync(userId, workspaceId, unreadOnly, cancellationToken);
        var allNotifications = unreadOnly
            ? await _repository.GetForRecipientAsync(userId, workspaceId, unreadOnly: false, cancellationToken)
            : notifications;
        return new PermissionNotificationsResponse(
            notifications.Select(ToDto).ToArray(),
            allNotifications.Count(notification => notification.ReadAt is null));
    }

    public async Task<PermissionNotificationDto> MarkReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var notification = await _repository.GetForUpdateAsync(notificationId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Notification was not found.");
        if (notification.RecipientUserId != userId)
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Notification access is denied.");
        }

        notification.MarkRead();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(notification);
    }

    public async Task MarkAllReadAsync(Guid? workspaceId, CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var notifications = await _repository.GetForRecipientAsync(
            userId,
            workspaceId,
            unreadOnly: true,
            cancellationToken);
        foreach (var notification in notifications)
        {
            var tracked = await _repository.GetForUpdateAsync(notification.Id, cancellationToken);
            tracked?.MarkRead();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private Guid GetRequiredUserId()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Authentication is required.");
        }

        return _currentUser.UserId.Value;
    }

    private static PermissionNotificationDto ToDto(PermissionNotification notification)
    {
        return new PermissionNotificationDto(
            notification.Id.ToString(),
            notification.WorkspaceId.ToString(),
            notification.RecipientUserId.ToString(),
            notification.ActorUserId?.ToString(),
            notification.Type,
            notification.ResourceType,
            notification.ResourceId?.ToString(),
            notification.AccessRequestId?.ToString(),
            notification.PermissionGrantId?.ToString(),
            notification.Title,
            notification.Body,
            notification.ActionUrl,
            notification.ReadAt,
            notification.CreatedAt);
    }
}
