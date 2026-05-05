using System;
using System.Threading.Tasks;
using NS.Framework.AspNetCore.Logging;
using NS.Module.Identity.Domain.Identities;
using NS.Module.Identity.Domain.Identities.Repositories;
using NS.Module.Identity.Domain.Shared.Enums;
using NS.Module.Identity.Domain.Shared.Events;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Uow;

namespace NS.Module.Identity.Application.EventHandlers;

/// <summary>
/// 登录尝试事件处理器
/// 异步记录登录日志，与主登录流程解耦
/// </summary>
public class LoginAttemptedEventHandler :
    ILocalEventHandler<LoginAttemptedEvent>,
    ITransientDependency
{
    private readonly ILoginLogRepository _loginLogRepository;
    private readonly IRequestClientInfoResolver _clientInfoResolver;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ILogger<LoginAttemptedEventHandler> _logger;

    public LoginAttemptedEventHandler(
        ILoginLogRepository loginLogRepository,
        IRequestClientInfoResolver clientInfoResolver,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<LoginAttemptedEventHandler> logger)
    {
        _loginLogRepository = loginLogRepository;
        _clientInfoResolver = clientInfoResolver;
        _unitOfWorkManager = unitOfWorkManager;
        _logger = logger;
    }

    public async Task HandleEventAsync(LoginAttemptedEvent evt)
    {
        try
        {
            var now = DateTime.UtcNow;
            var resolved = _clientInfoResolver.Resolve(
                evt.LoginLocation,
                evt.Browser,
                evt.Os,
                evt.NetworkType);

            var status = (LoginStatusEnum)evt.LoginStatus;

            var log = new LoginLog(now);
            log.SetUser(evt.UserId, evt.UserName);
            log.SetLoginType(LoginTypeEnum.UserNamePassword);
            log.SetLoginStatus(status, evt.FailureReason);
            log.SetLoginTime(now);
            log.SetSessionId(evt.SessionId);
            log.SetClientInfo(
                resolved.Ip,
                resolved.Location,
                resolved.UserAgent,
                resolved.Browser,
                resolved.Os,
                evt.DeviceType);
            log.SetClientContext(
                evt.ClientId,
                evt.DeviceId,
                evt.Fingerprint,
                evt.DeviceModel,
                evt.AppVersion,
                evt.AppChannel,
                resolved.NetworkType,
                evt.LoginSource);
            log.SetTraceId(resolved.TraceId);
            log.TenantId = evt.TenantId;

            using (var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false))
            {
                await _loginLogRepository.InsertAsync(log, autoSave: true);
                await uow.CompleteAsync();
            }

            _logger.LogDebug(
                "LoginAttemptedEvent: recorded login log for User={UserName}, Status={Status}",
                evt.UserName, status);
        }
        catch (Exception ex)
        {
            // 日志记录失败不影响主流程
            _logger.LogError(ex,
                "LoginAttemptedEvent: failed to record login log for User={UserName}",
                evt.UserName);
        }
    }
}

