using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfPermissionUserRepository : IPermissionUserRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfPermissionUserRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PermissionUserIdentity?> GetIdentityAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId && user.DeletedAt == null)
            .Select(user => new PermissionUserIdentity(
                user.Id,
                user.Email == null ? null : user.Email.ToLower(),
                user.ExternalProvider,
                user.ExternalSubjectId,
                user.DisplayName))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, PermissionUserIdentity>> GetIdentitiesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var distinctUserIds = userIds.Distinct().ToArray();
        if (distinctUserIds.Length == 0)
        {
            return new Dictionary<Guid, PermissionUserIdentity>();
        }

        return await _dbContext.Users
            .AsNoTracking()
            .Where(user => distinctUserIds.Contains(user.Id) && user.DeletedAt == null)
            .Select(user => new PermissionUserIdentity(
                user.Id,
                user.Email == null ? null : user.Email.ToLower(),
                user.ExternalProvider,
                user.ExternalSubjectId,
                user.DisplayName))
            .ToDictionaryAsync(user => user.Id, cancellationToken);
    }
}
