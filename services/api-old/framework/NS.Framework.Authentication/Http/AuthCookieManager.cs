using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NS.Framework.Authentication.Abstractions.Http;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;

namespace NS.Framework.Authentication.Http;

public sealed class AuthCookieManager : IAuthCookieManager, ITransientDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthOptions _authOptions;

    public AuthCookieManager(
        IHttpContextAccessor httpContextAccessor,
        IOptions<AuthOptions> authOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _authOptions = authOptions.Value;
    }

    // =========================
    // Refresh Token Cookie (App)
    // =========================

    public string? TryGetRefreshToken()
    {
        var http = TryGetHttpContext();
        if (http == null) return null;

        return http.Request.Cookies.TryGetValue(_authOptions.Token.RefreshCookieName, out var v) &&
               !string.IsNullOrWhiteSpace(v)
            ? v
            : null;
    }

    public string GetRequiredRefreshToken()
    {
        var token = TryGetRefreshToken();
        if (string.IsNullOrWhiteSpace(token))
            throw new AbpAuthorizationException("Missing refresh cookie.");
        return token;
    }

    public void SetRefreshToken(string refreshTokenPlain, DateTimeOffset expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenPlain))
            throw new ArgumentException("Refresh token is empty.", nameof(refreshTokenPlain));

        var http = GetHttpContext();

        http.Response.Cookies.Append(
            _authOptions.Token.RefreshCookieName,
            refreshTokenPlain,
            BuildRefreshCookieOptions(http, expiresAtUtc));
    }

    public void ClearRefreshToken()
    {
        var http = TryGetHttpContext();
        if (http == null) return;

        http.Response.Cookies.Delete(
            _authOptions.Token.RefreshCookieName,
            new CookieOptions
            {
                Path = _authOptions.Token.RefreshCookiePath
            });
    }

    // =========================
    // Admin Session Cookie (BFF)
    // =========================

    public string? TryGetAdminSessionId()
    {
        var http = TryGetHttpContext();
        if (http == null) return null;

        return http.Request.Cookies.TryGetValue(_authOptions.Session.CookieName, out var v) &&
               !string.IsNullOrWhiteSpace(v)
            ? v
            : null;
    }

    public string GetRequiredAdminSessionId()
    {
        var sid = TryGetAdminSessionId();
        if (string.IsNullOrWhiteSpace(sid))
            throw new AbpAuthorizationException("Missing session cookie.");
        return sid;
    }

    public void SetAdminSessionId(string sessionId, DateTimeOffset expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session id is empty.", nameof(sessionId));

        var http = GetHttpContext();

        http.Response.Cookies.Append(
            _authOptions.Session.CookieName,
            sessionId,
            BuildAdminSessionCookieOptions(http, expiresAtUtc));
    }

    public void ClearAdminSessionId()
    {
        var http = TryGetHttpContext();
        if (http == null) return;

        http.Response.Cookies.Delete(
            _authOptions.Session.CookieName,
            new CookieOptions
            {
                Path = _authOptions.Session.CookiePath
            });
    }

    // =========================
    // Internal helpers
    // =========================

    private HttpContext GetHttpContext()
        => _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext.");

    private HttpContext? TryGetHttpContext()
        => _httpContextAccessor.HttpContext;

    private CookieOptions BuildRefreshCookieOptions(HttpContext http, DateTimeOffset expiresAtUtc)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = ResolveSecure(http, _authOptions.Token.RefreshCookieSecurePolicy),
            SameSite = _authOptions.Token.RefreshCookieSameSite,
            Path = _authOptions.Token.RefreshCookiePath,
            Expires = expiresAtUtc.UtcDateTime
        };
    }

    private CookieOptions BuildAdminSessionCookieOptions(HttpContext http, DateTimeOffset expiresAtUtc)
    {
        // Admin session：子域名架构下同源请求，SameSite=Lax 即可
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = ResolveSecure(http, _authOptions.Session.CookieSecurePolicy),
            SameSite = _authOptions.Session.CookieSameSite,
            Path = _authOptions.Session.CookiePath,
            Expires = expiresAtUtc.UtcDateTime
        };
    }

    private static bool ResolveSecure(HttpContext http, CookieSecurePolicy policy)
    {
        return policy switch
        {
            CookieSecurePolicy.Always => true,
            CookieSecurePolicy.None => false,
            CookieSecurePolicy.SameAsRequest => http.Request.IsHttps,
            _ => http.Request.IsHttps
        };
    }
}
