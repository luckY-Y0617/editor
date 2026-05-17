using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Domain.Security;

namespace Northstar.Application.Tests;

public sealed class WorkspaceAccessServiceTests
{
    [Fact]
    public async Task EnsureCanViewWorkspaceAsync_AllowsViewer()
    {
        var service = CreateService(PermissionRole.Viewer);

        await service.EnsureCanViewWorkspaceAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task EnsureCanViewWorkspaceAsync_DeniesNonMember()
    {
        var service = CreateService(role: null);

        var exception = await Assert.ThrowsAsync<ApplicationErrorException>(() =>
            service.EnsureCanViewWorkspaceAsync(Guid.NewGuid()));

        Assert.Equal(ErrorCodes.Forbidden, exception.Code);
        Assert.Equal("Workspace access is denied.", exception.Message);
    }

    [Fact]
    public async Task EnsureCanViewWorkspaceAsync_DeniesDeferredCommenterWorkspaceRole()
    {
        var service = CreateService(PermissionRole.Commenter);

        var exception = await Assert.ThrowsAsync<ApplicationErrorException>(() =>
            service.EnsureCanViewWorkspaceAsync(Guid.NewGuid()));

        Assert.Equal(ErrorCodes.Forbidden, exception.Code);
        Assert.Equal("Workspace permission is insufficient.", exception.Message);
    }

    [Fact]
    public async Task EnsureCanEditWorkspaceAsync_AllowsEditor_AndDeniesViewer()
    {
        await CreateService(PermissionRole.Editor).EnsureCanEditWorkspaceAsync(Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<ApplicationErrorException>(() =>
            CreateService(PermissionRole.Viewer).EnsureCanEditWorkspaceAsync(Guid.NewGuid()));

        Assert.Equal(ErrorCodes.Forbidden, exception.Code);
        Assert.Equal("Workspace permission is insufficient.", exception.Message);
    }

    [Theory]
    [InlineData(PermissionRole.Admin, true)]
    [InlineData(PermissionRole.Owner, true)]
    [InlineData(PermissionRole.Editor, false)]
    public async Task EnsureCanManageWorkspaceAsync_UsesAdminOrOwnerThreshold(string role, bool allowed)
    {
        var service = CreateService(role);

        if (allowed)
        {
            await service.EnsureCanManageWorkspaceAsync(Guid.NewGuid());
            return;
        }

        var exception = await Assert.ThrowsAsync<ApplicationErrorException>(() =>
            service.EnsureCanManageWorkspaceAsync(Guid.NewGuid()));

        Assert.Equal(ErrorCodes.Forbidden, exception.Code);
        Assert.Equal("Workspace permission is insufficient.", exception.Message);
    }

    private static WorkspaceAccessService CreateService(string? role)
    {
        var userId = Guid.NewGuid();
        var membershipQuery = new TestWorkspaceMembershipQueryService(userId, role);
        var effectivePermissionService = new EffectivePermissionService(
            membershipQuery,
            new PermissionCatalog(),
            new TestResourceWorkspaceResolver(),
            new TestResourcePermissionRepository(),
            new TestWorkspaceGroupRepository(),
            new TestShareLinkRepository(),
            new ShareLinkTokenService(),
            new TestEmailInviteRepository(),
            new TestPermissionUserRepository(userId));

        return new WorkspaceAccessService(
            new TestCurrentUser(userId),
            effectivePermissionService);
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(Guid userId)
        {
            UserId = userId;
        }

        public Guid? UserId { get; }
        public bool IsAuthenticated => true;
    }

    private sealed class TestWorkspaceMembershipQueryService : IWorkspaceMembershipQueryService
    {
        private readonly Guid _userId;
        private readonly string? _role;

        public TestWorkspaceMembershipQueryService(Guid userId, string? role)
        {
            _userId = userId;
            _role = role;
        }

        public Task<string?> GetActiveRoleAsync(
            Guid workspaceId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(userId == _userId ? _role : null);
        }
    }

    private sealed class TestResourceWorkspaceResolver : IResourceWorkspaceResolver
    {
        public Task<Guid?> GetWorkspaceIdForSpaceAsync(Guid spaceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guid?>(null);
        }

        public Task<LibraryPermissionResource?> GetLibraryPermissionResourceAsync(Guid libraryId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<LibraryPermissionResource?>(null);
        }

        public Task<Guid?> GetWorkspaceIdForDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guid?>(null);
        }

        public Task<Guid?> GetWorkspaceIdForCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guid?>(null);
        }

        public Task<DocumentPermissionResource?> GetDocumentPermissionResourceAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DocumentPermissionResource?>(null);
        }

