using Northstar.Domain.Knowledge.Collections;

namespace Northstar.Application.Knowledge;

public interface ICollectionRepository
{
    Task<CollectionLocation?> GetSpaceLocationAsync(Guid spaceId, CancellationToken cancellationToken = default);
    Task<Collection?> GetCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Collection>> GetCollectionsForSpaceAsync(Guid spaceId, CancellationToken cancellationToken = default);
    Task<decimal> GetNextCollectionSortOrderAsync(Guid spaceId, CancellationToken cancellationToken = default);
    Task<bool> HasLiveDocumentsAsync(Guid collectionId, CancellationToken cancellationToken = default);
    Task AddAsync(Collection collection, CancellationToken cancellationToken = default);
}
