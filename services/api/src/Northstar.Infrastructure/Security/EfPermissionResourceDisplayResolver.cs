using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Domain.Security;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfPermissionResourceDisplayResolver : IPermissionResourceDisplayResolver
{
    private readonly NorthstarDbContext _dbContext;

    public EfPermissionResourceDisplayResolver(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PermissionResourceDisplaySummary>> GetDisplaySummariesAsync(
        Guid workspaceId,
        IReadOnlyCollection<PermissionResourceReference> resources,
        CancellationToken cancellationToken = default)
    {
        if (resources.Count == 0)
        {
            return Array.Empty<PermissionResourceDisplaySummary>();
        }

        var documentIds = resources
            .Where(resource => resource.ResourceType == ResourceTypes.Document)
            .Select(resource => resource.ResourceId)
            .Distinct()
            .ToArray();
        var collectionIds = resources
            .Where(resource => resource.ResourceType == ResourceTypes.Collection)
            .Select(resource => resource.ResourceId)
            .Distinct()
            .ToArray();

        var summaries = new List<PermissionResourceDisplaySummary>(documentIds.Length + collectionIds.Length);

        if (documentIds.Length > 0)
        {
            var documents = await (
                from document in _dbContext.Documents.AsNoTracking()
                join collection in _dbContext.Collections.AsNoTracking().Where(collection => collection.DeletedAt == null)
                    on document.CollectionId equals collection.Id into collectionJoin
                from collection in collectionJoin.DefaultIfEmpty()
                where document.WorkspaceId == workspaceId &&
                    document.DeletedAt == null &&
                    documentIds.Contains(document.Id)
                select new
                {
                    document.Id,
                    document.Title,
                    CollectionTitle = collection == null ? null : collection.Title
                })
                .ToListAsync(cancellationToken);

            summaries.AddRange(documents.Select(document => new PermissionResourceDisplaySummary(
                ResourceTypes.Document,
                document.Id,
                document.Title,
                document.CollectionTitle is null ? document.Title : $"{document.CollectionTitle} / {document.Title}")));
        }

        if (collectionIds.Length > 0)
        {
            var collections = await _dbContext.Collections
                .AsNoTracking()
                .Where(collection =>
                    collection.WorkspaceId == workspaceId &&
                    collection.DeletedAt == null &&
                    collectionIds.Contains(collection.Id))
                .Select(collection => new
                {
                    collection.Id,
                    collection.Title
                })
                .ToListAsync(cancellationToken);

            summaries.AddRange(collections.Select(collection => new PermissionResourceDisplaySummary(
                ResourceTypes.Collection,
                collection.Id,
                collection.Title,
                collection.Title)));
        }

        return summaries;
    }
}
