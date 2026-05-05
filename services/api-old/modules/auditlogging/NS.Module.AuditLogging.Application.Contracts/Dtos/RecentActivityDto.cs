using System;

namespace NS.Module.AuditLogging.Application.Contracts.Dtos;

/// <summary>
/// 仪表盘最近活动 DTO（简化版）
/// </summary>
public class RecentActivityDto
{
    /// <summary>
    /// 操作用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// HTTP 方法
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// HTTP 状态码
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// 是否有异常
    /// </summary>
    public bool HasException { get; set; }

    /// <summary>
    /// 执行时间
    /// </summary>
    public DateTime? ExecutionTime { get; set; }

    /// <summary>
    /// 执行耗时（毫秒）
    /// </summary>
    public int? ExecutionDuration { get; set; }

    /// <summary>
    /// 操作描述（自动生成）
    /// </summary>
    public string? Description { get; set; }
}

