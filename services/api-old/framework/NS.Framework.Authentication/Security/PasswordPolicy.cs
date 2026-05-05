using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NS.Framework.Authentication.Abstractions.Security;

namespace NS.Framework.Authentication.Security;

/// <summary>
/// 默认密码策略实现：
/// - 长度：8-128
/// - 必须包含：大写字母、小写字母、数字、特殊字符
/// - 不允许包含空白字符
/// </summary>
public class PasswordPolicy : IPasswordPolicy
{
    public int MinLength { get; }
    public int MaxLength { get; }
    public bool RequireUppercase { get; }
    public bool RequireLowercase { get; }
    public bool RequireDigit { get; }
    public bool RequireNonAlphanumeric { get; }
    public bool DisallowWhitespace { get; }

    public PasswordPolicy(
        int minLength = 8,
        int maxLength = 128,
        bool requireUppercase = true,
        bool requireLowercase = true,
        bool requireDigit = true,
        bool requireNonAlphanumeric = true,
        bool disallowWhitespace = true)
    {
        if (minLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minLength), "最小长度必须大于 0。");
        }

        if (maxLength < minLength)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength), "最大长度不能小于最小长度。");
        }

        MinLength = minLength;
        MaxLength = maxLength;
        RequireUppercase = requireUppercase;
        RequireLowercase = requireLowercase;
        RequireDigit = requireDigit;
        RequireNonAlphanumeric = requireNonAlphanumeric;
        DisallowWhitespace = disallowWhitespace;
    }

    public PasswordValidationResult Validate(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add("密码不能为空。");
            // 直接返回即可，没必要继续其他检查
            return PasswordValidationResult.Failed(errors.ToArray());
        }

        if (password.Length < MinLength)
        {
            errors.Add($"密码长度不能少于 {MinLength} 个字符。");
        }

        if (password.Length > MaxLength)
        {
            errors.Add($"密码长度不能超过 {MaxLength} 个字符。");
        }

        if (DisallowWhitespace && password.Any(char.IsWhiteSpace))
        {
            errors.Add("密码中不能包含空格或其他空白字符。");
        }

        if (RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add("密码中至少需要包含一个大写字母。");
        }

        if (RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add("密码中至少需要包含一个小写字母。");
        }

        if (RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add("密码中至少需要包含一个数字。");
        }

        if (RequireNonAlphanumeric && !Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
        {
            errors.Add("密码中至少需要包含一个特殊字符（如 !@#$%^&* 等）。");
        }

        if (errors.Count == 0)
        {
            return PasswordValidationResult.Success();
        }

        return PasswordValidationResult.Failed(errors.ToArray());
    }
}
