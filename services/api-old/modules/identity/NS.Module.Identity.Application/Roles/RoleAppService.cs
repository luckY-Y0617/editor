using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NS.Framework.Authorization.Abstractions.Permissions;
using NS.Framework.Authorization.AspNetCore;
using NS.Module.Identity.Domain.Shared.Authorization;
using NS.Module.Identity.Application.Contracts.Roles;
using NS.Module.Identity.Application.Contracts.Roles.Dtos;
using NS.Module.Identity.Domain.Roles;
using NS.Module.Identity.Domain.Roles.Manager;
using NS.Module.Identity.Domain.Roles.Repositories;
using NS.Module.Identity.Domain.Shared.Enums;
using NS.Module.Identity.Domain.Shared.Errors;
using NS.Module.Identity.Domain.Shared.Events;
using NS.Module.Identity.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Users;

namespace NS.Module.Identity.Application.Roles;

[Authorize]
[ApiController]
[Route("/api/app/roles")]
public class RoleAppService : ApplicationService, IRoleAppService
{
    private readonly RoleManager _roleManager;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILocalEventBus _localEventBus;
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;

    private readonly UserInfoStore _userInfoStore;
    private readonly SystemPermissionStore _systemPermissionStore;
    private readonly ILogger<RoleAppService> _logger;

    public RoleAppService(
        RoleManager roleManager,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        ILocalEventBus localEventBus,
        IPermissionDefinitionManager permissionDefinitionManager,
        UserInfoStore userInfoStore,
        SystemPermissionStore systemPermissionStore,
        ILogger<RoleAppService> logger)
    {
        _roleManager = roleManager;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _localEventBus = localEventBus;
        _permissionDefinitionManager = permissionDefinitionManager;
        _userInfoStore = userInfoStore;
        _systemPermissionStore = systemPermissionStore;
        _logger = logger;
    }

    #region CRUD (REST)

    [HttpPost]
    [RequirePermission(SystemPermissions.Roles.Manage)]
    public async Task<RoleDto> CreateAsync([FromBody] RoleCreateDto input)
    {
        var role = await _roleManager.CreateAsync(input.RoleName, input.RoleCode);

        role.SetDescription(input.Description);

        await _roleRepository.InsertAsync(role, autoSave: true);

        return ObjectMapper.Map<Role, RoleDto>(role);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(SystemPermissions.Roles.Manage)]
    public async Task<RoleDto> UpdateAsync([FromRoute] Guid id, [FromBody] RoleUpdateDto input)
    {
        var role = await _roleRepository.GetAsync(id);

        role.SetRoleName(input.RoleName);
        role.SetDescription(input.Description);

        await _roleRepository.UpdateAsync(role, autoSave: true);

        return ObjectMapper.Map<Role, RoleDto>(role);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(SystemPermissions.Roles.Manage)]
    public async Task DeleteAsync([FromRoute] Guid id)
    {
        await _roleManager.DeleteAsync(id);
        await InvalidateCachesByRoleAsync(id);
    }

    #endregion

    #region Query (REST)

    [HttpGet("{id:guid}")]
    [RequirePermission(SystemPermissions.Roles.View)]
    public async Task<RoleDto> GetAsync([FromRoute] Guid id)
    {
        var role = await _roleRepository.GetAsync(id, includeDetails: true);
        return ObjectMapper.Map<Role, RoleDto>(role);
    }

    [HttpGet]
    [RequirePermission(SystemPermissions.Roles.View)]
    public async Task<PagedResultDto<RoleDto>> GetListAsync([FromQuery] RoleGetListInputDto input)
    {
        var sorting = string.IsNullOrWhiteSpace(input.Sorting)
            ? $"{nameof(Role.CreationTime)} desc"
            : input.Sorting;

        var pageIndex = input.MaxResultCount <= 0
            ? 1
            : (input.SkipCount / input.MaxResultCount) + 1;

        RefAsync<int> totalCount = 0;

        var roles = await (await _roleRepository.GetQueryableAsync())
            .WhereIF(!string.IsNullOrWhiteSpace(input.Filter),
                r => r.RoleName.Contains(input.Filter!) || r.RoleCode.Contains(input.Filter!))
            .Includes(r => r.Permissions)
            .OrderBy(sorting)
            .ToPageListAsync(pageIndex, input.MaxResultCount, totalCount);

        var items = ObjectMapper.Map<List<Role>, List<RoleDto>>(roles);
        return new PagedResultDto<RoleDto>(totalCount, items);
    }

