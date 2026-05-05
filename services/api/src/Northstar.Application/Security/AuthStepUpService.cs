using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class AuthStepUpService : IAuthStepUpService
{
    private static readonly string[] StepUpActions = ["auth.mfa_verified"];

    private readonly IAuthRepository _authRepository;
    private readonly ICurrentUser _currentUser;
    private readonly MfaOptions _options;

    public AuthStepUpService(
        IAuthRepository authRepository,
        ICurrentUser currentUser,
        MfaOptions options)
    {
        _authRepository = authRepository;
        _currentUser = currentUser;
        _options = options;
    }

    public async Task EnsureSatisfiedAsync(CancellationToken cancellationToken = default)
    {
        var userId = await GetRequiredUserIdAsync(cancellationToken);
        var mfaMethod = await _authRepository.GetCurrentMfaMethodAsync(userId, cancellationToken);
        if (mfaMethod is null || !mfaMethod.IsEnabled)
        {
            return;
        }

        if (await HasRecentStepUpAsync(userId, cancellationToken))
        {
            return;
        }

        throw new ApplicationErrorException(ErrorCodes.Forbidden, "Step-up authentication is required.");
    }

    private async Task<bool> HasRecentStepUpAsync(Guid userId, CancellationToken cancellationToken)
    {
        var latest = await _authRepository.GetLatestSuccessfulAuthEventAsync(
            userId,
            StepUpActions,
            cancellationToken);
        return latest?.CreatedAt >= DateTimeOffset.UtcNow.AddMinutes(-EffectiveWindowMinutes());
    }

    private int EffectiveWindowMinutes()
    {
        return Math.Max(1, _options.StepUpWindowMinutes);
    }

    private async Task<Guid> GetRequiredUserIdAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Authentication is required.");
        }

        var user = await _authRepository.FindUserByIdAsync(_currentUser.UserId.Value, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Current user was not found.");
        return user.Id;
    }
}
