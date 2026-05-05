using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class PermissionCatalog : IPermissionCatalog
{
    private static readonly IReadOnlyDictionary<string, int> RoleRanks =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [PermissionRole.Owner] = PermissionRole.OwnerRank,
            [PermissionRole.Admin] = PermissionRole.AdminRank,
            [PermissionRole.Editor] = PermissionRole.EditorRank,
            [PermissionRole.Commenter] = PermissionRole.CommenterRank,
            [PermissionRole.Viewer] = PermissionRole.ViewerRank
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> RolePermissions =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [PermissionRole.Owner] = CreatePermissionSet(
                PermissionActions.WorkspaceView,
                PermissionActions.WorkspaceManageSettings,
                PermissionActions.WorkspaceManageMembers,
                PermissionActions.WorkspaceManagePermissions,
                PermissionActions.WorkspaceViewAudit,
                PermissionActions.CollectionView,
                PermissionActions.CollectionEdit,
                PermissionActions.CollectionCreateDocument,
                PermissionActions.CollectionShare,
                PermissionActions.CollectionManagePermissions,
                PermissionActions.CollectionArchive,
                PermissionActions.CollectionDelete,
                PermissionActions.DocumentView,
                PermissionActions.DocumentComment,
                PermissionActions.DocumentEdit,
                PermissionActions.DocumentShare,
                PermissionActions.DocumentManagePermissions,
                PermissionActions.DocumentArchive,
                PermissionActions.DocumentRestore,
                PermissionActions.DocumentDelete,
                PermissionActions.VersionView,
                PermissionActions.VersionCreate,
                PermissionActions.VersionRestore,
                PermissionActions.FileUpload,
                PermissionActions.FileDownload,
                PermissionActions.FileDelete,
                PermissionActions.AttachmentView,
                PermissionActions.AttachmentCreate,
                PermissionActions.AttachmentDelete,
                PermissionActions.SearchQuery,
                PermissionActions.ActivityView),
            [PermissionRole.Admin] = CreatePermissionSet(
                PermissionActions.WorkspaceView,
                PermissionActions.WorkspaceManageSettings,
                PermissionActions.WorkspaceManageMembers,
                PermissionActions.WorkspaceManagePermissions,
                PermissionActions.WorkspaceViewAudit,
                PermissionActions.CollectionView,
                PermissionActions.CollectionEdit,
                PermissionActions.CollectionCreateDocument,
                PermissionActions.CollectionShare,
                PermissionActions.CollectionManagePermissions,
                PermissionActions.CollectionArchive,
                PermissionActions.CollectionDelete,
                PermissionActions.DocumentView,
                PermissionActions.DocumentComment,
                PermissionActions.DocumentEdit,
                PermissionActions.DocumentShare,
                PermissionActions.DocumentManagePermissions,
                PermissionActions.DocumentArchive,
                PermissionActions.DocumentRestore,
                PermissionActions.DocumentDelete,
                PermissionActions.VersionView,
                PermissionActions.VersionCreate,
                PermissionActions.VersionRestore,
                PermissionActions.FileUpload,
                PermissionActions.FileDownload,
                PermissionActions.FileDelete,
                PermissionActions.AttachmentView,
                PermissionActions.AttachmentCreate,
                PermissionActions.AttachmentDelete,
                PermissionActions.SearchQuery,
                PermissionActions.ActivityView),
            [PermissionRole.Editor] = CreatePermissionSet(
                PermissionActions.WorkspaceView,
                PermissionActions.CollectionView,
                PermissionActions.CollectionEdit,
                PermissionActions.CollectionCreateDocument,
                PermissionActions.CollectionShare,
                PermissionActions.CollectionArchive,
                PermissionActions.CollectionDelete,
                PermissionActions.DocumentView,
                PermissionActions.DocumentComment,
                PermissionActions.DocumentEdit,
                PermissionActions.DocumentShare,
                PermissionActions.DocumentArchive,
                PermissionActions.DocumentRestore,
                PermissionActions.DocumentDelete,
                PermissionActions.VersionView,
                PermissionActions.VersionCreate,
                PermissionActions.VersionRestore,
                PermissionActions.FileUpload,
                PermissionActions.FileDownload,
                PermissionActions.FileDelete,
                PermissionActions.AttachmentView,
                PermissionActions.AttachmentCreate,
                PermissionActions.AttachmentDelete,
                PermissionActions.SearchQuery,
                PermissionActions.ActivityView),
            [PermissionRole.Commenter] = CreatePermissionSet(
                PermissionActions.CollectionView,
                PermissionActions.DocumentView,
                PermissionActions.DocumentComment,
                PermissionActions.FileDownload,
                PermissionActions.AttachmentView,
                PermissionActions.SearchQuery,
                PermissionActions.ActivityView,
                PermissionActions.VersionView),
            [PermissionRole.Viewer] = CreatePermissionSet(
                PermissionActions.WorkspaceView,
                PermissionActions.CollectionView,
                PermissionActions.DocumentView,
                PermissionActions.VersionView,
                PermissionActions.FileDownload,
                PermissionActions.AttachmentView,
                PermissionActions.SearchQuery,
                PermissionActions.ActivityView)
        };

    public int GetRank(string? role)
    {
        return role is not null && RoleRanks.TryGetValue(role, out var rank)
            ? rank
            : 0;
    }

    public bool RoleHasPermission(string? role, string? actionKey)
    {
        return role is not null &&
            actionKey is not null &&
            RolePermissions.TryGetValue(role, out var permissions) &&
            permissions.Contains(actionKey);
    }

    public bool CanGrantRole(string? actorRole, string? targetRole)
    {
        var actorRank = GetRank(actorRole);
        var targetRank = GetRank(targetRole);

        return actorRank > 0 &&
            targetRank > 0 &&
            actorRole is not null &&
            RoleHasPermission(actorRole, PermissionActions.WorkspaceManageMembers) &&
            actorRank >= targetRank;
    }

    public IReadOnlyList<string> GetAllowedActions(string? role, IReadOnlyList<string> actionKeys)
    {
        return actionKeys
            .Where(actionKey => RoleHasPermission(role, actionKey))
            .ToArray();
    }

    private static IReadOnlySet<string> CreatePermissionSet(params string[] actionKeys)
    {
        return new HashSet<string>(actionKeys, StringComparer.Ordinal);
    }
}
