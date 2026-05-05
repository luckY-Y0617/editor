using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Northstar.Application.Knowledge;
using Northstar.Contracts.Knowledge;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Shared;
using Northstar.Domain.Workspaces;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Knowledge;

public sealed class EfKnowledgeQueryService : IKnowledgeQueryService
{
    private readonly NorthstarDbContext _dbContext;

    public EfKnowledgeQueryService(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BootstrapResponse?> GetBootstrapAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var workspace = await (
            from member in _dbContext.WorkspaceMembers.AsNoTracking()
            join joinedWorkspace in _dbContext.Workspaces.AsNoTracking() on member.WorkspaceId equals joinedWorkspace.Id
            where member.UserId == userId &&
                member.Status == WorkspaceMemberStatus.Active &&
                joinedWorkspace.DeletedAt == null
            select joinedWorkspace)
            .OrderBy(workspace => workspace.Slug == SeedDataIds.WorkspaceSlug ? 0 : 1)
            .ThenBy(workspace => workspace.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (workspace is null)
        {
            return null;
        }

        var spaces = await _dbContext.Spaces
            .AsNoTracking()
            .Where(space => space.WorkspaceId == workspace.Id && space.DeletedAt == null)
            .OrderBy(space => space.SortOrder)
            .ThenBy(space => space.Name)
            .Select(space => new SpaceDto(space.Id.ToString(), space.Name))
            .ToListAsync(cancellationToken);

        var activeSpaceId = workspace.DefaultSpaceId
            ?? await _dbContext.Spaces
                .AsNoTracking()
                .Where(space => space.WorkspaceId == workspace.Id && space.DeletedAt == null)
                .OrderBy(space => space.SortOrder)
                .Select(space => (Guid?)space.Id)
                .FirstOrDefaultAsync(cancellationToken)
            ?? Guid.Empty;

        var map = activeSpaceId == Guid.Empty
            ? new KnowledgeMapResponse([], [])
            : await GetMapAsync(activeSpaceId, cancellationToken) ?? new KnowledgeMapResponse([], []);

        var activeDocumentId = map.Documents.FirstOrDefault()?.Id ?? string.Empty;

        return new BootstrapResponse(
            new WorkspaceDto(workspace.Id.ToString(), workspace.Name, activeSpaceId.ToString()),
            spaces,
            activeSpaceId.ToString(),
            map.Folders,
            map.Documents,
            activeDocumentId);
    }

    public async Task<KnowledgeMapResponse?> GetMapAsync(Guid spaceId, CancellationToken cancellationToken = default)
    {
        var spaceExists = await _dbContext.Spaces
            .AsNoTracking()
            .AnyAsync(space => space.Id == spaceId && space.DeletedAt == null, cancellationToken);

        if (!spaceExists)
        {
            return null;
        }

        var folders = await _dbContext.Collections
            .AsNoTracking()
            .Where(collection => collection.SpaceId == spaceId && collection.DeletedAt == null)
            .OrderBy(collection => collection.SortOrder)
            .ThenBy(collection => collection.Title)
            .Select(collection => new
            {
                collection.Id,
                collection.Title,
                collection.SortOrder
            })
            .ToListAsync(cancellationToken);

        var folderIds = folders.Select(folder => folder.Id).ToArray();

        var documentCounts = await _dbContext.Documents
            .AsNoTracking()
            .Where(document => document.SpaceId == spaceId &&
                document.CollectionId.HasValue &&
                folderIds.Contains(document.CollectionId.Value) &&
                document.DeletedAt == null &&
                document.Status != DocumentStatus.Archived)
            .GroupBy(document => document.CollectionId!.Value)
            .Select(group => new { FolderId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.FolderId, item => item.Count, cancellationToken);

        var folderDtos = folders
            .Select(folder => new KnowledgeFolderDto(
                folder.Id.ToString(),
                folder.Title,
                folder.SortOrder,
                documentCounts.TryGetValue(folder.Id, out var count) ? count : 0))
            .ToArray();

        var documentDtos = await GetDocumentSummariesForSpaceAsync(spaceId, cancellationToken);

        return new KnowledgeMapResponse(folderDtos, documentDtos);
    }

    public async Task<KnowledgeDocumentDto?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents
            .AsNoTracking()
            .Where(document => document.Id == documentId && document.DeletedAt == null)
            .Select(document => new
            {
                document.Id,
                document.CollectionId,
                document.Title,
                document.Status,
                document.UpdatedAt,
                document.SortOrder,
                document.OwnerId,
                document.Revision
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return null;
        }

        var draft = await _dbContext.DocumentDrafts
            .AsNoTracking()
            .Where(draft => draft.DocumentId == documentId)
            .Select(draft => draft.Content)
            .FirstOrDefaultAsync(cancellationToken);

        var owner = document.OwnerId.HasValue
            ? await _dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == document.OwnerId.Value)
                .Select(user => new OwnerDto(user.Id.ToString(), user.DisplayName))
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var tags = await GetTagsAsync(documentId, cancellationToken);

        return new KnowledgeDocumentDto(
            document.Id.ToString(),
            document.CollectionId?.ToString() ?? string.Empty,
            document.Title,
            document.Status,
            document.UpdatedAt,
            tags,
            document.SortOrder,
            owner ?? new OwnerDto(string.Empty, "Unknown"),
            document.Status == DocumentStatus.Published ? "published" : "draft",
            ParseJsonObject(draft ?? JsonDefaults.EmptyTiptapDocument),
            document.Revision);
    }

    public async Task<KnowledgeDocumentSummaryDto?> GetDocumentSummaryAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var summaries = await GetDocumentSummariesAsync(
            _dbContext.Documents.AsNoTracking().Where(document => document.Id == documentId && document.DeletedAt == null),
            cancellationToken);

        return summaries.FirstOrDefault();
    }

    private async Task<IReadOnlyList<KnowledgeDocumentSummaryDto>> GetDocumentSummariesForSpaceAsync(
        Guid spaceId,
        CancellationToken cancellationToken)
    {
        return await GetDocumentSummariesAsync(
            _dbContext.Documents.AsNoTracking().Where(document =>
                document.SpaceId == spaceId &&
                document.DeletedAt == null &&
                document.Status != DocumentStatus.Archived),
            cancellationToken);
    }

    private async Task<IReadOnlyList<KnowledgeDocumentSummaryDto>> GetDocumentSummariesAsync(
        IQueryable<Document> query,
        CancellationToken cancellationToken)
    {
        var documents = await query
            .OrderBy(document => document.SortOrder)
            .ThenBy(document => document.Title)
            .Select(document => new
            {
                document.Id,
                document.CollectionId,
                document.Title,
                document.Status,
                document.UpdatedAt,
                document.SortOrder
            })
            .ToListAsync(cancellationToken);

        var documentIds = documents.Select(document => document.Id).ToArray();
        var tagLookup = await GetTagLookupAsync(documentIds, cancellationToken);

        return documents
            .Select(document => new KnowledgeDocumentSummaryDto(
                document.Id.ToString(),
                document.CollectionId?.ToString() ?? string.Empty,
                document.Title,
                document.Status,
                document.UpdatedAt,
                tagLookup.TryGetValue(document.Id, out var tags) ? tags : [],
                document.SortOrder))
            .ToArray();
    }

    private async Task<IReadOnlyList<string>> GetTagsAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var lookup = await GetTagLookupAsync([documentId], cancellationToken);
        return lookup.TryGetValue(documentId, out var tags) ? tags : [];
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

    private static JsonElement ParseJsonObject(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
