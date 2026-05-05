using NS.Module.Identity.Domain.Shared;
using Volo.Abp.Modularity;

namespace NS.Module.Identity.Application.Contracts;

[DependsOn(typeof(IdentityDomainSharedModule))]
public class ClayMoFrameworkIdentityApplicationContractsModule : AbpModule
{
}

