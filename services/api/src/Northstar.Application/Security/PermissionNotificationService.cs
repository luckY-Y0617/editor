using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Contracts.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class PermissionNotificationService : IPermissionNotificationService
{
    private readonly IPermissionNotificationRepository _repository;
    private readonly IPermissionResourceDisplayResolver _resourceDisplayResolver;
    private readonly IPermissionUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public PermissionNotificationService(
        IPermissionNotificationRepository repository,
        IPermissionResourceDisplayResolver resourceDisplayResolver,
        IPermissionUserRepository userRepository,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _resourceDisplayResolver = resourceDisplayResolver;
        _userRepository = userRepository;
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
        var displaySummaries = await ResolveDisplaySummariesAsync(notifications, cancellationToken);
        var actors = await ResolveActorsAsync(notifications, cancellationToken);
        return new PermissionNotificationsResponse(
            notifications.Select(notification => ToDto(notification, displaySummaries, actors)).ToArray(),
            allNotifications.Count(notification => notification.ReadAt is null));
    }

    public async Task<AccessSharingSummaryResponse> GetSummaryAsync(
        Guid? workspaceId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var notifications = await _repository.GetForRecipientAsync(
            userId,
            workspaceId,
            unreadOnly: false,
            cancellationToken);

        return new AccessSharingSummaryResponse(
            TotalCount: notifications.Count,
            UnreadCount: notifications.Count(notification => notification.ReadAt is null),
            PendingReviewCount: notifications.Count(notification =>
                notification.Type == PermissionNotificationTypes.AccessRequestCreated &&
                notification.ReadAt is null),
            AccessRequestCount: notifications.Count(notification => GetCategory(notification.Type) == NotificationCategories.Access),
            GrantCount: notifications.Count(notification => GetCategory(notification.Type) == NotificationCategories.Grant),
            SharingCount: notifications.Count(notification => GetCategory(notification.Type) == NotificationCategories.Sharing),
            ExpiryCount: notifications.Count(notification => GetCategory(notification.Type) == NotificationCategories.Expiry),
            FailedInviteCount: notifications.Count(notification => notification.Type == PermissionNotificationTypes.EmailInviteDeliveryFailed));
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
        var displaySummaries = await ResolveDisplaySummariesAsync([notification], cancellationToken);
        var actors = await ResolveActorsAsync([notification], cancellationToken);
        return ToDto(notification, displaySummaries, actors);
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

    private async Task<IReadOnlyDictionary<NotificationResourceKey, PermissionResourceDisplaySummary>> ResolveDisplaySummariesAsync(
        IReadOnlyCollection<PermissionNotification> notifications,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<NotificationResourceKey, PermissionResourceDisplaySummary>();
        var groups = notifications
            .Where(notification => notification.ResourceType is not null && notification.ResourceId is not null)
            .GroupBy(notification => notification.WorkspaceId);

        foreach (var group in groups)
        {
            var resources = group
                .GroupBy(notification => new NotificationResourceKey(group.Key, notification.ResourceType!, notification.ResourceId!.Value))
                .Select(resourceGroup => new PermissionResourceReference(resourceGroup.Key.ResourceType, resourceGroup.Key.ResourceId))
                .ToArray();
            if (resources.Length == 0)
            {
                continue;
            }

            var summaries = await _resourceDisplayResolver.GetDisplaySummariesAsync(group.Key, resources, cancellationToken);
            foreach (var summary in summaries)
            {
                result[new NotificationResourceKey(group.Key, summary.ResourceType, summary.ResourceId)] = summary;
            }
        }

        return result;
    }

    private static PermissionNotificationDto ToDto(
        PermissionNotification notification,
        IReadOnlyDictionary<NotificationResourceKey, PermissionResourceDisplaySummary> displaySummaries,
        IReadOnlyDictionary<Guid, PermissionUserIdentity> actors)
    {
        var actor = CreateActorDto(notification, actors);
        var resource = CreateResourceDto(notification, displaySummaries);
        var action = CreateActionDto(notification);
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
            notification.CreatedAt,
            actor,
            resource,
            action,
            GetCategory(notification.Type),
            GetState(notification.Type));
    }

    private Task<IReadOnlyDictionary<Guid, PermissionUserIdentity>> ResolveActorsAsync(
        IReadOnlyCollection<PermissionNotification> notifications,
        CancellationToken cancellationToken)
    {
        var actorIds = notifications
            .Select(notification => notification.ActorUserId)
            .Where(actorId => actorId.HasValue)
            .Select(actorId => actorId!.Value)
            .Distinct()
            .ToArray();

        return _userRepository.GetIdentitiesAsync(actorIds, cancellationToken);
    }

    private static PermissionNotificationActorDto? CreateActorDto(
        PermissionNotification notification,
        IReadOnlyDictionary<Guid, PermissionUserIdentity> actors)
    {
        if (!notification.ActorUserId.HasValue ||
            !actors.TryGetValue(notification.ActorUserId.Value, out var actor))
        {
            return null;
        }

        return new PermissionNotificationActorDto(
            actor.Id.ToString(),
            actor.DisplayName,
            actor.Email);
    }

    private static PermissionNotificationResourceDto? CreateResourceDto(
        PermissionNotification notification,
        IReadOnlyDictionary<NotificationResourceKey, PermissionResourceDisplaySummary> displaySummaries)
    {
        if (notification.ResourceType is null || notification.ResourceId is null)
        {
            return null;
        }

        if (!displaySummaries.TryGetValue(
            new NotificationResourceKey(notification.WorkspaceId, notification.ResourceType, notification.ResourceId.Value),
            out var summary))
        {
            return null;
        }

        return new PermissionNotificationResourceDto(
            summary.ResourceType,
            summary.ResourceId.ToString(),
            summary.Title,
            summary.Path);
    }

    private static PermissionNotificationActionDto CreateActionDto(PermissionNotification notification)
    {
        var kind = GetActionKind(notification.Type);
        var subject = GetSubject(notification);
        return new PermissionNotificationActionDto(
            kind,
            GetActionLabel(kind),
            notification.ResourceType,
            notification.ResourceId?.ToString(),
            notification.AccessRequestId?.ToString(),
            notification.PermissionGrantId?.ToString(),
            subject?.SubjectType,
            subject?.SubjectId.ToString());
    }

    private static PermissionNotificationSubject? GetSubject(PermissionNotification notification)
    {
        if (notification.AccessRequestId.HasValue)
        {
            return new PermissionNotificationSubject("access_request", notification.AccessRequestId.Value);
        }

        if (notification.PermissionGrantId.HasValue)
        {
            return new PermissionNotificationSubject("permission_grant", notification.PermissionGrantId.Value);
        }

        var parts = notification.DedupeKey?.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts is not { Length: >= 2 } || !Guid.TryParse(parts[1], out var subjectId))
        {
            return null;
        }

        return parts[0] switch
        {
            "share-link" => new PermissionNotificationSubject("share_link", subjectId),
            "email-invite" => new PermissionNotificationSubject("email_invite", subjectId),
            _ => null
        };
    }

    private static string GetCategory(string type)
    {
        if (type is PermissionNotificationTypes.GrantExpiring
            or PermissionNotificationTypes.GrantExpired
            or PermissionNotificationTypes.GroupMemberExpiring
            or PermissionNotificationTypes.GroupMemberExpired)
        {
            return NotificationCategories.Expiry;
        }

        if (type.StartsWith("access_request.", StringComparison.Ordinal))
        {
            return NotificationCategories.Access;
        }

        if (type.StartsWith("permission.", StringComparison.Ordinal) ||
            type.StartsWith("group.", StringComparison.Ordinal))
        {
            return NotificationCategories.Grant;
        }

        if (type.StartsWith("share_link.", StringComparison.Ordinal) ||
            type.StartsWith("email_invite.", StringComparison.Ordinal))
        {
            return NotificationCategories.Sharing;
        }

        return NotificationCategories.Permission;
    }

    private static string GetState(string type)
    {
        return type switch
        {
            PermissionNotificationTypes.AccessRequestCreated => "pending_review",
            PermissionNotificationTypes.EmailInviteDeliveryFailed => "failed",
            PermissionNotificationTypes.GrantExpiring or PermissionNotificationTypes.GroupMemberExpiring => "expiring",
            PermissionNotificationTypes.GrantExpired or PermissionNotificationTypes.GroupMemberExpired => "expired",
            PermissionNotificationTypes.AccessRequestApproved or PermissionNotificationTypes.AccessRequestDenied => "resolved",
            PermissionNotificationTypes.ShareLinkRevoked or PermissionNotificationTypes.EmailInviteRevoked => "revoked",
            _ => "informational",
        };
    }

    private static string GetActionKind(string type)
    {
        return type switch
        {
            PermissionNotificationTypes.AccessRequestCreated => "review_access_request",
            PermissionNotificationTypes.EmailInviteDeliveryFailed => "review_failed_invite",
            PermissionNotificationTypes.GrantExpiring or PermissionNotificationTypes.GrantExpired => "review_permission_expiry",
            PermissionNotificationTypes.GroupMemberExpiring or PermissionNotificationTypes.GroupMemberExpired => "review_group_expiry",
            PermissionNotificationTypes.ShareLinkCreated or PermissionNotificationTypes.ShareLinkRevoked => "manage_share_link",
            PermissionNotificationTypes.EmailInviteCreated
                or PermissionNotificationTypes.EmailInviteAccepted
                or PermissionNotificationTypes.EmailInviteRevoked => "manage_email_invite",
            PermissionNotificationTypes.GrantCreated
                or PermissionNotificationTypes.GrantUpdated
                or PermissionNotificationTypes.GrantRevoked
                or PermissionNotificationTypes.GroupMemberAdded
                or PermissionNotificationTypes.GroupMemberRemoved => "open_permissions",
            _ => "open_access_sharing",
        };
    }

    private static string GetActionLabel(string kind)
    {
        return kind.StartsWith("review_", StringComparison.Ordinal) ? "Review" : kind.StartsWith("manage_", StringComparison.Ordinal) ? "Manage" : "Open";
    }

    private static class NotificationCategories
    {
        public const string Access = "access";
        public const string Expiry = "expiry";
        public const string Grant = "grant";
        public const string Permission = "permission";
        public const string Sharing = "sharing";
    }

    private sealed record NotificationResourceKey(Guid WorkspaceId, string ResourceType, Guid ResourceId);

    private sealed record PermissionNotificationSubject(string SubjectType, Guid SubjectId);
}
