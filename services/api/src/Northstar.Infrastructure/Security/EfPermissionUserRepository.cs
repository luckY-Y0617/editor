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
                user.ExternalSubjectId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
