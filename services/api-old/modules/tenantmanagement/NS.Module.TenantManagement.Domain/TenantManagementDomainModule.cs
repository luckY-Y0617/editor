using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace NS.Module.TenantManagement.Domain;

[DependsOn(typeof(AbpTenantManagementDomainSharedModule),
    typeof(AbpDddDomainModule))]
public class TenantManagementDomainModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        services.Replace(new ServiceDescriptor(typeof(ITenantStore), typeof(TenantStore), ServiceLifetime.Transient));
    }
}