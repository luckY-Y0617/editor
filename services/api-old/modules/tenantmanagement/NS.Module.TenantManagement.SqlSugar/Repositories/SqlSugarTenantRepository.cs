using NS.Framework.SqlSugar;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.TenantManagement.Domain;
using NS.Module.TenantManagement.Domain.Repositories;
using Volo.Abp.DependencyInjection;

namespace NS.Module.TenantManagement.SqlSugar.Repositories;

public class SqlSugarTenantRepository
    : SqlSugarRepository<SqlSugarDbContext, TenantAggregateRoot, Guid>,
      ISqlSugarTenantRepository,
      ITransientDependency
{
    public SqlSugarTenantRepository(
        ISqlSugarDbContextProvider<SqlSugarDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task InsertWithConnStrAsync(TenantAggregateRoot tenantAggregateRoot)
    {
        var db = await GetDbContextAsync();
        
        await db.Client.InsertNav(tenantAggregateRoot)
            .Include(t => t.ConnectionStrings)
            .ExecuteCommandAsync();
    }

    public async Task<bool> IsDuplicatedAsync(string name)
    {
        var query = await GetSugarQueryableAsync();
        
        var isExist = await query.AnyAsync(t => t.NormalizedName == TenantAggregateRoot.NormalizeName(name));
        
        return isExist;
    }
}
