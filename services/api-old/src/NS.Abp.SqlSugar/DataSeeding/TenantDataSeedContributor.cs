using NS.Framework.Authorization.Abstractions.Permissions;
using NS.Framework.SqlSugar.Abstractions;
using NS.Framework.Authentication.Abstractions.Security;
using NS.Module.Identity.Domain.Roles;
using NS.Module.Identity.Domain.Roles.Repositories;
using NS.Module.Identity.Domain.Shared.Enums;
using NS.Module.Identity.Domain.Users;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace NS.Abp.SqlSugar.DataSeeding;

public sealed class TenantDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private const string SeedMarker = "[Seed] TenantDataSeed";
    private const string DefaultAdminPassword = "1q2w3E*";

    private static readonly IReadOnlyList<string> TenantAdminPermissionDefaults = ["*"];

    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISqlSugarRepository<RolePermission, Guid> _rolePermissionRepository;
    private readonly ISqlSugarRepository<UserRole, Guid> _userRoleRepository;
    private readonly ISqlSugarRepository<UserProfile, Guid> _userProfileRepository;
    private readonly IPermissionDefinitionManager _definitionManager;
    private readonly ICurrentTenant _currentTenant;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public TenantDataSeedContributor(
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        ISqlSugarRepository<RolePermission, Guid> rolePermissionRepository,
        ISqlSugarRepository<UserRole, Guid> userRoleRepository,
        ISqlSugarRepository<UserProfile, Guid> userProfileRepository,
        IPermissionDefinitionManager definitionManager,
        ICurrentTenant currentTenant,
        IPasswordHasher passwordHasher,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _userRoleRepository = userRoleRepository;
        _userProfileRepository = userProfileRepository;
        _definitionManager = definitionManager;
        _currentTenant = currentTenant;
        _passwordHasher = passwordHasher;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (!context.TenantId.HasValue)
        {
            return;
        }

        using (_currentTenant.Change(context.TenantId))
        {
            using var uow = _unitOfWorkManager.Begin(isTransactional: false);

            var role = await UpsertRoleAsync(
                roleCode: "TenantAdmin",
                roleName: "租户管理员",
                roleType: RoleTypeEnum.System,
                orderNum: 0,
                isEnabled: true,
                description: $"{SeedMarker} Tenant role");

            await GrantRolePermissionsAsync(role.Id, TenantAdminPermissionDefaults);

            var user = await UpsertUserAsync(
                userName: "tenant.admin",
                email: "tenant.admin@local",
                realName: "租户管理员");

            await EnsureUserRoleAsync(user.Id, role.Id);

            var definedPermissionCodes = GetDefinedPermissionCodes()
                .Append("*")
                .ToList();
            await CleanupRolePermissionsAsync(definedPermissionCodes);

            await uow.CompleteAsync();
        }
    }

    private async Task<Role> UpsertRoleAsync(
        string roleCode,
        string roleName,
        RoleTypeEnum roleType,
        int orderNum,
        bool isEnabled,
        string description)
    {
        var existing = await _roleRepository.FindAsync(x => x.RoleCode == roleCode, false);
        if (existing == null)
        {
            var role = new Role(roleName, roleCode)
            {
                TenantId = _currentTenant.Id
            };

            role.SetRoleType(roleType);
            role.SetOrder(orderNum);
            role.SetDescription(description);
            if (!isEnabled)
            {
                role.Deactivate();
            }

            await _roleRepository.InsertAsync(role, autoSave: true);
            return role;
        }

        var changed = false;
        if (!string.Equals(existing.RoleName, roleName, StringComparison.Ordinal))
        {
            existing.SetRoleName(roleName);
            changed = true;
        }

        if (existing.RoleType != roleType)
        {
            existing.SetRoleType(roleType);
            changed = true;
        }

        if (existing.OrderNum != orderNum)
        {
            existing.SetOrder(orderNum);
            changed = true;
        }

        if (existing.IsActive != isEnabled)
        {
            if (isEnabled)
            {
                existing.Activate();
            }
            else
            {
                existing.Deactivate();
            }
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(existing.Description))
        {
            existing.SetDescription(description);
            changed = true;
        }

        if (changed)
        {
            await _roleRepository.UpdateAsync(existing, autoSave: true);
        }

        return existing;
    }

    private async Task<User> UpsertUserAsync(string userName, string email, string realName)
    {
        var existing = await _userRepository.FindAsync(x => x.UserName == userName, false);
        if (existing == null)
        {
            var user = new User();
            user.SetUserName(userName);
            user.SetPassword(_passwordHasher.Hash(DefaultAdminPassword));
            user.SetEmail(email);
            user.ConfirmEmail();
            user.Activate();

            user = await _userRepository.InsertAsync(user, autoSave: true);
            await UpsertUserProfileAsync(user.Id, realName);

            return user;
        }

        var changed = false;

        if (string.IsNullOrWhiteSpace(existing.PasswordHash))
        {
            existing.SetPassword(_passwordHasher.Hash(DefaultAdminPassword));
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(existing.Email))
        {
            existing.SetEmail(email);
            existing.ConfirmEmail();
            changed = true;
        }

        if (!existing.IsEnabled)
        {
            existing.Activate();
            changed = true;
        }

        if (changed)
        {
            await _userRepository.UpdateAsync(existing, autoSave: true);
        }

        await UpsertUserProfileAsync(existing.Id, realName);
        return existing;
    }

    private async Task UpsertUserProfileAsync(Guid userId, string realName)
    {
        var profile = await _userProfileRepository.FindAsync(
            x => x.UserId == userId,
            includeDetails: false);

        if (profile == null)
        {
            profile = new UserProfile(userId);
            profile.SetNickName(realName);
            profile.SetIntroduction(SeedMarker);
            await _userProfileRepository.InsertAsync(profile, autoSave: true);
            return;
        }

        var changed = false;
        if (string.IsNullOrWhiteSpace(profile.NickName))
        {
            profile.SetNickName(realName);
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(profile.Introduction))
        {
            profile.SetIntroduction(SeedMarker);
            changed = true;
        }

        if (changed)
        {
            await _userProfileRepository.UpdateAsync(profile, autoSave: true);
        }
    }

    private async Task EnsureUserRoleAsync(Guid userId, Guid roleId)
    {
        var exists = await _userRoleRepository.AnyAsync(
            x => x.UserId == userId && x.RoleId == roleId);

        if (exists)
        {
            return;
        }

        var entity = new UserRole(userId, roleId)
        {
            TenantId = _currentTenant.Id
        };

        await _userRoleRepository.InsertAsync(entity, autoSave: true);
    }

    private async Task GrantRolePermissionsAsync(Guid roleId, IReadOnlyList<string> permissionCodes)
    {
        var codes = permissionCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (codes.Count == 0)
        {
            return;
        }

        var existingCodes = await _roleRepository.GetPermissionCodesAsync(roleId);
        var existingSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

        var missing = codes
            .Where(code => !existingSet.Contains(code))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        var entities = missing.Select(code => new RolePermission(roleId, code)
        {
            TenantId = _currentTenant.Id
        }).ToList();

        var db = await _rolePermissionRepository.GetDbContextAsync();
        await db.Client.Insertable(entities).ExecuteCommandAsync();
    }

    private async Task CleanupRolePermissionsAsync(IReadOnlyCollection<string> permissionCodes)
    {
        var permissionSet = new HashSet<string>(permissionCodes, StringComparer.OrdinalIgnoreCase);
        var db = await _rolePermissionRepository.GetDbContextAsync();

        await db.Client.Deleteable<RolePermission>()
            .Where(rp => rp.TenantId == _currentTenant.Id && !permissionSet.Contains(rp.PermissionCode))
            .ExecuteCommandAsync();
    }

    private IReadOnlyCollection<string> GetDefinedPermissionCodes()
        => _definitionManager.GetPermissions()
            .Select(p => p.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

