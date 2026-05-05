using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public interface IKnowledgeQueryService
{
    Task<BootstrapResponse?> GetBootstrapAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<KnowledgeMapResponse?> GetMapAsync(Guid spaceId, CancellationToken cancellationToken = default);
    Task<KnowledgeDocumentDto?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<KnowledgeDocumentSummaryDto?> GetDocumentSummaryAsync(Guid documentId, CancellationToken cancellationToken = default);
}
