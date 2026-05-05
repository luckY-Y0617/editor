using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NS.Framework.AspNetCore.Logging;
using NS.Framework.AspNetCore.RateLimiting;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;

namespace NS.Framework.AspNetCore;

/// <summary>
/// - RateLimiting 速率限制
/// - RequestClientInfo 解析
/// </summary>
public class ClayoMoFrameworkAspNetCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = services.GetConfiguration();

        // 请求客户端信息解析
        services.AddTransient<ITransientDependency, RequestClientInfoResolver>();

        // 速率限制
        ConfigureRateLimiting(services, configuration);
    }

    private static void ConfigureRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));

        var options = configuration.GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        if (!options.IsEnabled) return;

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiterOptions.OnRejected = CreateRejectionHandler();
            limiterOptions.GlobalLimiter = CreateGlobalLimiter(options);
        });
    }

    private static Func<OnRejectedContext, CancellationToken, ValueTask> CreateRejectionHandler()
    {
        return (rejectionContext, _) =>
        {
            if (rejectionContext.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                rejectionContext.HttpContext.Response.Headers.RetryAfter =
                    ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
            }

            rejectionContext.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            rejectionContext.HttpContext.Response.WriteAsync(
                "Too many requests. Please try again later.",
                cancellationToken: CancellationToken.None);

            return new ValueTask();
        };
    }

    private static PartitionedRateLimiter<HttpContext> CreateGlobalLimiter(RateLimitingOptions options)
    {
        return PartitionedRateLimiter.CreateChained(
            PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var userId = httpContext.User.FindFirstValue(AbpClaimTypes.UserId);
                var tenantId = httpContext.User.FindFirstValue(AbpClaimTypes.TenantId) ?? "host";
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var partitionKey = userId == null ? $"anon:{ip}" : $"{tenantId}:{userId}:{ip}";

                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ =>
                    new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.WindowSeconds),
                        SegmentsPerWindow = options.SegmentsPerWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            }));
    }
}
