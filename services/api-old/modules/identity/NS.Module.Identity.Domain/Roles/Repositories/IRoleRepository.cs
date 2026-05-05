using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;

namespace NS.Module.Identity.Domain.Roles.Repositories;

public interface IRoleRepository : ISqlSugarRepository<Role, Guid>
{
    Task<List<string>> GetPermissionCodesAsync(Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// 重置角色的权限列表：
    /// - 在一个事务中先删除该角色的所有权限，再写入新的权限编码集合（如果集合为空则只删除不插入）
    /// </summary>
    Task ResetPermissionsAsync(Guid roleId, IReadOnlyCollection<string> permissionCodes, CancellationToken ct = default);
}

