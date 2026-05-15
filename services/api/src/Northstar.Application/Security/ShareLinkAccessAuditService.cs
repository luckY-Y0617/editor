using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Contracts.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class ShareLinkAccessAuditService : IShareLinkAccessAuditService
{
    private const int RecentWindowDays = 7;

    private readonly IShareLinkRepository _shareLinkRepository;
    private readonly IShareLinkTokenService _tokenService;
    private readonly IShareLinkAccessRepository _accessRepository;
    private readonly IEffectivePermissionService _effectivePermissionService;
    private readonly IPermissionUserRepository _userRepository;
    private readonly IAuthRequestContext _requestContext;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ShareLinkAccessAuditService(
        IShareLinkRepository shareLinkRepository,
        IShareLinkTokenService tokenService,
        IShareLinkAccessRepository accessRepository,
        IEffectivePermissionService effectivePermissionService,
        IPermissionUserRepository userRepository,
        IAuthRequestContext requestContext,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _shareLinkRepository = shareLinkRepository;
        _tokenService = tokenService;
        _accessRepository = accessRepository;
        _effectivePermissionService = effectivePermissionService;
        _userRepository = userRepository;
        _requestContext = requestContext;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task RecordResolveAsync(
        string token,
        string result,
        string? failureCategory,
        CancellationToken cancellationToken = default)
    {
        await RecordKnownTokenAsync(token, ShareLinkAccessEventTypes.Resolve, result, failureCategory, cancellationToken);
    }

    public async Task RecordPublicAccessAsync(
        string token,
        string eventType,
        string result,
        string? failureCategory,
        CancellationToken cancellationToken = default)
    {
        await RecordKnownTokenAsync(token, eventType, result, failureCategory, cancellationToken);
    }

    public async Task RecordProtectedResourceAccessAsync(
        string? shareToken,
        Guid requestedResourceId,
        EffectivePermissionResult result,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shareToken))
        {
            return;
        }

        var link = await GetKnownLinkAsync(shareToken, cancellationToken);
        if (link is null)
        {
            return;
        }

        if (result.Allowed && !IsShareLinkSource(result.Source))
        {
            return;
        }

        var category = result.Allowed
            ? null
            : FailureCategoryForProtectedAccess(link, requestedResourceId, result);
        await RecordAsync(
            link,
            ShareLinkAccessEventTypes.Access,
            result.Allowed ? ShareLinkAccessResults.Success : ShareLinkAccessResults.Fail,
            category,
            cancellationToken);
    }

    public async Task<ShareLinkAccessStatsResponse> GetStatsAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default)
    {
        var link = await GetManagedLinkAsync(shareLinkId, cancellationToken);
        var stats = await _accessRepository.GetStatsAsync(link.WorkspaceId, link.Id, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var from = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero)
            .AddDays(-(RecentWindowDays - 1));
        var trend = await _accessRepository.GetTrendAsync(link.WorkspaceId, link.Id, from, cancellationToken);
        var sources = await _accessRepository.GetSourceBreakdownAsync(link.WorkspaceId, link.Id, cancellationToken);
        var total = sources.Sum(source => source.Count);

        return new ShareLinkAccessStatsResponse(
            link.Id.ToString(),
            stats?.LastAccessedAt,
            stats?.AccessCount ?? 0,
            stats?.UniqueVisitorCount ?? 0,
            stats?.LastAccessIp,
            RecentWindowDays,
            trend
                .OrderBy(row => row.Date)
                .Select(row => new ShareLinkAccessTrendPointDto(row.Date, row.SuccessCount, row.FailCount))
                .ToArray(),
            sources
                .OrderByDescending(row => row.Count)
                .Select(row => new ShareLinkSourceBreakdownDto(
                    row.Source,
                    row.Count,
                    total == 0 ? 0 : Math.Round(row.Count * 100m / total, 2)))
                .ToArray());
    }

    public async Task<ShareLinkAccessEventsResponse> GetEventsAsync(
        Guid shareLinkId,
        string? result,
        string? eventType,
        int? offset,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        var link = await GetManagedLinkAsync(shareLinkId, cancellationToken);
        var normalizedResult = NormalizeResultFilter(result);
        var normalizedEventType = NormalizeEventTypeFilter(eventType);
        var normalizedOffset = Math.Max(0, offset ?? 0);
        var normalizedLimit = Math.Clamp(limit ?? 50, 1, 100);
        var page = await _accessRepository.GetEventsAsync(
            link.WorkspaceId,
            link.Id,
            normalizedResult,
            normalizedEventType,
            normalizedOffset,
            normalizedLimit,
            cancellationToken);
        var actorIds = page.Events
            .Select(accessEvent => accessEvent.ActorUserId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        var actors = actorIds.Length == 0
            ? new Dictionary<Guid, PermissionUserIdentity>()
            : await _userRepository.GetIdentitiesAsync(actorIds, cancellationToken);

        return new ShareLinkAccessEventsResponse(
            page.Events.Select(accessEvent =>
            {
                var actor = accessEvent.ActorUserId.HasValue &&
                    actors.TryGetValue(accessEvent.ActorUserId.Value, out var identity)
                        ? identity
                        : null;
                var actorType = GetActorType(accessEvent.Audience, accessEvent.ActorUserId);
                return new ShareLinkAccessEventDto(
                    accessEvent.Id.ToString(),
                    accessEvent.ShareLinkId.ToString(),
                    actor?.DisplayName ?? actor?.Email ?? accessEvent.ActorUserId?.ToString(),
                    accessEvent.ActorUserId?.ToString(),
                    actor?.DisplayName,
                    actorType,
                    accessEvent.OccurredAt,
                    accessEvent.OccurredAt,
                    accessEvent.RemoteIp,
                    accessEvent.UserAgent,
                    accessEvent.UserAgent,
                    accessEvent.EventType,
                    accessEvent.Result,
                    accessEvent.FailureCategory);
            }).ToArray(),
            normalizedOffset,
            normalizedLimit,
            page.TotalCount,
            normalizedOffset + normalizedLimit < page.TotalCount);
    }

    private async Task<ShareLink> GetManagedLinkAsync(Guid shareLinkId, CancellationToken cancellationToken)
    {
        var link = await _shareLinkRepository.GetByIdAsync(shareLinkId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Authentication is required.");
        }

        var userId = _currentUser.UserId.Value;
        var action = link.ResourceType == ResourceTypes.Document
            ? PermissionActions.DocumentManagePermissions
            : PermissionActions.CollectionManagePermissions;
        var result = link.ResourceType == ResourceTypes.Document
            ? await _effectivePermissionService.AuthorizeDocumentAsync(link.ResourceId, userId, action, cancellationToken)
            : await _effectivePermissionService.AuthorizeCollectionAsync(link.ResourceId, userId, action, cancellationToken);
        if (!result.Allowed)
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace permission is insufficient.");
        }

        return link;
    }

    private async Task RecordKnownTokenAsync(
        string token,
        string eventType,
        string result,
        string? failureCategory,
        CancellationToken cancellationToken)
    {
        var link = await GetKnownLinkAsync(token, cancellationToken);
        if (link is null)
        {
            return;
        }

        await RecordAsync(link, eventType, result, failureCategory, cancellationToken);
    }

    private async Task<ShareLink?> GetKnownLinkAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return await _shareLinkRepository.GetByTokenHashAsync(
            _tokenService.HashToken(token.Trim()),
            cancellationToken);
    }

    private async Task RecordAsync(
        ShareLink link,
        string eventType,
        string result,
        string? failureCategory,
        CancellationToken cancellationToken)
    {
        var accessEvent = new ShareLinkAccessEvent(
            link.WorkspaceId,
            link.Id,
            link.ResourceType,
            link.ResourceId,
            _currentUser.UserId,
            link.Audience,
            eventType,
            result,
            failureCategory,
            _requestContext.IpAddress,
            _requestContext.UserAgent);
        await _accessRepository.AddEventAndUpdateStatsAsync(accessEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string FailureCategoryForProtectedAccess(
        ShareLink link,
        Guid requestedResourceId,
        EffectivePermissionResult result)
    {
        var now = DateTimeOffset.UtcNow;
        if (link.RevokedAt.HasValue)
        {
            return "revoked";
        }

        if (link.PausedAt.HasValue)
        {
            return "paused";
        }

        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value <= now)
        {
            return "expired";
        }

        if (link.Audience == ShareLinkAudiences.Public)
        {
            return "audience_mismatch";
        }

        if (link.ResourceType == ResourceTypes.Document && link.ResourceId != requestedResourceId)
        {
            return "resource_mismatch";
        }

        return result.Reason == EffectivePermissionService.NoMembershipReason
            ? "audience_mismatch"
            : "forbidden";
    }

    private static bool IsShareLinkSource(string source)
    {
        return source is EffectivePermissionService.DocumentShareLinkSource or EffectivePermissionService.CollectionShareLinkSource;
    }

    private static string? NormalizeResultFilter(string? result)
    {
        if (string.IsNullOrWhiteSpace(result))
        {
            return null;
        }

        var normalized = result.Trim().ToLowerInvariant();
        return ShareLinkAccessResults.IsSupported(normalized)
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "result is invalid.");
    }

    private static string? NormalizeEventTypeFilter(string? eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return null;
        }

        var normalized = eventType.Trim().ToLowerInvariant();
        return ShareLinkAccessEventTypes.IsSupported(normalized)
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "eventType is invalid.");
    }

    private static string GetActorType(string audience, Guid? actorUserId)
    {
        return audience switch
        {
            ShareLinkAudiences.Workspace when actorUserId.HasValue => "workspace_member",
            ShareLinkAudiences.External when actorUserId.HasValue => "external_authenticated",
            ShareLinkAudiences.Public when actorUserId is null => "public_visitor",
            _ => "unknown"
        };
    }
}
