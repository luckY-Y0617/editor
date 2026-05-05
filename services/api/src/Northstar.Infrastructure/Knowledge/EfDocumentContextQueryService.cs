using Microsoft.EntityFrameworkCore;
using Northstar.Application.Knowledge;
using Northstar.Contracts.Knowledge;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Knowledge.Links;
using Northstar.Domain.Knowledge.Versions;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Knowledge;

public sealed class EfDocumentContextQueryService : IDocumentContextQueryService
{
    private const int ExcerptLength = 160;

    private readonly NorthstarDbContext _dbContext;

    public EfDocumentContextQueryService(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DocumentContextResponse?> GetContextAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents
            .AsNoTracking()
            .Where(document => document.Id == documentId && document.DeletedAt == null)
            .Select(document => new
            {
                document.Id,
                document.WorkspaceId,
                document.CollectionId,
                document.Title,
                document.UpdatedAt,
                document.LastEditedBy,
                document.OwnerId,
                document.CreatedBy
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return null;
        }

        var codes = await BuildDocumentCodesAsync(document.WorkspaceId, cancellationToken);

        var relatedRows = await (
            from link in _dbContext.DocumentLinks.AsNoTracking()
            join target in _dbContext.Documents.AsNoTracking() on link.TargetDocumentId equals target.Id
            where link.WorkspaceId == document.WorkspaceId &&
                link.SourceDocumentId == documentId &&
                target.DeletedAt == null &&
                target.Status != DocumentStatus.Archived &&
                (link.LinkType == DocumentLinkType.Reference || link.LinkType == DocumentLinkType.Related)
            orderby target.SortOrder, target.Title
            select new
            {
                target.Id,
                target.Title
            })
            .ToListAsync(cancellationToken);

        var backlinkRows = await (
            from link in _dbContext.DocumentLinks.AsNoTracking()
            join source in _dbContext.Documents.AsNoTracking() on link.SourceDocumentId equals source.Id
            join draft in _dbContext.DocumentDrafts.AsNoTracking() on source.Id equals draft.DocumentId into drafts
            from draft in drafts.DefaultIfEmpty()
            where link.WorkspaceId == document.WorkspaceId &&
                link.TargetDocumentId == documentId &&
                source.DeletedAt == null &&
                source.Status != DocumentStatus.Archived &&
                (link.LinkType == DocumentLinkType.Reference || link.LinkType == DocumentLinkType.Related)
            orderby source.SortOrder, source.Title
            select new
            {
                source.Id,
                source.Title,
                link.AnchorText,
                TextContent = draft == null ? string.Empty : draft.TextContent
            })
            .ToListAsync(cancellationToken);

        var versionRows = await _dbContext.DocumentVersions
            .AsNoTracking()
            .Where(version => version.DocumentId == documentId)
            .OrderByDescending(version => version.VersionNo)
            .Select(version => new
            {
                version.Id,
                version.Label,
                version.VersionType,
                version.CreatedAt,
                version.CreatedBy
            })
            .ToListAsync(cancellationToken);

        var authorIds = versionRows
            .Select(version => version.CreatedBy)
            .Append(document.LastEditedBy)
            .Append(document.OwnerId)
            .Append(document.CreatedBy)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        var authors = authorIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.Users
                .AsNoTracking()
                .Where(user => authorIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);

        var currentAuthorId = document.LastEditedBy ?? document.OwnerId ?? document.CreatedBy;
        var versionTrail = new List<VersionTrailItemDto>
        {
            new(
                $"{document.Id}:draft",
                "Current Draft",
                document.UpdatedAt,
                ResolveAuthor(authors, currentAuthorId),
                "draft")
        };

        versionTrail.AddRange(versionRows.Select(version => new VersionTrailItemDto(
            version.Id.ToString(),
            version.Label,
            version.CreatedAt,
            ResolveAuthor(authors, version.CreatedBy),
            version.VersionType == DocumentVersionType.Published ? "published" : "draft")));

        var relatedDocuments = relatedRows
            .Select(row => new RelatedDocumentDto(
                row.Id.ToString(),
                codes.TryGetValue(row.Id, out var code) ? code : string.Empty,
                row.Title))
            .ToArray();

        var backlinks = backlinkRows
            .Select(row => new BacklinkItemDto(
                row.Id.ToString(),
                codes.TryGetValue(row.Id, out var code) ? code : string.Empty,
                row.Title,
                CreateExcerpt(row.AnchorText, row.TextContent)))
            .ToArray();

        return new DocumentContextResponse(relatedDocuments, versionTrail, backlinks);
    }

    private async Task<Dictionary<Guid, string>> BuildDocumentCodesAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from document in _dbContext.Documents.AsNoTracking()
            join collection in _dbContext.Collections.AsNoTracking()
                on document.CollectionId equals collection.Id into collections
            from collection in collections.DefaultIfEmpty()
            where document.WorkspaceId == workspaceId && document.DeletedAt == null
                && document.Status != DocumentStatus.Archived
            orderby collection == null ? decimal.MaxValue : collection.SortOrder,
                collection == null ? string.Empty : collection.Title,
                document.SortOrder,
                document.Title
            select new
            {
                document.Id,
                CollectionId = document.CollectionId,
                CollectionSortOrder = collection == null ? decimal.MaxValue : collection.SortOrder,
                CollectionTitle = collection == null ? string.Empty : collection.Title
            })
            .ToListAsync(cancellationToken);

        var collectionOrdinals = rows
            .GroupBy(row => row.CollectionId ?? Guid.Empty)
            .OrderBy(group => group.Min(row => row.CollectionSortOrder))
            .ThenBy(group => group.Min(row => row.CollectionTitle))
            .Select((group, index) => new { group.Key, Ordinal = index + 1 })
            .ToDictionary(item => item.Key, item => item.Ordinal);

        var documentOrdinals = new Dictionary<Guid, string>();
        foreach (var group in rows.GroupBy(row => row.CollectionId ?? Guid.Empty))
        {
            var collectionOrdinal = collectionOrdinals[group.Key];
            var index = 1;
            foreach (var row in group)
            {
                documentOrdinals[row.Id] = $"{collectionOrdinal:00}.{index:000}";
                index++;
            }
        }

        return documentOrdinals;
    }

    private static string ResolveAuthor(IReadOnlyDictionary<Guid, string> authors, Guid? authorId)
    {
        return authorId.HasValue && authors.TryGetValue(authorId.Value, out var name)
            ? name
            : "Unknown";
    }

    private static string CreateExcerpt(string? anchorText, string? fallbackText)
    {
        var text = string.IsNullOrWhiteSpace(anchorText) ? fallbackText : anchorText;
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        text = text.Trim();
        return text.Length <= ExcerptLength ? text : text[..ExcerptLength];
    }
}
