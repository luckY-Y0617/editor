using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using NS.Module.AuditLogging.Application.Contracts;
using NS.Module.AuditLogging.Domain;

namespace NS.Module.AuditLogging.Application;

[DependsOn(
    typeof(AbpAutoMapperModule),
    typeof(AuditLoggingApplicationContractsModule),
    typeof(AuditLoggingDomainModule))]
public class AuditLoggingApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<AuditLoggingApplicationModule>();
        
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AuditLoggingApplicationModule>();
        });
    }
}

