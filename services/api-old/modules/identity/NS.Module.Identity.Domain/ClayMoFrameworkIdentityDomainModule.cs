using NS.Module.Identity.Domain.Shared;
using NS.Framework.SqlSugar.Abstractions;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace NS.Module.Identity.Domain;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(IdentityDomainSharedModule))]
public class ClayMoFrameworkIdentityDomainModule : AbpModule
{
}

