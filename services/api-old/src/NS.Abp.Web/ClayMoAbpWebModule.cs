using NS.Abp.Application;
using NS.Abp.SqlSugar;
using NS.Framework.AspNetCore;
using NS.Framework.AspNetCore.Extensions;
using NS.Framework.AspNetCore.MultiTenancy;
using NS.Framework.BackgroundJobs.Hangfire;
using NS.Module.Identity.Application;
using NS.Module.Knowledge.Application;
using NS.Module.TenantManagement.Application;
using Microsoft.OpenApi.Models;
using NS.Framework.Authentication;
using NS.Framework.Authorization;
using NS.Framework.Core;
using NS.Framework.SqlSugar;
using NS.Module.AuditLogging.Application;
using NS.Module.Identity.Domain.Shared;
using Volo.Abp;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Auditing;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs.Hangfire;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Swashbuckle;

namespace NS.Abp.Web;

[DependsOn(
    // 应用层
    typeof(ClayMoAbpApplicationModule),
    typeof(IdentityDomainSharedModule),
    
    // 框架层（自治模块）
    typeof(ClayMoFrameworkAuthenticationModule),      // 认证
    typeof(ClayMoFrameworkAuthorizationModule),       // 授权
    typeof(ClayMoFrameworkCoreModule),                // 核心 + Redis
    typeof(ClayoMoFrameworkAspNetCoreModule),         // AspNetCore + RateLimiting
    typeof(ClayMoFrameworkBackgroundJobsHangfireModule), // Hangfire
    
    // 数据层
    typeof(ClayMoAbpSqlSugarCoreModule),
    typeof(ClayMoFrameworkSqlSugarModule),
    
    // ABP 基础设施
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpAspNetCoreMultiTenancyModule),
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAutofacModule),
    typeof(AbpSwashbuckleModule),
    
    // 缓存与后台任务
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpBackgroundJobsHangfireModule))]
public class ClayMoAbpWebModule : AbpModule
{
    public override Task PreConfigureServicesAsync(ServiceConfigurationContext context)
    {
        PreConfigure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(
                typeof(AuditLoggingApplicationModule).Assembly,
                option => option.RemoteServiceName = "Audit");
            
            options.ConventionalControllers.Create(
                typeof(KnowledgeApplicationModule).Assembly,
                option => option.RemoteServiceName = "Knowledge");

            options.ConventionalControllers.Create(
                typeof(IdentityApplicationModule).Assembly,
                option => option.RemoteServiceName = "Identity");

            options.ConventionalControllers.Create(
                typeof(TenantManagementApplicationModule).Assembly,
                option => option.RemoteServiceName = "Tenant");

        });

        return Task.CompletedTask;
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = services.GetConfiguration();

        // Host 特有配置
        ConfigureAbpOptions(configuration);
        ConfigureMultiTenancy();

        services.AddHttpContextAccessor();

        // Swagger（需要泛型参数，暂不迁移到 Module）
        services.AddCustomSwaggerGen<ClayMoAbpWebModule>(options =>
        {
            options.SwaggerDoc("default", new OpenApiInfo
            {
                Title = "ClayMo.Abp.Net9 API",
                Version = "v1",
                Description = "ClayMo 框架 API 文档"
            });
        });
    }

    private void ConfigureAbpOptions(IConfiguration configuration)
    {
        Configure<AbpAuditingOptions>(options =>
        {
            options.IsEnabled = true;
            options.IsEnabledForGetRequests = true;
        });

        Configure<AbpDistributedCacheOptions>(options =>
        {
            configuration.GetSection("AbpDistributedCacheOptions").Bind(options);
        });
    }

    private void ConfigureMultiTenancy()
    {
        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = true;
        });

        Configure<AbpTenantResolveOptions>(options =>
        {
            options.AddXHeaderTenantResolver();
        });

        Configure<AbpAspNetCoreMultiTenancyOptions>(_ => { });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        
        // 转发头中间件
        app.UseForwardedHeaders();

        // 路由
        app.UseRouting();
        app.UseRateLimiter();

        // 认证授权
        app.UseAuthentication();
        app.UseMultiTenancy();

        // 静态文件
        app.UseStaticFiles();

        // ABP 核心中间件
        app.UseUnitOfWork();
        app.UseAuthorization();
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();

        // Swagger
        app.UseClayMoSwagger();

        // Hangfire Dashboard
        app.UseClayMoHangfireDashboard("/api/hangfire");

        // 终结点
        app.UseConfiguredEndpoints();
    }
}
