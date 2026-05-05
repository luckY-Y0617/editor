using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using NS.Abp.Domain.Shared;
using NS.Module.AuditLogging.Domain;
using NS.Module.TenantManagement.Domain;

namespace NS.Abp.Domain;

[DependsOn(typeof(AbpDddDomainModule),
    
    typeof(ClayMoAbpDomainSharedModule),
    typeof(TenantManagementDomainModule),
    typeof(AuditLoggingDomainModule)
    )]
public class ClayMoAbpDomainModule: AbpModule
{
    
}