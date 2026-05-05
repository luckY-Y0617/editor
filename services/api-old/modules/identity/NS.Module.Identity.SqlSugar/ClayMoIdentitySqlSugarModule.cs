using NS.Framework.SqlSugar;
using NS.Module.Identity.Domain;
using NS.Module.Identity.Domain.Roles;
using NS.Module.Identity.Domain.Users;
using NS.Framework.SqlSugar.Abstractions;
using NS.Framework.SqlSugar.Abstractions.Migrations;
using NS.Module.Identity.Domain.Teams;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace NS.Module.Identity.SqlSugar;

[DependsOn(
    typeof(ClayMoFrameworkSqlSugarModule),
    typeof(ClayMoFrameworkIdentityDomainModule))]
public class ClayMoIdentitySqlSugarModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IMigrationContributor, IdentityMigrationContributor>();

        Configure<SqlSugarEntityOptions>(options =>
        {
            options.Entity<User>(entityOptions =>
            {
                entityOptions.DefaultWithDetailsFunc = query =>
                    query.Includes(x => x.Profile)
                         .Includes(x => x.Roles);
            });
            
            options.Entity<Role>(entityOptions =>
            {
                entityOptions.DefaultWithDetailsFunc = query =>
                    query.Includes(x => x.Permissions);
            });
            
            options.Entity<Team>(entityOptions =>
            {
                entityOptions.DefaultWithDetailsFunc = query =>
                    query.Includes(x => x.Members);
            });
        });
    }
}

