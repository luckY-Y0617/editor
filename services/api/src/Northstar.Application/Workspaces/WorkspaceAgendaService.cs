using Northstar.Application.Security;
using Northstar.Contracts.Workspaces;

namespace Northstar.Application.Workspaces;

public sealed class WorkspaceAgendaService : IWorkspaceAgendaService
{
    private readonly IWorkspaceAccessService _accessService;
    private readonly IWorkspaceAgendaQueryService _queryService;

    public WorkspaceAgendaService(
        IWorkspaceAccessService accessService,
        IWorkspaceAgendaQueryService queryService)
    {
        _accessService = accessService;
        _queryService = queryService;
    }

    public async Task<WorkspaceAgendaResponse> GetAgendaAsync(
        Guid workspaceId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        await _accessService.EnsureCanViewWorkspaceAsync(workspaceId, cancellationToken);
        return await _queryService.GetAgendaAsync(workspaceId, date, cancellationToken);
    }
}
