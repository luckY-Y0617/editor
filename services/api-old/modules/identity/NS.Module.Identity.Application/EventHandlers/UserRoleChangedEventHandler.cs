using System;
using System.Threading.Tasks;
using NS.Module.Identity.Domain.Shared.Events;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace NS.Module.Identity.Application.EventHandlers;

/// <summary>
/// 用户角色变更事件处理器
/// 当用户角色分配变更时，清除该用户的系统权限缓存
/// </summary>
public class UserRoleChangedEventHandler :
    ILocalEventHandler<UserRoleChangedEvent>,
    ITransientDependency
{
    private readonly SystemPermissionStore _systemPermissionStore;
    private readonly ILogger<UserRoleChangedEventHandler> _logger;

    public UserRoleChangedEventHandler(
        SystemPermissionStore systemPermissionStore,
        ILogger<UserRoleChangedEventHandler> logger)
    {
        _systemPermissionStore = systemPermissionStore;
        _logger = logger;
    }

    public async Task HandleEventAsync(UserRoleChangedEvent eventData)
    {
        try
        {
            await _systemPermissionStore.InvalidateAsync(eventData.UserId);

            _logger.LogDebug(
                "UserRoleChangedEvent: cleared system permission cache for User={UserId}",
                eventData.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UserRoleChangedEvent: failed to clear permission cache, User={UserId}",
                eventData.UserId);
        }
    }
}
