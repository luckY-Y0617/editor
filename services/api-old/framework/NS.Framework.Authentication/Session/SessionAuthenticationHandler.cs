using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Security.Claims;

namespace NS.Framework.Authentication.Session;

public sealed class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Session";

    private readonly ISessionStore _store;
    private readonly AuthOptions _authOptions;

    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        ISystemClock clock,
        ISessionStore store,
        IOptions<AuthOptions> authOptions)
        : base(options, logger, encoder, clock)
    {
        _store = store;
        _authOptions = authOptions.Value;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(_authOptions.Session.CookieName, out var sid) || string.IsNullOrWhiteSpace(sid))
            return AuthenticateResult.NoResult();

        var payload = await _store.GetAsync(sid, Context.RequestAborted);
        if (payload is null)
            return AuthenticateResult.Fail("Invalid session");

        // 可选：滑动过期（大厂常用）
        // 接近过期时刷新 TTL
        var now = DateTimeOffset.UtcNow;
        if (payload.ExpiresAtUtc < now)
            return AuthenticateResult.Fail("Session expired");

        // 构建 claims（保持与你现有 BuildClaimsAsync 一致的关键项）
        var claims = new List<Claim>
        {
            new(AbpClaimTypes.UserId, payload.UserId.ToString()),
            new(AbpClaimTypes.UserName, payload.UserName),
            new("sub", payload.UserId.ToString()),
            new("uid", payload.UserId.ToString())
        };

        if (payload.TenantId.HasValue)
        {
            claims.Add(new Claim(AbpClaimTypes.TenantId, payload.TenantId.Value.ToString()));
            claims.Add(new Claim("tid", payload.TenantId.Value.ToString()));
        }

        foreach (var r in payload.Roles ?? Array.Empty<string>())
        {
            claims.Add(new Claim(AbpClaimTypes.Role, r));
            claims.Add(new Claim("roles", r));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
