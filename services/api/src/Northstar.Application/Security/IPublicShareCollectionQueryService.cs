using Northstar.Contracts.Security;

namespace Northstar.Application.Security;

public interface IPublicShareCollectionQueryService
{
    Task<PublicShareCollectionDto?> GetCollectionAsync(
        Guid workspaceId,
        Guid collectionId,
        CancellationToken cancellationToken = default);
}
