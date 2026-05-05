using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using NS.Module.TenantManagement.Application.Contracts;
using NS.Module.TenantManagement.Domain;

namespace NS.Module.TenantManagement.Application;

[DependsOn(
    typeof(TenantManagementDomainModule),
    typeof(TenantManagementApplicationContractsModule))]
public class TenantManagementApplicationModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<TenantManagementApplicationModule>();
        });
        
    }
}