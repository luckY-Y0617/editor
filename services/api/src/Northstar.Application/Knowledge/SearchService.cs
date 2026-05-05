using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public sealed class SearchService : ISearchService
{
    private readonly ISearchQueryService _queryService;
    private readonly IResourceWorkspaceResolver _workspaceResolver;
    private readonly IWorkspaceAccessService _accessService;
    private readonly IDocumentPermissionFilterService _permissionFilterService;

    public SearchService(
        ISearchQueryService queryService,
        IResourceWorkspaceResolver workspaceResolver,
        IWorkspaceAccessService accessService,
        IDocumentPermissionFilterService permissionFilterService)
    {
        _queryService = queryService;
        _workspaceResolver = workspaceResolver;
        _accessService = accessService;
        _permissionFilterService = permissionFilterService;
    }

    public async Task<SearchResponse> SearchAsync(
        string? query,
        Guid spaceId,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = await _workspaceResolver.GetWorkspaceIdForSpaceAsync(spaceId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");
        await _accessService.EnsureCanViewWorkspaceAsync(workspaceId, cancellationToken);

        var response = await _queryService.SearchAsync(query, spaceId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");
        return await _permissionFilterService.FilterSearchAsync(response, cancellationToken);
    }
}
