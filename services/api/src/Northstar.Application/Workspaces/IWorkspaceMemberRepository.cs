using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Application.Workspaces;

public interface IWorkspaceMemberRepository
{
    Task<bool> WorkspaceExistsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkspaceMemberReadModel>> GetMembersAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<WorkspaceMember?> GetMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountOwnersAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task AddMemberAsync(WorkspaceMember member, CancellationToken cancellationToken = default);
    void RemoveMember(WorkspaceMember member);
}
