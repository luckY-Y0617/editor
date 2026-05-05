using Volo.Abp.Modularity;
using NS.Module.AuditLogging.Domain;
using NS.Module.AuditLogging.Domain.Shared;
using NS.Framework.SqlSugar;
using NS.Framework.SqlSugar.Abstractions.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace NS.Module.AuditLogging.SqlSugar;

[DependsOn(typeof(ClayMoFrameworkSqlSugarModule),
    typeof(AuditLoggingDomainModule),
    typeof(AuditLoggingDomainSharedModule)
    )]
public class AuditLoggingSqlSugarModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IMigrationContributor, AuditLoggingMigrationContributor>();
    }
}