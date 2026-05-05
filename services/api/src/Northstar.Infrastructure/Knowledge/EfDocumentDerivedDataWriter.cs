using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Northstar.Application.Knowledge;
using Northstar.Domain.Knowledge.Activity;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Knowledge.Links;
using Northstar.Domain.Knowledge.Versions;
using Northstar.Infrastructure.Persistence;
using Northstar.Infrastructure.Search;

namespace Northstar.Infrastructure.Knowledge;

public sealed class EfDocumentDerivedDataWriter : IDocumentDerivedDataWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly NorthstarDbContext _dbContext;
    private readonly IDocumentLinkExtractor _linkExtractor;

    public EfDocumentDerivedDataWriter(NorthstarDbContext dbContext, IDocumentLinkExtractor linkExtractor)
    {
        _dbContext = dbContext;
        _linkExtractor = linkExtractor;
    }

    public async Task RecordDocumentCreatedAsync(
        Document document,
        DocumentDraft draft,
        Guid? actorId,
        CancellationToken cancellationToken = default)
    {
        var hasInitialVersion = await _dbContext.DocumentVersions
            .AnyAsync(version => version.DocumentId == document.Id && version.VersionNo == 1, cancellationToken);

        if (!hasInitialVersion)
        {
            await _dbContext.DocumentVersions.AddAsync(
                new DocumentVersion(
                    document.WorkspaceId,
                    document.Id,
                    1,
                    "1.0",
                    DocumentVersionType.System,
                    draft.Content,
                    draft.TextContent,
                    draft.Outline,
                    draft.WordCount,
                    actorId),
                cancellationToken);
        }

        await RebuildOutgoingLinksAsync(document, draft, actorId, cancellationToken);
        await UpsertSearchIndexAsync(document, draft, cancellationToken);
        await AddActivityAsync(
            document,
            actorId,
            ActivityActions.DocumentCreated,
            "Created document.",
            "{}",
            cancellationToken);
    }

    public async Task RecordDocumentUpdatedAsync(
        Document document,
        DocumentDraft draft,
        IReadOnlyCollection<string> changedFields,
        bool rebuildLinks,
        Guid? actorId,
        CancellationToken cancellationToken = default)
    {
        if (rebuildLinks)
        {
            await RebuildOutgoingLinksAsync(document, draft, actorId, cancellationToken);
        }

        await UpsertSearchIndexAsync(document, draft, cancellationToken);

        var fields = changedFields.Count == 0
            ? "document"
            : string.Join(", ", changedFields.Distinct(StringComparer.OrdinalIgnoreCase).Order());
        var metadata = JsonSerializer.Serialize(new { changedFields }, JsonOptions);

        await AddActivityAsync(
            document,
            actorId,
            ActivityActions.DocumentUpdated,
            $"Updated {fields}.",
            metadata,
            cancellationToken);
    }

    public async Task RecordDocumentMovedAsync(
        Document document,
        DocumentDraft draft,
        Guid? oldCollectionId,
        Guid? actorId,
        CancellationToken cancellationToken = default)
    {
        await UpsertSearchIndexAsync(document, draft, cancellationToken);

        var metadata = JsonSerializer.Serialize(
            new
            {
                oldFolderId = oldCollectionId?.ToString(),
                newFolderId = document.CollectionId?.ToString()
            },
            JsonOptions);

        await AddActivityAsync(
            document,
            actorId,
            ActivityActions.DocumentMoved,
            "Moved document.",
            metadata,
            cancellationToken);
    }

    public async Task RecordDocumentArchivedAsync(
        Document document,
        Guid? actorId,
        CancellationToken cancellationToken = default)
    {
        await RemoveSearchIndexAsync(document.Id, cancellationToken);
        await AddActivityAsync(
            document,
            actorId,
            ActivityActions.DocumentArchived,
            "Archived document.",
            "{}",
            cancellationToken);
    }

    public async Task RecordDocumentRestoredAsync(
        Document document,
        DocumentDraft draft,
        Guid? actorId,
        CancellationToken cancellationToken = default)
    {
        await UpsertSearchIndexAsync(document, draft, cancellationToken);
        await AddActivityAsync(
            document,
            actorId,
            ActivityActions.DocumentRestored,
            "Restored document.",
            "{}",
            cancellationToken);
    }

    public async Task RecordDocumentDeletedAsync(
        Document document,
        Guid? actorId,
        CancellationToken cancellationToken = default)
    {
        await RemoveSearchIndexAsync(document.Id, cancellationToken);

        var links = await _dbContext.DocumentLinks
            .Where(link => link.SourceDocumentId == document.Id || link.TargetDocumentId == document.Id)
            .ToListAsync(cancellationToken);
        _dbContext.DocumentLinks.RemoveRange(links);

        await AddActivityAsync(
            document,
            actorId,
            ActivityActions.DocumentDeleted,
            "Deleted document.",
            "{}",
            cancellationToken);
    }

    public async Task RecordDocumentImportedAsync(
        Document document,
        DocumentDraft draft,
        Guid? actorId,
        CancellationToken cancellationToken = default)
    {
        var hasInitialVersion = await _dbContext.DocumentVersions
            .AnyAsync(version => version.DocumentId == document.Id && version.VersionNo == 1, cancellationToken);

        if (!hasInitialVersion)
        {
            await _dbContext.DocumentVersions.AddAsync(
                new DocumentVersion(
                    document.WorkspaceId,
                    document.Id,
                    1,
                    "1.0",
                    DocumentVersionType.Imported,
                    draft.Content,
                    draft.TextContent,
                    draft.Outline,
                    draft.WordCount,
                    actorId),
                cancellationToken);
        }

        await RebuildOutgoingLinksAsync(document, draft, actorId, cancellationToken);
        await UpsertSearchIndexAsync(document, draft, cancellationToken);
        await AddActivityAsync(
            document,
            actorId,
            ActivityActions.DocumentImported,
            "Imported document.",
            "{}",
            cancellationToken);
    }

    private async Task RebuildOutgoingLinksAsync(
        Document document,
        DocumentDraft draft,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var existingLinks = await _dbContext.DocumentLinks
            .Where(link => link.SourceDocumentId == document.Id)
            .ToListAsync(cancellationToken);

        _dbContext.DocumentLinks.RemoveRange(existingLinks);

        using var jsonDocument = JsonDocument.Parse(draft.Content);
        var candidates = _linkExtractor
            .Extract(jsonDocument.RootElement)
            .Where(candidate => candidate.TargetDocumentId != document.Id)
            .GroupBy(candidate => candidate.TargetDocumentId)
            .Select(group => group.First())
            .ToArray();

        if (candidates.Length == 0)
        {
            return;
        }

        var targetIds = candidates.Select(candidate => candidate.TargetDocumentId).ToArray();
        var validTargetIds = await _dbContext.Documents
            .AsNoTracking()
            .Where(target => target.WorkspaceId == document.WorkspaceId &&
                target.DeletedAt == null &&
                target.Status != DocumentStatus.Archived &&
                targetIds.Contains(target.Id))
            .Select(target => target.Id)
            .ToListAsync(cancellationToken);

        var trackedTargetIds = _dbContext.Documents.Local
            .Where(target => target.WorkspaceId == document.WorkspaceId &&
                target.DeletedAt == null &&
                target.Status != DocumentStatus.Archived &&
                targetIds.Contains(target.Id))
            .Select(target => target.Id);

        var validTargetSet = validTargetIds.Concat(trackedTargetIds).ToHashSet();
        foreach (var candidate in candidates)
        {
            if (!validTargetSet.Contains(candidate.TargetDocumentId))
            {
                continue;
            }

            await _dbContext.DocumentLinks.AddAsync(
                new DocumentLink(
                    document.WorkspaceId,
                    document.Id,
                    candidate.TargetDocumentId,
                    targetUrl: null,
                    DocumentLinkType.Reference,
                    candidate.AnchorText,
                    createdBy: actorId),
                cancellationToken);
        }
    }

    private async Task UpsertSearchIndexAsync(
        Document document,
        DocumentDraft draft,
        CancellationToken cancellationToken)
    {
        if (document.DeletedAt.HasValue || document.Status == DocumentStatus.Archived)
        {
            await RemoveSearchIndexAsync(document.Id, cancellationToken);
            return;
        }

        var index = await _dbContext.DocumentSearchIndexes
            .FirstOrDefaultAsync(index => index.DocumentId == document.Id, cancellationToken);

        if (index is null)
        {
            await _dbContext.DocumentSearchIndexes.AddAsync(
                new DocumentSearchIndex(
                    document.Id,
                    document.WorkspaceId,
                    document.SpaceId,
                    document.Title,
                    draft.TextContent),
                cancellationToken);
            return;
        }

        index.Update(document.Title, draft.TextContent, document.SpaceId);
    }

    private async Task RemoveSearchIndexAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var index = await _dbContext.DocumentSearchIndexes
            .FirstOrDefaultAsync(index => index.DocumentId == documentId, cancellationToken);
        if (index is not null)
        {
            _dbContext.DocumentSearchIndexes.Remove(index);
        }
    }

    private async Task AddActivityAsync(
        Document document,
        Guid? actorId,
        string action,
        string summary,
        string metadata,
        CancellationToken cancellationToken)
    {
        await _dbContext.ActivityEvents.AddAsync(
            new ActivityEvent(
                document.WorkspaceId,
                actorId,
                ActivityEntityTypes.Document,
                document.Id,
                action,
                summary,
                metadata),
            cancellationToken);
    }
}
