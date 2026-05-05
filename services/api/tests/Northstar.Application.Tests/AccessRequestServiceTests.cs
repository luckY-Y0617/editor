using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Tests;

public sealed class AccessRequestServiceTests
{
    [Fact]
    public async Task CreateAccessRequestAsync_ViewerCreatesPendingRequestAndManagerNotification()
    {
        var fixture = AccessRequestFixture.Create(PermissionRole.Viewer);

        var request = await fixture.Service.CreateAccessRequestAsync(
            new CreateAccessRequestRequest(
                ResourceTypes.Document,
                fixture.DocumentId.ToString(),
                PermissionRole.Editor,
                "Need to update wording"));

        Assert.Equal(AccessRequestStatus.Pending, request.Status);
        Assert.Equal(PermissionRole.Editor, request.RequestedRole);
        Assert.Single(fixture.AccessRequests.Requests);
        Assert.Contains(fixture.Audit.Events, audit => audit.Action == PermissionAuditActions.AccessRequestCreated);
        Assert.Contains(fixture.Notifications.Notifications, notification =>
            notification.RecipientUserId == fixture.ManagerId &&
            notification.Type == PermissionNotificationTypes.AccessRequestCreated);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateAccessRequestAsync_DuplicatePendingRequestIsRejected()
    {
        var fixture = AccessRequestFixture.Create(PermissionRole.Viewer);
        fixture.AccessRequests.Requests.Add(new AccessRequest(
            fixture.WorkspaceId,
            ResourceTypes.Document,
            fixture.DocumentId,
            fixture.UserId,
            SubjectTypes.User,
            fixture.UserId,
            PermissionRole.Viewer));

        var exception = await Assert.ThrowsAsync<ApplicationErrorException>(() =>
            fixture.Service.CreateAccessRequestAsync(
                new CreateAccessRequestRequest(
                    ResourceTypes.Document,
                    fixture.DocumentId.ToString(),
                    PermissionRole.Viewer,
                    "again")));

        Assert.Equal(ErrorCodes.Conflict, exception.Code);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    private sealed class AccessRequestFixture
    {
        private AccessRequestFixture(
            Guid workspaceId,
            Guid documentId,
            Guid userId,
            Guid managerId,
            TestAccessRequestRepository accessRequests,
            TestPermissionAuditService audit,
            TestPermissionNotificationService notifications,
            TestUnitOfWork unitOfWork,
            AccessRequestService service)
        {
            WorkspaceId = workspaceId;
            DocumentId = documentId;
            UserId = userId;
            ManagerId = managerId;
            AccessRequests = accessRequests;
            Audit = audit;
            Notifications = notifications;
            UnitOfWork = unitOfWork;
            Service = service;
        }

        public Guid WorkspaceId { get; }
        public Guid DocumentId { get; }
        public Guid UserId { get; }
        public Guid ManagerId { get; }
        public TestAccessRequestRepository AccessRequests { get; }
        public TestPermissionAuditService Audit { get; }
        public TestPermissionNotificationService Notifications { get; }
        public TestUnitOfWork UnitOfWork { get; }
        public AccessRequestService Service { get; }

        public static AccessRequestFixture Create(string workspaceRole)
        {
            var workspaceId = Guid.NewGuid();
            var documentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var managerId = Guid.NewGuid();
            var accessRequests = new TestAccessRequestRepository(managerId);
            var resourcePermissions = new TestResourcePermissionRepository();
            var groupRepository = new TestWorkspaceGroupRepository(workspaceId, userId);
            var audit = new TestPermissionAuditService();
            var notifications = new TestPermissionNotificationService();
            var permissionCatalog = new PermissionCatalog();
            var notificationFanout = new PermissionNotificationFanoutService(
                accessRequests,
                resourcePermissions,
                groupRepository,
                new TestPermissionNotificationPreferenceRepository(),
                notifications,
                permissionCatalog);
            var unitOfWork = new TestUnitOfWork();
            var service = new AccessRequestService(
                accessRequests,
                resourcePermissions,
                groupRepository,
                new TestResourceWorkspaceResolver(workspaceId, documentId),
                new TestWorkspaceMembershipQueryService(workspaceId, userId, workspaceRole),
                new TestCurrentUser(userId),
                new TestEffectivePermissionService(documentId),
                new TestScopedResourceAccessService(userId),
                new TestWorkspaceAccessService(userId),
                permissionCatalog,
                audit,
                notifications,
                notificationFanout,
                new TestTransactionRunner(),
                unitOfWork);

            return new AccessRequestFixture(
                workspaceId,
                documentId,
                userId,
                managerId,
                accessRequests,
                audit,
                notifications,
                unitOfWork,
                service);
        }
    }

    private sealed class TestAccessRequestRepository : IAccessRequestRepository
    {
        private readonly Guid _managerId;

        public TestAccessRequestRepository(Guid managerId)
        {
            _managerId = managerId;
        }

        public List<AccessRequest> Requests { get; } = [];

        public Task<AccessRequest?> GetForUpdateAsync(Guid requestId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Requests.SingleOrDefault(request => request.Id == requestId));
        }

        public Task<AccessRequest?> GetAsync(Guid requestId, CancellationToken cancellationToken = default)
        {
            return GetForUpdateAsync(requestId, cancellationToken);
        }

        public Task<AccessRequest?> GetPendingAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            string subjectType,
            Guid subjectId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Requests.SingleOrDefault(request =>
                request.WorkspaceId == workspaceId &&
                request.ResourceType == resourceType &&
                request.ResourceId == resourceId &&
                request.SubjectType == subjectType &&
                request.SubjectId == subjectId &&
                request.Status == AccessRequestStatus.Pending));
        }

