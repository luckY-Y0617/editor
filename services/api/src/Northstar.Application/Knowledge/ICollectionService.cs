using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public interface ICollectionService
{
    Task<CollectionMutationResponse> CreateAsync(
        Guid spaceId,
        CreateCollectionRequest request,
        CancellationToken cancellationToken = default);

    Task<CollectionMutationResponse> UpdateAsync(
        Guid spaceId,
        Guid collectionId,
        UpdateCollectionRequest request,
        CancellationToken cancellationToken = default);

    Task<KnowledgeMapResponse> ReorderAsync(
        Guid spaceId,
        ReorderCollectionsRequest request,
        CancellationToken cancellationToken = default);

    Task<KnowledgeMapResponse> DeleteAsync(
        Guid spaceId,
        Guid collectionId,
        CancellationToken cancellationToken = default);
}
