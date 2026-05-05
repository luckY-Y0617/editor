using Northstar.Contracts.Auth;

namespace Northstar.Application.Security;

public interface IAuthSecurityStateService
{
    Task<AuthSecurityStateResponse> GetSecurityStateAsync(CancellationToken cancellationToken = default);
}
