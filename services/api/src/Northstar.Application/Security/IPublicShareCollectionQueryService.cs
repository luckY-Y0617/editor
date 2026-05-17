using Northstar.Contracts.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IPublicShareCollectionQueryService
{
    Task<PublicShareCollectionDto?> GetCollectionAsync(
        Guid workspaceId,
        Guid collectionId,
        CancellationToken cancellationToken = default);

    Task<PublicShareTreeResponse?> GetTreeAsync(
        ShareLink scope,
        CancellationToken cancellationToken = default);

    Task<PublicShareDocumentDto?> GetDocumentAsync(
        ShareLink scope,
        Guid documentId,
        CancellationToken cancellationToken = default);
}
