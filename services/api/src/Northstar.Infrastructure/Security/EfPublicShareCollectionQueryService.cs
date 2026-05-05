using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Contracts.Security;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Security;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfPublicShareCollectionQueryService : IPublicShareCollectionQueryService
{
    private readonly NorthstarDbContext _dbContext;

    public EfPublicShareCollectionQueryService(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PublicShareCollectionDto?> GetCollectionAsync(
        Guid workspaceId,
        Guid collectionId,
        CancellationToken cancellationToken = default)
    {
        var collection = await _dbContext.Collections
            .AsNoTracking()
            .Where(item =>
                item.WorkspaceId == workspaceId &&
                item.Id == collectionId &&
                item.ArchivedAt == null &&
                item.DeletedAt == null)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.UpdatedAt,
                item.SortOrder
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (collection is null)
        {
            return null;
        }

        var documents = await _dbContext.Documents
            .AsNoTracking()
            .Where(document =>
                document.WorkspaceId == workspaceId &&
                document.CollectionId == collectionId &&
                document.DeletedAt == null &&
                document.Status != DocumentStatus.Archived)
            .OrderBy(document => document.SortOrder)
            .ThenBy(document => document.Title)
            .Select(document => new
            {
                document.Id,
                document.Title,
                document.Status,
                document.UpdatedAt,
                document.SortOrder
            })
            .ToListAsync(cancellationToken);
        var documentIds = documents.Select(document => document.Id).ToArray();
        List<Guid> restrictedDocumentIdRows = documentIds.Length == 0
            ? []
            : await _dbContext.ResourceAccessPolicies
                .AsNoTracking()
                .Where(policy =>
                    policy.WorkspaceId == workspaceId &&
                    policy.ResourceType == ResourceTypes.Document &&
                    documentIds.Contains(policy.ResourceId) &&
                    policy.InheritanceMode == InheritanceModes.Restricted)
                .Select(policy => policy.ResourceId)
                .ToListAsync(cancellationToken);
        var restrictedDocumentIds = restrictedDocumentIdRows.ToHashSet();
        var tagLookup = await GetTagLookupAsync(documentIds, cancellationToken);

        return new PublicShareCollectionDto(
            collection.Id.ToString(),
            collection.Title,
            collection.UpdatedAt,
            collection.SortOrder,
            documents
                .Where(document => !restrictedDocumentIds.Contains(document.Id))
                .Select(document => new PublicShareCollectionDocumentDto(
                    document.Id.ToString(),
                    document.Title,
                    document.Status,
                    document.UpdatedAt,
                    tagLookup.TryGetValue(document.Id, out var tags) ? tags : [],
                    document.SortOrder))
                .ToArray());
    }

    private async Task<Dictionary<Guid, IReadOnlyList<string>>> GetTagLookupAsync(
        IReadOnlyCollection<Guid> documentIds,
        CancellationToken cancellationToken)
    {
        if (documentIds.Count == 0)
        {
            return [];
        }

        var tagRows = await (
            from documentTag in _dbContext.DocumentTags.AsNoTracking()
            join tag in _dbContext.Tags.AsNoTracking() on documentTag.TagId equals tag.Id
            where documentIds.Contains(documentTag.DocumentId) && tag.DeletedAt == null
            orderby tag.Name
            select new
            {
                documentTag.DocumentId,
                tag.Name
            })
            .ToListAsync(cancellationToken);

        return tagRows
            .GroupBy(row => row.DocumentId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(row => row.Name).ToArray());
    }
}
