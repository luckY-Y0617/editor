using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;

namespace NS.Framework.Mapster;

[DependsOn(typeof(AbpObjectMappingModule))]
public class ClayMoFrameworkMapsterModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IAutoObjectMappingProvider, MapsterAutoObjectMappingProvider>();
    }
}