namespace NS.Framework.AspNetCore.RateLimiting;

/// <summary>
/// 速率限制配置选项
/// </summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// 是否启用速率限制
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 允许的请求数
    /// </summary>
    public int PermitLimit { get; set; } = 1000;

    /// <summary>
    /// 时间窗口（秒）
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// 每个窗口的段数
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 6;
}

