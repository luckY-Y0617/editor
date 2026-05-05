using System;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MemoryStorage;
using Hangfire.Redis.StackExchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.Hangfire;
using Volo.Abp.Modularity;

namespace NS.Framework.BackgroundJobs.Hangfire;

[DependsOn(typeof(AbpHangfireModule))]
public class ClayMoFrameworkBackgroundJobsHangfireModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = services.GetConfiguration();

        // 1. 绑定配置
        ConfigureOptions(services, configuration);

        // 2. 注册服务
        RegisterServices(services);

        // 3. 配置 Hangfire 存储（立即执行，设置 JobStorage.Current）
        ConfigureHangfireStorage(services, configuration);
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var registrar = context.ServiceProvider.GetRequiredService<IHangfireFilterRegistrar>();
        registrar.RegisterFilters(context.ServiceProvider);
    }

    private static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HangfireOptions>(configuration.GetSection(HangfireOptions.SectionName));
        services.Configure<HangfireDashboardAuthOptions>(
            configuration.GetSection("Hangfire:Dashboard"));
        services.Configure<HangfireUnitOfWorkOptions>(opt =>
        {
            opt.IsTransactional = true;
            opt.RequiresNew = true;
        });
        services.Configure<HangfireUnitOfWorkOptions>(
            configuration.GetSection("Hangfire:UnitOfWork"));
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.TryAddSingleton<UnitOfWorkHangfireFilter>();
        services.TryAddSingleton<IDashboardAsyncAuthorizationFilter, ClayMoHangfireDashboardAuthorizationFilter>();
        services.AddSingleton<IHangfireFilterRegistrar, HangfireFilterRegistrar>();
    }

    private static void ConfigureHangfireStorage(IServiceCollection services, IConfiguration configuration)
    {
        var hangfireOptions = configuration
            .GetSection(HangfireOptions.SectionName)
            .Get<HangfireOptions>() ?? new HangfireOptions();

        // ⚠️ 必须用 Action<IGlobalConfiguration> 重载 — 立即执行并设置 JobStorage.Current
        //    Action<IServiceProvider, IGlobalConfiguration> 重载会延迟执行，
        //    导致 AbpHangfireModule.OnApplicationInitialization 时 JobStorage.Current 未初始化
        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                  .UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings();

            if (IsRedisEnabled(configuration))
            {
                ConfigureRedisStorage(config, configuration, hangfireOptions);
            }
            else
            {
                ConfigureMemoryStorage(config, configuration);
            }
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 2;
            options.Queues = ["default", "critical", "low"];
        });
    }

    #region Storage Configuration

    private static bool IsRedisEnabled(IConfiguration configuration)
        => configuration.GetSection("Redis").GetValue<bool>("IsEnabled");

    private static void ConfigureRedisStorage(
        IGlobalConfiguration config,
        IConfiguration configuration,
        HangfireOptions options)
    {
        // 直接从配置创建连接（不走 DI，因为此时是立即执行）
        var connectionString = configuration.GetSection("Redis").GetValue<string>("Configuration")
                               ?? "localhost:6379";
        var multiplexer = ConnectionMultiplexer.Connect(connectionString);
        var jobDb = configuration.GetSection("Redis").GetValue<int>("JobDb");

        config.UseRedisStorage(multiplexer, new RedisStorageOptions
            {
                Db = jobDb,
                InvisibilityTimeout = TimeSpan.FromHours(options.JobTimeoutHours),
                Prefix = options.RedisPrefix,
                FetchTimeout = TimeSpan.FromMinutes(5),
                ExpiryCheckInterval = TimeSpan.FromMinutes(5),
                DeletedListSize = 1000,
                SucceededListSize = 1000
            })
            .WithJobExpirationTimeout(TimeSpan.FromHours(options.JobExpirationTimeoutHours));
    }

    private static void ConfigureMemoryStorage(IGlobalConfiguration config, IConfiguration configuration)
    {
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? Environments.Production;
        var allowMemoryStorage = configuration.GetSection("Hangfire")
            .GetValue<bool?>("AllowMemoryStorage") ?? environment == Environments.Development;

        if (!allowMemoryStorage)
        {
            throw new InvalidOperationException(
                "Hangfire Redis 已禁用且未允许使用内存存储。" +
                "请启用 Redis 或设置 Hangfire:AllowMemoryStorage=true");
        }

        config.UseMemoryStorage(new MemoryStorageOptions
        {
            FetchNextJobTimeout = TimeSpan.FromMinutes(5)
        });
    }

    #endregion
}

/// <summary>
/// Hangfire Filter 注册器接口
/// </summary>
public interface IHangfireFilterRegistrar
{
    void RegisterFilters(IServiceProvider serviceProvider);
}

/// <summary>
/// 延迟注册 Hangfire 全局 Filter
/// </summary>
internal sealed class HangfireFilterRegistrar : IHangfireFilterRegistrar
{
    private static bool _registered;
    private static readonly object _lock = new();

    public void RegisterFilters(IServiceProvider serviceProvider)
    {
        if (_registered) return;

        lock (_lock)
        {
            if (_registered) return;

            var uowFilter = serviceProvider.GetRequiredService<UnitOfWorkHangfireFilter>();
            GlobalJobFilters.Filters.Add(uowFilter);

            _registered = true;
        }
    }
}
