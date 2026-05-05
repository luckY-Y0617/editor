using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Knowledge;
using Northstar.Domain.Security;

namespace Northstar.Application.Knowledge;

public sealed class DocumentActivityService : IDocumentActivityService
{
    private readonly IDocumentActivityQueryService _queryService;
    private readonly IScopedResourceAccessService _scopedAccessService;

    public DocumentActivityService(
        IDocumentActivityQueryService queryService,
        IScopedResourceAccessService scopedAccessService)
    {
        _queryService = queryService;
        _scopedAccessService = scopedAccessService;
    }

    public async Task<DocumentActivityResponse> GetAsync(
        Guid documentId,
        CancellationToken cancellationToken = default,
        string? shareToken = null)
    {
        await _scopedAccessService.EnsureCanAccessDocumentAsync(
            documentId,
            PermissionActions.ActivityView,
            cancellationToken,
            shareToken);

        return await _queryService.GetActivityAsync(documentId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
    }
}
