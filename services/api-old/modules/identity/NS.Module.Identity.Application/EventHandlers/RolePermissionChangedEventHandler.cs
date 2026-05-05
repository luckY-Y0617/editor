using System;
using System.Threading.Tasks;
using NS.Framework.Authorization.Abstractions.Permissions;
using NS.Module.Identity.Domain.Shared.Events;
using NS.Module.Identity.Domain.Users;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace NS.Module.Identity.Application.EventHandlers;

/// <summary>
/// 角色权限变更事件处理器
/// 当角色的权限码发生变化时，清除所有受影响用户的“系统级权限”缓存
/// </summary>
public class RolePermissionChangedEventHandler :
    ILocalEventHandler<RolePermissionChangedEvent>,
    ITransientDependency
{
    private readonly SystemPermissionStore _systemPermissionStore;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<RolePermissionChangedEventHandler> _logger;

    public RolePermissionChangedEventHandler(
        SystemPermissionStore systemPermissionStore,
        IUserRepository userRepository,
        ILogger<RolePermissionChangedEventHandler> logger)
    {
        _systemPermissionStore = systemPermissionStore;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task HandleEventAsync(RolePermissionChangedEvent eventData)
    {
        try
        {
            var dbContext = await _userRepository.GetDbContextAsync();

            var userIds = await dbContext.Client.Queryable<UserRole>()
                .Where(ur => ur.RoleId == eventData.RoleId)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            if (userIds == null || userIds.Count == 0)
            {
                _logger.LogDebug(
                    "RolePermissionChangedEvent: role {RoleId} has no users, skip cache clear",
                    eventData.RoleId);
                return;
            }

            foreach (var userId in userIds)
            {
                await _systemPermissionStore.InvalidateAsync(userId);
            }

            _logger.LogInformation(
                "RolePermissionChangedEvent: role {RoleId} changed, cleared {UserCount} users' system permission cache",
                eventData.RoleId, userIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "RolePermissionChangedEvent: failed to clear cache for Role={RoleId}",
                eventData.RoleId);
        }
    }
}

