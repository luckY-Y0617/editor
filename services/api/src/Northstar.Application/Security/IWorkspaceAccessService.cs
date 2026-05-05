namespace Northstar.Application.Security;

public interface IWorkspaceAccessService
{
    Task<Guid> GetRequiredUserIdAsync(CancellationToken cancellationToken = default);
    Task EnsureCanViewWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task EnsureCanEditWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task EnsureCanManageWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}
