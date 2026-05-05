using System.Collections.Generic;
using System.Threading.Tasks;

namespace NS.Framework.Authorization.Abstractions.PermissionCatalog;

public interface IPermissionCatalogStore
{
    Task<IReadOnlyList<PermissionModule>> GetModules();
    Task<PermissionModuleDetail> GetModule(string moduleCode);
}

