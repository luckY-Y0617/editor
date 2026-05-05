using System;
using System.Threading.Tasks;
using NS.Framework.Authentication;
using NS.Framework.Authentication.Abstractions.Http;
using NS.Framework.Authentication.Session;
using NS.Framework.Core.Abstractions.Time;
using NS.Module.Identity.Application.Contracts.identities.Dtos;
using NS.Module.Identity.Application.Contracts.Users;
using NS.Module.Identity.Domain.Shared.Enums;
using NS.Module.Identity.Domain.Shared.Errors;
using NS.Module.Identity.Domain.Shared.Events;
using NS.Module.Identity.Domain.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;

namespace NS.Module.Identity.Application.Identitys;

[ApiController]
[Route("/api/admin/auth")]
public class AdminAuthAppService : ApplicationService
{
    // ======================================================
    // 依赖注入
    // ======================================================
    private readonly IUserRepository _userRepository;
    private readonly IAuthUserProfileProvider _profileProvider;
    private readonly IAuthCookieManager _cookieManager;
    private readonly ISystemClock _clock;
    private readonly AuthOptions _authOptions;
    private readonly ILocalEventBus _localEventBus;
    private readonly ISessionStore _sessionStore;

    public AdminAuthAppService(
        IUserRepository userRepository,
        IAuthUserProfileProvider profileProvider,
        IAuthCookieManager cookieManager,
        ISystemClock clock,
        IOptions<AuthOptions> authOptions,
        ILocalEventBus localEventBus,
        ISessionStore sessionStore)
    {
        _userRepository = userRepository;
        _profileProvider = profileProvider;
        _cookieManager = cookieManager;
        _clock = clock;
        _authOptions = authOptions.Value;
        _localEventBus = localEventBus;
        _sessionStore = sessionStore;
    }

    [HttpPost("login")]
    public async Task<AdminSessionLoginOutputDto> LoginAsync(LoginInputDto input)
    {
        User? user = null;
        try
        {
            user = await _userRepository.FindAndVerifyAsync(input.UserName, input.Password);
            if (user.IsLockedOut())
                throw new BusinessException(IdentityErrorCodes.AccountDisabled, "账号已被禁用或锁定");

            var now = _clock.UtcNow;
            var expiresAt = now.Add(_authOptions.Session.Ttl);

            var payload = new SessionPayload
            {
                UserId = user.Id,
                TenantId = CurrentTenant.Id,
                UserName = user.UserName,
                Roles = [],
                IssuedAtUtc = now,
                ExpiresAtUtc = expiresAt
            };

            var sid = await _sessionStore.CreateAsync(payload);

            _cookieManager.SetAdminSessionId(sid, expiresAt);

            await _profileProvider.WarmupAsync(user.Id);

            await PublishLoginAttemptedAsync(user.Id, user.UserName, LoginStatusEnum.Success, null, input);

            return new AdminSessionLoginOutputDto
            {
                ExpireAtUtc = expiresAt
            };
        }
        catch (Exception ex)
        {
            await PublishLoginAttemptedAsync(
                user?.Id,
                user?.UserName ?? input.UserName,
                LoginStatusEnum.Failed,
                ex.Message,
                input);
            throw;
        }
    }

    [HttpPost("logout")]
    public async Task LogoutAsync()
    {
        _cookieManager.ClearAdminSessionId();

        if (CurrentUser.Id.HasValue)
        {
            await _profileProvider.InvalidateAsync(CurrentUser.Id.Value);
        }
    }

    private async Task PublishLoginAttemptedAsync(
        Guid? userId,
        string? userName,
        LoginStatusEnum status,
        string? failureReason,
        LoginInputDto input)
    {
        await _localEventBus.PublishAsync(new LoginAttemptedEvent
        {
            UserId = userId,
            UserName = userName,
            TenantId = CurrentTenant.Id,
            LoginStatus = (int)status,
            FailureReason = failureReason,
            SessionId = input.SessionId,
            ClientId = input.ClientId,
            DeviceId = input.DeviceId,
            Fingerprint = input.Fingerprint,
            LoginLocation = input.LoginLocation,
            Browser = input.Browser,
            Os = input.Os,
            DeviceType = input.DeviceType,
            DeviceModel = input.DeviceModel,
            AppVersion = input.AppVersion,
            AppChannel = input.AppChannel,
            NetworkType = input.NetworkType,
            LoginSource = input.LoginSource
        });
    }
}
