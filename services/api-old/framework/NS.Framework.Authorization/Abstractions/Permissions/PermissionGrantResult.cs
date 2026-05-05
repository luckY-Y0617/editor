using System;
using System.Collections.Generic;

namespace NS.Framework.Authorization.Abstractions.Permissions;

/// <summary>
/// 权限授予检查结果
/// </summary>
public enum PermissionGrantResult
{
    /// <summary>
    /// 未定义 - Provider 不处理此权限，继续下一个 Provider
    /// </summary>
    Undefined = 0,

    /// <summary>
    /// 已授予 - 明确授予权限
    /// </summary>
    Granted = 1,

    /// <summary>
    /// 已禁止 - 明确拒绝权限（优先级最高，短路）
    /// </summary>
    Prohibited = 2
}

/// <summary>
/// 权限检查上下文
/// </summary>
public sealed class PermissionCheckContext
{
    public Guid UserId { get; init; }
    public string PermissionCode { get; init; } = string.Empty;

    /// <summary>
    /// 额外上下文数据（可扩展）
    /// </summary>
    public IDictionary<string, object>? ExtraProperties { get; init; }
}

/// <summary>
/// 权限检查结果（含溯源信息）
/// </summary>
public sealed class PermissionCheckResult
{
    public PermissionGrantResult Result { get; init; }

    /// <summary>
    /// 返回此结果的 Provider 名称
    /// </summary>
    public string? ProviderName { get; init; }

    /// <summary>
    /// 附加说明（用于调试）
    /// </summary>
    public string? Reason { get; init; }

    public static PermissionCheckResult Undefined(string providerName)
        => new() { Result = PermissionGrantResult.Undefined, ProviderName = providerName };

    public static PermissionCheckResult Granted(string providerName, string? reason = null)
        => new() { Result = PermissionGrantResult.Granted, ProviderName = providerName, Reason = reason };

    public static PermissionCheckResult Prohibited(string providerName, string? reason = null)
        => new() { Result = PermissionGrantResult.Prohibited, ProviderName = providerName, Reason = reason };
}
