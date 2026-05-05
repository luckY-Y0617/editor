using Northstar.Contracts.Files;
using Northstar.Domain.Files;

namespace Northstar.Application.Files;

public interface IFileRepository
{
    Task<UploadSession?> GetUploadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<UploadSession?> GetUploadSessionByIdempotencyKeyAsync(
        Guid workspaceId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task AddUploadSessionAsync(UploadSession session, CancellationToken cancellationToken = default);

    Task<StoredFile?> GetFileAsync(Guid fileId, bool includeDeleted = false, CancellationToken cancellationToken = default);

    Task AddFileAsync(StoredFile file, CancellationToken cancellationToken = default);

    Task<DocumentLocation?> GetDocumentLocationAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentAttachmentDto>> GetDocumentAttachmentsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<DocumentAttachmentDto?> GetDocumentAttachmentAsync(
        Guid documentId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<DocumentAttachment?> GetDocumentAttachmentEntityAsync(
        Guid documentId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<DocumentAttachment?> FindDocumentAttachmentAsync(
        Guid documentId,
        Guid fileId,
        string relationType,
        CancellationToken cancellationToken = default);

    Task<DocumentAttachmentDto?> FindDocumentAttachmentDtoAsync(
        Guid documentId,
        Guid fileId,
        string relationType,
        CancellationToken cancellationToken = default);

    Task AddDocumentAttachmentAsync(DocumentAttachment attachment, CancellationToken cancellationToken = default);

    void RemoveDocumentAttachment(DocumentAttachment attachment);

    Task<int> CountActiveAttachmentsAsync(Guid fileId, CancellationToken cancellationToken = default);

    Task AddOutboxEventAsync(FileOutboxEvent outboxEvent, CancellationToken cancellationToken = default);

    Task<int> CountOutboxEventsAsync(
        Guid aggregateId,
        string eventType,
        CancellationToken cancellationToken = default);
}
