using System.Text.Json;

namespace Northstar.Contracts.Security;

public sealed record PermissionPolicyDto(
    string InheritanceMode,
    string LinkMode,
    string? DefaultLinkRole);

public sealed record PermissionGrantDto(
    string Id,
    string SubjectType,
    string SubjectId,
    string RoleKey,
    string? GrantedBy,
    DateTimeOffset GrantedAt,
    DateTimeOffset? ExpiresAt,
    string? Reason);

public sealed record ResourcePermissionsResponse(
    string ResourceType,
    string ResourceId,
    PermissionPolicyDto Policy,
    IReadOnlyList<PermissionGrantDto> Grants,
    EffectivePermissionResponse EffectiveAccess,
    string InheritedFrom,
    IReadOnlyList<string> AvailableRoles);

public sealed record UpdateResourcePolicyRequest(
    string InheritanceMode,
    string? LinkMode,
    string? DefaultLinkRole);

public sealed record CreatePermissionGrantRequest(
    string SubjectType,
    string SubjectId,
    string RoleKey,
    DateTimeOffset? ExpiresAt,
    string? Reason);

public sealed class UpdatePermissionGrantRequest
{
    public UpdatePermissionGrantRequest()
    {
    }

    public UpdatePermissionGrantRequest(string? roleKey, DateTimeOffset? expiresAt, string? reason)
    {
        RoleKey = roleKey;
        ExpiresAt = ToJsonElement(expiresAt);
        Reason = reason;
    }

    public string? RoleKey { get; init; }
    public JsonElement ExpiresAt { get; init; }
    public string? Reason { get; init; }

    private static JsonElement ToJsonElement(DateTimeOffset? value)
    {
        return value.HasValue
            ? JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(value.Value))
            : JsonSerializer.Deserialize<JsonElement>("null");
    }
}

public sealed record RevokePermissionGrantRequest(string? Reason);

public sealed record PermissionAuditEventDto(
    string Id,
    string WorkspaceId,
    string? ActorId,
    string Action,
    string ResourceType,
    string ResourceId,
    string? SubjectType,
    string? SubjectId,
    string? BeforeJson,
    string? AfterJson,
    string Metadata,
    DateTimeOffset CreatedAt);

public sealed record PermissionAuditResponse(IReadOnlyList<PermissionAuditEventDto> Events);

public sealed record WorkspaceAuditEventDto(
    string Id,
    string WorkspaceId,
    string? ActorId,
    string? ActorName,
    string? ActorEmail,
    string Action,
    string ResourceType,
    string ResourceId,
    string? SubjectType,
    string? SubjectId,
    string? BeforeJson,
    string? AfterJson,
    string Metadata,
    DateTimeOffset CreatedAt);

public sealed record WorkspaceAuditLogResponse(
    IReadOnlyList<WorkspaceAuditEventDto> Events,
    int Offset,
    int Limit,
    int TotalCount,
    bool HasMore);

public sealed record ShareLinkDto(
    string Id,
    string WorkspaceId,
    string ResourceType,
    string ResourceId,
    string RoleKey,
    string Audience,
    string? SubjectEmail,
    string? CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt,
    bool HasPassword);

public sealed record ShareLinksResponse(IReadOnlyList<ShareLinkDto> Links);

public sealed record CreateShareLinkRequest(
    string RoleKey,
    string? Audience,
    DateTimeOffset? ExpiresAt,
    string? SubjectEmail = null,
    string? Password = null);

public sealed record CreateShareLinkResponse(
    ShareLinkDto Link,
    string Token,
    string Url);

public sealed record ResolveShareLinkResponse(
    string WorkspaceId,
    string ResourceType,
    string ResourceId,
    string RoleKey,
    string Audience,
    string? SubjectEmail,
    DateTimeOffset? ExpiresAt);

public sealed record ResolvePublicShareLinkResponse(
    string WorkspaceId,
    string ResourceType,
    string ResourceId,
    string RoleKey,
    string Audience,
    DateTimeOffset ExpiresAt,
    bool HasPassword);

public sealed record PublicShareDocumentDto(
    string Id,
    string Title,
    string Status,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> Tags,
    JsonElement Content,
    long Revision);

public sealed record PublicShareDocumentResponse(
    ResolvePublicShareLinkResponse Link,
    PublicShareDocumentDto Document);

public sealed record PublicShareCollectionDocumentDto(
    string Id,
    string Title,
    string Status,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> Tags,
    decimal SortOrder);

public sealed record PublicShareCollectionDto(
    string Id,
    string Title,
    DateTimeOffset UpdatedAt,
    decimal SortOrder,
    IReadOnlyList<PublicShareCollectionDocumentDto> Documents);

public sealed record PublicShareCollectionResponse(
    ResolvePublicShareLinkResponse Link,
    PublicShareCollectionDto Collection);

public sealed record EmailInviteDto(
    string Id,
    string WorkspaceId,
    string ResourceType,
    string ResourceId,
    string Email,
    string RoleKey,
    string Status,
    string? InvitedBy,
    string? AcceptedBy,
    string? RevokedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? RevokedAt,
    DateTimeOffset? ExpiredAt,
    string DeliveryStatus,
    string DeliveryProvider,
    DateTimeOffset? DeliveryAttemptedAt,
    string? DeliveryErrorCode);

public sealed record EmailInviteDeliveryDto(
    string Status,
    string Provider,
    DateTimeOffset? AttemptedAt,
    string? ErrorCode);

public sealed record EmailInvitesResponse(IReadOnlyList<EmailInviteDto> Invites);

public sealed record CreateEmailInviteRequest(
    string Email,
    string RoleKey,
    DateTimeOffset ExpiresAt);

public sealed record CreateEmailInviteResponse(
    EmailInviteDto Invite,
    string Token,
    string Url,
    EmailInviteDeliveryDto Delivery);

public sealed record ResolveEmailInviteResponse(
    string WorkspaceId,
    string ResourceType,
    string ResourceId,
    string Email,
    string RoleKey,
    string Status,
    DateTimeOffset ExpiresAt);

public sealed record AcceptEmailInviteResponse(EmailInviteDto Invite);
