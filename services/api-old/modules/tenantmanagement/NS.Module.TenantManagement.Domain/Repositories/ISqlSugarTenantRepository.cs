using System;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;

namespace NS.Module.TenantManagement.Domain.Repositories;

public interface ISqlSugarTenantRepository : ISqlSugarRepository<TenantAggregateRoot, Guid>
{
    public Task InsertWithConnStrAsync(TenantAggregateRoot tenantAggregateRoot);
    
    public Task<bool> IsDuplicatedAsync(string name);
}