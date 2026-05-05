using NS.Framework.SqlSugar.Abstractions.Migrations;
using NS.Framework.SqlSugar.Migrations;
using NS.Module.Identity.Domain.Identities;
using NS.Module.Identity.Domain.Roles;
using NS.Module.Identity.Domain.Teams;
using NS.Module.Identity.Domain.Users;

namespace NS.Module.Identity.SqlSugar;

[Migration(MigrationScopes.Both, order: 100)]
public sealed class IdentityMigrationContributor : CodeFirstMigrationContributorBase
{
    public override string Id => "identity/both/codefirst";
    public override string Description => "Identity module tables (CodeFirst InitTables) for host & tenant";

    protected override IEnumerable<Type> GetEntityTypes()
    {
        return new[]
        {
            typeof(User),
            typeof(UserProfile),
            typeof(Role),
            typeof(UserRole),
            typeof(RolePermission),
            typeof(ExternalAuth),
            typeof(LoginLog),
            typeof(Team),
            typeof(TeamMember),
            typeof(RefreshToken)
        };
    }
}