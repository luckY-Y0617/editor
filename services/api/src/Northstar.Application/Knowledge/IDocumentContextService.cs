using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public interface IDocumentContextService
{
    Task<DocumentContextResponse> GetAsync(
        Guid documentId,
        CancellationToken cancellationToken = default,
        string? shareToken = null);
}
