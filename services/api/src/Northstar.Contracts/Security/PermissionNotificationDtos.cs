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
    DateTimeOffset CreatedAt,
    PermissionNotificationActorDto? Actor = null,
    PermissionNotificationResourceDto? Resource = null,
    PermissionNotificationActionDto? Action = null,
    string? Category = null,
    string? State = null);

public sealed record PermissionNotificationActorDto(
    string Id,
    string DisplayName,
    string? Email);

public sealed record PermissionNotificationResourceDto(
    string ResourceType,
    string ResourceId,
    string Title,
    string? Path);

public sealed record PermissionNotificationActionDto(
    string Kind,
    string Label,
    string? ResourceType,
    string? ResourceId,
    string? AccessRequestId,
    string? PermissionGrantId,
    string? SubjectType = null,
    string? SubjectId = null);

public sealed record PermissionNotificationsResponse(
    IReadOnlyList<PermissionNotificationDto> Notifications,
    int UnreadCount);

public sealed record AccessSharingSummaryResponse(
    int TotalCount,
    int UnreadCount,
    int PendingReviewCount,
    int AccessRequestCount,
    int GrantCount,
    int SharingCount,
    int ExpiryCount,
    int FailedInviteCount);

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
    DateTimeOffset UpdatedAt,
    PermissionNotificationPreferenceResourceDto? Resource = null);

public sealed record PermissionNotificationPreferenceResourceDto(
    string ResourceType,
    string ResourceId,
    string Title,
    string? Path);

public sealed record PermissionNotificationPreferencesResponse(
    IReadOnlyList<PermissionNotificationPreferenceDto> Preferences);

public sealed record UpdatePermissionNotificationPreferenceRequest(
    string WorkspaceId,
    string? ResourceType,
    string? ResourceId,
    bool Watched,
    bool Muted);
