using Northstar.Contracts.Workspaces;

namespace Northstar.Application.Workspaces;

public interface IIamSyncService
{
    Task<IamSyncResponse> SyncAsync(
        Guid workspaceId,
        IamSyncRequest request,
        CancellationToken cancellationToken = default);
}