        public Task<DocumentPermissionResource?> GetDocumentPermissionResourceIncludingDeletedAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DocumentPermissionResource?>(null);
        }

        public Task<IReadOnlyList<DocumentPermissionResource>> GetDocumentPermissionResourcesAsync(
            IReadOnlyCollection<Guid> documentIds,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DocumentPermissionResource>>(Array.Empty<DocumentPermissionResource>());
        }

        public Task<CollectionPermissionResource?> GetCollectionPermissionResourceAsync(
            Guid collectionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CollectionPermissionResource?>(null);
        }
    }

    private sealed class TestResourcePermissionRepository : IResourcePermissionRepository
    {
        public Task<Northstar.Domain.Security.ResourceAccessPolicy?> GetPolicyAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Northstar.Domain.Security.ResourceAccessPolicy?>(null);
        }

        public Task<Northstar.Domain.Security.ResourceAccessPolicy?> GetPolicyForUpdateAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Northstar.Domain.Security.ResourceAccessPolicy?>(null);
        }

        public Task<IReadOnlyList<Northstar.Domain.Security.ResourceAccessPolicy>> GetPoliciesForResourcesAsync(
            Guid workspaceId,
            string resourceType,
            IReadOnlyCollection<Guid> resourceIds,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Northstar.Domain.Security.ResourceAccessPolicy>>(
                Array.Empty<Northstar.Domain.Security.ResourceAccessPolicy>());
        }

        public Task<IReadOnlyList<Northstar.Domain.Security.ResourceAccessGrant>> GetUserGrantsAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Northstar.Domain.Security.ResourceAccessGrant>>(
                Array.Empty<Northstar.Domain.Security.ResourceAccessGrant>());
        }

        public Task<IReadOnlyList<Northstar.Domain.Security.ResourceAccessGrant>> GetGrantsAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Northstar.Domain.Security.ResourceAccessGrant>>(
                Array.Empty<Northstar.Domain.Security.ResourceAccessGrant>());
        }

        public Task<Northstar.Domain.Security.ResourceAccessGrant?> GetActiveUserGrantAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            Guid userId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Northstar.Domain.Security.ResourceAccessGrant?>(null);
        }

        public Task<Northstar.Domain.Security.ResourceAccessGrant?> GetActiveSubjectGrantAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            string subjectType,
            Guid subjectId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Northstar.Domain.Security.ResourceAccessGrant?>(null);
        }

        public Task<IReadOnlyList<Northstar.Domain.Security.ResourceAccessGrant>> GetActiveGroupGrantsAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            IReadOnlyCollection<Guid> groupIds,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Northstar.Domain.Security.ResourceAccessGrant>>(
                Array.Empty<Northstar.Domain.Security.ResourceAccessGrant>());
        }

        public Task<IReadOnlyList<Northstar.Domain.Security.ResourceAccessGrant>> GetActiveGroupGrantsForResourcesAsync(
            Guid workspaceId,
            string resourceType,
            IReadOnlyCollection<Guid> resourceIds,
            IReadOnlyCollection<Guid> groupIds,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Northstar.Domain.Security.ResourceAccessGrant>>(
                Array.Empty<Northstar.Domain.Security.ResourceAccessGrant>());
        }

        public Task<IReadOnlyList<Northstar.Domain.Security.ResourceAccessGrant>> GetActiveUserGrantsForResourcesAsync(
            Guid workspaceId,
            string resourceType,
            IReadOnlyCollection<Guid> resourceIds,
            Guid userId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Northstar.Domain.Security.ResourceAccessGrant>>(
                Array.Empty<Northstar.Domain.Security.ResourceAccessGrant>());
        }

        public Task<Northstar.Domain.Security.ResourceAccessGrant?> GetGrantForUpdateAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            Guid grantId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Northstar.Domain.Security.ResourceAccessGrant?>(null);
        }

        public Task<Northstar.Domain.Security.ResourceAccessGrant?> GetUserGrantForUpdateAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Northstar.Domain.Security.ResourceAccessGrant?>(null);
        }

        public Task<Northstar.Domain.Security.ResourceAccessGrant?> GetSubjectGrantForUpdateAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            string subjectType,
            Guid subjectId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Northstar.Domain.Security.ResourceAccessGrant?>(null);
        }

        public Task AddPolicyAsync(
            Northstar.Domain.Security.ResourceAccessPolicy policy,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task AddGrantAsync(
            Northstar.Domain.Security.ResourceAccessGrant grant,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestWorkspaceGroupRepository : IWorkspaceGroupRepository
    {
        public Task<bool> WorkspaceExistsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> UserIsWorkspaceMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<WorkspaceGroupReadModel>> GetGroupsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<WorkspaceGroupReadModel>>(Array.Empty<WorkspaceGroupReadModel>());
        }

        public Task<WorkspaceGroupDetailReadModel?> GetGroupDetailAsync(Guid workspaceId, Guid groupId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WorkspaceGroupDetailReadModel?>(null);
        }

        public Task<Northstar.Domain.Security.WorkspaceGroup?> GetGroupAsync(Guid workspaceId, Guid groupId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Northstar.Domain.Security.WorkspaceGroup?>(null);
        }

        public Task<Northstar.Domain.Security.WorkspaceGroup?> GetGroupForUpdateAsync(Guid workspaceId, Guid groupId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Northstar.Domain.Security.WorkspaceGroup?>(null);
        }

        public Task<bool> ActiveGroupNameExistsAsync(Guid workspaceId, string name, Guid? exceptGroupId = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<Northstar.Domain.Security.WorkspaceGroupMember?> GetActiveMemberForUpdateAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Northstar.Domain.Security.WorkspaceGroupMember?>(null);
        }

        public Task<IReadOnlyList<Guid>> GetActiveGroupIdsForUserAsync(Guid workspaceId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
        }

        public Task<IReadOnlyList<Guid>> GetActiveGroupMemberUserIdsAsync(Guid workspaceId, Guid groupId, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
        }

        public Task AddGroupAsync(Northstar.Domain.Security.WorkspaceGroup group, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task AddMemberAsync(Northstar.Domain.Security.WorkspaceGroupMember member, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestShareLinkRepository : IShareLinkRepository
    {
        public Task<IReadOnlyList<Northstar.Domain.Security.ShareLink>> GetActiveByResourceAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Northstar.Domain.Security.ShareLink>>(
                Array.Empty<Northstar.Domain.Security.ShareLink>());
        }

        public Task<Northstar.Domain.Security.ShareLink?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Northstar.Domain.Security.ShareLink?>(null);
        }

        public Task<Northstar.Domain.Security.ShareLink?> GetByIdAsync(
            Guid shareLinkId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Northstar.Domain.Security.ShareLink?>(null);
        }

        public Task<Northstar.Domain.Security.ShareLink?> GetForUpdateAsync(
            Guid shareLinkId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Northstar.Domain.Security.ShareLink?>(null);
        }

        public Task<IReadOnlyList<Northstar.Domain.Security.ShareLink>> SearchAsync(
            ShareLinkSearchQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Northstar.Domain.Security.ShareLink>>(
                Array.Empty<Northstar.Domain.Security.ShareLink>());
        }

        public Task AddAsync(
            Northstar.Domain.Security.ShareLink link,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
