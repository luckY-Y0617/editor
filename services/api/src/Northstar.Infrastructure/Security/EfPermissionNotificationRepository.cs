using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Domain.Security;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfPermissionNotificationRepository : IPermissionNotificationRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfPermissionNotificationRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PermissionNotification>> GetForRecipientAsync(
        Guid recipientUserId,
        Guid? workspaceId,
        bool unreadOnly,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PermissionNotifications
            .AsNoTracking()
            .Where(notification => notification.RecipientUserId == recipientUserId);

        if (workspaceId.HasValue)
        {
            query = query.Where(notification => notification.WorkspaceId == workspaceId.Value);
        }

        if (unreadOnly)
        {
            query = query.Where(notification => notification.ReadAt == null);
        }

        return await query
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public Task<PermissionNotification?> GetForUpdateAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.PermissionNotifications
            .FirstOrDefaultAsync(notification => notification.Id == notificationId, cancellationToken);
    }

    public Task AddAsync(PermissionNotification notification, CancellationToken cancellationToken = default)
    {
        return _dbContext.PermissionNotifications.AddAsync(notification, cancellationToken).AsTask();
    }

    public async Task AddRangeAsync(IEnumerable<PermissionNotification> notifications, CancellationToken cancellationToken = default)
    {
        var notificationList = notifications.ToList();
        if (notificationList.Count == 0)
        {
            return;
        }

        var dedupeKeys = notificationList
            .Select(notification => notification.DedupeKey)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        List<string> existingDedupeKeys = dedupeKeys.Count == 0
            ? []
            : await _dbContext.PermissionNotifications
                .Where(notification => notification.DedupeKey != null && dedupeKeys.Contains(notification.DedupeKey))
                .Select(notification => notification.DedupeKey!)
                .ToListAsync(cancellationToken);
        var seenDedupeKeys = new HashSet<string>(existingDedupeKeys, StringComparer.Ordinal);
        var filteredNotifications = notificationList
            .Where(notification =>
                string.IsNullOrWhiteSpace(notification.DedupeKey) ||
                seenDedupeKeys.Add(notification.DedupeKey))
            .ToList();

        _dbContext.PermissionNotifications.AddRange(filteredNotifications);
    }
}
