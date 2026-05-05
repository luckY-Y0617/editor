namespace Northstar.Application.Security;

public interface IWorkspaceMembershipQueryService
{
    Task<string?> GetActiveRoleAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
}
