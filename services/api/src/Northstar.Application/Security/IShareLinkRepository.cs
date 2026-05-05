using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IShareLinkRepository
{
    Task<IReadOnlyList<ShareLink>> GetActiveByResourceAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<ShareLink?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<ShareLink?> GetForUpdateAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default);

    Task AddAsync(ShareLink link, CancellationToken cancellationToken = default);
}
