using System.Security.Claims;

namespace NS.Framework.BackgroundJobs.Hangfire;

public class HangfireOptions
{
    public const string SectionName = "Hangfire";

    /// <summary>
    /// 任务执行超时时间（小时）
    /// 默认 1 小时
    /// </summary>
    public int JobTimeoutHours { get; set; } = 1;

    /// <summary>
    /// 任务结果保留时间（小时）
    /// 默认 24 小时
    /// </summary>
    public int JobExpirationTimeoutHours { get; set; } = 24;

    /// <summary>
    /// Redis 键前缀
    /// </summary>
    public string RedisPrefix { get; set; } = "ClayMo:HangfireJob:";

    /// <summary>
    /// 是否允许使用内存存储（仅开发环境）
    /// </summary>
    public bool AllowMemoryStorage { get; set; } = false;
}

/// <summary>
/// Hangfire Dashboard 认证授权配置
/// 
/// 大厂标准实践：
/// 1. 支持 Session (Cookie) + JWT 双模式认证
/// 2. 可选权限码检查
/// 3. 可选 IP 白名单（生产环境强烈建议）
/// </summary>
public sealed class HangfireDashboardAuthOptions
{
    /// <summary>
    /// 进入 Hangfire Dashboard 所需权限码
    /// 为空则只做认证检查（不检查权限）
    /// </summary>
    public string? RequiredPermissionCode { get; set; } = "system.hangfire.dashboard";

    /// <summary>
    /// IP 白名单（CIDR 格式）
    /// 生产环境强烈建议配置，如：["10.0.0.0/8", "192.168.0.0/16"]
    /// 留空表示不做 IP 限制
    /// </summary>
    public string[] AllowedCidrs { get; set; } = [];

    /// <summary>
    /// 认证方案名称
    /// 默认使用 "Smart" 方案（自动选择 Session/JWT）
    /// 可指定为 "Bearer" 仅使用 JWT，或 "Session" 仅使用 Cookie
    /// </summary>
    public string? AuthenticationScheme { get; set; } = null;

    /// <summary>
    /// UserId Claim 类型候选列表
    /// 按项目实际使用的 JWT Claim 命名配置
    /// </summary>
    public string[] UserIdClaimTypes { get; set; } =
    [
        "sub",                             // OpenID Connect 标准
        "user_id",
        "userid",
        "uid",
        ClaimTypes.NameIdentifier          // ASP.NET 标准
    ];

    /// <summary>
    /// Dashboard 是否只读
    /// 生产环境建议设置为 true
    /// </summary>
    public bool ReadOnly { get; set; } = false;

    /// <summary>
    /// 是否在开发环境跳过认证
    /// ⚠️ 仅用于本地开发调试，生产环境必须为 false
    /// </summary>
    public bool SkipAuthInDevelopment { get; set; } = false;
}

/// <summary>
/// Hangfire UnitOfWork 配置
/// 
/// 大厂标准实践：
/// - 后台任务统一包装 UOW，保证事务一致性
/// - 与 Web 请求的 DbContext/事务/审计行为一致
/// </summary>
public sealed class HangfireUnitOfWorkOptions
{
    /// <summary>
    /// 默认是否开启事务
    /// 建议：大部分 job 开启；纯读/幂等任务可关闭
    /// </summary>
    public bool IsTransactional { get; set; } = true;

    /// <summary>
    /// 默认事务隔离级别
    /// null 表示使用数据库默认隔离级别
    /// </summary>
    public System.Data.IsolationLevel? IsolationLevel { get; set; } = null;

    /// <summary>
    /// 是否要求新 UOW
    /// 通常为 true，确保每个 job 独立事务
    /// </summary>
    public bool RequiresNew { get; set; } = true;

    /// <summary>
    /// UOW 超时时间（秒）
    /// null 表示使用默认超时
    /// </summary>
    public int? Timeout { get; set; } = null;
}
