using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using Volo.Abp.TenantManagement;
using NS.Module.AuditLogging.Domain.Shared;
using NS.Module.Identity.Domain.Shared;

namespace NS.Abp.Domain.Shared;

[DependsOn(typeof(AbpDddDomainSharedModule),
    typeof(AbpTenantManagementDomainSharedModule),
    
    typeof(AuditLoggingDomainSharedModule),
    typeof(IdentityDomainSharedModule)
    )]
public class ClayMoAbpDomainSharedModule: AbpModule
{
    
}