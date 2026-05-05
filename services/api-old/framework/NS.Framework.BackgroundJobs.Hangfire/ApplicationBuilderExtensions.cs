using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NS.Framework.BackgroundJobs.Hangfire;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 使用 ClayMo Hangfire Dashboard
    /// 配置来自 HangfireDashboardAuthOptions
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <param name="path">Dashboard 路径（默认 /hangfire）</param>
    public static IApplicationBuilder UseClayMoHangfireDashboard(
        this IApplicationBuilder app,
        string path = "/hangfire")
    {
        var authFilter = app.ApplicationServices.GetRequiredService<IDashboardAsyncAuthorizationFilter>();
        var options = app.ApplicationServices.GetService<IOptions<HangfireDashboardAuthOptions>>()?.Value
                      ?? new HangfireDashboardAuthOptions();

        var dashboardOptions = new DashboardOptions
        {
            AsyncAuthorization = [authFilter],
            DashboardTitle = "ClayMo 任务调度中心",
            DisplayStorageConnectionString = false,
            IsReadOnlyFunc = _ => options.ReadOnly,
            IgnoreAntiforgeryToken = true,
            StatsPollingInterval = 5000
        };

        app.UseHangfireDashboard(path, dashboardOptions);

        return app;
    }
}
