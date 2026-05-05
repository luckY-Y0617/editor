using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.Authentication.Abstractions.Security;
using NS.Module.Identity.Domain.Shared.Errors;
using Microsoft.Extensions.Logging;
using NS.Module.Identity.Domain.Roles.Repositories;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.MultiTenancy;

namespace NS.Module.Identity.Domain.Users;

public class UserManager : DomainService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordPolicy _passwordPolicy;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<UserManager> _logger;

    public UserManager(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IPasswordPolicy passwordPolicy,
        ICurrentTenant currentTenant,
        ILogger<UserManager> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _passwordPolicy = passwordPolicy;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    /// <summary>
    /// 创建用户（仅构造领域对象 + 进行领域规则校验，不落库）
    /// </summary>
    public virtual async Task<User> CreateAsync(
        string userName,
        string password,
        string? email,
        string? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 这些属于领域规则（非 DTO 校验）
        if (string.IsNullOrWhiteSpace(userName))
            throw new BusinessException(IdentityErrorCodes.UserNameRequired);

        if (string.IsNullOrWhiteSpace(password))
            throw new BusinessException(IdentityErrorCodes.PasswordRequired);

        await EnsureUserNameNotExistsAsync(userName, null, cancellationToken);
        await EnsureEmailNotExistsAsync(email, null, cancellationToken);

        ValidatePassword(password);
        var hashedPassword = _passwordHasher.Hash(password);

        var user = new User(userName, hashedPassword);

        // entity 内部已保证 SetEmail / SetPhoneNumber 的领域合法性
        user.SetEmail(email);
        user.SetPhoneNumber(phoneNumber);

        return user;
    }

    /// <summary>
    /// 更新用户基本信息（领域规则：唯一性 + 实体内部校验）
    /// </summary>
    public virtual async Task UpdateAsync(
        User user,
        string userName,
        string? email,
        string? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (user == null)
            throw new BusinessException(IdentityErrorCodes.UserNotFound);

        if (string.IsNullOrWhiteSpace(userName))
            throw new BusinessException(IdentityErrorCodes.UserNameRequired);

        await EnsureUserNameNotExistsAsync(userName, user.Id, cancellationToken);
        await EnsureEmailNotExistsAsync(email, user.Id, cancellationToken);

        user.SetUserName(userName);
        user.SetEmail(email);
        user.SetPhoneNumber(phoneNumber);
    }

    /// <summary>
    /// 修改密码（严格使用领域规则）
    /// </summary>
    public virtual Task ChangePasswordAsync(
        User user,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (user == null)
            throw new BusinessException(IdentityErrorCodes.UserNotFound);

        if (string.IsNullOrWhiteSpace(newPassword))
            throw new BusinessException(IdentityErrorCodes.PasswordRequired);

        var errors = ValidatePassword(newPassword);

        if (errors.Any())
        {
            throw new BusinessException("Identity:PasswordPolicyViolation")
                .WithData("Errors", errors);
        }

        var hashedPassword = _passwordHasher.Hash(newPassword);
        user.SetPassword(hashedPassword);

        return Task.CompletedTask;
    }


    /// <summary>
    /// 重置用户角色（清空并重新绑定）
    /// </summary>
    public virtual async Task AssignRolesAsync(
        User user,
        IEnumerable<Guid>? roleIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (user == null)
            throw new BusinessException(IdentityErrorCodes.UserNotFound);

        var normalizedRoleIds = NormalizeRoleIds(roleIds);

        // 校验角色合法性是业务规则 → 必须在领域层
        if (normalizedRoleIds.Count > 0)
        {
            var roles = await _roleRepository.GetListAsync(
                predicate: r => normalizedRoleIds.Contains(r.Id),
                includeDetails: false,
                cancellationToken: cancellationToken
            );

            if (roles.Count != normalizedRoleIds.Count)
            {
                var missing = normalizedRoleIds.Except(roles.Select(r => r.Id)).ToList();
                throw new BusinessException(IdentityErrorCodes.RoleNotFound)
                    .WithData("RoleIds", string.Join(",", missing));
            }
        }

        await _userRepository.ResetRolesAsync(
            userId: user.Id,
            tenantId: user.TenantId ?? _currentTenant.Id,
            roleIds: normalizedRoleIds,
            cancellationToken: cancellationToken);
    }

    #region Helper Methods (领域规则校验)

    private async Task EnsureUserNameNotExistsAsync(
        string userName, Guid? excludeUserId, CancellationToken cancellationToken)
    {
        var normalized = userName.ToUpperInvariant();

        if (await _userRepository.UserNameExistsAsync(normalized, excludeUserId, cancellationToken))
        {
            throw new BusinessException(IdentityErrorCodes.UserNameExists);
        }
    }

    private async Task EnsureEmailNotExistsAsync(
        string? email, Guid? excludeUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        var normalized = email.ToUpperInvariant();

        if (await _userRepository.EmailExistsAsync(normalized, excludeUserId, cancellationToken))
        {
            throw new BusinessException(IdentityErrorCodes.EmailExists);
        }
    }

    protected virtual List<string> ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (password.Length < 8)
            errors.Add("密码长度不能少于 8 个字符。");

        if (!password.Any(char.IsUpper))
            errors.Add("密码必须包含大写字母。");

        if (!password.Any(char.IsLower))
            errors.Add("密码必须包含小写字母。");

        if (!password.Any(char.IsDigit))
            errors.Add("密码必须包含数字。");

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            errors.Add("密码必须包含特殊字符。");

        return errors;
    }


    private static IReadOnlyCollection<Guid> NormalizeRoleIds(IEnumerable<Guid>? roleIds)
        => roleIds?.Where(id => id != Guid.Empty).Distinct().ToArray()
           ?? Array.Empty<Guid>();

    #endregion
}
