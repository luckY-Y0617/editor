using Northstar.Application.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Tests;

public sealed class PermissionCatalogTests
{
    private readonly PermissionCatalog _catalog = new();

    [Fact]
    public void GetRank_ReturnsContractRanks_AndUnknownRoleIsUnranked()
    {
        Assert.Equal(500, _catalog.GetRank(PermissionRole.Owner));
        Assert.Equal(400, _catalog.GetRank(PermissionRole.Admin));
        Assert.Equal(300, _catalog.GetRank(PermissionRole.Editor));
        Assert.Equal(200, _catalog.GetRank(PermissionRole.Commenter));
        Assert.Equal(100, _catalog.GetRank(PermissionRole.Viewer));
        Assert.Equal(0, _catalog.GetRank("unknown"));

        Assert.True(_catalog.GetRank(PermissionRole.Owner) > _catalog.GetRank(PermissionRole.Admin));
        Assert.True(_catalog.GetRank(PermissionRole.Admin) > _catalog.GetRank(PermissionRole.Editor));
        Assert.True(_catalog.GetRank(PermissionRole.Editor) > _catalog.GetRank(PermissionRole.Viewer));
    }

    [Fact]
    public void RoleHasPermission_ReflectsWorkspaceBaselineCapabilities()
    {
        Assert.True(_catalog.RoleHasPermission(PermissionRole.Viewer, PermissionActions.WorkspaceView));
        Assert.True(_catalog.RoleHasPermission(PermissionRole.Editor, PermissionActions.DocumentEdit));
        Assert.True(_catalog.RoleHasPermission(PermissionRole.Admin, PermissionActions.WorkspaceManageMembers));
        Assert.True(_catalog.RoleHasPermission(PermissionRole.Owner, PermissionActions.WorkspaceManageMembers));

        Assert.False(_catalog.RoleHasPermission(PermissionRole.Viewer, PermissionActions.DocumentEdit));
        Assert.False(_catalog.RoleHasPermission(PermissionRole.Editor, PermissionActions.WorkspaceManageMembers));
        Assert.False(_catalog.RoleHasPermission("unknown", PermissionActions.WorkspaceView));
    }

    [Fact]
    public void CanGrantRole_FollowsManagementHierarchy()
    {
        Assert.True(_catalog.CanGrantRole(PermissionRole.Owner, PermissionRole.Admin));
        Assert.True(_catalog.CanGrantRole(PermissionRole.Owner, PermissionRole.Editor));
        Assert.True(_catalog.CanGrantRole(PermissionRole.Owner, PermissionRole.Viewer));

        Assert.True(_catalog.CanGrantRole(PermissionRole.Admin, PermissionRole.Editor));
        Assert.True(_catalog.CanGrantRole(PermissionRole.Admin, PermissionRole.Viewer));
        Assert.False(_catalog.CanGrantRole(PermissionRole.Admin, PermissionRole.Owner));

        Assert.False(_catalog.CanGrantRole(PermissionRole.Editor, PermissionRole.Owner));
        Assert.False(_catalog.CanGrantRole(PermissionRole.Editor, PermissionRole.Admin));
        Assert.False(_catalog.CanGrantRole(PermissionRole.Editor, PermissionRole.Editor));
        Assert.False(_catalog.CanGrantRole(PermissionRole.Editor, PermissionRole.Viewer));
    }
}
