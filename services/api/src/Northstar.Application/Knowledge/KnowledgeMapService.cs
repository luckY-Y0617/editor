using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public sealed class KnowledgeMapService : IKnowledgeMapService
{
    private readonly IKnowledgeQueryService _queryService;
    private readonly IResourceWorkspaceResolver _workspaceResolver;
    private readonly IWorkspaceAccessService _accessService;
    private readonly IDocumentPermissionFilterService _permissionFilterService;

    public KnowledgeMapService(
        IKnowledgeQueryService queryService,
        IResourceWorkspaceResolver workspaceResolver,
        IWorkspaceAccessService accessService,
        IDocumentPermissionFilterService permissionFilterService)
    {
        _queryService = queryService;
        _workspaceResolver = workspaceResolver;
        _accessService = accessService;
        _permissionFilterService = permissionFilterService;
    }

    public async Task<KnowledgeMapResponse> GetMapAsync(Guid spaceId, CancellationToken cancellationToken = default)
    {
        var workspaceId = await _workspaceResolver.GetWorkspaceIdForSpaceAsync(spaceId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");
        await _accessService.EnsureCanViewWorkspaceAsync(workspaceId, cancellationToken);

        var map = await _queryService.GetMapAsync(spaceId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");
        return await _permissionFilterService.FilterMapAsync(map, cancellationToken);
    }
}
