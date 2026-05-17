using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Contracts.Security;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Security;
using Northstar.Domain.Shared;
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
                document.Status == DocumentStatus.Published)
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

    public async Task<PublicShareTreeResponse?> GetTreeAsync(
        ShareLink scope,
        CancellationToken cancellationToken = default)
    {
        if (scope.ResourceType == ResourceTypes.Document)
        {
            var document = await GetPublicDocumentRowAsync(
                scope.WorkspaceId,
                scope.ResourceId,
                excludeRestrictedPolicy: false,
                cancellationToken);
            if (document is null)
            {
                return null;
            }

            return new PublicShareTreeResponse(
                scope.Id.ToString(),
                scope.ResourceType,
                document.Title,
                DefaultContentProtection(),
                [
                    new PublicShareTreeNodeDto(
                        document.Id.ToString(),
                        ResourceTypes.Document,
                        document.Title,
                        null,
                        document.UpdatedAt,
                        false,
                        document.SortOrder)
                ]);
        }

        if (scope.ResourceType == ResourceTypes.Library)
        {
            var library = await _dbContext.Spaces
                .AsNoTracking()
                .Where(space =>
                    space.WorkspaceId == scope.WorkspaceId &&
                    space.Id == scope.ResourceId &&
                    space.ArchivedAt == null &&
                    space.DeletedAt == null)
                .Select(space => new
                {
                    space.Id,
                    space.Name
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (library is null)
            {
                return null;
            }

            var libraryCollections = await _dbContext.Collections
                .AsNoTracking()
                .Where(collection =>
                    collection.WorkspaceId == scope.WorkspaceId &&
                    collection.SpaceId == library.Id &&
                    collection.ArchivedAt == null &&
                    collection.DeletedAt == null)
                .Select(collection => new CollectionTreeRow(
                    collection.Id,
                    collection.SpaceId,
                    collection.ParentCollectionId,
                    collection.Title,
                    collection.UpdatedAt,
                    collection.SortOrder))
                .ToListAsync(cancellationToken);
            var libraryVisibleCollectionIds = GetVisibleLibraryCollectionIds(
                libraryCollections,
                await GetRestrictedCollectionIdsAsync(
                    scope.WorkspaceId,
                    libraryCollections.Select(collection => collection.Id).ToArray(),
                    cancellationToken));
            var libraryDocuments = await GetPublicDocumentRowsForLibraryAsync(
                scope.WorkspaceId,
                library.Id,
                libraryVisibleCollectionIds,
                cancellationToken);
            var libraryChildParentIds = libraryVisibleCollectionIds
                .Select(id => libraryCollections.First(collection => collection.Id == id).ParentId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToHashSet();
            var libraryDocumentParentIds = libraryDocuments
                .Where(document => document.CollectionId.HasValue)
                .Select(document => document.CollectionId!.Value)
                .ToHashSet();

            var libraryCollectionNodes = libraryVisibleCollectionIds
                .Select(id => libraryCollections.First(collection => collection.Id == id))
                .OrderBy(collection => collection.SortOrder)
                .ThenBy(collection => collection.Title)
                .Select(collection => new PublicShareTreeNodeDto(
                    collection.Id.ToString(),
                    ResourceTypes.Collection,
                    collection.Title,
                    collection.ParentId?.ToString(),
                    collection.UpdatedAt,
                    libraryChildParentIds.Contains(collection.Id) || libraryDocumentParentIds.Contains(collection.Id),
                    collection.SortOrder));

            var libraryDocumentNodes = libraryDocuments
                .OrderBy(document => document.SortOrder)
                .ThenBy(document => document.Title)
                .Select(document => new PublicShareTreeNodeDto(
                    document.Id.ToString(),
                    ResourceTypes.Document,
                    document.Title,
                    document.CollectionId?.ToString(),
                    document.UpdatedAt,
                    false,
                    document.SortOrder));

            return new PublicShareTreeResponse(
                scope.Id.ToString(),
                scope.ResourceType,
                library.Name,
                DefaultContentProtection(),
                libraryCollectionNodes.Concat(libraryDocumentNodes)
                    .OrderBy(node => node.SortOrder)
                    .ThenBy(node => node.Title)
                    .ToArray());
        }

        if (scope.ResourceType != ResourceTypes.Collection)
        {
            return null;
        }

        var root = await _dbContext.Collections
            .AsNoTracking()
            .Where(collection =>
                collection.WorkspaceId == scope.WorkspaceId &&
                collection.Id == scope.ResourceId &&
                collection.ArchivedAt == null &&
                collection.DeletedAt == null)
            .Select(collection => new CollectionTreeRow(
                collection.Id,
                collection.SpaceId,
                collection.ParentCollectionId,
                collection.Title,
                collection.UpdatedAt,
                collection.SortOrder))
            .FirstOrDefaultAsync(cancellationToken);
        if (root is null)
        {
            return null;
        }

        var collections = await _dbContext.Collections
            .AsNoTracking()
            .Where(collection =>
                collection.WorkspaceId == scope.WorkspaceId &&
                collection.SpaceId == root.SpaceId &&
                collection.ArchivedAt == null &&
                collection.DeletedAt == null)
            .Select(collection => new CollectionTreeRow(
                collection.Id,
                collection.SpaceId,
                collection.ParentCollectionId,
                collection.Title,
                collection.UpdatedAt,
                collection.SortOrder))
            .ToListAsync(cancellationToken);

        var descendantCollectionIds = GetVisibleDescendantCollectionIds(root.Id, collections, await GetRestrictedCollectionIdsAsync(
            scope.WorkspaceId,
            collections.Select(collection => collection.Id).ToArray(),
            cancellationToken));
        var scopeCollectionIds = descendantCollectionIds.Prepend(root.Id).ToArray();
        var documents = await GetPublicDocumentRowsAsync(scope.WorkspaceId, scopeCollectionIds, cancellationToken);
        var childParentIds = descendantCollectionIds
            .Select(id => collections.First(collection => collection.Id == id).ParentId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();
        var documentParentIds = documents
            .Where(document => document.CollectionId.HasValue)
            .Select(document => document.CollectionId!.Value)
            .ToHashSet();

        var collectionNodes = descendantCollectionIds
            .Select(id => collections.First(collection => collection.Id == id))
            .OrderBy(collection => collection.SortOrder)
            .ThenBy(collection => collection.Title)
            .Select(collection => new PublicShareTreeNodeDto(
                collection.Id.ToString(),
                ResourceTypes.Collection,
                collection.Title,
                collection.ParentId == root.Id ? null : collection.ParentId?.ToString(),
                collection.UpdatedAt,
                childParentIds.Contains(collection.Id) || documentParentIds.Contains(collection.Id),
                collection.SortOrder));

        var documentNodes = documents
            .OrderBy(document => document.SortOrder)
            .ThenBy(document => document.Title)
            .Select(document => new PublicShareTreeNodeDto(
                document.Id.ToString(),
                ResourceTypes.Document,
                document.Title,
                document.CollectionId?.ToString(),
                document.UpdatedAt,
                false,
                document.SortOrder));

        return new PublicShareTreeResponse(
            scope.Id.ToString(),
            scope.ResourceType,
            root.Title,
            DefaultContentProtection(),
            collectionNodes.Concat(documentNodes)
                .OrderBy(node => node.SortOrder)
                .ThenBy(node => node.Title)
                .ToArray());
    }

    public async Task<PublicShareDocumentDto?> GetDocumentAsync(
        ShareLink scope,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        if (scope.ResourceType == ResourceTypes.Document)
        {
            if (scope.ResourceId != documentId)
            {
                return null;
            }

            return await GetPublicDocumentDtoAsync(
                scope.WorkspaceId,
                documentId,
                excludeRestrictedPolicy: false,
                cancellationToken);
        }

        if (scope.ResourceType != ResourceTypes.Collection)
        {
            if (scope.ResourceType != ResourceTypes.Library)
            {
                return null;
            }

            var libraryDocument = await GetPublicDocumentRowAsync(
                scope.WorkspaceId,
                documentId,
                excludeRestrictedPolicy: true,
                cancellationToken);
            if (libraryDocument?.SpaceId != scope.ResourceId)
            {
                return null;
            }

            if (libraryDocument.CollectionId.HasValue)
            {
                var libraryCollections = await _dbContext.Collections
                    .AsNoTracking()
                    .Where(collection =>
                        collection.WorkspaceId == scope.WorkspaceId &&
                        collection.SpaceId == scope.ResourceId &&
                        collection.ArchivedAt == null &&
                        collection.DeletedAt == null)
                    .Select(collection => new CollectionTreeRow(
                        collection.Id,
                        collection.SpaceId,
                        collection.ParentCollectionId,
                        collection.Title,
                        collection.UpdatedAt,
                        collection.SortOrder))
                    .ToListAsync(cancellationToken);
                var libraryRestrictedCollectionIds = await GetRestrictedCollectionIdsAsync(
                    scope.WorkspaceId,
                    libraryCollections.Select(collection => collection.Id).ToArray(),
                    cancellationToken);
                var libraryVisibleCollectionIds = GetVisibleLibraryCollectionIds(libraryCollections, libraryRestrictedCollectionIds).ToHashSet();
                if (!libraryVisibleCollectionIds.Contains(libraryDocument.CollectionId.Value))
                {
                    return null;
                }
            }

            return await GetPublicDocumentDtoAsync(
                scope.WorkspaceId,
                documentId,
                excludeRestrictedPolicy: true,
                cancellationToken);
        }

        var document = await GetPublicDocumentRowAsync(
            scope.WorkspaceId,
            documentId,
            excludeRestrictedPolicy: true,
            cancellationToken);
        if (document?.CollectionId is null)
        {
            return null;
        }

        var collections = await _dbContext.Collections
            .AsNoTracking()
            .Where(collection =>
                collection.WorkspaceId == scope.WorkspaceId &&
                collection.ArchivedAt == null &&
                collection.DeletedAt == null)
            .Select(collection => new CollectionTreeRow(
                collection.Id,
                collection.SpaceId,
                collection.ParentCollectionId,
                collection.Title,
                collection.UpdatedAt,
                collection.SortOrder))
            .ToListAsync(cancellationToken);
        var restrictedCollectionIds = await GetRestrictedCollectionIdsAsync(
            scope.WorkspaceId,
            collections.Select(collection => collection.Id).ToArray(),
            cancellationToken);
        var visibleScopeIds = GetVisibleDescendantCollectionIds(scope.ResourceId, collections, restrictedCollectionIds)
            .Prepend(scope.ResourceId)
            .ToHashSet();
        if (!visibleScopeIds.Contains(document.CollectionId.Value))
        {
            return null;
        }

        return await GetPublicDocumentDtoAsync(
            scope.WorkspaceId,
            documentId,
            excludeRestrictedPolicy: true,
            cancellationToken);
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

    private async Task<HashSet<Guid>> GetRestrictedCollectionIdsAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> collectionIds,
        CancellationToken cancellationToken)
    {
        if (collectionIds.Count == 0)
        {
            return [];
        }

        var rows = await _dbContext.ResourceAccessPolicies
            .AsNoTracking()
            .Where(policy =>
                policy.WorkspaceId == workspaceId &&
                policy.ResourceType == ResourceTypes.Collection &&
                collectionIds.Contains(policy.ResourceId) &&
                policy.InheritanceMode == InheritanceModes.Restricted)
            .Select(policy => policy.ResourceId)
            .ToListAsync(cancellationToken);
        return rows.ToHashSet();
    }

    private static IReadOnlyList<Guid> GetVisibleDescendantCollectionIds(
        Guid rootCollectionId,
        IReadOnlyList<CollectionTreeRow> collections,
        HashSet<Guid> restrictedCollectionIds)
    {
        var byParent = collections
            .Where(collection => collection.ParentId.HasValue)
            .GroupBy(collection => collection.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var result = new List<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(rootCollectionId);
        while (queue.TryDequeue(out var parentId))
        {
            if (!byParent.TryGetValue(parentId, out var children))
            {
                continue;
            }

            foreach (var child in children.OrderBy(collection => collection.SortOrder).ThenBy(collection => collection.Title))
            {
                if (restrictedCollectionIds.Contains(child.Id))
                {
                    continue;
                }

                result.Add(child.Id);
                queue.Enqueue(child.Id);
            }
        }

        return result;
    }

    private static IReadOnlyList<Guid> GetVisibleLibraryCollectionIds(
        IReadOnlyList<CollectionTreeRow> collections,
        HashSet<Guid> restrictedCollectionIds)
    {
        var byParent = collections
            .GroupBy(collection => collection.ParentId ?? Guid.Empty)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var result = new List<Guid>();
        var queue = new Queue<Guid>();
        if (byParent.TryGetValue(Guid.Empty, out var roots))
        {
            foreach (var root in roots.OrderBy(collection => collection.SortOrder).ThenBy(collection => collection.Title))
            {
                if (restrictedCollectionIds.Contains(root.Id))
                {
                    continue;
                }

                result.Add(root.Id);
                queue.Enqueue(root.Id);
            }
        }

        while (queue.TryDequeue(out var parentId))
        {
            if (!byParent.TryGetValue(parentId, out var children))
            {
                continue;
            }

            foreach (var child in children.OrderBy(collection => collection.SortOrder).ThenBy(collection => collection.Title))
            {
                if (restrictedCollectionIds.Contains(child.Id))
                {
                    continue;
                }

                result.Add(child.Id);
                queue.Enqueue(child.Id);
            }
        }

        return result;
    }

    private async Task<PublicDocumentRow?> GetPublicDocumentRowAsync(
        Guid workspaceId,
        Guid documentId,
        bool excludeRestrictedPolicy,
        CancellationToken cancellationToken)
    {
        var rows = await GetPublicDocumentRowsAsync(
            workspaceId,
            _dbContext.Documents
                .AsNoTracking()
                .Where(document => document.Id == documentId),
            excludeRestrictedPolicy,
            cancellationToken);
        return rows.FirstOrDefault();
    }

    private Task<IReadOnlyList<PublicDocumentRow>> GetPublicDocumentRowsAsync(
        Guid workspaceId,
        IReadOnlyCollection<Guid> collectionIds,
        CancellationToken cancellationToken)
    {
        if (collectionIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<PublicDocumentRow>>([]);
        }

        return GetPublicDocumentRowsAsync(
            workspaceId,
            _dbContext.Documents
                .AsNoTracking()
                .Where(document => document.CollectionId.HasValue && collectionIds.Contains(document.CollectionId.Value)),
            excludeRestrictedPolicy: true,
            cancellationToken);
    }

    private Task<IReadOnlyList<PublicDocumentRow>> GetPublicDocumentRowsForLibraryAsync(
        Guid workspaceId,
        Guid libraryId,
        IReadOnlyCollection<Guid> visibleCollectionIds,
        CancellationToken cancellationToken)
    {
        return GetPublicDocumentRowsAsync(
            workspaceId,
            _dbContext.Documents
                .AsNoTracking()
                .Where(document =>
                    document.SpaceId == libraryId &&
                    (!document.CollectionId.HasValue ||
                        visibleCollectionIds.Contains(document.CollectionId.Value))),
            excludeRestrictedPolicy: true,
            cancellationToken);
    }

    private async Task<IReadOnlyList<PublicDocumentRow>> GetPublicDocumentRowsAsync(
        Guid workspaceId,
        IQueryable<Document> query,
        bool excludeRestrictedPolicy,
        CancellationToken cancellationToken)
    {
        var documents = await query
            .Where(document =>
                document.WorkspaceId == workspaceId &&
                document.DeletedAt == null &&
                document.Status == DocumentStatus.Published)
            .Select(document => new PublicDocumentRow(
                document.Id,
                document.SpaceId,
                document.CollectionId,
                document.Title,
                document.Status,
                document.UpdatedAt,
                document.SortOrder,
                document.Revision))
            .ToListAsync(cancellationToken);
        if (!excludeRestrictedPolicy)
        {
            return documents;
        }

        var documentIds = documents.Select(document => document.Id).ToArray();
        var restrictedDocumentIds = documentIds.Length == 0
            ? []
            : (await _dbContext.ResourceAccessPolicies
                .AsNoTracking()
                .Where(policy =>
                    policy.WorkspaceId == workspaceId &&
                    policy.ResourceType == ResourceTypes.Document &&
                    documentIds.Contains(policy.ResourceId) &&
                    policy.InheritanceMode == InheritanceModes.Restricted)
                .Select(policy => policy.ResourceId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        return documents
            .Where(document => !restrictedDocumentIds.Contains(document.Id))
            .ToArray();
    }

    private async Task<PublicShareDocumentDto?> GetPublicDocumentDtoAsync(
        Guid workspaceId,
        Guid documentId,
        bool excludeRestrictedPolicy,
        CancellationToken cancellationToken)
    {
        var document = await GetPublicDocumentRowAsync(
            workspaceId,
            documentId,
            excludeRestrictedPolicy,
            cancellationToken);
        if (document is null)
        {
            return null;
        }

        var content = await _dbContext.DocumentDrafts
            .AsNoTracking()
            .Where(draft => draft.DocumentId == documentId)
            .Select(draft => draft.Content)
            .FirstOrDefaultAsync(cancellationToken);
        using var json = JsonDocument.Parse(content ?? JsonDefaults.EmptyTiptapDocument);
        var tagLookup = await GetTagLookupAsync([documentId], cancellationToken);
        return new PublicShareDocumentDto(
            document.Id.ToString(),
            document.Title,
            document.Status,
            document.UpdatedAt,
            tagLookup.TryGetValue(document.Id, out var tags) ? tags : [],
            json.RootElement.Clone(),
            document.Revision);
    }

    private static ShareLinkContentProtectionDto DefaultContentProtection()
    {
        return new ShareLinkContentProtectionDto(
            DisableDownload: true,
            DisablePrint: false,
            DisableCopy: false,
            WatermarkEnabled: false,
            WatermarkText: "Public link");
    }

    private sealed record CollectionTreeRow(
        Guid Id,
        Guid SpaceId,
        Guid? ParentId,
        string Title,
        DateTimeOffset UpdatedAt,
        decimal SortOrder);

    private sealed record PublicDocumentRow(
        Guid Id,
        Guid SpaceId,
        Guid? CollectionId,
        string Title,
        string Status,
        DateTimeOffset UpdatedAt,
        decimal SortOrder,
        long Revision);
}
