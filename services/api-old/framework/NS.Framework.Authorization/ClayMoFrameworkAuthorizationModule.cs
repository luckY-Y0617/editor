using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NS.Framework.Authorization.Abstractions.Permissions;
using NS.Framework.Authorization.AspNetCore;
using NS.Framework.Authorization.Core;
using NS.Framework.Authorization.Core.Permissions;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security;

namespace NS.Framework.Authorization;

[DependsOn(
    typeof(Volo.Abp.Authorization.AbpAuthorizationModule),
    typeof(AbpSecurityModule),
    typeof(AbpMultiTenancyModule)
)]
public class ClayMoFrameworkAuthorizationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        services.TryAddTransient<IPermissionChecker, DefaultAuthorizationExecutor>();
        services.TryAddTransient<IAuthorizationExecutor, DefaultAuthorizationExecutor>();
        services.TryAddSingleton<IPermissionDefinitionManager, PermissionDefinitionManager>();
        // 注册 ASP.NET Core Authorization 相关服务
        // 1. 权限授权处理器（Handler）
        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // 2. 动态策略提供者（PolicyProvider）
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        // 3. HttpContext 访问器（如未注册）
        services.AddHttpContextAccessor();
    }
}
