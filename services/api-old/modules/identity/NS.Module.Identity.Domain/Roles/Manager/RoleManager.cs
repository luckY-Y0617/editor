using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NS.Module.Identity.Domain.Roles.Repositories;
using NS.Module.Identity.Domain.Shared.Errors;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace NS.Module.Identity.Domain.Roles.Manager;

public class RoleManager : DomainService
{
    private readonly IRoleRepository _roleRepository;

    public RoleManager(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public virtual async Task<Role> CreateAsync(
        string roleName,
        string roleCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(roleName))
            throw new BusinessException("角色名不能为空");

        if (string.IsNullOrWhiteSpace(roleCode))
            throw new BusinessException("角色编码不能为空");

        var normalizedCode = roleCode.Trim().ToLowerInvariant();

        var exits = await _roleRepository.AnyAsync(x => x.RoleCode == normalizedCode);

        if (exits)
        {
            throw new BusinessException(IdentityErrorCodes.RoleCodeExists, "角色编码已存在")
                .WithData(nameof(Role.RoleCode), normalizedCode);
        }

        var role = new Role(roleName.Trim(), normalizedCode);

        return role;
    }


    public virtual async Task DeleteAsync(Guid roleId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var role = await _roleRepository.FindAsync(roleId, true, ct);
        if (role == null)
        {
            return;
        }

        await _roleRepository.ResetPermissionsAsync(role.Id, [], ct);

        await _roleRepository.DeleteAsync(roleId, true, ct);
    }

    /// <summary>
    /// 重置角色权限（清空 → 插入）
    /// </summary>
    public virtual async Task AssignPermissionsAsync(
        Role role,
        IEnumerable<string>? permissionCodes,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (role == null)
            throw new ArgumentNullException(nameof(role));
        
        if (permissionCodes == null)
            throw new ArgumentNullException(nameof(permissionCodes));

        var codes = permissionCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        
        await _roleRepository.ResetPermissionsAsync(role.Id, codes, ct);
    }
}
