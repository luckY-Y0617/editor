using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IShareLinkAccessRepository
{
    Task AddEventAndUpdateStatsAsync(
        ShareLinkAccessEvent accessEvent,
        CancellationToken cancellationToken = default);

    Task<ShareLinkAccessStats?> GetStatsAsync(
        Guid workspaceId,
        Guid shareLinkId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, ShareLinkAccessSummaryRow>> GetSummaryRowsAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> shareLinkIds,
        DateTimeOffset recentFrom,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShareLinkAccessTrendRow>> GetTrendAsync(
        Guid workspaceId,
        Guid shareLinkId,
        DateTimeOffset from,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShareLinkAccessSourceRow>> GetSourceBreakdownAsync(
        Guid workspaceId,
        Guid shareLinkId,
        CancellationToken cancellationToken = default);

    Task<ShareLinkAccessCategorySummary> GetCategorySummaryAsync(
        Guid workspaceId,
        Guid shareLinkId,
        CancellationToken cancellationToken = default);

    Task<ShareLinkAccessEventPage> GetEventsAsync(
        Guid workspaceId,
        Guid shareLinkId,
        string? result,
        string? eventType,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);
}

public sealed record ShareLinkAccessTrendRow(DateOnly Date, long SuccessCount, long FailCount);

public sealed record ShareLinkAccessSourceRow(string Source, long Count);

public sealed record ShareLinkAccessCategorySummary(
    long TreeViewCount,
    long DocumentViewCount,
    long ScopeDeniedCount,
    long PasswordFailedCount,
    string? LatestEventCategory);

public sealed record ShareLinkAccessSummaryRow(
    Guid ShareLinkId,
    DateTimeOffset? LastAccessedAt,
    long AccessCount,
    long UniqueVisitorCount,
    long RecentFailCount,
    long ExternalOrPublicAccessCount);

public sealed record ShareLinkAccessEventPage(
    IReadOnlyList<ShareLinkAccessEvent> Events,
    int TotalCount);
