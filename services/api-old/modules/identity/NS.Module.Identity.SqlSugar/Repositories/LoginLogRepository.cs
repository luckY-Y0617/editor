using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Identity.Domain.Identities;
using NS.Module.Identity.Domain.Identities.Repositories;
using SqlSugar;
using Volo.Abp.DependencyInjection;

namespace NS.Module.Identity.SqlSugar.Repositories;

public class LoginLogRepository : 
    SqlSugarRepository<IdentityDbContext, LoginLog, Guid>, 
    ILoginLogRepository, 
    ITransientDependency
{
    public LoginLogRepository(ISqlSugarDbContextProvider<IdentityDbContext> dbContextProvider): base(dbContextProvider){}
    
    public async Task<List<LoginLog>> GetRecentAsync(
        Guid userId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dbContext = await GetDbContextAsync();

        var query = dbContext.Client.Queryable<LoginLog>()
            .Where(log => log.CreatorId == userId)
            .OrderBy(log => log.LoginTime, OrderByType.Desc);

        return await query.Take(count).ToListAsync(cancellationToken);
    }

}

