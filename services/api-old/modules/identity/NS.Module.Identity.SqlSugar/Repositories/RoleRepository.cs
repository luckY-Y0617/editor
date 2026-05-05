using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Identity.Domain.Roles;
using NS.Module.Identity.Domain.Roles.Repositories;
using Volo.Abp.DependencyInjection;

namespace NS.Module.Identity.SqlSugar.Repositories;

public class RoleRepository : 
    SqlSugarRepository<IdentityDbContext, Role, Guid>, 
    IRoleRepository, 
    ITransientDependency
{
    public RoleRepository(ISqlSugarDbContextProvider<IdentityDbContext> dbContextProvider): base(dbContextProvider){}
    
    public async Task<List<string>> GetPermissionCodesAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dbContext = await GetDbContextAsync();

        return await dbContext.Client.Queryable<RolePermission>()
            .Where(rr => rr.RoleId == roleId)
            .Select(rr => rr.PermissionCode)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task ResetPermissionsAsync(
        Guid roleId,
        IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken = default)
    {
        var db = await GetDbContextAsync();

        await db.Client.Deleteable<RolePermission>()
            .Where(rp => rp.RoleId == roleId)
            .ExecuteCommandAsync(cancellationToken);

        if (permissionCodes.Count == 0)
        {
            return;
        }

        var entities = permissionCodes
            .Select(code => new RolePermission(roleId: roleId, permissionCode: code))
            .ToList();

        await db.Client.Insertable(entities).ExecuteCommandAsync(cancellationToken);
    }
}

