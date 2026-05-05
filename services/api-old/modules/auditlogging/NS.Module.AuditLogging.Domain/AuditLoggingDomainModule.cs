using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using NS.Module.AuditLogging.Domain.Shared;
using NS.Framework.SqlSugar.Abstractions;

namespace NS.Module.AuditLogging.Domain;

[DependsOn(typeof(AbpDddDomainModule),
    typeof(AuditLoggingDomainSharedModule)
)]
public class AuditLoggingDomainModule: AbpModule
{
    
}