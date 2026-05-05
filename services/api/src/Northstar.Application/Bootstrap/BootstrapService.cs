using Northstar.Application.Common;
using Northstar.Application.Knowledge;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Bootstrap;

public sealed class BootstrapService : IBootstrapService
{
    private readonly IKnowledgeQueryService _queryService;
    private readonly IWorkspaceAccessService _accessService;
    private readonly IDocumentPermissionFilterService _permissionFilterService;

    public BootstrapService(
        IKnowledgeQueryService queryService,
        IWorkspaceAccessService accessService,
        IDocumentPermissionFilterService permissionFilterService)
    {
        _queryService = queryService;
        _accessService = accessService;
        _permissionFilterService = permissionFilterService;
    }

    public async Task<BootstrapResponse> GetBootstrapAsync(CancellationToken cancellationToken = default)
    {
        var userId = await _accessService.GetRequiredUserIdAsync(cancellationToken);
        var response = await _queryService.GetBootstrapAsync(userId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.Forbidden, "No accessible workspace was found.");
        return await _permissionFilterService.FilterBootstrapAsync(response, cancellationToken);
    }
}
