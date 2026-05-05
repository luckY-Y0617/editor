using Northstar.Application.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Tests;

public sealed class EffectivePermissionServiceTests
{
    [Fact]
    public async Task AuthorizeDocumentAsync_WhenNoPolicy_InheritsWorkspaceViewerForView()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentView);

        Assert.True(result.Allowed);
        Assert.Equal(PermissionRole.Viewer, result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.WorkspaceSource, result.Source);
        Assert.Equal(InheritanceModes.Inherit, result.InheritanceMode);
    }

    [Fact]
    public async Task AuthorizeDocumentAsync_WhenNoPolicy_DeniesWorkspaceViewerForEdit()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentEdit);

        Assert.False(result.Allowed);
        Assert.Equal(PermissionRole.Viewer, result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.PermissionDeniedReason, result.Reason);
    }

    [Fact]
    public async Task AuthorizeDocumentAsync_DirectEditorGrantElevatesWorkspaceViewerForEdit()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            SubjectTypes.User,
            fixture.UserId,
            PermissionRole.Editor));

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentEdit);

        Assert.True(result.Allowed);
        Assert.Equal(PermissionRole.Editor, result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.DocumentSource, result.Source);
    }

    [Fact]
    public async Task AuthorizeDocumentAsync_RestrictedPolicyBlocksInheritedWorkspaceViewer()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        fixture.Repository.AddPolicy(new ResourceAccessPolicy(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            InheritanceModes.Restricted));

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentView);

        Assert.False(result.Allowed);
        Assert.Null(result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.PermissionDeniedReason, result.Reason);
        Assert.Equal(InheritanceModes.Restricted, result.InheritanceMode);
    }

    [Fact]
    public async Task AuthorizeDocumentAsync_RestrictedPolicyAllowsDirectViewerGrantForView()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        fixture.Repository.AddPolicy(new ResourceAccessPolicy(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            InheritanceModes.Restricted));
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            SubjectTypes.User,
            fixture.UserId,
            PermissionRole.Viewer));

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentView);

        Assert.True(result.Allowed);
        Assert.Equal(PermissionRole.Viewer, result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.DocumentSource, result.Source);
    }

    [Theory]
    [InlineData(PermissionRole.Admin, EffectivePermissionService.AdminEscapeSource)]
    [InlineData(PermissionRole.Owner, EffectivePermissionService.OwnerEscapeSource)]
    public async Task AuthorizeDocumentAsync_RestrictedPolicyKeepsAdminOwnerEscapePath(
        string workspaceRole,
        string expectedSource)
    {
        var fixture = PermissionFixture.Create(workspaceRole);
        fixture.Repository.AddPolicy(new ResourceAccessPolicy(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            InheritanceModes.Restricted));

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentManagePermissions);

        Assert.True(result.Allowed);
        Assert.Equal(workspaceRole, result.EffectiveRole);
        Assert.Equal(expectedSource, result.Source);
    }

    [Fact]
    public async Task AuthorizeCollectionAsync_DirectGrantAppliesToCollectionActions()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Collection,
            fixture.CollectionId,
            SubjectTypes.User,
            fixture.UserId,
            PermissionRole.Editor));

        var result = await fixture.Service.AuthorizeCollectionAsync(
            fixture.CollectionId,
            fixture.UserId,
            PermissionActions.CollectionEdit);

        Assert.True(result.Allowed);
        Assert.Equal(PermissionRole.Editor, result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.CollectionSource, result.Source);
    }

    [Fact]
    public async Task AuthorizeDocumentAsync_CollectionEditorGrantAllowsInheritedDocumentEdit()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Collection,
            fixture.CollectionId,
            SubjectTypes.User,
            fixture.UserId,
            PermissionRole.Editor));

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentEdit);

        Assert.True(result.Allowed);
        Assert.Equal(PermissionRole.Editor, result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.CollectionSource, result.Source);
    }

    [Fact]
    public async Task AuthorizeDocumentAsync_CollectionRestrictedBlocksInheritedWorkspaceViewer()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        fixture.Repository.AddPolicy(new ResourceAccessPolicy(
            fixture.WorkspaceId,
            ResourceTypes.Collection,
            fixture.CollectionId,
            InheritanceModes.Restricted));

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentView);

        Assert.False(result.Allowed);
        Assert.Null(result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.PermissionDeniedReason, result.Reason);
    }

    [Fact]
    public async Task AuthorizeDocumentAsync_DocumentDirectGrantBeatsLowerCollectionGrant()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Collection,
            fixture.CollectionId,
            SubjectTypes.User,
            fixture.UserId,
            PermissionRole.Viewer));
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            SubjectTypes.User,
            fixture.UserId,
            PermissionRole.Editor));

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentEdit);

        Assert.True(result.Allowed);
        Assert.Equal(PermissionRole.Editor, result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.DocumentSource, result.Source);
    }

    [Fact]
    public async Task AuthorizeDocumentAsync_DocumentRestrictedBlocksCollectionGrantWithoutDirectDocumentGrant()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        fixture.Repository.AddPolicy(new ResourceAccessPolicy(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            InheritanceModes.Restricted));
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Collection,
            fixture.CollectionId,
            SubjectTypes.User,
            fixture.UserId,
            PermissionRole.Editor));

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentView);

        Assert.False(result.Allowed);
        Assert.Null(result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.PermissionDeniedReason, result.Reason);
    }

    [Fact]
    public async Task AuthorizeDocumentAsync_DocumentRestrictedAllowsDirectDocumentGrant()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        fixture.Repository.AddPolicy(new ResourceAccessPolicy(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            InheritanceModes.Restricted));
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Collection,
            fixture.CollectionId,
            SubjectTypes.User,
            fixture.UserId,
            PermissionRole.Editor));
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            SubjectTypes.User,
            fixture.UserId,
            PermissionRole.Viewer));

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentView);

        Assert.True(result.Allowed);
        Assert.Equal(PermissionRole.Viewer, result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.DocumentSource, result.Source);
    }

    [Fact]
    public async Task AuthorizeDocumentAsync_GroupGrantElevatesWorkspaceViewerForDocumentEdit()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        var groupId = Guid.NewGuid();
        fixture.GroupRepository.AddActiveGroup(groupId);
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            SubjectTypes.Group,
            groupId,
            PermissionRole.Editor));

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentEdit);

        Assert.True(result.Allowed);
        Assert.Equal(PermissionRole.Editor, result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.DocumentGroupSource, result.Source);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("removed")]
    [InlineData("archived")]
    public async Task AuthorizeDocumentAsync_InactiveGroupMembershipIsIgnored(string inactiveReason)
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        var groupId = Guid.NewGuid();
        fixture.GroupRepository.AddInactiveGroup(groupId, inactiveReason);
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            SubjectTypes.Group,
            groupId,
            PermissionRole.Editor));

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentEdit);

        Assert.False(result.Allowed);
        Assert.Equal(PermissionRole.Viewer, result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.PermissionDeniedReason, result.Reason);
    }

    [Fact]
    public async Task AuthorizeDocumentAsync_CollectionGroupGrantInheritsToDocument()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        var groupId = Guid.NewGuid();
        fixture.GroupRepository.AddActiveGroup(groupId);
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Collection,
            fixture.CollectionId,
            SubjectTypes.Group,
            groupId,
            PermissionRole.Editor));

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentEdit);

        Assert.True(result.Allowed);
        Assert.Equal(PermissionRole.Editor, result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.CollectionGroupSource, result.Source);
    }

    [Fact]
    public async Task AuthorizeDocumentAsync_DocumentRestrictedCutsOffCollectionGroupGrant()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        var groupId = Guid.NewGuid();
        fixture.GroupRepository.AddActiveGroup(groupId);
        fixture.Repository.AddPolicy(new ResourceAccessPolicy(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            InheritanceModes.Restricted));
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Collection,
            fixture.CollectionId,
            SubjectTypes.Group,
            groupId,
            PermissionRole.Editor));

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentEdit);

        Assert.False(result.Allowed);
        Assert.Null(result.EffectiveRole);
    }

    [Fact]
    public async Task AuthorizeDocumentAsync_DocumentRestrictedAllowsDocumentGroupGrant()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        var groupId = Guid.NewGuid();
        fixture.GroupRepository.AddActiveGroup(groupId);
        fixture.Repository.AddPolicy(new ResourceAccessPolicy(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            InheritanceModes.Restricted));
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            SubjectTypes.Group,
            groupId,
            PermissionRole.Editor));

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentEdit);

        Assert.True(result.Allowed);
        Assert.Equal(PermissionRole.Editor, result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.DocumentGroupSource, result.Source);
    }

    [Fact]
    public async Task AuthorizeDocumentAsync_UserGrantAndGroupGrantUseHighestRole()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        var groupId = Guid.NewGuid();
        fixture.GroupRepository.AddActiveGroup(groupId);
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            SubjectTypes.Group,
            groupId,
            PermissionRole.Viewer));
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            SubjectTypes.User,
            fixture.UserId,
            PermissionRole.Editor));

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentEdit);

        Assert.True(result.Allowed);
        Assert.Equal(PermissionRole.Editor, result.EffectiveRole);
        Assert.Equal(EffectivePermissionService.DocumentSource, result.Source);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AuthorizeDocumentAsync_ExpiredOrRevokedGrantDoesNotApply(bool expired)
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        fixture.Repository.AddPolicy(new ResourceAccessPolicy(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            InheritanceModes.Restricted));
        var grant = new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            SubjectTypes.User,
            fixture.UserId,
            PermissionRole.Editor,
            expiresAt: expired ? DateTimeOffset.UtcNow.AddMinutes(-1) : null);
        if (!expired)
        {
            grant.Revoke(Guid.NewGuid(), "revoked");
        }

        fixture.Repository.AddGrant(grant);

        var result = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentEdit);

        Assert.False(result.Allowed);
        Assert.Equal(EffectivePermissionService.PermissionDeniedReason, result.Reason);
    }

    [Fact]
    public async Task AuthorizeDocumentAsync_CommenterGrantCanCommentButCannotEdit()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Viewer);
        fixture.Repository.AddPolicy(new ResourceAccessPolicy(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            InheritanceModes.Restricted));
        fixture.Repository.AddGrant(new ResourceAccessGrant(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            SubjectTypes.User,
            fixture.UserId,
            PermissionRole.Commenter));

        var comment = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentComment);
        var edit = await fixture.Service.AuthorizeDocumentAsync(
            fixture.DocumentId,
            fixture.UserId,
            PermissionActions.DocumentEdit);

        Assert.True(comment.Allowed);
        Assert.Equal(PermissionRole.Commenter, comment.EffectiveRole);
        Assert.False(edit.Allowed);
        Assert.Equal(PermissionRole.Commenter, edit.EffectiveRole);
    }

    [Fact]
    public async Task AuthorizeWorkspaceAsync_RejectsWorkspaceCommenterRole()
    {
        var fixture = PermissionFixture.Create(PermissionRole.Commenter);

        var result = await fixture.Service.AuthorizeWorkspaceAsync(
            fixture.WorkspaceId,
            fixture.UserId,
            PermissionActions.WorkspaceView);

        Assert.False(result.Allowed);
        Assert.Equal(EffectivePermissionService.PermissionDeniedReason, result.Reason);
    }

    private sealed class PermissionFixture
    {
        private PermissionFixture(
            Guid workspaceId,
            Guid collectionId,
            Guid documentId,
            Guid userId,
            TestResourcePermissionRepository repository,
            TestWorkspaceGroupRepository groupRepository,
            EffectivePermissionService service)
        {
            WorkspaceId = workspaceId;
            CollectionId = collectionId;
            DocumentId = documentId;
            UserId = userId;
            Repository = repository;
            GroupRepository = groupRepository;
            Service = service;
        }

        public Guid WorkspaceId { get; }
        public Guid CollectionId { get; }
        public Guid DocumentId { get; }
        public Guid UserId { get; }
        public TestResourcePermissionRepository Repository { get; }
        public TestWorkspaceGroupRepository GroupRepository { get; }
        public EffectivePermissionService Service { get; }

        public static PermissionFixture Create(string workspaceRole)
        {
            var workspaceId = Guid.NewGuid();
            var collectionId = Guid.NewGuid();
            var documentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var repository = new TestResourcePermissionRepository();
            var groupRepository = new TestWorkspaceGroupRepository(workspaceId, userId);
            var service = new EffectivePermissionService(
                new TestWorkspaceMembershipQueryService(workspaceId, userId, workspaceRole),
                new PermissionCatalog(),
                new TestResourceWorkspaceResolver(workspaceId, collectionId, documentId),
                repository,
                groupRepository,
                new TestShareLinkRepository(),
                new ShareLinkTokenService(),
                new TestEmailInviteRepository(),
                new TestPermissionUserRepository(userId));

            return new PermissionFixture(workspaceId, collectionId, documentId, userId, repository, groupRepository, service);
        }
    }

    private sealed class TestWorkspaceMembershipQueryService : IWorkspaceMembershipQueryService
    {
        private readonly Guid _workspaceId;
        private readonly Guid _userId;
        private readonly string _role;

        public TestWorkspaceMembershipQueryService(Guid workspaceId, Guid userId, string role)
        {
            _workspaceId = workspaceId;
            _userId = userId;
            _role = role;
        }

        public Task<string?> GetActiveRoleAsync(
            Guid workspaceId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>(workspaceId == _workspaceId && userId == _userId ? _role : null);
        }
    }

    private sealed class TestResourceWorkspaceResolver : IResourceWorkspaceResolver
    {
        private readonly Guid _workspaceId;
        private readonly Guid _collectionId;
        private readonly Guid _documentId;

        public TestResourceWorkspaceResolver(Guid workspaceId, Guid collectionId, Guid documentId)
        {
            _workspaceId = workspaceId;
            _collectionId = collectionId;
            _documentId = documentId;
        }

        public Task<Guid?> GetWorkspaceIdForSpaceAsync(Guid spaceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guid?>(_workspaceId);
        }

        public Task<Guid?> GetWorkspaceIdForDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guid?>(documentId == _documentId ? _workspaceId : null);
        }

        public Task<Guid?> GetWorkspaceIdForCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guid?>(collectionId == _collectionId ? _workspaceId : null);
        }

        public Task<DocumentPermissionResource?> GetDocumentPermissionResourceAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(documentId == _documentId
                ? new DocumentPermissionResource(_documentId, _workspaceId, _collectionId)
                : null);
        }

        public Task<DocumentPermissionResource?> GetDocumentPermissionResourceIncludingDeletedAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            return GetDocumentPermissionResourceAsync(documentId, cancellationToken);
        }

        public async Task<IReadOnlyList<DocumentPermissionResource>> GetDocumentPermissionResourcesAsync(
            IReadOnlyCollection<Guid> documentIds,
            CancellationToken cancellationToken = default)
        {
            var resources = new List<DocumentPermissionResource>();
            foreach (var documentId in documentIds)
            {
                var resource = await GetDocumentPermissionResourceAsync(documentId, cancellationToken);
                if (resource is not null)
                {
                    resources.Add(resource);
                }
            }

            return resources;
        }

        public Task<CollectionPermissionResource?> GetCollectionPermissionResourceAsync(
            Guid collectionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(collectionId == _collectionId
                ? new CollectionPermissionResource(_collectionId, _workspaceId)
                : null);
        }
    }

    private sealed class TestResourcePermissionRepository : IResourcePermissionRepository
    {
        private readonly List<ResourceAccessPolicy> _policies = [];
        private readonly List<ResourceAccessGrant> _grants = [];

        public void AddPolicy(ResourceAccessPolicy policy)
        {
            _policies.Add(policy);
        }

        public void AddGrant(ResourceAccessGrant grant)
        {
            _grants.Add(grant);
        }

        public Task<ResourceAccessPolicy?> GetPolicyAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_policies.FirstOrDefault(policy =>
                policy.WorkspaceId == workspaceId &&
                policy.ResourceType == resourceType &&
                policy.ResourceId == resourceId));
        }

        public Task<ResourceAccessPolicy?> GetPolicyForUpdateAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            CancellationToken cancellationToken = default)
        {
            return GetPolicyAsync(workspaceId, resourceType, resourceId, cancellationToken);
        }

        public Task<IReadOnlyList<ResourceAccessPolicy>> GetPoliciesForResourcesAsync(
            Guid workspaceId,
            string resourceType,
            IReadOnlyCollection<Guid> resourceIds,
            CancellationToken cancellationToken = default)
        {
            var policies = _policies
                .Where(policy =>
                    policy.WorkspaceId == workspaceId &&
                    policy.ResourceType == resourceType &&
                    resourceIds.Contains(policy.ResourceId))
                .ToArray();
            return Task.FromResult<IReadOnlyList<ResourceAccessPolicy>>(policies);
        }

        public Task<IReadOnlyList<ResourceAccessGrant>> GetUserGrantsAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            CancellationToken cancellationToken = default)
        {
            var grants = _grants
                .Where(grant =>
                    grant.WorkspaceId == workspaceId &&
                    grant.ResourceType == resourceType &&
                    grant.ResourceId == resourceId &&
                    grant.SubjectType == SubjectTypes.User &&
                    grant.RevokedAt == null)
                .ToArray();
            return Task.FromResult<IReadOnlyList<ResourceAccessGrant>>(grants);
        }

        public Task<IReadOnlyList<ResourceAccessGrant>> GetGrantsAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            CancellationToken cancellationToken = default)
        {
            var grants = _grants
                .Where(grant =>
                    grant.WorkspaceId == workspaceId &&
                    grant.ResourceType == resourceType &&
                    grant.ResourceId == resourceId &&
                    grant.RevokedAt == null)
                .ToArray();
            return Task.FromResult<IReadOnlyList<ResourceAccessGrant>>(grants);
        }

        public Task<ResourceAccessGrant?> GetActiveUserGrantAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            Guid userId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_grants.FirstOrDefault(grant =>
                grant.WorkspaceId == workspaceId &&
                grant.ResourceType == resourceType &&
                grant.ResourceId == resourceId &&
                grant.SubjectType == SubjectTypes.User &&
                grant.SubjectId == userId &&
                grant.IsActive(now)));
        }

        public Task<ResourceAccessGrant?> GetActiveSubjectGrantAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            string subjectType,
            Guid subjectId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_grants.FirstOrDefault(grant =>
                grant.WorkspaceId == workspaceId &&
                grant.ResourceType == resourceType &&
                grant.ResourceId == resourceId &&
                grant.SubjectType == subjectType &&
                grant.SubjectId == subjectId &&
                grant.IsActive(now)));
        }

        public Task<IReadOnlyList<ResourceAccessGrant>> GetActiveGroupGrantsAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            IReadOnlyCollection<Guid> groupIds,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            var grants = _grants
                .Where(grant =>
                    grant.WorkspaceId == workspaceId &&
                    grant.ResourceType == resourceType &&
                    grant.ResourceId == resourceId &&
                    grant.SubjectType == SubjectTypes.Group &&
                    groupIds.Contains(grant.SubjectId) &&
                    grant.IsActive(now))
                .ToArray();
            return Task.FromResult<IReadOnlyList<ResourceAccessGrant>>(grants);
        }

        public Task<IReadOnlyList<ResourceAccessGrant>> GetActiveGroupGrantsForResourcesAsync(
            Guid workspaceId,
            string resourceType,
            IReadOnlyCollection<Guid> resourceIds,
            IReadOnlyCollection<Guid> groupIds,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            var grants = _grants
                .Where(grant =>
                    grant.WorkspaceId == workspaceId &&
                    grant.ResourceType == resourceType &&
                    resourceIds.Contains(grant.ResourceId) &&
                    grant.SubjectType == SubjectTypes.Group &&
                    groupIds.Contains(grant.SubjectId) &&
                    grant.IsActive(now))
                .ToArray();
            return Task.FromResult<IReadOnlyList<ResourceAccessGrant>>(grants);
        }

        public Task<ResourceAccessGrant?> GetGrantForUpdateAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            Guid grantId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_grants.FirstOrDefault(grant =>
                grant.Id == grantId &&
                grant.WorkspaceId == workspaceId &&
                grant.ResourceType == resourceType &&
                grant.ResourceId == resourceId));
        }

        public Task<ResourceAccessGrant?> GetUserGrantForUpdateAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_grants.FirstOrDefault(grant =>
                grant.WorkspaceId == workspaceId &&
                grant.ResourceType == resourceType &&
                grant.ResourceId == resourceId &&
                grant.SubjectType == SubjectTypes.User &&
                grant.SubjectId == userId &&
                grant.RevokedAt == null));
        }

        public Task<ResourceAccessGrant?> GetSubjectGrantForUpdateAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            string subjectType,
            Guid subjectId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_grants.FirstOrDefault(grant =>
                grant.WorkspaceId == workspaceId &&
                grant.ResourceType == resourceType &&
                grant.ResourceId == resourceId &&
                grant.SubjectType == subjectType &&
                grant.SubjectId == subjectId &&
                grant.RevokedAt == null));
        }

        public Task<IReadOnlyList<ResourceAccessGrant>> GetActiveUserGrantsForResourcesAsync(
            Guid workspaceId,
            string resourceType,
            IReadOnlyCollection<Guid> resourceIds,
            Guid userId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            var grants = _grants
                .Where(grant =>
                    grant.WorkspaceId == workspaceId &&
                    grant.ResourceType == resourceType &&
                    resourceIds.Contains(grant.ResourceId) &&
                    grant.SubjectType == SubjectTypes.User &&
                    grant.SubjectId == userId &&
                    grant.IsActive(now))
                .ToArray();
            return Task.FromResult<IReadOnlyList<ResourceAccessGrant>>(grants);
        }

        public Task AddPolicyAsync(ResourceAccessPolicy policy, CancellationToken cancellationToken = default)
        {
            AddPolicy(policy);
            return Task.CompletedTask;
        }

        public Task AddGrantAsync(ResourceAccessGrant grant, CancellationToken cancellationToken = default)
        {
            AddGrant(grant);
            return Task.CompletedTask;
        }
    }

    private sealed class TestWorkspaceGroupRepository : IWorkspaceGroupRepository
    {
        private readonly Guid _workspaceId;
        private readonly Guid _userId;
        private readonly List<Guid> _activeGroupIds = [];

        public TestWorkspaceGroupRepository(Guid workspaceId, Guid userId)
        {
            _workspaceId = workspaceId;
            _userId = userId;
        }

        public void AddActiveGroup(Guid groupId)
        {
            _activeGroupIds.Add(groupId);
        }

        public void AddInactiveGroup(Guid groupId, string reason)
        {
            _ = groupId;
            _ = reason;
        }

        public Task<bool> WorkspaceExistsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(workspaceId == _workspaceId);
        }

        public Task<bool> UserIsWorkspaceMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(workspaceId == _workspaceId && userId == _userId);
        }

        public Task<IReadOnlyList<WorkspaceGroupReadModel>> GetGroupsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<WorkspaceGroupReadModel>>(Array.Empty<WorkspaceGroupReadModel>());
        }

        public Task<WorkspaceGroupDetailReadModel?> GetGroupDetailAsync(Guid workspaceId, Guid groupId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WorkspaceGroupDetailReadModel?>(null);
        }

        public Task<WorkspaceGroup?> GetGroupAsync(Guid workspaceId, Guid groupId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WorkspaceGroup?>(null);
        }

        public Task<WorkspaceGroup?> GetGroupForUpdateAsync(Guid workspaceId, Guid groupId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WorkspaceGroup?>(null);
        }

        public Task<bool> ActiveGroupNameExistsAsync(Guid workspaceId, string name, Guid? exceptGroupId = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<WorkspaceGroupMember?> GetActiveMemberForUpdateAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WorkspaceGroupMember?>(null);
        }

        public Task<IReadOnlyList<Guid>> GetActiveGroupIdsForUserAsync(
            Guid workspaceId,
            Guid userId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Guid>>(
                workspaceId == _workspaceId && userId == _userId
                    ? _activeGroupIds.ToArray()
                    : Array.Empty<Guid>());
        }

        public Task<IReadOnlyList<Guid>> GetActiveGroupMemberUserIdsAsync(
            Guid workspaceId,
            Guid groupId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
        }

        public Task AddGroupAsync(WorkspaceGroup group, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task AddMemberAsync(WorkspaceGroupMember member, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestShareLinkRepository : IShareLinkRepository
    {
        public Task<IReadOnlyList<ShareLink>> GetActiveByResourceAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ShareLink>>(Array.Empty<ShareLink>());
        }

        public Task<ShareLink?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ShareLink?>(null);
        }

        public Task<ShareLink?> GetForUpdateAsync(
            Guid shareLinkId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ShareLink?>(null);
        }

        public Task AddAsync(ShareLink link, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
