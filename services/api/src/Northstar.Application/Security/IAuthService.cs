using Northstar.Contracts.Auth;

namespace Northstar.Application.Security;

public interface IAuthService
{
    Task<AuthTokenResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthTokenResponse> IdpLoginAsync(IdpLoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
    Task<MeResponse> GetMeAsync(CancellationToken cancellationToken = default);
}
