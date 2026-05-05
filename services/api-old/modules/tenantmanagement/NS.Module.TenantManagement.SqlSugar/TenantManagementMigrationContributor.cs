using NS.Framework.SqlSugar.Abstractions.Migrations;
using NS.Framework.SqlSugar.Migrations;
using NS.Module.TenantManagement.Domain;

namespace NS.Module.TenantManagement.SqlSugar;

[Migration(MigrationScopes.Host, order: 100)]
public sealed class TenantManagementMigrationContributor : CodeFirstMigrationContributorBase
{
    public override string Id => "tenant-management/host/codefirst";

    public override string Description => "TenantManagement host tables (Tenant, TenantConnectionString)";

    protected override IEnumerable<Type> GetEntityTypes()
    {
        return
        [
            typeof(TenantAggregateRoot),
            typeof(TenantConnectionString),
            typeof(TenantProvisioningJob)
        ];
    }
}