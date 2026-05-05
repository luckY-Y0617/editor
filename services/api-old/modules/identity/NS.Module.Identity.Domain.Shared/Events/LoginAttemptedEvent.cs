using System;

namespace NS.Module.Identity.Domain.Shared.Events;

/// <summary>
/// 登录尝试事件（领域事件）
/// 登录成功或失败时发布，由 EventHandler 异步写入日志
/// </summary>
public class LoginAttemptedEvent
{
    // ======================================================
    // 用户信息
    // ======================================================
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public Guid? TenantId { get; set; }

    // ======================================================
    // 登录结果
    // ======================================================

    /// <summary>
    /// 登录状态：0 = Success, 1 = Failed（与 LoginStatusEnum 对应）
    /// </summary>
    public int LoginStatus { get; set; }
    public string? FailureReason { get; set; }

    // ======================================================
    // 客户端信息（前端传入的原始 Hint）
    // ======================================================
    public string? SessionId { get; set; }
    public string? ClientId { get; set; }
    public string? DeviceId { get; set; }
    public string? Fingerprint { get; set; }
    public string? LoginLocation { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceModel { get; set; }
    public string? AppVersion { get; set; }
    public string? AppChannel { get; set; }
    public string? NetworkType { get; set; }
    public string? LoginSource { get; set; }
}

