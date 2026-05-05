using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class WorkspaceAccessService : IWorkspaceAccessService
{
    private readonly ICurrentUser _currentUser;
    private readonly IEffectivePermissionService _effectivePermissionService;

    public WorkspaceAccessService(
        ICurrentUser currentUser,
        IEffectivePermissionService effectivePermissionService)
    {
        _currentUser = currentUser;
        _effectivePermissionService = effectivePermissionService;
    }

    public Task<Guid> GetRequiredUserIdAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Authentication is required.");
        }

        return Task.FromResult(_currentUser.UserId.Value);
    }

    public Task EnsureCanViewWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return EnsureWorkspacePermissionAsync(workspaceId, PermissionActions.WorkspaceView, cancellationToken);
    }

    public Task EnsureCanEditWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return EnsureWorkspacePermissionAsync(workspaceId, PermissionActions.DocumentEdit, cancellationToken);
    }

    public Task EnsureCanManageWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return EnsureWorkspacePermissionAsync(workspaceId, PermissionActions.WorkspaceManageMembers, cancellationToken);
    }

    private async Task EnsureWorkspacePermissionAsync(
        Guid workspaceId,
        string actionKey,
        CancellationToken cancellationToken)
    {
        var userId = await GetRequiredUserIdAsync(cancellationToken);
        var result = await _effectivePermissionService.AuthorizeWorkspaceAsync(
            workspaceId,
            userId,
            actionKey,
            cancellationToken);

        if (result.Reason == EffectivePermissionService.NoMembershipReason)
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace access is denied.");
        }

        if (!result.Allowed)
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace permission is insufficient.");
        }
    }
}
