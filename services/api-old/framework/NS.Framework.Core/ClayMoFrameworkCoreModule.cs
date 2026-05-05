using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NS.Framework.Core.Abstractions.Time;
using NS.Framework.Core.Serialization;
using StackExchange.Redis;
using Volo.Abp.Modularity;

namespace NS.Framework.Core;

/// <summary>
/// - Redis 连接管理
/// - JSON 序列化配置
/// - 系统时钟
/// </summary>
public class ClayMoFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = services.GetConfiguration();

        // 系统时钟
        services.AddSingleton<ISystemClock, SystemClock>();

        // JSON 序列化配置
        Configure<JsonSerializerSettings>(settings =>
        {
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.ContractResolver = new NonPublicPropertiesResolver();
        });

        // Redis 连接
        ConfigureRedis(services, configuration);
    }

    private static void ConfigureRedis(IServiceCollection services, IConfiguration configuration)
    {
        var isEnabled = configuration.GetSection("Redis").GetValue<bool>("IsEnabled");
        if (!isEnabled) return;

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redisConfig = configuration["Redis:Configuration"];
            if (string.IsNullOrWhiteSpace(redisConfig))
            {
                throw new InvalidOperationException(
                    "Redis 配置缺失，请在 appsettings.json 中配置 Redis:Configuration");
            }

            var options = ConfigurationOptions.Parse(redisConfig);
            options.AbortOnConnectFail = false;
            options.ConnectRetry = Math.Max(options.ConnectRetry, 3);
            options.ConnectTimeout = Math.Max(options.ConnectTimeout, 5000);

            return ConnectionMultiplexer.Connect(options);
        });
    }
}
