using Volo.Abp.Data;
using Volo.Abp.Modularity;
using NS.Abp.Domain;
using NS.Abp.SqlSugar.DataSeeding;
using NS.Module.AuditLogging.SqlSugar;
using NS.Module.Identity.SqlSugar;
using NS.Framework.SqlSugar;
using NS.Module.TenantManagement.SqlSugar;
using NS.Module.Knowledge.SqlSugar;

namespace NS.Abp.SqlSugar;

[DependsOn(
    typeof(ClayMoAbpDomainModule),
    typeof(ClayMoFrameworkSqlSugarModule),
    
    typeof(TenantManagementSqlSugarModule),
    typeof(AuditLoggingSqlSugarModule),
    typeof(KnowledgeSqlSugarModule),
    typeof(ClayMoIdentitySqlSugarModule)
)]

public class ClayMoAbpSqlSugarCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpDataSeedOptions>(options =>
        {
            options.Contributors.Add<HostDataSeedContributor>();
            options.Contributors.Add<TenantDataSeedContributor>();
        });
    }
}
