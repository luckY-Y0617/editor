using Volo.Abp.Modularity;
using NS.Module.AuditLogging.Domain.Shared;

namespace NS.Module.AuditLogging.Application.Contracts;

[DependsOn(typeof(AuditLoggingDomainSharedModule))]
public class AuditLoggingApplicationContractsModule : AbpModule
{
    
}

