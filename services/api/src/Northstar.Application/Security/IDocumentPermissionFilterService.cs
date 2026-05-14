using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Security;

public interface IDocumentPermissionFilterService
{
    Task<BootstrapResponse> FilterBootstrapAsync(
        BootstrapResponse response,
        CancellationToken cancellationToken = default);

    Task<KnowledgeMapResponse> FilterMapAsync(
        KnowledgeMapResponse response,
        CancellationToken cancellationToken = default);

    Task<SearchResponse> FilterSearchAsync(
        SearchResponse response,
        CancellationToken cancellationToken = default);

    Task<DocumentContextResponse> FilterContextAsync(
        DocumentContextResponse response,
        CancellationToken cancellationToken = default);

    Task<ExportSpaceResponse> FilterExportAsync(
        ExportSpaceResponse response,
        CancellationToken cancellationToken = default);
}
