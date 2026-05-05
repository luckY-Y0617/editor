using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IScimTokenRepository
{
    Task AddAsync(ScimToken token, CancellationToken cancellationToken = default);

    Task<ScimToken?> GetByTokenHashForUpdateAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScimToken>> GetByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<ScimToken?> GetForUpdateAsync(
        Guid workspaceId,
        Guid tokenId,
        CancellationToken cancellationToken = default);
}
