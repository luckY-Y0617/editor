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

    public DocumentContextService(
        IDocumentContextQueryService queryService,
        IScopedResourceAccessService scopedAccessService)
    {
        _queryService = queryService;
        _scopedAccessService = scopedAccessService;
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

        return await _queryService.GetContextAsync(documentId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
    }
}
