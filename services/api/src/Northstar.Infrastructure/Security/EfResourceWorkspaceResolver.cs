using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfResourceWorkspaceResolver : IResourceWorkspaceResolver
{
    private readonly NorthstarDbContext _dbContext;

    public EfResourceWorkspaceResolver(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Guid?> GetWorkspaceIdForSpaceAsync(Guid spaceId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Spaces
            .AsNoTracking()
            .Where(space => space.Id == spaceId && space.DeletedAt == null)
            .Select(space => (Guid?)space.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Guid?> GetWorkspaceIdForDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Documents
            .AsNoTracking()
            .Where(document => document.Id == documentId && document.DeletedAt == null)
            .Select(document => (Guid?)document.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Guid?> GetWorkspaceIdForCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Collections
            .AsNoTracking()
            .Where(collection => collection.Id == collectionId && collection.DeletedAt == null)
            .Select(collection => (Guid?)collection.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<DocumentPermissionResource?> GetDocumentPermissionResourceAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Documents
            .AsNoTracking()
            .Where(document => document.Id == documentId && document.DeletedAt == null)
            .Select(document => new DocumentPermissionResource(
                document.Id,
                document.WorkspaceId,
                document.CollectionId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<DocumentPermissionResource?> GetDocumentPermissionResourceIncludingDeletedAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Documents
            .AsNoTracking()
            .Where(document => document.Id == documentId)
            .Select(document => new DocumentPermissionResource(
                document.Id,
                document.WorkspaceId,
                document.CollectionId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentPermissionResource>> GetDocumentPermissionResourcesAsync(
        IReadOnlyCollection<Guid> documentIds,
        CancellationToken cancellationToken = default)
    {
        if (documentIds.Count == 0)
        {
            return Array.Empty<DocumentPermissionResource>();
        }

        return await _dbContext.Documents
            .AsNoTracking()
            .Where(document => documentIds.Contains(document.Id) && document.DeletedAt == null)
            .Select(document => new DocumentPermissionResource(
                document.Id,
                document.WorkspaceId,
                document.CollectionId))
            .ToListAsync(cancellationToken);
    }

    public Task<CollectionPermissionResource?> GetCollectionPermissionResourceAsync(
        Guid collectionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Collections
            .AsNoTracking()
            .Where(collection => collection.Id == collectionId && collection.DeletedAt == null)
            .Select(collection => new CollectionPermissionResource(
                collection.Id,
                collection.WorkspaceId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
