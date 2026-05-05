using System.Text.Json;
using Northstar.Application.Common;
using Northstar.Contracts.Auth;
using Northstar.Contracts.Common;
using Northstar.Domain.Security;
using Northstar.Domain.Users;

namespace Northstar.Application.Security;

public sealed class AuthMfaService : IAuthMfaService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAuthRepository _authRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthRequestContext _requestContext;
    private readonly IMfaSecretProtector _secretProtector;
    private readonly ITotpService _totpService;
    private readonly IAuthSecurityStateService _securityStateService;
    private readonly IAuthStepUpService _stepUpService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MfaOptions _options;

    public AuthMfaService(
        IAuthRepository authRepository,
        ICurrentUser currentUser,
        IAuthRequestContext requestContext,
        IMfaSecretProtector secretProtector,
        ITotpService totpService,
        IAuthSecurityStateService securityStateService,
        IAuthStepUpService stepUpService,
        IUnitOfWork unitOfWork,
        MfaOptions options)
    {
        _authRepository = authRepository;
        _currentUser = currentUser;
        _requestContext = requestContext;
        _secretProtector = secretProtector;
        _totpService = totpService;
        _securityStateService = securityStateService;
        _stepUpService = stepUpService;
        _unitOfWork = unitOfWork;
        _options = options;
    }

    public async Task<TotpEnrollmentResponse> EnrollTotpAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredUserAsync(cancellationToken);
        var existing = await _authRepository.GetCurrentMfaMethodForUpdateAsync(user.Id, cancellationToken);
        if (existing is not null && existing.IsEnabled)
        {
            throw new ApplicationErrorException(ErrorCodes.Conflict, "TOTP MFA is already enabled.");
        }

        if (existing is not null && existing.IsPending)
        {
            existing.Disable(DateTimeOffset.UtcNow);
        }

        var secret = _totpService.GenerateSecret();
        var method = new UserMfaMethod(
            user.Id,
            MfaMethodTypes.Totp,
            _secretProtector.Protect(secret));
        await _authRepository.AddMfaMethodAsync(method, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TotpEnrollmentResponse(
            method.Id.ToString(),
            secret,
            _totpService.BuildProvisioningUri(_options.Issuer, user.Email ?? user.Id.ToString(), secret));
    }

    public async Task<AuthSecurityStateResponse> VerifyTotpAsync(
        VerifyTotpRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredUserAsync(cancellationToken);
        var method = await _authRepository.GetCurrentMfaMethodForUpdateAsync(user.Id, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.ValidationError, "TOTP MFA enrollment was not found.");
        var now = DateTimeOffset.UtcNow;
        var secret = _secretProtector.Unprotect(method.SecretCiphertext);
        if (!_totpService.VerifyCode(
                secret,
                request.Code,
                now,
                Math.Max(1, _options.TotpStepSeconds),
                Math.Max(0, _options.TotpAllowedSkewSteps)))
        {
            await _authRepository.AddAuthEventAsync(
                CreateAuthEvent(user.Id, "auth.mfa_failed", false),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "TOTP code is invalid.");
        }

        method.MarkVerified(now);
        await _authRepository.AddAuthEventAsync(
            CreateAuthEvent(user.Id, "auth.mfa_verified", true),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await _securityStateService.GetSecurityStateAsync(cancellationToken);
    }

    public async Task<AuthSecurityStateResponse> DisableTotpAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredUserAsync(cancellationToken);
        await _stepUpService.EnsureSatisfiedAsync(cancellationToken);
        var method = await _authRepository.GetCurrentMfaMethodForUpdateAsync(user.Id, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Enabled MFA method was not found.");
        method.Disable(DateTimeOffset.UtcNow);
        await _authRepository.AddAuthEventAsync(
            CreateAuthEvent(user.Id, "auth.mfa_disabled", true),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await _securityStateService.GetSecurityStateAsync(cancellationToken);
    }

    private async Task<User> GetRequiredUserAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Authentication is required.");
        }

        return await _authRepository.FindUserByIdAsync(_currentUser.UserId.Value, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Current user was not found.");
    }

    private AuthEvent CreateAuthEvent(Guid userId, string action, bool succeeded)
    {
        return new AuthEvent(
            userId,
            action,
            succeeded,
            _requestContext.IpAddress,
            _requestContext.UserAgent,
            JsonSerializer.Serialize(new { method = MfaMethodTypes.Totp }, JsonOptions));
    }
}
