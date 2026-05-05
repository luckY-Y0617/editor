using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public interface IDocumentActivityService
{
    Task<DocumentActivityResponse> GetAsync(
        Guid documentId,
        CancellationToken cancellationToken = default,
        string? shareToken = null);
}
