using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace NS.Module.AuditLogging.Domain.Shared;

[DependsOn(typeof(AbpDddDomainSharedModule))]
public class AuditLoggingDomainSharedModule: AbpModule
{
    
}