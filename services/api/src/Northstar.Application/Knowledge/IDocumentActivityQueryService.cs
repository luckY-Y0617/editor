using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public interface IDocumentActivityQueryService
{
    Task<DocumentActivityResponse?> GetActivityAsync(Guid documentId, CancellationToken cancellationToken = default);
}
