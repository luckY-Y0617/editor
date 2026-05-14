using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public interface IDocumentService
{
    Task<CreateDocumentResponse> CreateAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default);
    Task<GetDocumentResponse> GetAsync(Guid documentId, CancellationToken cancellationToken = default, string? shareToken = null);
    Task<DocumentVersionsResponse> GetVersionsAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<DocumentVersionResponse> GetVersionAsync(Guid documentId, Guid versionId, CancellationToken cancellationToken = default);
    Task<PublishDocumentVersionResponse> PublishVersionAsync(Guid documentId, PublishDocumentVersionRequest request, CancellationToken cancellationToken = default);
    Task<UnpublishDocumentVersionResponse> UnpublishVersionAsync(Guid documentId, UnpublishDocumentVersionRequest request, CancellationToken cancellationToken = default);
    Task<RestoreDocumentVersionResponse> RestoreVersionAsync(Guid documentId, Guid versionId, RestoreDocumentVersionRequest request, CancellationToken cancellationToken = default);
    Task<CompareDocumentVersionsResponse> CompareVersionsAsync(Guid documentId, CompareDocumentVersionsRequest request, CancellationToken cancellationToken = default);
    Task<UpdateDocumentResponse> UpdateAsync(Guid documentId, UpdateDocumentRequest request, CancellationToken cancellationToken = default);
    Task<MoveDocumentResponse> MoveAsync(Guid documentId, MoveDocumentRequest request, CancellationToken cancellationToken = default);
    Task<MoveDocumentResponse> ArchiveAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<MoveDocumentResponse> RestoreAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default);
}
