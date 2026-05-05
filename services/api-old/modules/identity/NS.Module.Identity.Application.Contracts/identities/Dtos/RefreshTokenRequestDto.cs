namespace NS.Module.Identity.Application.Contracts.identities.Dtos;

public class RefreshTokenRequestDto
{
    /// <summary>
    /// 旧 RefreshToken（opaque string）。
    /// Web 场景通常由 HttpApi 从 HttpOnly Cookie 读取后传进来。
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>客户端会话/设备信息（建议保留，便于风控与会话管理）</summary>
    public string? SessionId { get; set; }
    public string? ClientId { get; set; }
    public string? DeviceId { get; set; }
    public string? Fingerprint { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    /// <summary>是否把新 refresh token 明文返回（默认 false：更安全）</summary>
    public bool IncludeRefreshToken { get; set; } = false;
}