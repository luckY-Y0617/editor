using Northstar.Application.Common;
using Northstar.Contracts.Auth;
using Northstar.Contracts.Common;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class AuthSecurityStateService : IAuthSecurityStateService
{
    private static readonly string[] RecentAuthActions =
    [
        "auth.login",
        "auth.register",
        "auth.idp_login",
        "auth.mfa_verified"
    ];

    private static readonly string[] StepUpActions = ["auth.mfa_verified"];

    private readonly IAuthRepository _authRepository;
    private readonly ICurrentUser _currentUser;
    private readonly MfaOptions _options;

    public AuthSecurityStateService(
        IAuthRepository authRepository,
        ICurrentUser currentUser,
        MfaOptions options)
    {
        _authRepository = authRepository;
        _currentUser = currentUser;
        _options = options;
    }

    public async Task<AuthSecurityStateResponse> GetSecurityStateAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Authentication is required.");
        }

        var user = await _authRepository.FindUserByIdAsync(_currentUser.UserId.Value, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Current user was not found.");

        var latestRecentAuthEvent = await _authRepository.GetLatestSuccessfulAuthEventAsync(
            user.Id,
            RecentAuthActions,
            cancellationToken);
        var recentAuthAt = latestRecentAuthEvent?.CreatedAt;
        var windowMinutes = EffectiveWindowMinutes();
        var hasRecentAuth = recentAuthAt is not null &&
            recentAuthAt.Value >= DateTimeOffset.UtcNow.AddMinutes(-windowMinutes);

        var mfaMethod = await _authRepository.GetCurrentMfaMethodAsync(user.Id, cancellationToken);
        var mfaEnabled = mfaMethod?.IsEnabled == true;
        var latestMfaVerifiedEvent = await _authRepository.GetLatestSuccessfulAuthEventAsync(
            user.Id,
            StepUpActions,
            cancellationToken);
        var mfaVerifiedAt = latestMfaVerifiedEvent?.CreatedAt;
        var mfaVerified = mfaEnabled &&
            mfaVerifiedAt is not null &&
            mfaVerifiedAt.Value >= DateTimeOffset.UtcNow.AddMinutes(-windowMinutes);

        return new AuthSecurityStateResponse(
            user.Id.ToString(),
            recentAuthAt,
            windowMinutes,
            hasRecentAuth,
            mfaEnabled,
            mfaVerified,
            mfaVerifiedAt,
            mfaEnabled && !mfaVerified,
            mfaEnabled ? [MfaMethodTypes.Totp] : []);
    }

    private int EffectiveWindowMinutes()
    {
        return Math.Max(1, _options.StepUpWindowMinutes);
    }
}
