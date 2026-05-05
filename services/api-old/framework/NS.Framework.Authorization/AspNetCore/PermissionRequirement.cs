using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace NS.Framework.Authorization.AspNetCore;

/// <summary>
/// 权限授权需求：
/// - Single：单权限（perm:{code}）
/// - All：必须同时具备（AND）（perm:all:code1,code2）
/// - Any：具备任意一个即可（OR）（perm:any:code1,code2）
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// 基础前缀：用于区分 “权限型 policy” 与传统 policy
    /// </summary>
    public const string PolicyPrefix = "perm:";

    /// <summary>AND 前缀：perm:all:code1,code2</summary>
    public const string PolicyAllPrefix = "perm:all:";

    /// <summary>OR 前缀：perm:any:code1,code2</summary>
    public const string PolicyAnyPrefix = "perm:any:";

    /// <summary>
    /// 多权限的分隔符（PolicyName 内部使用）
    /// </summary>
    public const char MultiSeparator = ',';

    /// <summary>需求模式：Single / All / Any</summary>
    public PermissionRequirementMode Mode { get; }

    /// <summary>权限码列表（至少 1 个）</summary>
    public IReadOnlyList<string> PermissionCodes { get; }

    /// <summary>规范化权限码列表（建议用于比较）</summary>
    public IReadOnlyList<string> NormalizedPermissionCodes { get; }

    /// <summary>
    /// 单权限兼容属性：
    /// - 仅当 Mode == Single 时可用
    /// </summary>
    public string PermissionCode =>
        Mode == PermissionRequirementMode.Single
            ? PermissionCodes[0]
            : throw new InvalidOperationException(
                $"This requirement is '{Mode}', use '{nameof(PermissionCodes)}' instead of '{nameof(PermissionCode)}'.");

    /// <summary>
    /// 单权限兼容属性：
    /// - 仅当 Mode == Single 时可用
    /// </summary>
    public string NormalizedPermissionCode =>
        Mode == PermissionRequirementMode.Single
            ? NormalizedPermissionCodes[0]
            : throw new InvalidOperationException(
                $"This requirement is '{Mode}', use '{nameof(NormalizedPermissionCodes)}' instead of '{nameof(NormalizedPermissionCode)}'.");

    /// <summary>
    /// 构造单权限 requirement（等价 Mode=Single）
    /// </summary>
    public PermissionRequirement(
        string permissionCode)
        : this(PermissionRequirementMode.Single, new[] { permissionCode })
    {
    }

    /// <summary>
    /// 构造多权限 requirement（All/Any/Single 都可）
    /// </summary>
    public PermissionRequirement(
        PermissionRequirementMode mode,
        IEnumerable<string> permissionCodes)
    {
        if (permissionCodes == null)
        {
            throw new ArgumentNullException(nameof(permissionCodes));
        }

        var codes = permissionCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (codes.Length == 0)
        {
            throw new ArgumentException("Permission codes cannot be null or empty.", nameof(permissionCodes));
        }

        Mode = mode;
        PermissionCodes = codes;
        NormalizedPermissionCodes = codes.Select(Normalize).ToArray();
    }

    /// <summary>
    /// 构造 policyName：perm:{permissionCode}
    /// </summary>
    public static string BuildPolicyName(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            throw new ArgumentException("Permission code cannot be null or whitespace.", nameof(permissionCode));
        }

        return $"{PolicyPrefix}{permissionCode.Trim()}";
    }

    /// <summary>
    /// 构造 AND policyName：perm:all:code1,code2
    /// </summary>
    public static string BuildAllPolicyName(params string[] permissionCodes)
        => BuildMultiPolicyName(PermissionRequirementMode.All, permissionCodes);

    /// <summary>
    /// 构造 OR policyName：perm:any:code1,code2
    /// </summary>
    public static string BuildAnyPolicyName(params string[] permissionCodes)
        => BuildMultiPolicyName(PermissionRequirementMode.Any, permissionCodes);

    private static string BuildMultiPolicyName(
        PermissionRequirementMode mode,
        IEnumerable<string> permissionCodes)
    {
        if (permissionCodes == null)
        {
            throw new ArgumentNullException(nameof(permissionCodes));
        }

        var codes = permissionCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (codes.Length == 0)
        {
            throw new ArgumentException("Permission codes cannot be null or empty.", nameof(permissionCodes));
        }

        return mode switch
        {
            PermissionRequirementMode.Single => BuildPolicyName(codes[0]),
            PermissionRequirementMode.All => $"{PolicyAllPrefix}{string.Join(MultiSeparator, codes)}",
            PermissionRequirementMode.Any => $"{PolicyAnyPrefix}{string.Join(MultiSeparator, codes)}",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported permission requirement mode.")
        };
    }

    /// <summary>
    /// 兼容旧签名：只解析单权限 perm:{code}
    /// - 如果传入的是 perm:all: / perm:any: 则返回 false
    /// </summary>
    public static bool TryParsePolicyName(string policyName, out string permissionCode)
    {
        permissionCode = string.Empty;

        if (!TryParsePolicyName(policyName, out var mode, out var codes))
        {
            return false;
        }

        if (mode != PermissionRequirementMode.Single || codes.Length != 1)
        {
            return false;
        }

        permissionCode = codes[0];
        return true;
    }

    /// <summary>
    /// 解析 policyName：
    /// - perm:{code} => mode=Single, codes=[code]
    /// - perm:all:code1,code2 => mode=All
    /// - perm:any:code1,code2 => mode=Any
    /// </summary>
    public static bool TryParsePolicyName(
        string policyName,
        out PermissionRequirementMode mode,
        out string[] permissionCodes)
    {
        mode = PermissionRequirementMode.Single;
        permissionCodes = Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(policyName))
        {
            return false;
        }

        var s = policyName.Trim();

        // 注意：必须先匹配更具体的前缀（all/any），否则会被 perm: 吞掉
        if (s.StartsWith(PolicyAllPrefix, StringComparison.OrdinalIgnoreCase))
        {
            mode = PermissionRequirementMode.All;
            return TryParseMultiCodes(s.Substring(PolicyAllPrefix.Length), out permissionCodes);
        }

        if (s.StartsWith(PolicyAnyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            mode = PermissionRequirementMode.Any;
            return TryParseMultiCodes(s.Substring(PolicyAnyPrefix.Length), out permissionCodes);
        }

        if (s.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var code = s.Substring(PolicyPrefix.Length).Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            mode = PermissionRequirementMode.Single;
            permissionCodes = new[] { code };
            return true;
        }

        return false;
    }

    private static bool TryParseMultiCodes(string raw, out string[] codes)
    {
        codes = Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var arr = raw
            .Split(MultiSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (arr.Length == 0)
        {
            return false;
        }

        codes = arr;
        return true;
    }

    private static string Normalize(string permissionCode)
        => permissionCode.Trim().ToLowerInvariant();

    public override string ToString()
    {
        var codes = string.Join(",", PermissionCodes);

        return $"{nameof(PermissionRequirement)}(Mode={Mode}, Codes=[{codes}])";
    }
}

/// <summary>
/// 权限需求模式
/// </summary>
public enum PermissionRequirementMode
{
    /// <summary>单权限（perm:{code}）</summary>
    Single = 0,

    /// <summary>必须同时具备（AND）（perm:all:code1,code2）</summary>
    All = 1,

    /// <summary>具备任意一个即可（OR）（perm:any:code1,code2）</summary>
    Any = 2
}
