using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Knowledge.Domain.Shared;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace NS.Module.Knowledge.Domain;

[DependsOn(typeof(AbpDddDomainModule),
    typeof(KnowledgeDomainSharedModule))]
public class KnowledgeDomainModule: AbpModule
{
    
}