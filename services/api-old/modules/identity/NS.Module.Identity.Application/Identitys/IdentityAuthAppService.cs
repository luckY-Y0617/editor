using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NS.Framework.Authentication;
using NS.Framework.Authentication.Abstractions.Http;
using NS.Framework.Authentication.Token;
using NS.Framework.AspNetCore.Logging;
using NS.Framework.Core.Abstractions.Time;
using NS.Module.Identity.Application.Contracts.identities.Dtos;
using NS.Module.Identity.Application.Contracts.Users;
using NS.Module.Identity.Domain.Identities;
using NS.Module.Identity.Domain.Identities.Repositories;
using NS.Module.Identity.Domain.Shared.Enums;
using NS.Module.Identity.Domain.Shared.Errors;
using NS.Module.Identity.Domain.Shared.Events;
using NS.Module.Identity.Domain.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Security.Claims;

namespace NS.Module.Identity.Application.Identitys;

[ApiController]
[Route("/api/app/auth")]
public class IdentityAuthAppService : ApplicationService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthUserProfileProvider _profileProvider;
    private readonly IAuthCookieManager _cookieManager;
    private readonly ISystemClock _clock;
    private readonly AuthOptions _authOptions;
    private readonly ILocalEventBus _localEventBus;
    private readonly TokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IRequestClientInfoResolver _clientInfoResolver;

    public IdentityAuthAppService(
        IUserRepository userRepository,
        IAuthUserProfileProvider profileProvider,
        IAuthCookieManager cookieManager,
        ISystemClock clock,
        IOptions<AuthOptions> authOptions,
        ILocalEventBus localEventBus,
        TokenService tokenService,
        IRefreshTokenRepository refreshTokenRepo,
        IRequestClientInfoResolver clientInfoResolver)
    {
        _userRepository = userRepository;
        _profileProvider = profileProvider;
        _cookieManager = cookieManager;
        _clock = clock;
        _authOptions = authOptions.Value;
        _localEventBus = localEventBus;
        _tokenService = tokenService;
        _refreshTokenRepo = refreshTokenRepo;
        _clientInfoResolver = clientInfoResolver;
    }

    [HttpPost("login")]
    public async Task<AppLoginOutputDto> LoginAsync(LoginInputDto input)
    {
        User? user = null;
        try
        {
            user = await _userRepository.FindAndVerifyAsync(input.UserName, input.Password);
            if (user.IsLockedOut())
                throw new BusinessException(IdentityErrorCodes.AccountDisabled, "账号已被禁用或锁定");

            var now = _clock.UtcNow;
            var tenantId = CurrentTenant.Id;

            var claims = BuildClaimsMinimal(user.Id, tenantId);
            var accessToken = await _tokenService.GenerateAccessTokenAsync(claims);

            var refreshPlain = _tokenService.GenerateRefreshTokenPlain();
            var refreshHash = _tokenService.HashRefreshToken(refreshPlain);
            var refreshExpiresAt = now.Add(_authOptions.Token.Lifetime);

            var familyId = Guid.NewGuid();
            var refreshId = Guid.NewGuid();

            var (ip, ua) = _clientInfoResolver.GetBasicInfo();

            var entity = new RefreshToken(
                id: refreshId,
                userId: user.Id,
                tokenFamilyId: familyId,
                tokenHash: refreshHash,
                issuedAt: now,
                expiresAt: refreshExpiresAt,
                parentTokenId: null);

            entity.TenantId = tenantId;
            entity.SetClientContext(
                sessionId: input.SessionId,
                clientId: input.ClientId,
                deviceId: input.DeviceId,
                fingerprint: input.Fingerprint,
                createdByIp: ip,
                createdByUserAgent: ua);

            await _refreshTokenRepo.InsertAsync(entity, autoSave: true);

            _cookieManager.SetRefreshToken(refreshPlain, refreshExpiresAt);

            await _profileProvider.WarmupAsync(user.Id);

            var userDto = await _profileProvider.GetUserAsync(user.Id);

            await PublishLoginAttemptedAsync(user.Id, user.UserName, LoginStatusEnum.Success, null, input);

            return new AppLoginOutputDto
            {
                AccessToken = accessToken,
                AccessTokenExpiresAtUtc = now.Add(_authOptions.Jwt.AccessTokenLifetime),
                User = userDto
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

    [HttpPut("refresh")]
    public async Task<AppRefreshOutputDto> RefreshAsync()
    {
        var now = _clock.UtcNow;
        var tenantId = CurrentTenant.Id;

        var oldPlain = _cookieManager.GetRequiredRefreshToken();
        var oldHash = _tokenService.HashRefreshToken(oldPlain);

        var oldEntity = await _refreshTokenRepo.FindByTokenHashAsync(oldHash);
        if (oldEntity == null || oldEntity.TenantId != tenantId)
            throw new AbpAuthorizationException("Invalid refresh token.");

        // 过期/撤销：如果疑似重放（已 Rotated）=> family 一锅端撤销
        if (!oldEntity.IsActive(now))
        {
            var reuseLike = oldEntity.ReplacedByTokenId.HasValue ||
                            string.Equals(oldEntity.RevokedReason, "Rotated", StringComparison.OrdinalIgnoreCase);

            var (ip0, ua0) = _clientInfoResolver.GetBasicInfo();

            if (reuseLike)
            {
                await _refreshTokenRepo.RevokeFamilyAsync(
                    oldEntity.TokenFamilyId,
                    revokedAt: now,
                    reason: "ReuseDetected",
                    revokedByIp: ip0,
                    revokedByUserAgent: ua0);
            }

            throw new AbpAuthorizationException("Refresh token expired/revoked.");
        }

        // 预生成新 token
        var newId = Guid.NewGuid();
        var newPlain = _tokenService.GenerateRefreshTokenPlain();
        var newHash = _tokenService.HashRefreshToken(newPlain);

        var refreshExpiresAt = now.Add(_authOptions.Token.Lifetime);

        var (ip, ua) = _clientInfoResolver.GetBasicInfo();

        // ✅ CAS 原子标记 Rotated（并发只有一个成功）
        var rotated = await _refreshTokenRepo.TryMarkRotatedAsync(
            tokenId: oldEntity.Id,
            expectedConcurrencyStamp: oldEntity.ConcurrencyStamp,
            replacedByTokenId: newId,
            now: now,
            reason: "Rotated",
            revokedByIp: ip,
            revokedByUserAgent: ua);

        if (!rotated)
        {
            // 并发输了 or 已被使用：按安全策略一锅端
            await _refreshTokenRepo.RevokeFamilyAsync(
                oldEntity.TokenFamilyId,
                revokedAt: now,
                reason: "ReuseDetected",
                revokedByIp: ip,
                revokedByUserAgent: ua);

            throw new AbpAuthorizationException("Refresh token reuse detected.");
        }

        // 插入新 token（同一 family）
        var newEntity = new RefreshToken(
            id: newId,
            userId: oldEntity.UserId,
            tokenFamilyId: oldEntity.TokenFamilyId,
            tokenHash: newHash,
            issuedAt: now,
            expiresAt: refreshExpiresAt,
            parentTokenId: oldEntity.Id);

        newEntity.TenantId = tenantId;

        // 客户端上下文延续旧 token
        newEntity.SetClientContext(
            sessionId: oldEntity.SessionId,
            clientId: oldEntity.ClientId,
            deviceId: oldEntity.DeviceId,
            fingerprint: oldEntity.Fingerprint,
            createdByIp: ip,
            createdByUserAgent: ua);

        await _refreshTokenRepo.InsertAsync(newEntity, autoSave: true);

        // 重写 refresh cookie
        _cookieManager.SetRefreshToken(newPlain, refreshExpiresAt);

        // 换发 access（最小 claims）
        var claims = BuildClaimsMinimal(oldEntity.UserId, tenantId);
        var accessToken = await _tokenService.GenerateAccessTokenAsync(claims);

        return new AppRefreshOutputDto
        {
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = now.Add(_authOptions.Jwt.AccessTokenLifetime)
        };
    }

    [HttpPost("logout")]
    public async Task LogoutAsync(string reason = "Logout")
    {
        var now = _clock.UtcNow;
        var tenantId = CurrentTenant.Id;

        var (ip, ua) = _clientInfoResolver.GetBasicInfo();

        var oldPlain = _cookieManager.TryGetRefreshToken();
        if (!string.IsNullOrWhiteSpace(oldPlain))
        {
            var hash = _tokenService.HashRefreshToken(oldPlain);
            var entity = await _refreshTokenRepo.FindByTokenHashAsync(hash);
            if (entity != null && entity.TenantId == tenantId)
            {
                await _refreshTokenRepo.RevokeFamilyAsync(
                    entity.TokenFamilyId,
                    revokedAt: now,
                    reason: reason,
                    revokedByIp: ip,
                    revokedByUserAgent: ua);

                await _profileProvider.InvalidateAsync(entity.UserId);
            }
        }

        _cookieManager.ClearRefreshToken();
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

    private static List<Claim> BuildClaimsMinimal(Guid userId, Guid? tenantId)
    {
        var claims = new List<Claim>
        {
            new(AbpClaimTypes.UserId, userId.ToString()),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("uid", userId.ToString())
        };

        if (tenantId.HasValue)
        {
            claims.Add(new Claim(AbpClaimTypes.TenantId, tenantId.Value.ToString()));
            claims.Add(new Claim("tid", tenantId.Value.ToString()));
        }

        return claims;
    }
}
