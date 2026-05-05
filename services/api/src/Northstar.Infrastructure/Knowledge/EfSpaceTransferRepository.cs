using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Northstar.Application.Knowledge;
using Northstar.Contracts.Knowledge;
using Northstar.Domain.Knowledge.Collections;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Knowledge.Tags;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Knowledge;

public sealed class EfSpaceTransferRepository : ISpaceTransferRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex DocumentPathIdRegex = new(
        @"/documents/(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly NorthstarDbContext _dbContext;
    private readonly IDocumentDerivedDataWriter _derivedDataWriter;

    public EfSpaceTransferRepository(
        NorthstarDbContext dbContext,
        IDocumentDerivedDataWriter derivedDataWriter)
    {
        _dbContext = dbContext;
        _derivedDataWriter = derivedDataWriter;
    }

    public async Task<ExportSpaceResponse?> ExportAsync(
        Guid spaceId,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var header = await (
            from space in _dbContext.Spaces.AsNoTracking()
            join workspace in _dbContext.Workspaces.AsNoTracking() on space.WorkspaceId equals workspace.Id
            where space.Id == spaceId &&
                space.DeletedAt == null &&
                workspace.DeletedAt == null
            select new
            {
                WorkspaceId = workspace.Id,
                WorkspaceName = workspace.Name,
                SpaceId = space.Id,
                SpaceName = space.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (header is null)
        {
            return null;
        }

        var collections = await _dbContext.Collections
            .AsNoTracking()
            .Where(collection => collection.SpaceId == spaceId && collection.DeletedAt == null)
            .OrderBy(collection => collection.SortOrder)
            .ThenBy(collection => collection.Title)
            .Select(collection => new CollectionExportDto(
                collection.Id.ToString(),
                collection.Title,
                collection.SortOrder))
            .ToListAsync(cancellationToken);

        var documentQuery = _dbContext.Documents
            .AsNoTracking()
            .Where(document => document.SpaceId == spaceId && document.DeletedAt == null);
        if (!includeArchived)
        {
            documentQuery = documentQuery.Where(document => document.Status != DocumentStatus.Archived);
        }

        var documentRows = await (
            from document in documentQuery
            join draft in _dbContext.DocumentDrafts.AsNoTracking() on document.Id equals draft.DocumentId into drafts
            from draft in drafts.DefaultIfEmpty()
            orderby document.SortOrder, document.Title
            select new
            {
                document.Id,
                document.CollectionId,
                document.Title,
                document.Status,
                document.SortOrder,
                document.Revision,
                document.CreatedAt,
                document.UpdatedAt,
                Content = draft == null ? null : draft.Content
            })
            .ToListAsync(cancellationToken);

        var documentIds = documentRows.Select(document => document.Id).ToArray();
        var tagLookup = await GetTagLookupAsync(documentIds, cancellationToken);

        var documents = documentRows
            .Select(document => new DocumentExportDto(
                document.Id.ToString(),
                document.CollectionId?.ToString() ?? string.Empty,
                document.Title,
                document.Status,
                document.SortOrder,
                tagLookup.TryGetValue(document.Id, out var tags) ? tags : [],
                ParseJsonObject(document.Content),
                document.Revision,
                document.CreatedAt,
                document.UpdatedAt))
            .ToArray();

        return new ExportSpaceResponse(
            "northstar.space.v1",
            DateTimeOffset.UtcNow,
            new WorkspaceExportDto(header.WorkspaceId.ToString(), header.WorkspaceName),
            new SpaceExportDto(header.SpaceId.ToString(), header.SpaceName),
            collections,
            documents);
    }

    public async Task<ImportSpaceResult> ImportAppendAsync(
        Guid spaceId,
        ImportSpaceRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var space = await _dbContext.Spaces
            .FirstOrDefaultAsync(space => space.Id == spaceId && space.DeletedAt == null, cancellationToken);
        if (space is null)
        {
            return new ImportSpaceResult(0, 0);
        }

        var collections = await _dbContext.Collections
            .Where(collection => collection.SpaceId == spaceId && collection.DeletedAt == null)
            .OrderBy(collection => collection.SortOrder)
            .ThenBy(collection => collection.Title)
            .ToListAsync(cancellationToken);

        var collectionMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var importedCollectionCount = 0;
        var nextCollectionSortOrder = collections.Count == 0 ? 0m : collections.Max(collection => collection.SortOrder) + 1m;

        foreach (var collectionRequest in request.Collections ?? [])
        {
            var title = collectionRequest.Title.Trim();
            var collection = collections.FirstOrDefault(existing =>
                string.Equals(existing.Title, title, StringComparison.OrdinalIgnoreCase));

            if (collection is null)
            {
                collection = new Collection(
                    space.WorkspaceId,
                    space.Id,
                    title,
                    createdBy: actorId,
                    slug: null,
                    sortOrder: collectionRequest.SortOrder ?? nextCollectionSortOrder++);
                await _dbContext.Collections.AddAsync(collection, cancellationToken);
                collections.Add(collection);
                importedCollectionCount++;
            }

            if (!string.IsNullOrWhiteSpace(collectionRequest.Id))
            {
                collectionMap[collectionRequest.Id] = collection.Id;
            }
        }

        var defaultCollection = await EnsureDefaultCollectionAsync(space.WorkspaceId, space.Id, actorId, collections, cancellationToken);
        var nextSortOrders = await GetNextDocumentSortOrdersAsync(space.Id, cancellationToken);
        var documents = new List<(DocumentImportDto Import, Document Document)>();
        var documentIdMap = new Dictionary<Guid, Guid>();

        foreach (var documentRequest in request.Documents)
        {
            var targetCollectionId = ResolveCollectionId(documentRequest.FolderId, collectionMap, collections, defaultCollection.Id);
            var sortOrder = documentRequest.SortOrder ?? NextSortOrder(nextSortOrders, targetCollectionId);
            var document = new Document(
                space.WorkspaceId,
                space.Id,
                targetCollectionId,
                documentRequest.Title.Trim(),
                actorId,
                actorId,
                sortOrder: sortOrder);

            if (string.Equals(documentRequest.Status, DocumentStatus.Archived, StringComparison.OrdinalIgnoreCase))
            {
                document.Archive(actorId);
            }

            await _dbContext.Documents.AddAsync(document, cancellationToken);
            documents.Add((documentRequest, document));

            if (Guid.TryParse(documentRequest.Id, out var oldDocumentId))
            {
                documentIdMap[oldDocumentId] = document.Id;
            }
        }

        foreach (var item in documents)
        {
            var rewrittenContent = RewriteInternalDocumentLinks(item.Import.Content, documentIdMap);
            var metadata = DocumentContentAnalyzer.Analyze(rewrittenContent);
            var draft = new DocumentDraft(item.Document.Id, item.Document.WorkspaceId, metadata.ContentJson, actorId);
            draft.UpdateContent(
                metadata.ContentJson,
                metadata.TextContent,
                metadata.OutlineJson,
                metadata.WordCount,
                metadata.ContentHash,
                actorId);

            await _dbContext.DocumentDrafts.AddAsync(draft, cancellationToken);
            await ReplaceDocumentTagsAsync(
                item.Document.WorkspaceId,
                item.Document.Id,
                NormalizeTags(item.Import.Tags ?? []),
                actorId,
                cancellationToken);
            await _derivedDataWriter.RecordDocumentImportedAsync(item.Document, draft, actorId, cancellationToken);
        }

        return new ImportSpaceResult(importedCollectionCount, documents.Count);
    }

    private async Task<Dictionary<Guid, IReadOnlyList<string>>> GetTagLookupAsync(
        IReadOnlyCollection<Guid> documentIds,
        CancellationToken cancellationToken)
    {
        if (documentIds.Count == 0)
        {
            return [];
        }

        var rows = await (
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

        return rows
            .GroupBy(row => row.DocumentId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(row => row.Name).ToArray());
    }

    private async Task<Collection> EnsureDefaultCollectionAsync(
        Guid workspaceId,
        Guid spaceId,
        Guid actorId,
        List<Collection> collections,
        CancellationToken cancellationToken)
    {
        var first = collections
            .OrderBy(collection => collection.SortOrder)
            .ThenBy(collection => collection.Title)
            .FirstOrDefault();
        if (first is not null)
        {
            return first;
        }

        var collection = new Collection(workspaceId, spaceId, "Imported", createdBy: actorId);
        await _dbContext.Collections.AddAsync(collection, cancellationToken);
        collections.Add(collection);
        return collection;
    }

    private async Task<Dictionary<Guid, decimal>> GetNextDocumentSortOrdersAsync(
        Guid spaceId,
        CancellationToken cancellationToken)
    {
        var rows = await _dbContext.Documents
            .AsNoTracking()
            .Where(document => document.SpaceId == spaceId && document.DeletedAt == null && document.CollectionId.HasValue)
            .GroupBy(document => document.CollectionId!.Value)
            .Select(group => new { CollectionId = group.Key, SortOrder = group.Max(document => document.SortOrder) + 1m })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.CollectionId, row => row.SortOrder);
    }

    private static Guid ResolveCollectionId(
        string? folderId,
        IReadOnlyDictionary<string, Guid> collectionMap,
        IReadOnlyCollection<Collection> collections,
        Guid defaultCollectionId)
    {
        if (!string.IsNullOrWhiteSpace(folderId))
        {
            if (collectionMap.TryGetValue(folderId, out var mappedCollectionId))
            {
                return mappedCollectionId;
            }

            if (Guid.TryParse(folderId, out var parsedCollectionId) &&
                collections.Any(collection => collection.Id == parsedCollectionId))
            {
                return parsedCollectionId;
            }
        }

        return defaultCollectionId;
    }

    private static decimal NextSortOrder(IDictionary<Guid, decimal> nextSortOrders, Guid collectionId)
    {
        if (!nextSortOrders.TryGetValue(collectionId, out var sortOrder))
        {
            sortOrder = 1m;
        }

        nextSortOrders[collectionId] = sortOrder + 1m;
        return sortOrder;
    }

    private async Task ReplaceDocumentTagsAsync(
        Guid workspaceId,
        Guid documentId,
        IReadOnlyCollection<string> tagNames,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        foreach (var tagName in tagNames)
        {
            var slug = SlugNormalizer.Normalize(tagName);
            if (string.IsNullOrWhiteSpace(slug))
            {
                continue;
            }

            var tag = await _dbContext.Tags
                .FirstOrDefaultAsync(tag => tag.WorkspaceId == workspaceId && tag.Slug == slug, cancellationToken);
            if (tag is null)
            {
                tag = new Tag(workspaceId, tagName.Trim(), slug, actorId);
                await _dbContext.Tags.AddAsync(tag, cancellationToken);
            }

            await _dbContext.DocumentTags.AddAsync(new DocumentTag(workspaceId, documentId, tag.Id), cancellationToken);
        }
    }

    private static IReadOnlyCollection<string> NormalizeTags(IReadOnlyList<string> tags)
    {
        return tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static JsonElement RewriteInternalDocumentLinks(
        JsonElement content,
        IReadOnlyDictionary<Guid, Guid> documentIdMap)
    {
        if (documentIdMap.Count == 0)
        {
            return content.Clone();
        }

        var node = JsonNode.Parse(content.GetRawText());
        if (node is null)
        {
            return content.Clone();
        }

        RewriteNode(node, documentIdMap);
        using var rewritten = JsonDocument.Parse(node.ToJsonString(JsonOptions));
        return rewritten.RootElement.Clone();
    }

    private static void RewriteNode(JsonNode node, IReadOnlyDictionary<Guid, Guid> documentIdMap)
    {
        if (node is JsonObject jsonObject)
        {
            RewriteAttr(jsonObject, "documentId", documentIdMap);
            RewriteAttr(jsonObject, "targetDocumentId", documentIdMap);
            RewriteHref(jsonObject, documentIdMap);

            foreach (var child in jsonObject.Select(property => property.Value).Where(value => value is not null).ToArray())
            {
                RewriteNode(child!, documentIdMap);
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var child in jsonArray.Where(value => value is not null).ToArray())
            {
                RewriteNode(child!, documentIdMap);
            }
        }
    }

    private static void RewriteAttr(
        JsonObject jsonObject,
        string propertyName,
        IReadOnlyDictionary<Guid, Guid> documentIdMap)
    {
        if (jsonObject[propertyName] is not JsonValue jsonValue ||
            !jsonValue.TryGetValue<string>(out var rawValue))
        {
            return;
        }

        if (Guid.TryParse(rawValue, out var oldDocumentId) &&
            documentIdMap.TryGetValue(oldDocumentId, out var newDocumentId))
        {
            jsonObject[propertyName] = newDocumentId.ToString();
        }
    }

    private static void RewriteHref(JsonObject jsonObject, IReadOnlyDictionary<Guid, Guid> documentIdMap)
    {
        if (jsonObject["href"] is not JsonValue jsonValue ||
            !jsonValue.TryGetValue<string>(out var href))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(href))
        {
            return;
        }

        if (Guid.TryParse(href, out var oldDocumentId) &&
            documentIdMap.TryGetValue(oldDocumentId, out var newDocumentId))
        {
            jsonObject["href"] = newDocumentId.ToString();
            return;
        }

        jsonObject["href"] = DocumentPathIdRegex.Replace(
            href,
            match =>
            {
                var oldId = Guid.Parse(match.Groups["id"].Value);
                return documentIdMap.TryGetValue(oldId, out var newId)
                    ? $"/documents/{newId}"
                    : match.Value;
            });
    }

    private static JsonElement ParseJsonObject(string? json)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? """{"type":"doc","content":[]}""" : json);
        return document.RootElement.Clone();
    }
}
