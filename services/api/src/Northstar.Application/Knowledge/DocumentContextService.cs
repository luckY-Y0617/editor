using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Knowledge;
using Northstar.Domain.Security;

namespace Northstar.Application.Knowledge;

public sealed class DocumentContextService : IDocumentContextService
{
    private readonly IDocumentContextQueryService _queryService;
    private readonly IScopedResourceAccessService _scopedAccessService;
    private readonly IDocumentPermissionFilterService _permissionFilterService;

    public DocumentContextService(
        IDocumentContextQueryService queryService,
        IScopedResourceAccessService scopedAccessService,
        IDocumentPermissionFilterService permissionFilterService)
    {
        _queryService = queryService;
        _scopedAccessService = scopedAccessService;
        _permissionFilterService = permissionFilterService;
    }

    public async Task<DocumentContextResponse> GetAsync(
        Guid documentId,
        CancellationToken cancellationToken = default,
        string? shareToken = null)
    {
        await _scopedAccessService.EnsureCanAccessDocumentAsync(
            documentId,
            PermissionActions.DocumentView,
            cancellationToken,
            shareToken);

        var response = await _queryService.GetContextAsync(documentId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
        return await _permissionFilterService.FilterContextAsync(response, cancellationToken);
    }
}
