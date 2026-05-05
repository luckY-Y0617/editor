using Northstar.Contracts.Security;

namespace Northstar.Application.Security;

public interface IScimTokenService
{
    Task<CreateScimTokenResponse> CreateAsync(
        Guid workspaceId,
        CreateScimTokenRequest request,
        CancellationToken cancellationToken = default);

    Task<ScimTokensResponse> GetAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        Guid workspaceId,
        Guid tokenId,
        CancellationToken cancellationToken = default);
}
