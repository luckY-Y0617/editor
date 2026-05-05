using NS.Module.Identity.Application.Contracts;
using NS.Module.Knowledge.Application.Contracts;
using NS.Module.Knowledge.Domain;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;


namespace NS.Module.Knowledge.Application;

[DependsOn(
    typeof(KnowledgeApplicationContractsModule),
    typeof(KnowledgeDomainModule),
    typeof(ClayMoFrameworkIdentityApplicationContractsModule),
    typeof(AbpAutoMapperModule))]
public class KnowledgeApplicationModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<KnowledgeApplicationModule>();
        
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<KnowledgeApplicationModule>();
        });
    }
}