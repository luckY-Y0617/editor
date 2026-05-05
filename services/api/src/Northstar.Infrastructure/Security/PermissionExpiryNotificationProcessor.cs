using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Northstar.Domain.Security;
using Northstar.Infrastructure.Persistence;
using Npgsql;

namespace Northstar.Infrastructure.Security;

public sealed class PermissionExpiryNotificationProcessor
{
    private const string UniqueViolation = "23505";

    private readonly NorthstarDbContext _dbContext;
    private readonly PermissionExpiryNotificationOptions _options;
    private readonly ILogger<PermissionExpiryNotificationProcessor> _logger;

    public PermissionExpiryNotificationProcessor(
        NorthstarDbContext dbContext,
        IOptions<PermissionExpiryNotificationOptions> options,
        ILogger<PermissionExpiryNotificationProcessor> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var expiringUntil = now.AddHours(Math.Max(1, _options.ExpiringWindowHours));
        var candidates = new List<ExpiryNotificationCandidate>();
        candidates.AddRange(await GetGrantCandidatesAsync(now, expiringUntil, cancellationToken));
        candidates.AddRange(await GetGroupMemberCandidatesAsync(now, expiringUntil, cancellationToken));
        if (candidates.Count == 0)
        {
            return;
        }

        var dedupeKeys = candidates.Select(candidate => candidate.DedupeKey).Distinct().ToArray();
        var existingDedupeKeys = await _dbContext.PermissionNotifications
            .AsNoTracking()
            .Where(notification => notification.DedupeKey != null && dedupeKeys.Contains(notification.DedupeKey))
            .Select(notification => notification.DedupeKey!)
            .ToListAsync(cancellationToken);
        var existingDedupeKeySet = existingDedupeKeys.ToHashSet(StringComparer.Ordinal);
        var notifications = candidates
            .Where(candidate => !existingDedupeKeySet.Contains(candidate.DedupeKey))
            .Select(ToNotification)
            .ToArray();
        if (notifications.Length == 0)
        {
            return;
        }

        await _dbContext.PermissionNotifications.AddRangeAsync(notifications, cancellationToken);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            _logger.LogDebug(exception, "Concurrent permission expiry notification insert was ignored.");
        }
    }

    private async Task<IReadOnlyList<ExpiryNotificationCandidate>> GetGrantCandidatesAsync(
        DateTimeOffset now,
        DateTimeOffset expiringUntil,
        CancellationToken cancellationToken)
    {
        var grants = await _dbContext.ResourceAccessGrants
            .AsNoTracking()
            .Where(grant =>
                grant.SubjectType == SubjectTypes.User &&
                grant.RevokedAt == null &&
                grant.ExpiresAt != null &&
                grant.ExpiresAt <= expiringUntil)
            .Select(grant => new
            {
                grant.Id,
                grant.WorkspaceId,
                grant.ResourceType,
                grant.ResourceId,
                grant.SubjectId,
                grant.RoleKey,
                grant.ExpiresAt
            })
            .ToListAsync(cancellationToken);

        return grants
            .Select(grant =>
            {
                var type = grant.ExpiresAt <= now
                    ? PermissionNotificationTypes.GrantExpired
                    : PermissionNotificationTypes.GrantExpiring;
                return new ExpiryNotificationCandidate(
                    grant.WorkspaceId,
                    grant.SubjectId,
                    type,
                    type == PermissionNotificationTypes.GrantExpired ? "Permission expired" : "Permission expiring",
                    type == PermissionNotificationTypes.GrantExpired
                        ? $"Your {grant.RoleKey} access expired."
                        : $"Your {grant.RoleKey} access expires soon.",
                    grant.ResourceType,
                    grant.ResourceId,
                    grant.Id,
                    $"permission-expiry:{type}:{grant.Id}:{grant.SubjectId}");
            })
            .ToArray();
    }

    private async Task<IReadOnlyList<ExpiryNotificationCandidate>> GetGroupMemberCandidatesAsync(
        DateTimeOffset now,
        DateTimeOffset expiringUntil,
        CancellationToken cancellationToken)
    {
        var members = await _dbContext.WorkspaceGroupMembers
            .AsNoTracking()
            .Where(member =>
                member.RemovedAt == null &&
                member.ExpiresAt != null &&
                member.ExpiresAt <= expiringUntil)
            .Join(
                _dbContext.WorkspaceGroups.AsNoTracking(),
                member => member.GroupId,
                group => group.Id,
                (member, group) => new
                {
                    member.Id,
                    member.UserId,
                    member.ExpiresAt,
                    group.WorkspaceId,
                    GroupName = group.Name,
                    group.ArchivedAt
                })
            .Where(member => member.ArchivedAt == null)
            .ToListAsync(cancellationToken);

        return members
            .Select(member =>
            {
                var type = member.ExpiresAt <= now
                    ? PermissionNotificationTypes.GroupMemberExpired
                    : PermissionNotificationTypes.GroupMemberExpiring;
                return new ExpiryNotificationCandidate(
                    member.WorkspaceId,
                    member.UserId,
                    type,
                    type == PermissionNotificationTypes.GroupMemberExpired ? "Group membership expired" : "Group membership expiring",
                    type == PermissionNotificationTypes.GroupMemberExpired
                        ? $"Your membership in {member.GroupName} expired."
                        : $"Your membership in {member.GroupName} expires soon.",
                    ResourceTypes.Workspace,
                    member.WorkspaceId,
                    PermissionGrantId: null,
                    $"permission-expiry:{type}:{member.Id}:{member.UserId}");
            })
            .ToArray();
    }

    private static PermissionNotification ToNotification(ExpiryNotificationCandidate candidate)
    {
        return new PermissionNotification(
            candidate.WorkspaceId,
            candidate.RecipientUserId,
            candidate.Type,
            candidate.Title,
            candidate.Body,
            actorUserId: null,
            resourceType: candidate.ResourceType,
            resourceId: candidate.ResourceId,
            permissionGrantId: candidate.PermissionGrantId,
            actionUrl: "#permissions",
            dedupeKey: candidate.DedupeKey);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: UniqueViolation };
    }

    private sealed record ExpiryNotificationCandidate(
        Guid WorkspaceId,
        Guid RecipientUserId,
        string Type,
        string Title,
        string Body,
        string ResourceType,
        Guid ResourceId,
        Guid? PermissionGrantId,
        string DedupeKey);
}
