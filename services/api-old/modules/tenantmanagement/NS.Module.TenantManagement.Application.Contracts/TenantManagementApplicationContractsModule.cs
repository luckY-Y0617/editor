using Volo.Abp.Application;
using Volo.Abp.Modularity;

namespace NS.Module.TenantManagement.Application.Contracts;

[DependsOn(typeof(AbpDddApplicationContractsModule))]
public class TenantManagementApplicationContractsModule: AbpModule
{
}