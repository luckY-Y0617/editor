using Volo.Abp.Modularity;
using NS.Abp.Application.Contracts;
using NS.Abp.Domain;
using NS.Module.AuditLogging.Application;
using NS.Module.Identity.Application;
using NS.Module.TenantManagement.Application;
using NS.Module.Knowledge.Application;
using Microsoft.Extensions.DependencyInjection;
using NS.Abp.Application.Worker;

namespace NS.Abp.Application;

[DependsOn(typeof(ClayMoAbpApplicationContractsModule),
    typeof(ClayMoAbpDomainModule),
    
    typeof(TenantManagementApplicationModule),
    typeof(AuditLoggingApplicationModule),
    typeof(IdentityApplicationModule),
    typeof(KnowledgeApplicationModule)
    )]
public class ClayMoAbpApplicationModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<TenantProvisioningWorker>();

    }
}