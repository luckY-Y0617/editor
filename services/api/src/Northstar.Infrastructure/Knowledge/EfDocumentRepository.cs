using Microsoft.EntityFrameworkCore;
using Northstar.Application.Knowledge;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Knowledge.Tags;
using Northstar.Domain.Workspaces;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Knowledge;

public sealed class EfDocumentRepository : IDocumentRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfDocumentRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CollectionLocation?> GetCollectionLocationAsync(
        Guid collectionId,
        CancellationToken cancellationToken = default)
    {
        var collection = await _dbContext.Collections
            .AsNoTracking()
            .Where(collection => collection.Id == collectionId && collection.DeletedAt == null)
            .Select(collection => new
            {
                collection.Id,
                collection.WorkspaceId,
                collection.SpaceId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (collection is null)
        {
            return null;
        }

        var ownerId = await _dbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(member => member.WorkspaceId == collection.WorkspaceId &&
                member.Role == WorkspaceMemberRole.Owner &&
                member.Status == WorkspaceMemberStatus.Active)
            .OrderBy(member => member.CreatedAt)
            .Select(member => (Guid?)member.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        return new CollectionLocation(collection.WorkspaceId, collection.SpaceId, collection.Id, ownerId);
    }

    public async Task<decimal> GetNextDocumentSortOrderAsync(
        Guid collectionId,
        CancellationToken cancellationToken = default)
    {
        var maxSortOrder = await _dbContext.Documents
            .AsNoTracking()
            .Where(document => document.CollectionId == collectionId && document.DeletedAt == null)
            .Select(document => (decimal?)document.SortOrder)
            .MaxAsync(cancellationToken);

        return (maxSortOrder ?? 0m) + 1m;
    }

    public async Task AddDocumentAsync(
        Document document,
        DocumentDraft draft,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Documents.AddAsync(document, cancellationToken);
        await _dbContext.DocumentDrafts.AddAsync(draft, cancellationToken);
    }

    public async Task<DocumentEditState?> GetDocumentEditStateAsync(
        Guid documentId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var documentQuery = _dbContext.Documents.Where(document => document.Id == documentId);
        if (!includeDeleted)
        {
            documentQuery = documentQuery.Where(document => document.DeletedAt == null);
        }

        var document = await documentQuery.FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return null;
        }

        var draft = await _dbContext.DocumentDrafts
            .FirstOrDefaultAsync(draft => draft.DocumentId == documentId, cancellationToken);

        if (draft is null)
        {
            draft = new DocumentDraft(document.Id, document.WorkspaceId);
            await _dbContext.DocumentDrafts.AddAsync(draft, cancellationToken);
        }

        return new DocumentEditState(document, draft);
    }

    public async Task<IReadOnlyList<string>> GetDocumentTagNamesAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from documentTag in _dbContext.DocumentTags.AsNoTracking()
            join tag in _dbContext.Tags.AsNoTracking() on documentTag.TagId equals tag.Id
            where documentTag.DocumentId == documentId && tag.DeletedAt == null
            orderby tag.Name
            select tag.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceDocumentTagsAsync(
        Guid workspaceId,
        Guid documentId,
        IReadOnlyCollection<string> tagNames,
        Guid? actorId,
        CancellationToken cancellationToken = default)
    {
        var existingDocumentTags = await _dbContext.DocumentTags
            .Where(documentTag => documentTag.DocumentId == documentId)
            .ToListAsync(cancellationToken);

        _dbContext.DocumentTags.RemoveRange(existingDocumentTags);

        if (tagNames.Count == 0)
        {
            return;
        }

        var normalizedTags = tagNames
            .Select(name => new { Name = name, Slug = SlugNormalizer.Normalize(name) })
            .Where(tag => !string.IsNullOrWhiteSpace(tag.Slug))
            .DistinctBy(tag => tag.Slug)
            .ToArray();

        var slugs = normalizedTags.Select(tag => tag.Slug).ToArray();
        var existingTags = await _dbContext.Tags
            .Where(tag => tag.WorkspaceId == workspaceId && slugs.Contains(tag.Slug))
            .ToListAsync(cancellationToken);

        foreach (var normalizedTag in normalizedTags)
        {
            var tag = existingTags.FirstOrDefault(tag => tag.Slug == normalizedTag.Slug);
            if (tag is null)
            {
                tag = new Tag(workspaceId, normalizedTag.Name, normalizedTag.Slug, actorId);
                await _dbContext.Tags.AddAsync(tag, cancellationToken);
                existingTags.Add(tag);
            }

            await _dbContext.DocumentTags.AddAsync(new DocumentTag(workspaceId, documentId, tag.Id), cancellationToken);
        }
    }
}
