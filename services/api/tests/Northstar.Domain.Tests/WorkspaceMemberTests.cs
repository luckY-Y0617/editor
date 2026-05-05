using Northstar.Domain.Security;
using Northstar.Domain.Shared;
using Northstar.Domain.Workspaces;

namespace Northstar.Domain.Tests;

public sealed class WorkspaceMemberTests
{
    [Fact]
    public void Constructor_RejectsCommenterWorkspaceRole()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new WorkspaceMember(Guid.NewGuid(), Guid.NewGuid(), PermissionRole.Commenter));

        Assert.Equal(DomainErrorCodes.ValidationError, exception.Code);
    }
}
