using Northstar.Contracts.Security;

namespace Northstar.Application.Security;

public interface IShareLinkAccessAuditService
{
    Task RecordResolveAsync(
        string token,
        string result,
        string? failureCategory,
        CancellationToken cancellationToken = default);

    Task RecordPublicAccessAsync(
        string token,
        string eventType,
        string result,
        string? failureCategory,
        CancellationToken cancellationToken = default);

    Task RecordProtectedResourceAccessAsync(
        string? shareToken,
        Guid requestedResourceId,
        EffectivePermissionResult result,
        CancellationToken cancellationToken = default);

    Task<ShareLinkAccessStatsResponse> GetStatsAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default);

    Task<ShareLinkAccessEventsResponse> GetEventsAsync(
        Guid shareLinkId,
        string? result,
        string? eventType,
        int? offset,
        int? limit,
        CancellationToken cancellationToken = default);
}
