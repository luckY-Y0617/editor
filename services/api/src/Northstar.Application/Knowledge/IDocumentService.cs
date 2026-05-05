using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public interface IDocumentService
{
    Task<CreateDocumentResponse> CreateAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default);
    Task<GetDocumentResponse> GetAsync(Guid documentId, CancellationToken cancellationToken = default, string? shareToken = null);
    Task<UpdateDocumentResponse> UpdateAsync(Guid documentId, UpdateDocumentRequest request, CancellationToken cancellationToken = default);
    Task<MoveDocumentResponse> MoveAsync(Guid documentId, MoveDocumentRequest request, CancellationToken cancellationToken = default);
    Task<MoveDocumentResponse> ArchiveAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<MoveDocumentResponse> RestoreAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default);
}
