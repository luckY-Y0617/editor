using Northstar.Contracts.Security;

namespace Northstar.Application.Security;

public interface IShareLinkService
{
    Task<ShareLinksResponse> GetShareLinksAsync(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task<CreateShareLinkResponse> CreateShareLinkAsync(
        string resourceType,
        Guid resourceId,
        CreateShareLinkRequest request,
        CancellationToken cancellationToken = default);

    Task RevokeShareLinkAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default);

    Task<ResolveShareLinkResponse> ResolveShareLinkAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<ResolvePublicShareLinkResponse> ResolvePublicShareLinkAsync(
        string token,
        string? passwordProof = null,
        CancellationToken cancellationToken = default);

    Task<PublicShareDocumentResponse> GetPublicShareDocumentAsync(
        string token,
        string? passwordProof = null,
        CancellationToken cancellationToken = default);

    Task<PublicShareCollectionResponse> GetPublicShareCollectionAsync(
        string token,
        string? passwordProof = null,
        CancellationToken cancellationToken = default);
}
