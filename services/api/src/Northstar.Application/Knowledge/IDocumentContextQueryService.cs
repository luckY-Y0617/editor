using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public interface IDocumentContextQueryService
{
    Task<DocumentContextResponse?> GetContextAsync(Guid documentId, CancellationToken cancellationToken = default);
}
