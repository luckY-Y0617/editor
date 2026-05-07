using Microsoft.EntityFrameworkCore;
using Northstar.Application.Knowledge;
using Northstar.Domain.Knowledge.Collections;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Knowledge;

public sealed class EfCollectionRepository : ICollectionRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfCollectionRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CollectionLocation?> GetSpaceLocationAsync(
        Guid spaceId,
        CancellationToken cancellationToken = default)
    {
        var space = await _dbContext.Spaces
            .AsNoTracking()
            .Where(space => space.Id == spaceId && space.DeletedAt == null)
            .Select(space => new
            {
                space.Id,
                space.WorkspaceId
            })
            .FirstOrDefaultAsync(cancellationToken);

        return space is null
            ? null
            : new CollectionLocation(space.WorkspaceId, space.Id, Guid.Empty, null);
    }

    public Task<Collection?> GetCollectionAsync(
        Guid collectionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Collections
            .Where(collection => collection.Id == collectionId && collection.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Collection>> GetCollectionsForSpaceAsync(
        Guid spaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Collections
            .Where(collection => collection.SpaceId == spaceId && collection.DeletedAt == null)
            .OrderBy(collection => collection.SortOrder)
            .ThenBy(collection => collection.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetNextCollectionSortOrderAsync(
        Guid spaceId,
        CancellationToken cancellationToken = default)
    {
        var maxSortOrder = await _dbContext.Collections
            .AsNoTracking()
            .Where(collection => collection.SpaceId == spaceId && collection.DeletedAt == null)
            .Select(collection => (decimal?)collection.SortOrder)
            .MaxAsync(cancellationToken);

        return (maxSortOrder ?? 0m) + 1m;
    }

    public Task<bool> HasLiveDocumentsAsync(
        Guid collectionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Documents
            .AsNoTracking()
            .AnyAsync(document => document.CollectionId == collectionId && document.DeletedAt == null, cancellationToken);
    }

    public Task AddAsync(Collection collection, CancellationToken cancellationToken = default)
    {
        return _dbContext.Collections.AddAsync(collection, cancellationToken).AsTask();
    }
}
