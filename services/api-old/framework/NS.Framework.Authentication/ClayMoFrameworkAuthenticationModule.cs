using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NS.Framework.Authentication.Abstractions.Captcha;
using NS.Framework.Authentication.Abstractions.Http;
using NS.Framework.Authentication.Abstractions.Security;
using NS.Framework.Authentication.Captcha;
using NS.Framework.Authentication.Http;
using NS.Framework.Authentication.Security;
using NS.Framework.Authentication.Session;
using NS.Framework.Authentication.Token;
using Volo.Abp.Modularity;

namespace NS.Framework.Authentication;

public class ClayMoFrameworkAuthenticationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = services.GetConfiguration();

        // 1. 绑定配置
        ConfigureOptions(services, configuration);

        // 2. 注册服务
        RegisterServices(services);

        // 3. 配置认证方案
        ConfigureAuthentication(services, configuration);
    }

    private static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection("Authentication"));

        services.PostConfigure<AuthOptions>(opt =>
        {
            // JWT 配置默认值
            if (opt.Jwt.MinimumAccessTokenLifetime.TotalMinutes < 1)
                opt.Jwt.MinimumAccessTokenLifetime = TimeSpan.FromMinutes(1);
            if (opt.Jwt.AccessTokenLifetime < opt.Jwt.MinimumAccessTokenLifetime)
                opt.Jwt.AccessTokenLifetime = opt.Jwt.MinimumAccessTokenLifetime;

            // Token 配置默认值
            if (opt.Token.Length < 32) opt.Token.Length = 32;
            if (opt.Token.Lifetime.TotalHours < 1) opt.Token.Lifetime = TimeSpan.FromHours(1);
            if (opt.Token.MinimumRefreshTokenLength < 16)
                opt.Token.MinimumRefreshTokenLength = 16;
        });
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // Session
        services.AddSingleton<ISessionStore, DistributedCacheSessionStore>();
        services.AddTransient<IAuthCookieManager, AuthCookieManager>();

        // Captcha
        services.AddSingleton<ICaptchaStore, InMemoryCaptchaStore>();
        services.AddScoped<ICaptchaManager, CaptchaManager>();

        // Token
        services.AddScoped<TokenService>();

        // Security
        services.AddScoped<IPasswordPolicy, PasswordPolicy>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
    }

    private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        // 获取配置
        var authOptions = configuration.GetSection("Authentication").Get<AuthOptions>();
        if (authOptions?.Jwt?.SigningKey == null)
        {
            // 配置未就绪，跳过认证配置（可能在测试环境）
            return;
        }

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(authOptions.Jwt.SigningKey));

        services.AddAuthentication("Smart")
            .AddPolicyScheme("Smart", "Smart", options =>
            {
                /// <summary>
                /// 子域名架构：基于 Gateway 注入的 X-Client-Type 头判断认证方式
                ///   X-Client-Type: admin  → Session（Cookie: claymo.sid）
                ///   X-Client-Type: web    → JWT（Bearer Token）
                ///   兜底：检查 Bearer header → JWT，否则 Session
                /// </summary>
                options.ForwardDefaultSelector = ctx =>
                {
                    // 优先使用 Gateway 设置的客户端类型头
                    var clientType = ctx.Request.Headers["X-Client-Type"].ToString();

                    if (string.Equals(clientType, "admin", StringComparison.OrdinalIgnoreCase))
                        return SessionAuthenticationHandler.SchemeName;

                    if (string.Equals(clientType, "web", StringComparison.OrdinalIgnoreCase))
                        return JwtBearerDefaults.AuthenticationScheme;

                    // 兜底（直连后端调试时无 Gateway 注入头）：检查 Bearer header
                    var auth = ctx.Request.Headers.Authorization.ToString();
                    if (!string.IsNullOrWhiteSpace(auth) &&
                        auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        return JwtBearerDefaults.AuthenticationScheme;
                    }

                    return SessionAuthenticationHandler.SchemeName;
                };
            })
            .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
                SessionAuthenticationHandler.SchemeName, _ => { })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authOptions.Jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authOptions.Jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("ClayMo.Jwt");
                        logger.LogError(context.Exception, "JWT 认证失败");
                        return Task.CompletedTask;
                    }
                };
            });
    }
}

