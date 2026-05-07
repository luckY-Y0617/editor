using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Knowledge;
using Northstar.Domain.Knowledge.Collections;
using Northstar.Domain.Security;

namespace Northstar.Application.Knowledge;

public sealed class CollectionService : ICollectionService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IKnowledgeQueryService _queryService;
    private readonly IWorkspaceAccessService _accessService;
    private readonly IScopedResourceAccessService _scopedAccessService;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;

    public CollectionService(
        ICollectionRepository collectionRepository,
        IKnowledgeQueryService queryService,
        IWorkspaceAccessService accessService,
        IScopedResourceAccessService scopedAccessService,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork)
    {
        _collectionRepository = collectionRepository;
        _queryService = queryService;
        _accessService = accessService;
        _scopedAccessService = scopedAccessService;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
    }

    public Task<CollectionMutationResponse> CreateAsync(
        Guid spaceId,
        CreateCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var location = await _collectionRepository.GetSpaceLocationAsync(spaceId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");
            await _accessService.EnsureCanEditWorkspaceAsync(location.WorkspaceId, ct);
            var actorId = await _accessService.GetRequiredUserIdAsync(ct);
            var sortOrder = request.SortOrder ?? await _collectionRepository.GetNextCollectionSortOrderAsync(spaceId, ct);

            var collection = new Collection(
                location.WorkspaceId,
                location.SpaceId,
                request.Title,
                createdBy: actorId,
                sortOrder: sortOrder);

            await _collectionRepository.AddAsync(collection, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return await CreateMutationResponseAsync(spaceId, collection.Id, ct);
        }, cancellationToken);
    }

    public Task<CollectionMutationResponse> UpdateAsync(
        Guid spaceId,
        Guid collectionId,
        UpdateCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var collection = await GetCollectionInSpaceAsync(spaceId, collectionId, ct);
            await _scopedAccessService.EnsureCanAccessCollectionAsync(
                collectionId,
                PermissionActions.CollectionEdit,
                ct);

            if (request.Title is not null)
            {
                collection.Rename(request.Title);
            }

            if (request.SortOrder.HasValue)
            {
                collection.SetSortOrder(request.SortOrder.Value);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return await CreateMutationResponseAsync(spaceId, collectionId, ct);
        }, cancellationToken);
    }

    public Task<KnowledgeMapResponse> DeleteAsync(
        Guid spaceId,
        Guid collectionId,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var collection = await GetCollectionInSpaceAsync(spaceId, collectionId, ct);
            await _scopedAccessService.EnsureCanAccessCollectionAsync(
                collectionId,
                PermissionActions.CollectionDelete,
                ct);

            if (await _collectionRepository.HasLiveDocumentsAsync(collectionId, ct))
            {
                throw new ApplicationErrorException(
                    ErrorCodes.Conflict,
                    "Collection cannot be deleted while it contains documents.");
            }

            collection.Delete();
            await _unitOfWork.SaveChangesAsync(ct);

            return await _queryService.GetMapAsync(spaceId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");
        }, cancellationToken);
    }

    public Task<KnowledgeMapResponse> ReorderAsync(
        Guid spaceId,
        ReorderCollectionsRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var location = await _collectionRepository.GetSpaceLocationAsync(spaceId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");
            await _accessService.EnsureCanEditWorkspaceAsync(location.WorkspaceId, ct);

            var requestedIds = ParseCollectionIds(request.CollectionIds);
            var collections = await _collectionRepository.GetCollectionsForSpaceAsync(spaceId, ct);

            if (!SameCollectionSet(requestedIds, collections.Select(collection => collection.Id)))
            {
                throw new ApplicationErrorException(
                    ErrorCodes.ValidationError,
                    "collectionIds must include every live collection in this space exactly once.");
            }

            for (var index = 0; index < requestedIds.Count; index++)
            {
                var collection = collections.First(item => item.Id == requestedIds[index]);
                collection.SetSortOrder(index + 1m);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            return await _queryService.GetMapAsync(spaceId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");
        }, cancellationToken);
    }

    private async Task<Collection> GetCollectionInSpaceAsync(
        Guid spaceId,
        Guid collectionId,
        CancellationToken cancellationToken)
    {
        var collection = await _collectionRepository.GetCollectionAsync(collectionId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Collection was not found.");

        if (collection.SpaceId != spaceId)
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Collection was not found.");
        }

        return collection;
    }

    private async Task<CollectionMutationResponse> CreateMutationResponseAsync(
        Guid spaceId,
        Guid collectionId,
        CancellationToken cancellationToken)
    {
        var map = await _queryService.GetMapAsync(spaceId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");
        var collection = map.Folders.FirstOrDefault(folder => folder.Id == collectionId.ToString())
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Collection was not found.");

        return new CollectionMutationResponse(collection, map);
    }

    private static IReadOnlyList<Guid> ParseCollectionIds(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "collectionIds must not be empty.");
        }

        var ids = new List<Guid>(values.Count);
        var seen = new HashSet<Guid>();
        foreach (var value in values)
        {
            if (!Guid.TryParse(value, out var id))
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "collectionIds must contain valid UUIDs.");
            }

            if (!seen.Add(id))
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "collectionIds must not contain duplicates.");
            }

            ids.Add(id);
        }

        return ids;
    }

    private static bool SameCollectionSet(IEnumerable<Guid> left, IEnumerable<Guid> right)
    {
        return left.Order().SequenceEqual(right.Order());
    }
}
