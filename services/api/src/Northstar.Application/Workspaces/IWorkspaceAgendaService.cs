using Northstar.Contracts.Workspaces;

namespace Northstar.Application.Workspaces;

public interface IWorkspaceAgendaService
{
    Task<WorkspaceAgendaResponse> GetAgendaAsync(
        Guid workspaceId,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
