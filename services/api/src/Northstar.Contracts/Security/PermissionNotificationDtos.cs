namespace Northstar.Contracts.Security;

public sealed record PermissionNotificationDto(
    string Id,
    string WorkspaceId,
    string RecipientUserId,
    string? ActorUserId,
    string Type,
    string? ResourceType,
    string? ResourceId,
    string? AccessRequestId,
    string? PermissionGrantId,
    string Title,
    string? Body,
    string? ActionUrl,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt);

public sealed record PermissionNotificationsResponse(
    IReadOnlyList<PermissionNotificationDto> Notifications,
    int UnreadCount);

public sealed record MarkNotificationReadRequest(bool Read = true);

public sealed record MarkAllNotificationsReadRequest(string? WorkspaceId);

public sealed record PermissionNotificationPreferenceDto(
    string Id,
    string WorkspaceId,
    string UserId,
    string? ResourceType,
    string? ResourceId,
    bool Watched,
    bool Muted,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PermissionNotificationPreferencesResponse(
    IReadOnlyList<PermissionNotificationPreferenceDto> Preferences);

public sealed record UpdatePermissionNotificationPreferenceRequest(
    string WorkspaceId,
    string? ResourceType,
    string? ResourceId,
    bool Watched,
    bool Muted);
