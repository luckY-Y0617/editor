using NS.Module.Identity.Application.Contracts.Permissions.Dtos;
using Volo.Abp.Application.Services;

namespace NS.Module.Identity.Application.Contracts.Permissions;

public interface IPermissionDefinitionAppService : IApplicationService
{
    Task<List<PermissionModuleDto>> GetModulesAsync();
}

