using System;
using System.Threading.Tasks;
using NS.Module.Identity.Application.Contracts.identities.Dtos;
using NS.Module.Identity.Application.Contracts.Users.Dtos;
using Volo.Abp.Application.Services;

namespace NS.Module.Identity.Application.Contracts.Users;

public interface IUserAppService : IApplicationService
{
    /// <summary>
    /// GET /api/app/users/me — 获取当前登录用户信息
    /// </summary>
    Task<AuthUserDto> GetMeAsync();

    /// <summary>
    /// POST /api/app/users
    /// </summary>
    Task<UserDto> CreateAsync(UserCreateDto input);

    /// <summary>
    /// PUT /api/app/users/{id}
    /// </summary>
    Task<UserDto> UpdateAsync(Guid id, UserUpdateDto input);

    /// <summary>
    /// DELETE /api/app/users/{id}
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// GET /api/app/users/{id}
    /// </summary>
    Task<UserDto> GetAsync(Guid id);

    /// <summary>
    /// GET /api/app/users
    /// </summary>
    Task<UserGetListOutputDto> GetListAsync(UserGetListInputDto input);

    /// <summary>
    /// POST /api/app/users/{id}/change-password
    /// </summary>
    Task ChangePasswordAsync(Guid id, ChangePasswordDto input);

    /// <summary>
    /// POST /api/app/users/{id}/reset-password
    /// </summary>
    Task ResetPasswordAsync(Guid id);

    /// <summary>
    /// PUT /api/app/users/{id}/roles
    /// </summary>
    Task AssignRolesAsync(Guid id, AssignRolesDto input);
}