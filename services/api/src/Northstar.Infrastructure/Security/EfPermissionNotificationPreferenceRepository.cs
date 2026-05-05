using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Domain.Security;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfPermissionNotificationPreferenceRepository : IPermissionNotificationPreferenceRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfPermissionNotificationPreferenceRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PermissionNotificationPreference>> GetForUserWorkspaceAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PermissionNotificationPreferences
            .AsNoTracking()
            .Where(preference =>
                preference.UserId == userId &&
                preference.WorkspaceId == workspaceId)
            .OrderBy(preference => preference.ResourceType == null ? 0 : 1)
            .ThenBy(preference => preference.ResourceType)
            .ThenBy(preference => preference.ResourceId)
            .ToListAsync(cancellationToken);
    }

    public Task<PermissionNotificationPreference?> GetForUpdateAsync(
        Guid userId,
        Guid workspaceId,
        string? resourceType,
        Guid? resourceId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.PermissionNotificationPreferences
            .FirstOrDefaultAsync(preference =>
                preference.UserId == userId &&
                preference.WorkspaceId == workspaceId &&
                (
                    resourceType == null
                        ? preference.ResourceType == null && preference.ResourceId == null
                        : preference.ResourceType == resourceType && preference.ResourceId == resourceId
                ),
                cancellationToken);
    }

    public Task AddAsync(
        PermissionNotificationPreference preference,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.PermissionNotificationPreferences.AddAsync(preference, cancellationToken).AsTask();
    }
}
