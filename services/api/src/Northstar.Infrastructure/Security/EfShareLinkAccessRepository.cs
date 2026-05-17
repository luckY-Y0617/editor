using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Domain.Security;
using Northstar.Infrastructure.Persistence;
using System.Text.Json;

namespace Northstar.Infrastructure.Security;

public sealed class EfShareLinkAccessRepository : IShareLinkAccessRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfShareLinkAccessRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddEventAndUpdateStatsAsync(
        ShareLinkAccessEvent accessEvent,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ShareLinkAccessEvents.AddAsync(accessEvent, cancellationToken);
        if (accessEvent.Result != ShareLinkAccessResults.Success)
        {
            return;
        }

        var stats = await _dbContext.ShareLinkAccessStats
            .FirstOrDefaultAsync(item => item.ShareLinkId == accessEvent.ShareLinkId, cancellationToken);
        if (stats is null)
        {
            stats = new ShareLinkAccessStats(accessEvent.WorkspaceId, accessEvent.ShareLinkId);
            await _dbContext.ShareLinkAccessStats.AddAsync(stats, cancellationToken);
        }

        var isNewAuthenticatedVisitor = false;
        if (accessEvent.ActorUserId.HasValue)
        {
            isNewAuthenticatedVisitor = !await _dbContext.ShareLinkAccessEvents.AnyAsync(
                item =>
                    item.WorkspaceId == accessEvent.WorkspaceId &&
                    item.ShareLinkId == accessEvent.ShareLinkId &&
                    item.ActorUserId == accessEvent.ActorUserId &&
                    item.Result == ShareLinkAccessResults.Success,
                cancellationToken);
        }

        stats.RecordSuccessfulAccess(accessEvent.OccurredAt, accessEvent.RemoteIp, isNewAuthenticatedVisitor);
    }

    public Task<ShareLinkAccessStats?> GetStatsAsync(
        Guid workspaceId,
        Guid shareLinkId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ShareLinkAccessStats
            .AsNoTracking()
            .FirstOrDefaultAsync(
                stats => stats.WorkspaceId == workspaceId && stats.ShareLinkId == shareLinkId,
                cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, ShareLinkAccessSummaryRow>> GetSummaryRowsAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> shareLinkIds,
        DateTimeOffset recentFrom,
        CancellationToken cancellationToken = default)
    {
        if (shareLinkIds.Count == 0)
        {
            return new Dictionary<Guid, ShareLinkAccessSummaryRow>();
        }

        var statsRows = await _dbContext.ShareLinkAccessStats
            .AsNoTracking()
            .Where(stats => stats.WorkspaceId == workspaceId && shareLinkIds.Contains(stats.ShareLinkId))
            .Select(stats => new
            {
                stats.ShareLinkId,
                stats.LastAccessedAt,
                stats.AccessCount,
                stats.UniqueVisitorCount
            })
            .ToListAsync(cancellationToken);

        var recentRows = await _dbContext.ShareLinkAccessEvents
            .AsNoTracking()
            .Where(accessEvent =>
                accessEvent.WorkspaceId == workspaceId &&
                shareLinkIds.Contains(accessEvent.ShareLinkId) &&
                accessEvent.OccurredAt >= recentFrom)
            .GroupBy(accessEvent => accessEvent.ShareLinkId)
            .Select(group => new
            {
                ShareLinkId = group.Key,
                RecentFailCount = group.LongCount(accessEvent => accessEvent.Result == ShareLinkAccessResults.Fail),
                ExternalOrPublicAccessCount = group.LongCount(accessEvent =>
                    accessEvent.Audience == ShareLinkAudiences.External ||
                    accessEvent.Audience == ShareLinkAudiences.Public)
            })
            .ToDictionaryAsync(row => row.ShareLinkId, cancellationToken);

        return statsRows.ToDictionary(
            stats => stats.ShareLinkId,
            stats =>
            {
                recentRows.TryGetValue(stats.ShareLinkId, out var recent);
                return new ShareLinkAccessSummaryRow(
                    stats.ShareLinkId,
                    stats.LastAccessedAt,
                    stats.AccessCount,
                    stats.UniqueVisitorCount,
                    recent?.RecentFailCount ?? 0,
                    recent?.ExternalOrPublicAccessCount ?? 0);
            });
    }

    public async Task<IReadOnlyList<ShareLinkAccessTrendRow>> GetTrendAsync(
        Guid workspaceId,
        Guid shareLinkId,
        DateTimeOffset from,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.ShareLinkAccessEvents
            .AsNoTracking()
            .Where(accessEvent =>
                accessEvent.WorkspaceId == workspaceId &&
                accessEvent.ShareLinkId == shareLinkId &&
                accessEvent.OccurredAt >= from)
            .GroupBy(accessEvent => accessEvent.OccurredAt.Date)
            .Select(group => new
            {
                Date = group.Key,
                SuccessCount = group.LongCount(accessEvent => accessEvent.Result == ShareLinkAccessResults.Success),
                FailCount = group.LongCount(accessEvent => accessEvent.Result == ShareLinkAccessResults.Fail)
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ShareLinkAccessTrendRow(DateOnly.FromDateTime(row.Date), row.SuccessCount, row.FailCount))
            .ToArray();
    }

    public async Task<IReadOnlyList<ShareLinkAccessSourceRow>> GetSourceBreakdownAsync(
        Guid workspaceId,
        Guid shareLinkId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.ShareLinkAccessEvents
            .AsNoTracking()
            .Where(accessEvent =>
                accessEvent.WorkspaceId == workspaceId &&
                accessEvent.ShareLinkId == shareLinkId)
            .GroupBy(accessEvent => new { accessEvent.Audience, HasActor = accessEvent.ActorUserId != null })
            .Select(group => new
            {
                group.Key.Audience,
                group.Key.HasActor,
                Count = group.LongCount()
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ShareLinkAccessSourceRow(GetSource(row.Audience, row.HasActor), row.Count))
            .GroupBy(row => row.Source)
            .Select(group => new ShareLinkAccessSourceRow(group.Key, group.Sum(row => row.Count)))
            .ToArray();
    }

    public async Task<ShareLinkAccessCategorySummary> GetCategorySummaryAsync(
        Guid workspaceId,
        Guid shareLinkId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.ShareLinkAccessEvents
            .AsNoTracking()
            .Where(accessEvent =>
                accessEvent.WorkspaceId == workspaceId &&
                accessEvent.ShareLinkId == shareLinkId)
            .OrderByDescending(accessEvent => accessEvent.OccurredAt)
            .Select(accessEvent => new
            {
                accessEvent.EventType,
                accessEvent.Result,
                accessEvent.FailureCategory,
                accessEvent.Metadata
            })
            .ToListAsync(cancellationToken);

        var categories = rows.Select(row => GetEventCategory(row.EventType, row.Result, row.FailureCategory, row.Metadata)).ToArray();
        return new ShareLinkAccessCategorySummary(
            categories.LongCount(category => category == "tree_view"),
            categories.LongCount(category => category == "document_view"),
            categories.LongCount(category => category == "scope_denied"),
            categories.LongCount(category => category == "password_failed"),
            categories.FirstOrDefault());
    }

    public async Task<ShareLinkAccessEventPage> GetEventsAsync(
        Guid workspaceId,
        Guid shareLinkId,
        string? result,
        string? eventType,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ShareLinkAccessEvents
            .AsNoTracking()
            .Where(accessEvent =>
                accessEvent.WorkspaceId == workspaceId &&
                accessEvent.ShareLinkId == shareLinkId);

        if (!string.IsNullOrWhiteSpace(result))
        {
            query = query.Where(accessEvent => accessEvent.Result == result);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(accessEvent => accessEvent.EventType == eventType);
        }

        var total = await query.CountAsync(cancellationToken);
        var events = await query
            .OrderByDescending(accessEvent => accessEvent.OccurredAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return new ShareLinkAccessEventPage(events, total);
    }

    private static string GetSource(string audience, bool hasActor)
    {
        return audience switch
        {
            ShareLinkAudiences.Workspace when hasActor => "workspace_member",
            ShareLinkAudiences.External when hasActor => "external_authenticated",
            ShareLinkAudiences.Public when !hasActor => "public_visitor",
            _ => "unknown"
        };
    }

    private static string GetEventCategory(string eventType, string result, string? failureCategory, string metadata)
    {
        if (failureCategory == "scope_denied")
        {
            return "scope_denied";
        }

        if (failureCategory is "password_failed" or "password_required_or_invalid")
        {
            return "password_failed";
        }

        var metadataCategory = ReadMetadataString(metadata, "category");
        if (!string.IsNullOrWhiteSpace(metadataCategory))
        {
            return metadataCategory!;
        }

        if (eventType == ShareLinkAccessEventTypes.Resolve)
        {
            return "resolve";
        }

        return result == ShareLinkAccessResults.Fail ? "denied" : eventType;
    }

    private static string? ReadMetadataString(string metadata, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadata);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.String
                    ? property.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
