using Northstar.Contracts.Files;

namespace Northstar.Application.Files;

public interface IDocumentAttachmentService
{
    Task<DocumentAttachmentsResponse> GetAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<DocumentAttachmentDto> AttachAsync(
        Guid documentId,
        AttachFileToDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid documentId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);
}
