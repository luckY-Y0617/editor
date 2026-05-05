using Northstar.Contracts.Auth;

namespace Northstar.Application.Security;

public interface IAuthMfaService
{
    Task<TotpEnrollmentResponse> EnrollTotpAsync(CancellationToken cancellationToken = default);
    Task<AuthSecurityStateResponse> VerifyTotpAsync(VerifyTotpRequest request, CancellationToken cancellationToken = default);
    Task<AuthSecurityStateResponse> DisableTotpAsync(CancellationToken cancellationToken = default);
}