    #endregion

    #region Role Permissions (REST sub-resource)

    [HttpGet("{id:guid}/permissions")]
    [RequirePermission(SystemPermissions.Roles.View)]
    public async Task<List<string>> GetPermissionCodesAsync([FromRoute] Guid id)
    {
        var role = await _roleRepository.GetAsync(id);
        return await _roleRepository.GetPermissionCodesAsync(role.Id);
    }

    [HttpPut("{id:guid}/permissions")]
    [RequirePermission(SystemPermissions.Roles.Manage)]
    public async Task AssignPermissionsAsync([FromRoute] Guid id, [FromBody] AssignPermissionsDto input)
    {
        if (input.PermissionCodes == null)
        {
            throw new BusinessException(IdentityErrorCodes.PermissionCodesRequired, "权限列表不能为空");
        }

        var permissionCodes = input.PermissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (permissionCodes.Count == 0)
        {
            throw new BusinessException(IdentityErrorCodes.PermissionCodesRequired, "权限列表不能为空");
        }

        var invalidCodes = permissionCodes
            .Where(code => _permissionDefinitionManager.GetOrNull(code) == null)
            .ToList();

        if (invalidCodes.Count > 0)
        {
            throw new BusinessException(
                    IdentityErrorCodes.PermissionNotFound,
                    $"权限码无效：{string.Join(", ", invalidCodes)}")
                .WithData("InvalidPermissionCodes", invalidCodes);
        }

        var role = await _roleRepository.GetAsync(id);

        // 2) 系统角色权限不可修改
        if (role.RoleType == RoleTypeEnum.System)
        {
            throw new BusinessException(IdentityErrorCodes.SystemRolePermissionImmutable, "系统角色权限由平台维护，禁止修改");
        }

        // 3) 禁止修改“当前用户所属角色”的权限（防自毁/自举）
        var currentUserId = CurrentUser.GetId();
        var isCurrentUserInRole = await _userRepository.IsUserInRoleAsync(currentUserId, role.Id);

        if (isCurrentUserInRole)
        {
            throw new BusinessException(
                IdentityErrorCodes.CannotModifyOwnRolePermissions,
                "禁止修改当前用户所属角色的权限");
        }

        // 4) 执行权限重建（领域行为）
        await _roleManager.AssignPermissionsAsync(role, permissionCodes);

        // 5) 发布领域事件（权限变更）
        await _localEventBus.PublishAsync(new RolePermissionChangedEvent
        {
            RoleId = role.Id,
            PermissionCodes = permissionCodes
        });

        // 6) 清理角色关联用户缓存
        await InvalidateCachesByRoleAsync(role.Id);

        _logger.LogInformation(
            "角色 {RoleId} 权限已更新，操作者 {OperatorUserId}，权限数 {PermissionCount}",
            role.Id,
            currentUserId,
            permissionCodes.Count);
    }

    #endregion

    #region Cache Invalidation

    private async Task InvalidateCachesByRoleAsync(Guid roleId)
    {
        var dbContext = await _userRepository.GetDbContextAsync();

        var userIds = await dbContext.Client.Queryable<UserRole>()
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync();

        if (userIds.Count == 0)
        {
            _logger.LogDebug(
                "角色 {RoleId} 未关联任何用户，跳过缓存清理",
                roleId);
            return;
        }

        foreach (var userId in userIds)
        {
            // 1) 用户身份缓存
            await _userInfoStore.InvalidateAsync(userId);

            // 2) 系统级权限缓存
            await _systemPermissionStore.InvalidateAsync(userId);
        }

        _logger.LogInformation(
            "角色 {RoleId} 变更，已清理 {Count} 个用户的身份与权限缓存",
            roleId,
            userIds.Count);
    }

    #endregion
}