        public Task<IReadOnlyList<AccessRequest>> GetByWorkspaceAsync(
            Guid workspaceId,
            string? status,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AccessRequest>>(
                Requests.Where(request => request.WorkspaceId == workspaceId && (status is null || request.Status == status)).ToArray());
        }

        public Task<IReadOnlyList<AccessRequest>> GetByResourceAsync(
            Guid workspaceId,
            string resourceType,
            Guid resourceId,
            string? status,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AccessRequest>>(
                Requests.Where(request =>
                    request.WorkspaceId == workspaceId &&
                    request.ResourceType == resourceType &&
                    request.ResourceId == resourceId &&
                    (status is null || request.Status == status)).ToArray());
        }

        public Task<IReadOnlyList<Guid>> GetWorkspaceManagerUserIdsAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default)
        {
            _ = workspaceId;
            return Task.FromResult<IReadOnlyList<Guid>>([_managerId]);
        }

        public Task AddAsync(AccessRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }

    private sealed class TestResourcePermissionRepository : IResourcePermissionRepository
    {
        public List<ResourceAccessGrant> Grants { get; } = [];

        public Task<ResourceAccessPolicy?> GetPolicyAsync(Guid workspaceId, string resourceType, Guid resourceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ResourceAccessPolicy?>(null);
        }

        public Task<ResourceAccessPolicy?> GetPolicyForUpdateAsync(Guid workspaceId, string resourceType, Guid resourceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ResourceAccessPolicy?>(null);
        }

        public Task<IReadOnlyList<ResourceAccessPolicy>> GetPoliciesForResourcesAsync(Guid workspaceId, string resourceType, IReadOnlyCollection<Guid> resourceIds, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ResourceAccessPolicy>>(Array.Empty<ResourceAccessPolicy>());
        }

        public Task<IReadOnlyList<ResourceAccessGrant>> GetUserGrantsAsync(Guid workspaceId, string resourceType, Guid resourceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ResourceAccessGrant>>(Array.Empty<ResourceAccessGrant>());
        }

        public Task<IReadOnlyList<ResourceAccessGrant>> GetGrantsAsync(Guid workspaceId, string resourceType, Guid resourceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ResourceAccessGrant>>(Grants.ToArray());
        }

        public Task<ResourceAccessGrant?> GetActiveUserGrantAsync(Guid workspaceId, string resourceType, Guid resourceId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ResourceAccessGrant?>(null);
        }

        public Task<ResourceAccessGrant?> GetActiveSubjectGrantAsync(Guid workspaceId, string resourceType, Guid resourceId, string subjectType, Guid subjectId, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ResourceAccessGrant?>(null);
        }

        public Task<IReadOnlyList<ResourceAccessGrant>> GetActiveGroupGrantsAsync(Guid workspaceId, string resourceType, Guid resourceId, IReadOnlyCollection<Guid> groupIds, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ResourceAccessGrant>>(Array.Empty<ResourceAccessGrant>());
        }

        public Task<IReadOnlyList<ResourceAccessGrant>> GetActiveGroupGrantsForResourcesAsync(Guid workspaceId, string resourceType, IReadOnlyCollection<Guid> resourceIds, IReadOnlyCollection<Guid> groupIds, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ResourceAccessGrant>>(Array.Empty<ResourceAccessGrant>());
        }

        public Task<ResourceAccessGrant?> GetGrantForUpdateAsync(Guid workspaceId, string resourceType, Guid resourceId, Guid grantId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Grants.SingleOrDefault(grant => grant.Id == grantId));
        }

        public Task<ResourceAccessGrant?> GetUserGrantForUpdateAsync(Guid workspaceId, string resourceType, Guid resourceId, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ResourceAccessGrant?>(null);
        }

        public Task<ResourceAccessGrant?> GetSubjectGrantForUpdateAsync(Guid workspaceId, string resourceType, Guid resourceId, string subjectType, Guid subjectId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Grants.SingleOrDefault(grant =>
                grant.WorkspaceId == workspaceId &&
                grant.ResourceType == resourceType &&
                grant.ResourceId == resourceId &&
                grant.SubjectType == subjectType &&
                grant.SubjectId == subjectId &&
                grant.RevokedAt is null));
        }

        public Task<IReadOnlyList<ResourceAccessGrant>> GetActiveUserGrantsForResourcesAsync(Guid workspaceId, string resourceType, IReadOnlyCollection<Guid> resourceIds, Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ResourceAccessGrant>>(Array.Empty<ResourceAccessGrant>());
        }

        public Task AddPolicyAsync(ResourceAccessPolicy policy, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task AddGrantAsync(ResourceAccessGrant grant, CancellationToken cancellationToken = default)
        {
            Grants.Add(grant);
            return Task.CompletedTask;
        }
    }

    private sealed class TestWorkspaceGroupRepository : IWorkspaceGroupRepository
    {
        private readonly Guid _workspaceId;
        private readonly Guid _userId;

        public TestWorkspaceGroupRepository(Guid workspaceId, Guid userId)
        {
            _workspaceId = workspaceId;
            _userId = userId;
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

        public Task<IReadOnlyList<Guid>> GetActiveGroupIdsForUserAsync(Guid workspaceId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
        }

        public Task<IReadOnlyList<Guid>> GetActiveGroupMemberUserIdsAsync(Guid workspaceId, Guid groupId, DateTimeOffset now, CancellationToken cancellationToken = default)
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

    private sealed class TestResourceWorkspaceResolver : IResourceWorkspaceResolver
    {
        private readonly Guid _workspaceId;
        private readonly Guid _documentId;

        public TestResourceWorkspaceResolver(Guid workspaceId, Guid documentId)
        {
            _workspaceId = workspaceId;
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
            return Task.FromResult<Guid?>(_workspaceId);
        }

        public Task<DocumentPermissionResource?> GetDocumentPermissionResourceAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(documentId == _documentId
                ? new DocumentPermissionResource(_documentId, _workspaceId, Guid.NewGuid())
                : null);
        }

        public Task<DocumentPermissionResource?> GetDocumentPermissionResourceIncludingDeletedAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            return GetDocumentPermissionResourceAsync(documentId, cancellationToken);
        }

        public async Task<IReadOnlyList<DocumentPermissionResource>> GetDocumentPermissionResourcesAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken cancellationToken = default)
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

        public Task<CollectionPermissionResource?> GetCollectionPermissionResourceAsync(Guid collectionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CollectionPermissionResource?>(new CollectionPermissionResource(collectionId, _workspaceId));
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

        public Task<string?> GetActiveRoleAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>(workspaceId == _workspaceId && userId == _userId ? _role : null);
        }
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

    private sealed class TestEffectivePermissionService : IEffectivePermissionService
    {
        private readonly Guid _documentId;

        public TestEffectivePermissionService(Guid documentId)
        {
            _documentId = documentId;
        }

        public Task<EffectivePermissionResult> AuthorizeWorkspaceAsync(Guid workspaceId, Guid userId, string actionKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EffectivePermissionResult(true, PermissionRole.Viewer, EffectivePermissionService.WorkspaceSource, null));
        }

        public Task<EffectivePermissionResult> AuthorizeCollectionAsync(Guid collectionId, Guid userId, string actionKey, CancellationToken cancellationToken = default, string? shareToken = null)
        {
            return Task.FromResult(new EffectivePermissionResult(false, null, EffectivePermissionService.CollectionSource, EffectivePermissionService.PermissionDeniedReason));
        }

        public Task<EffectivePermissionResult> AuthorizeDocumentAsync(Guid documentId, Guid userId, string actionKey, CancellationToken cancellationToken = default, string? shareToken = null)
        {
            return Task.FromResult(documentId == _documentId
                ? new EffectivePermissionResult(false, null, EffectivePermissionService.DocumentSource, EffectivePermissionService.PermissionDeniedReason)
                : new EffectivePermissionResult(false, null, EffectivePermissionService.DocumentSource, EffectivePermissionService.ResourceNotFoundReason));
        }

        public Task<IReadOnlyDictionary<Guid, EffectivePermissionResult>> AuthorizeDocumentsAsync(IReadOnlyCollection<Guid> documentIds, Guid userId, string actionKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<Guid, EffectivePermissionResult>>(
                documentIds.ToDictionary(
                    documentId => documentId,
                    documentId => documentId == _documentId
                        ? new EffectivePermissionResult(false, null, EffectivePermissionService.DocumentSource, EffectivePermissionService.PermissionDeniedReason)
                        : new EffectivePermissionResult(false, null, EffectivePermissionService.DocumentSource, EffectivePermissionService.ResourceNotFoundReason)));
        }

        public Task<EffectivePermissionResult> AuthorizeDocumentIncludingDeletedAsync(Guid documentId, Guid userId, string actionKey, CancellationToken cancellationToken = default, string? shareToken = null)
        {
            return AuthorizeDocumentAsync(documentId, userId, actionKey, cancellationToken);
        }
    }

    private sealed class TestScopedResourceAccessService : IScopedResourceAccessService
    {
        private readonly Guid _userId;

        public TestScopedResourceAccessService(Guid userId)
        {
            _userId = userId;
        }

        public Task<Guid> GetRequiredUserIdAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_userId);
        }

        public Task<EffectivePermissionResult> EnsureCanAccessDocumentAsync(Guid documentId, string actionKey, CancellationToken cancellationToken = default, string? shareToken = null)
        {
            return Task.FromResult(new EffectivePermissionResult(true, PermissionRole.Admin, EffectivePermissionService.AdminEscapeSource, null));
        }

        public Task<EffectivePermissionResult> EnsureCanAccessDocumentIncludingDeletedAsync(Guid documentId, string actionKey, CancellationToken cancellationToken = default, string? shareToken = null)
        {
            return EnsureCanAccessDocumentAsync(documentId, actionKey, cancellationToken);
        }

        public Task<EffectivePermissionResult> EnsureCanAccessCollectionAsync(Guid collectionId, string actionKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EffectivePermissionResult(true, PermissionRole.Admin, EffectivePermissionService.AdminEscapeSource, null));
        }

        public Task<EffectivePermissionResult> EnsureCanAccessDocumentAnyAsync(Guid documentId, IReadOnlyList<string> actionKeys, CancellationToken cancellationToken = default, string? shareToken = null)
        {
            return EnsureCanAccessDocumentAsync(documentId, actionKeys[0], cancellationToken);
        }
    }

    private sealed class TestWorkspaceAccessService : IWorkspaceAccessService
    {
        private readonly Guid _userId;

        public TestWorkspaceAccessService(Guid userId)
        {
            _userId = userId;
        }

        public Task<Guid> GetRequiredUserIdAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_userId);
        }

        public Task EnsureCanViewWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task EnsureCanEditWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task EnsureCanManageWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestPermissionAuditService : IPermissionAuditService
    {
        public List<PermissionAuditEvent> Events { get; } = [];

        public Task AddAsync(PermissionAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task<PermissionAuditResponse> GetAuditAsync(Guid workspaceId, string? resourceType, Guid? resourceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PermissionAuditResponse([]));
        }
    }

    private sealed class TestPermissionNotificationService : IPermissionNotificationService
    {
        public List<PermissionNotification> Notifications { get; } = [];

        public Task AddAsync(PermissionNotification notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<PermissionNotification> notifications, CancellationToken cancellationToken = default)
        {
            Notifications.AddRange(notifications);
            return Task.CompletedTask;
        }

        public Task<PermissionNotificationsResponse> GetNotificationsAsync(Guid? workspaceId, bool unreadOnly, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PermissionNotificationsResponse([], 0));
        }

        public Task<PermissionNotificationDto> MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task MarkAllReadAsync(Guid? workspaceId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestPermissionNotificationPreferenceRepository : IPermissionNotificationPreferenceRepository
    {
        public Task<IReadOnlyList<PermissionNotificationPreference>> GetForUserWorkspaceAsync(
            Guid userId,
            Guid workspaceId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PermissionNotificationPreference>>(Array.Empty<PermissionNotificationPreference>());
        }

        public Task<PermissionNotificationPreference?> GetForUpdateAsync(
            Guid userId,
            Guid workspaceId,
            string? resourceType,
            Guid? resourceId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PermissionNotificationPreference?>(null);
        }

        public Task AddAsync(PermissionNotificationPreference preference, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestTransactionRunner : ITransactionRunner
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            return operation(cancellationToken);
        }
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
