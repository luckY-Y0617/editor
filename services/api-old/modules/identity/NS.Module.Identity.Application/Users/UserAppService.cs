using NS.Framework.Authentication.Abstractions.Security;
using NS.Framework.Authorization.AspNetCore;
using NS.Module.Identity.Application.Contracts.identities.Dtos;
using NS.Module.Identity.Application.Contracts.Users;
using NS.Module.Identity.Application.Contracts.Users.Dtos;
using NS.Module.Identity.Domain.Shared.Authorization;
using NS.Module.Identity.Domain.Shared.Errors;
using NS.Module.Identity.Domain.Shared.Events;
using NS.Module.Identity.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Threading;
using Volo.Abp.Users;

namespace NS.Module.Identity.Application.Users;

[Authorize]
[ApiController]
[Route("/api/app/users")]
public class UserAppService : ApplicationService, IUserAppService
{
    private readonly UserManager _userManager;
    private readonly IUserRepository _userRepository;
    private readonly ILocalEventBus _localEventBus;
    private readonly UserInfoStore _userInfoStore;
    private readonly IAuthUserProfileProvider _profileProvider;

    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UserAppService> _logger;
    private readonly ICancellationTokenProvider _cancellationTokenProvider;

    private readonly SystemPermissionStore _systemPermissionStore;

