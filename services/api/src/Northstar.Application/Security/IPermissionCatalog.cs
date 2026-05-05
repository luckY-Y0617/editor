namespace Northstar.Application.Security;

public interface IPermissionCatalog
{
    int GetRank(string? role);
    bool RoleHasPermission(string? role, string? actionKey);
    bool CanGrantRole(string? actorRole, string? targetRole);
    IReadOnlyList<string> GetAllowedActions(string? role, IReadOnlyList<string> actionKeys);
}
