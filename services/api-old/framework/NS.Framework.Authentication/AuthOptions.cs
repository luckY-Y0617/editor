using System;
using Microsoft.AspNetCore.Http;

namespace NS.Framework.Authentication;

/// <summary>
/// 统一认证配置
/// </summary>
public sealed class AuthOptions
{
    /// <summary>
    /// JWT 配置
    /// </summary>
    public JwtConfig Jwt { get; set; } = new();

    /// <summary>
    /// Token 配置
    /// </summary>
    public TokenConfig Token { get; set; } = new();

    /// <summary>
    /// Session 配置
    /// </summary>
    public SessionConfig Session { get; set; } = new();

    /// <summary>
    /// 安全配置
    /// </summary>
    public SecurityConfig Security { get; set; } = new();

    /// <summary>
    /// 限流配置
    /// </summary>
    public ThrottleConfig Throttle { get; set; } = new();
}

/// <summary>
/// JWT 相关配置
/// </summary>
public sealed class JwtConfig
{
    /// <summary>
    /// 签名密钥（对称密钥模式）
    /// </summary>
    public string? SigningKey { get; set; }

    /// <summary>
    /// 发行者
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// 受众（子域名架构下仅 App 前台使用 JWT，建议配为 "claymo:web"）
    /// Admin 后台走 Session，不依赖此值
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// AccessToken 有效期，默认 30 分钟
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// AccessToken 最短有效期，默认 5 分钟
    /// </summary>
    public TimeSpan MinimumAccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(5);
}


/// <summary>
/// Token 相关配置
/// </summary>
public sealed class TokenConfig
{
    /// <summary>
    /// RefreshToken 最小长度，默认 32
    /// </summary>
    public int MinimumRefreshTokenLength { get; set; } = 32;

    /// <summary>
    /// 是否启用滑动过期
    /// </summary>
    public bool UseSlidingExpiration { get; set; } = true;

    /// <summary>
    /// 刷新令牌有效期，默认 7 天
    /// </summary>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// 刷新令牌长度（字符长度目标），默认 64
    /// </summary>
    public int Length { get; set; } = 64;

    /// <summary>
    /// RefreshToken hash 专用 key（强制配置）
    /// </summary>
    public string HashKey { get; set; } = string.Empty;

    /// <summary>
    /// RefreshToken Cookie 名称（HttpOnly）
    /// </summary>
    public string RefreshCookieName { get; set; } = "claymo.rt";

    /// <summary>
    /// RefreshToken Cookie Path（建议收窄到 refresh endpoint）
    /// </summary>
    public string RefreshCookiePath { get; set; } = "/api/app/auth";

    /// <summary>
    /// RefreshToken Cookie SameSite 策略（默认 Lax）
    /// </summary>
    public SameSiteMode RefreshCookieSameSite { get; set; } = SameSiteMode.Lax;

    /// <summary>
    /// RefreshToken Cookie Secure 策略（默认 SameAsRequest；生产建议 Always）
    /// </summary>
    public CookieSecurePolicy RefreshCookieSecurePolicy { get; set; } = CookieSecurePolicy.SameAsRequest;
}


/// <summary>
/// Session 相关配置
/// </summary>
public sealed class SessionConfig
{
    /// <summary>
    /// Cookie 名称
    /// </summary>
    public string CookieName { get; set; } = "claymo.sid";

    /// <summary>
    /// Session 有效期，默认 7 天
    /// </summary>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// 缓存键前缀
    /// </summary>
    public string CacheKeyPrefix { get; set; } = "sess:";

    /// <summary>
    /// Admin Session Cookie Path（子域名架构下 "/" 即可，Cookie 域自动隔离）
    /// </summary>
    public string CookiePath { get; set; } = "/";

    /// <summary>
    /// Admin Session Cookie SameSite（子域名同源：Lax；旧路径模式可能需要 None）
    /// </summary>
    public SameSiteMode CookieSameSite { get; set; } = SameSiteMode.Lax;

    /// <summary>
    /// Admin Session Cookie Secure 策略（默认 SameAsRequest；生产建议 Always）
    /// </summary>
    public CookieSecurePolicy CookieSecurePolicy { get; set; } = CookieSecurePolicy.SameAsRequest;
}

/// <summary>
/// 安全相关配置
/// </summary>
public sealed class SecurityConfig
{
    // 密码相关配置可以在这里扩展
}

/// <summary>
/// 限流相关配置
/// </summary>
public sealed class ThrottleConfig
{
    /// <summary>
    /// 是否启用登录/验证码速率限制（默认开启）
    /// </summary>
    public bool EnableRateLimit { get; set; } = true;

    /// <summary>
    /// 登录尝试窗口大小（默认 5 分钟）
    /// </summary>
    public TimeSpan LoginWindow { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 登录尝试次数阈值（默认 10 次）
    /// </summary>
    public int LoginAttemptsThreshold { get; set; } = 10;

    /// <summary>
    /// 登录封禁时长（默认 10 分钟）
    /// </summary>
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(10);
}
