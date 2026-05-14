using Northstar.Contracts.Knowledge;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class DocumentPermissionFilterService : IDocumentPermissionFilterService
{
    private readonly IScopedResourceAccessService _resourceAccessService;
    private readonly IEffectivePermissionService _effectivePermissionService;

    public DocumentPermissionFilterService(
        IScopedResourceAccessService resourceAccessService,
        IEffectivePermissionService effectivePermissionService)
    {
        _resourceAccessService = resourceAccessService;
        _effectivePermissionService = effectivePermissionService;
    }

    public async Task<BootstrapResponse> FilterBootstrapAsync(
        BootstrapResponse response,
        CancellationToken cancellationToken = default)
    {
        var filteredMap = await FilterMapAsync(
            new KnowledgeMapResponse(response.Folders, response.Documents),
            cancellationToken);

        return response with
        {
            Folders = filteredMap.Folders,
            Documents = filteredMap.Documents,
            ActiveDocumentId = filteredMap.Documents.FirstOrDefault()?.Id ?? string.Empty
        };
    }

    public async Task<KnowledgeMapResponse> FilterMapAsync(
        KnowledgeMapResponse response,
        CancellationToken cancellationToken = default)
    {
        var allowedDocuments = await FilterDocumentSummariesAsync(response.Documents, cancellationToken);
        var counts = allowedDocuments
            .Where(document => Guid.TryParse(document.FolderId, out _))
            .GroupBy(document => document.FolderId)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        var folders = response.Folders
            .Select(folder => folder with
            {
                DocumentCount = counts.TryGetValue(folder.Id, out var count) ? count : 0
            })
            .ToArray();

        return new KnowledgeMapResponse(folders, allowedDocuments);
    }

    public async Task<SearchResponse> FilterSearchAsync(
        SearchResponse response,
        CancellationToken cancellationToken = default)
    {
        var allowedDocumentIds = await GetAllowedDocumentIdsAsync(
            response.Results.Select(result => result.Id),
            cancellationToken);
        return new SearchResponse(
            response.Results
                .Where(result => Guid.TryParse(result.Id, out var documentId) && allowedDocumentIds.Contains(documentId))
                .ToArray());
    }

    public async Task<DocumentContextResponse> FilterContextAsync(
        DocumentContextResponse response,
        CancellationToken cancellationToken = default)
    {
        var relatedIds = response.RelatedDocuments.Select(related => related.Id);
        var backlinkIds = response.Backlinks.Select(backlink => backlink.Id);
        var allowedDocumentIds = await GetAllowedDocumentIdsAsync(
            relatedIds.Concat(backlinkIds),
            cancellationToken);

        return response with
        {
            RelatedDocuments = response.RelatedDocuments
                .Where(related => Guid.TryParse(related.Id, out var documentId) && allowedDocumentIds.Contains(documentId))
                .ToArray(),
            Backlinks = response.Backlinks
                .Where(backlink => Guid.TryParse(backlink.Id, out var documentId) && allowedDocumentIds.Contains(documentId))
                .ToArray()
        };
    }

    public async Task<ExportSpaceResponse> FilterExportAsync(
        ExportSpaceResponse response,
        CancellationToken cancellationToken = default)
    {
        var allowedDocumentIds = await GetAllowedDocumentIdsAsync(
            response.Documents.Select(document => document.Id),
            cancellationToken);
        return response with
        {
            Documents = response.Documents
                .Where(document => Guid.TryParse(document.Id, out var documentId) && allowedDocumentIds.Contains(documentId))
                .ToArray()
        };
    }

    private async Task<IReadOnlyList<KnowledgeDocumentSummaryDto>> FilterDocumentSummariesAsync(
        IReadOnlyList<KnowledgeDocumentSummaryDto> documents,
        CancellationToken cancellationToken)
    {
        var allowedDocumentIds = await GetAllowedDocumentIdsAsync(
            documents.Select(document => document.Id),
            cancellationToken);
        return documents
            .Where(document => Guid.TryParse(document.Id, out var documentId) && allowedDocumentIds.Contains(documentId))
            .ToArray();
    }

    private async Task<IReadOnlySet<Guid>> GetAllowedDocumentIdsAsync(
        IEnumerable<string> documentIds,
        CancellationToken cancellationToken)
    {
        var userId = await _resourceAccessService.GetRequiredUserIdAsync(cancellationToken);
        var parsedDocumentIds = documentIds
            .Select(value => Guid.TryParse(value, out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        var permissions = await _effectivePermissionService.AuthorizeDocumentsAsync(
            parsedDocumentIds,
            userId,
            PermissionActions.DocumentView,
            cancellationToken);
        return permissions
            .Where(item => item.Value.Allowed)
            .Select(item => item.Key)
            .ToHashSet();
    }
}
