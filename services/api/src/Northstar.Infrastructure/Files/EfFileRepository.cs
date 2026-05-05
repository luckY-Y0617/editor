using Microsoft.EntityFrameworkCore;
using Northstar.Application.Files;
using Northstar.Contracts.Files;
using Northstar.Domain.Files;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Files;

public sealed class EfFileRepository : IFileRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfFileRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UploadSession?> GetUploadSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UploadSessions
            .FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken);
    }

    public Task<UploadSession?> GetUploadSessionByIdempotencyKeyAsync(
        Guid workspaceId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UploadSessions
            .FirstOrDefaultAsync(
                session => session.WorkspaceId == workspaceId &&
                    session.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task AddUploadSessionAsync(
        UploadSession session,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.UploadSessions.AddAsync(session, cancellationToken);
    }

    public Task<StoredFile?> GetFileAsync(
        Guid fileId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Files.Where(file => file.Id == fileId);
        if (!includeDeleted)
        {
            query = query.Where(file => file.DeletedAt == null);
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddFileAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        await _dbContext.Files.AddAsync(file, cancellationToken);
    }

    public Task<DocumentLocation?> GetDocumentLocationAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Documents
            .AsNoTracking()
            .Where(document => document.Id == documentId)
            .Select(document => new DocumentLocation(
                document.Id,
                document.WorkspaceId,
                document.DeletedAt != null))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentAttachmentDto>> GetDocumentAttachmentsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
            from attachment in _dbContext.DocumentAttachments.AsNoTracking()
            join file in _dbContext.Files.AsNoTracking() on attachment.FileId equals file.Id
            join document in _dbContext.Documents.AsNoTracking() on attachment.DocumentId equals document.Id
            where attachment.DocumentId == documentId &&
                document.DeletedAt == null &&
                file.DeletedAt == null
            orderby attachment.CreatedAt
            select new AttachmentReadRow(attachment, file))
            .ToListAsync(cancellationToken);

        return rows.Select(MapAttachment).ToArray();
    }

    public async Task<DocumentAttachmentDto?> GetDocumentAttachmentAsync(
        Guid documentId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var row = await (
            from attachment in _dbContext.DocumentAttachments.AsNoTracking()
            join file in _dbContext.Files.AsNoTracking() on attachment.FileId equals file.Id
            where attachment.DocumentId == documentId &&
                attachment.Id == attachmentId &&
                file.DeletedAt == null
            select new AttachmentReadRow(attachment, file))
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : MapAttachment(row);
    }

    public Task<DocumentAttachment?> GetDocumentAttachmentEntityAsync(
        Guid documentId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.DocumentAttachments
            .FirstOrDefaultAsync(
                attachment => attachment.DocumentId == documentId &&
                    attachment.Id == attachmentId,
                cancellationToken);
    }

    public Task<DocumentAttachment?> FindDocumentAttachmentAsync(
        Guid documentId,
        Guid fileId,
        string relationType,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.DocumentAttachments
            .FirstOrDefaultAsync(
                attachment => attachment.DocumentId == documentId &&
                    attachment.FileId == fileId &&
                    attachment.RelationType == relationType,
                cancellationToken);
    }

    public async Task<DocumentAttachmentDto?> FindDocumentAttachmentDtoAsync(
        Guid documentId,
        Guid fileId,
        string relationType,
        CancellationToken cancellationToken = default)
    {
        var row = await (
            from attachment in _dbContext.DocumentAttachments.AsNoTracking()
            join file in _dbContext.Files.AsNoTracking() on attachment.FileId equals file.Id
            where attachment.DocumentId == documentId &&
                attachment.FileId == fileId &&
                attachment.RelationType == relationType &&
                file.DeletedAt == null
            select new AttachmentReadRow(attachment, file))
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : MapAttachment(row);
    }

    public async Task AddDocumentAttachmentAsync(
        DocumentAttachment attachment,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentAttachments.AddAsync(attachment, cancellationToken);
    }

    public void RemoveDocumentAttachment(DocumentAttachment attachment)
    {
        _dbContext.DocumentAttachments.Remove(attachment);
    }

    public Task<int> CountActiveAttachmentsAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        return (
            from attachment in _dbContext.DocumentAttachments.AsNoTracking()
            join document in _dbContext.Documents.AsNoTracking() on attachment.DocumentId equals document.Id
            where attachment.FileId == fileId && document.DeletedAt == null
            select attachment.Id)
            .CountAsync(cancellationToken);
    }

    public async Task AddOutboxEventAsync(
        FileOutboxEvent outboxEvent,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.FileOutboxEvents.AddAsync(outboxEvent, cancellationToken);
    }

    public Task<int> CountOutboxEventsAsync(
        Guid aggregateId,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.FileOutboxEvents
            .AsNoTracking()
            .CountAsync(
                outboxEvent => outboxEvent.AggregateId == aggregateId &&
                    outboxEvent.EventType == eventType,
                cancellationToken);
    }

    private static DocumentAttachmentDto MapAttachment(AttachmentReadRow row)
    {
        return new DocumentAttachmentDto(
            row.Attachment.Id.ToString(),
            row.Attachment.WorkspaceId.ToString(),
            row.Attachment.DocumentId.ToString(),
            row.Attachment.FileId.ToString(),
            row.Attachment.RelationType,
            FileDtoMapper.ParseJson(row.Attachment.Metadata),
            row.Attachment.CreatedAt,
            FileDtoMapper.ToDto(row.File));
    }

    private sealed record AttachmentReadRow(DocumentAttachment Attachment, StoredFile File);
}
