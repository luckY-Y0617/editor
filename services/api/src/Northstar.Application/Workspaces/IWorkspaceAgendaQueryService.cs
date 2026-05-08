using Northstar.Contracts.Workspaces;

namespace Northstar.Application.Workspaces;

public interface IWorkspaceAgendaQueryService
{
    Task<WorkspaceAgendaResponse> GetAgendaAsync(
        Guid workspaceId,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
