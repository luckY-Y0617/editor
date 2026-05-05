namespace NS.Framework.Authentication.Abstractions.Http;

public interface IAuthCookieManager
{
    /// <summary>读取 RefreshToken 明文（HttpOnly Cookie）。不存在则返回 null。</summary>
    string? TryGetRefreshToken();

    /// <summary>必须获取 RefreshToken；不存在则抛出 401/AbpAuthorizationException。</summary>
    string GetRequiredRefreshToken();

    /// <summary>写入 RefreshToken Cookie。</summary>
    void SetRefreshToken(string refreshTokenPlain, DateTimeOffset expiresAtUtc);

    /// <summary>删除 RefreshToken Cookie（携带 Path）。</summary>
    void ClearRefreshToken();

    string? TryGetAdminSessionId();
    string GetRequiredAdminSessionId();

    void SetAdminSessionId(string sessionId, DateTimeOffset expiresAtUtc);
    void ClearAdminSessionId();
}
