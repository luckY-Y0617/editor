using Volo.Abp.Modularity;
using NS.Abp.Domain.Shared;
using NS.Module.TenantManagement.Application.Contracts;

namespace NS.Abp.Application.Contracts;

[DependsOn(
    typeof(ClayMoAbpDomainSharedModule)
    )]
public class ClayMoAbpApplicationContractsModule: AbpModule
{
    
}