    public UserAppService(
        UserManager userManager,
        IUserRepository userRepository,
        ILocalEventBus localEventBus,
        UserInfoStore userInfoStore,
        IAuthUserProfileProvider profileProvider,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser,
        ILogger<UserAppService> logger,
        ICancellationTokenProvider cancellationTokenProvider,
        SystemPermissionStore systemPermissionStore)
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _localEventBus = localEventBus;
        _userInfoStore = userInfoStore;
        _profileProvider = profileProvider;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _logger = logger;
        _cancellationTokenProvider = cancellationTokenProvider;
        _systemPermissionStore = systemPermissionStore;
    }

    #region Create / Update / Delete

    [HttpPost]
    [RequirePermission(SystemPermissions.Users.Manage)]
    public async Task<UserDto> CreateAsync([FromBody] UserCreateDto input)
    {
        var user = await _userManager.CreateAsync(
            input.UserName,
            input.Password,
            input.Email,
            input.PhoneNumber);

        if (input.IsEnabled)
            user.Activate();
        else
            user.Deactivate();

        await _userRepository.InsertAsync(user, autoSave: true);

        var ct = _cancellationTokenProvider.Token;
        var profile = new UserProfile(user.Id);
        var db = await _userRepository.GetDbContextAsync();
        await db.Client.Insertable(profile).ExecuteCommandAsync(ct);

        // 创建时允许分配角色（如果你后续决定“用户编辑不分配角色”，这块也可以拆到 /roles 接口）
        await _userManager.AssignRolesAsync(user, input.RoleIds);

        await InvalidateCachesAsync(user.Id);
        return ObjectMapper.Map<User, UserDto>(await _userRepository.GetAsync(user.Id));
    }


    [HttpPost("{id:guid}/activate")]
    [RequirePermission(SystemPermissions.Users.Manage)]
    public async Task ActivateAsync(Guid id)
    {
        var user = await _userRepository.GetAsync(id);
        
        user.Activate();
        
        await _userRepository.UpdateAsync(user);
    }
    
    [HttpPost("{id:guid}/deactivate")]
    [RequirePermission(SystemPermissions.Users.Manage)]
    public async Task DeActivateAsync(Guid id)
    {
        if (id == CurrentUser.GetId())
        {
            throw new BusinessException("Identity:CannotDeactivateSelf", "不能停用当前登录用户。");
        }
        
        var user = await _userRepository.GetAsync(id);
        
        user.Deactivate();
        await InvalidateCachesAsync(id);
        
        await _userRepository.UpdateAsync(user);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(SystemPermissions.Users.Manage)]
    public async Task<UserDto> UpdateAsync([FromRoute] Guid id, [FromBody] UserUpdateDto input)
    {
        var user = await _userRepository.GetAsync(id);

        var newEmail = input.Email?.Trim();
        var oldEmail = user.Email;

        if (!string.IsNullOrWhiteSpace(newEmail) &&
            !string.Equals(oldEmail, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            var normalizedEmail = newEmail.ToUpperInvariant();

            if (await _userRepository.EmailExistsAsync(normalizedEmail, id))
            {
                throw new BusinessException(
                        IdentityErrorCodes.EmailExists,
                        "邮箱已存在")
                    .WithData(nameof(User.Email), newEmail);
            }
        }

        user.SetEmail(input.Email);
        user.SetPhoneNumber(input.PhoneNumber);
        
        await _userRepository.UpdateAsync(user, autoSave: true);

        await InvalidateCachesAsync(user.Id);

        return ObjectMapper.Map<User, UserDto>(await _userRepository.GetAsync(user.Id));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(SystemPermissions.Users.Manage)]
    public async Task DeleteAsync([FromRoute] Guid id)
    {
        await _userRepository.DeleteAsync(id);
        await InvalidateCachesAsync(id);
    }

    #endregion

    #region Me (当前登录用户)

    /// <summary>
    /// 获取当前登录用户信息（含权限和团队）
    /// GET /api/app/users/me
    /// Admin 和 App 均可调用（Smart Scheme 自动适配 Session / JWT）
    /// </summary>
    [HttpGet("me")]
    public async Task<AuthUserDto> GetMeAsync()
    {
        var userId = CurrentUser.GetId();
        await _profileProvider.WarmupAsync(userId);

        var userDto = await _profileProvider.GetUserAsync(userId);
        userDto.Permissions = await _profileProvider.GetPermissionsAsync(userId);
        userDto.Teams = await _profileProvider.GetUserTeamsAsync(userId);

        return userDto;
    }

    #endregion

    #region Get / List (REST)

    /// <summary>
    /// GET /api/app/users/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(SystemPermissions.Users.View)]
    public async Task<UserDto> GetAsync([FromRoute] Guid id)
    {
        return ObjectMapper.Map<User, UserDto>(await _userRepository.GetAsync(id));
    }

    [HttpGet]
    [RequirePermission(SystemPermissions.Users.View)]
    public async Task<UserGetListOutputDto> GetListAsync([FromQuery] UserGetListInputDto input)
    {
        var query = await _userRepository.GetQueryableAsync();
        var ct = _cancellationTokenProvider.Token;

        var filter = input.Filter?.Trim() ?? string.Empty;

        query = query
            .WhereIF(filter.Length > 0,
                x =>
                    (x.UserName.Contains(filter)) ||
                    (x.Email != null && x.Email.Contains(filter)) ||
                    (x.PhoneNumber != null && x.PhoneNumber.Contains(filter)))
            .Includes(x => x.Profile)
            .Includes(x => x.Roles);

        var totalCount = await query.CountAsync(ct);

        var pageIndex = (input.SkipCount / input.MaxResultCount) + 1;
        var users = await query.ToPageListAsync(pageIndex, input.MaxResultCount, ct);

        var dtos = ObjectMapper.Map<List<User>, List<UserDto>>(users);
        return new UserGetListOutputDto(totalCount, dtos);
    }

    #endregion

    #region Password (REST)

    [HttpPost("{id:guid}/change-password")]
    public async Task ChangePasswordAsync([FromRoute] Guid id, [FromBody] ChangePasswordDto input)
    {
        if (_currentUser.Id != id || input.UserId != id)
        {
            throw new AbpAuthorizationException("只能修改自己的密码");
        }

        var user = await _userRepository.GetAsync(id);

        var verify = _passwordHasher.Verify(user.PasswordHash, input.CurrentPassword);
        if (verify == PasswordVerificationResult.Failed)
        {
            throw new BusinessException(
                IdentityErrorCodes.UserPasswordMismatch,
                "当前密码不正确");
        }

        await _userManager.ChangePasswordAsync(user, input.NewPassword);
        await _userRepository.UpdateAsync(user, autoSave: true);

        await InvalidateCachesAsync(user.Id);
    }

    [HttpPost("{id:guid}/reset-password")]
    [RequirePermission(SystemPermissions.Users.Manage)]
    public async Task ResetPasswordAsync([FromRoute] Guid id)
    {
        var user = await _userRepository.GetAsync(id);

        // TODO: 从配置读取默认密码/随机密码
        await _userManager.ChangePasswordAsync(user, "123456");
        await _userRepository.UpdateAsync(user, autoSave: true);

        await InvalidateCachesAsync(user.Id);
    }

    #endregion

    #region Roles (REST sub-resource)

    [HttpPut("{id:guid}/roles")]
    [RequirePermission(SystemPermissions.Users.Manage)]
    public async Task AssignRolesAsync([FromRoute] Guid id, [FromBody] AssignRolesDto input)
    {
        // REST：以 route id 为准
        input.UserId = id;

        var user = await _userRepository.GetAsync(input.UserId);

        await _userManager.AssignRolesAsync(user, input.RoleIds);

        await _localEventBus.PublishAsync(new UserRoleChangedEvent
        {
            UserId = user.Id,
            RoleIds = input.RoleIds
        });

        await InvalidateCachesAsync(user.Id);
    }

    #endregion

    #region Internal Helpers

    private async Task InvalidateCachesAsync(Guid userId)
    {
        var ct = _cancellationTokenProvider.Token;

        await _userInfoStore.InvalidateAsync(userId, ct);
        await _systemPermissionStore.InvalidateAsync(userId, ct);

        _logger.LogDebug("User caches invalidated. UserId={UserId}", userId);
    }

    #endregion
}
