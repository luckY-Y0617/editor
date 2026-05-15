using Northstar.Contracts.Security;

namespace Northstar.Application.Security;

public interface IShareLinkService
{
    Task<ShareLinksResponse> GetShareLinksAsync(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task<LinkManagementListResponse> SearchShareLinksAsync(
        Guid workspaceId,
        string? resourceType,
        Guid? resourceId,
        string? audience,
        string? roleKey,
        string? status,
        string? q,
        int? offset,
        int? limit,
        CancellationToken cancellationToken = default);

    Task<ShareLinkDto> GetShareLinkAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default);

    Task<LinkManagementDto> GetManagedShareLinkAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default);

    Task<LinkManagementDto> UpdateShareLinkAsync(
        Guid shareLinkId,
        UpdateShareLinkRequest request,
        CancellationToken cancellationToken = default);

    Task<CreateShareLinkResponse> CreateShareLinkAsync(
        string resourceType,
        Guid resourceId,
        CreateShareLinkRequest request,
        CancellationToken cancellationToken = default);

    Task RevokeShareLinkAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default);

    Task<LinkManagementDto> PauseShareLinkAsync(
        Guid shareLinkId,
        ShareLinkPauseRequest? request,
        CancellationToken cancellationToken = default);

    Task<LinkManagementDto> ResumeShareLinkAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default);

    Task<CopyShareLinkResponse> CopyShareLinkAsync(
        Guid shareLinkId,
        ShareLinkCopyEventRequest? request,
        CancellationToken cancellationToken = default);

    Task RecordCopyEventAsync(
        Guid shareLinkId,
        ShareLinkCopyEventRequest? request,
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
