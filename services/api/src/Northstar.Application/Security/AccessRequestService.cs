using System.Text.Json;
using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Contracts.Security;
using Northstar.Domain.Security;
using Northstar.Domain.Workspaces;

namespace Northstar.Application.Security;

public sealed class AccessRequestService : IAccessRequestService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAccessRequestRepository _accessRequestRepository;
    private readonly IResourcePermissionRepository _permissionRepository;
    private readonly IWorkspaceGroupRepository _groupRepository;
    private readonly IResourceWorkspaceResolver _resourceResolver;
    private readonly IWorkspaceMembershipQueryService _membershipQueryService;
    private readonly ICurrentUser _currentUser;
    private readonly IEffectivePermissionService _effectivePermissionService;
    private readonly IScopedResourceAccessService _scopedResourceAccessService;
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IPermissionCatalog _permissionCatalog;
    private readonly IPermissionAuditService _auditService;
    private readonly IPermissionNotificationService _notificationService;
    private readonly IPermissionNotificationFanoutService _notificationFanoutService;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;

    public AccessRequestService(
        IAccessRequestRepository accessRequestRepository,
        IResourcePermissionRepository permissionRepository,
        IWorkspaceGroupRepository groupRepository,
        IResourceWorkspaceResolver resourceResolver,
        IWorkspaceMembershipQueryService membershipQueryService,
        ICurrentUser currentUser,
        IEffectivePermissionService effectivePermissionService,
        IScopedResourceAccessService scopedResourceAccessService,
        IWorkspaceAccessService workspaceAccessService,
        IPermissionCatalog permissionCatalog,
        IPermissionAuditService auditService,
        IPermissionNotificationService notificationService,
        IPermissionNotificationFanoutService notificationFanoutService,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork)
    {
        _accessRequestRepository = accessRequestRepository;
        _permissionRepository = permissionRepository;
        _groupRepository = groupRepository;
        _resourceResolver = resourceResolver;
        _membershipQueryService = membershipQueryService;
        _currentUser = currentUser;
        _effectivePermissionService = effectivePermissionService;
        _scopedResourceAccessService = scopedResourceAccessService;
        _workspaceAccessService = workspaceAccessService;
        _permissionCatalog = permissionCatalog;
        _auditService = auditService;
        _notificationService = notificationService;
        _notificationFanoutService = notificationFanoutService;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
    }

    public Task<AccessRequestDto> CreateAccessRequestAsync(
        CreateAccessRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var actorId = GetRequiredUserId();
            var resourceId = ParseGuid(request.ResourceId, "resourceId");
            var target = await ResolveTargetAsync(request.ResourceType, resourceId, ct);
            await EnsureActiveWorkspaceMemberAsync(target.WorkspaceId, actorId, ct);

            var subjectType = NormalizeSubjectType(request.SubjectType ?? SubjectTypes.User);
            var subjectId = string.IsNullOrWhiteSpace(request.SubjectId)
                ? actorId
                : ParseGuid(request.SubjectId, "subjectId");
            if (subjectType != SubjectTypes.User || subjectId != actorId)
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "Phase 5 only supports user access requests for the current user.");
            }

            var requestedRole = NormalizeRole(request.RequestedRole);
            await EnsureRequesterCanAskForRoleAsync(target.WorkspaceId, actorId, requestedRole, ct);
            await EnsureRequesterDoesNotAlreadyHaveRoleAsync(target, actorId, requestedRole, ct);

            var duplicate = await _accessRequestRepository.GetPendingAsync(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                subjectType,
                subjectId,
                ct);
            if (duplicate is not null)
            {
                throw new ApplicationErrorException(ErrorCodes.Conflict, "A pending access request already exists.");
            }

            var accessRequest = new AccessRequest(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                actorId,
                subjectType,
                subjectId,
                requestedRole,
                request.Reason);
            await _accessRequestRepository.AddAsync(accessRequest, ct);
            await _auditService.AddAsync(CreateAuditEvent(accessRequest, actorId, PermissionAuditActions.AccessRequestCreated, null, Snapshot(accessRequest)), ct);

            await _notificationFanoutService.AddAccessRequestCreatedAsync(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                accessRequest.Id,
                actorId,
                requestedRole,
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return ToDto(accessRequest);
        }, cancellationToken);
    }

    public async Task<AccessRequestsResponse> GetAccessRequestsAsync(
        Guid workspaceId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        await _workspaceAccessService.EnsureCanManageWorkspaceAsync(workspaceId, cancellationToken);
        var normalizedStatus = NormalizeOptionalStatus(status);
        var requests = await _accessRequestRepository.GetByWorkspaceAsync(workspaceId, normalizedStatus, cancellationToken);
        return new AccessRequestsResponse(requests.Select(ToDto).ToArray());
    }

    public async Task<AccessRequestsResponse> GetResourceAccessRequestsAsync(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var target = await ResolveTargetAsync(resourceType, resourceId, cancellationToken);
        await EnsureCanManageTargetAsync(target, cancellationToken);
        var requests = await _accessRequestRepository.GetByResourceAsync(
            target.WorkspaceId,
            target.ResourceType,
            target.ResourceId,
            AccessRequestStatus.Pending,
            cancellationToken);
        return new AccessRequestsResponse(requests.Select(ToDto).ToArray());
    }

    public Task<AccessRequestDto> ReviewAccessRequestAsync(
        Guid requestId,
        ReviewAccessRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var actorId = GetRequiredUserId();
            var accessRequest = await _accessRequestRepository.GetForUpdateAsync(requestId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Access request was not found.");
            var target = await ResolveTargetAsync(accessRequest.ResourceType, accessRequest.ResourceId, ct);
            var manageResult = await EnsureCanManageTargetAsync(target, ct);
            var before = Snapshot(accessRequest);
            var decision = NormalizeDecision(request.Decision);

            if (decision == "approve")
            {
                var roleKey = NormalizeRole(request.RoleKey ?? accessRequest.RequestedRole);
                EnsureCanGrant(manageResult.EffectiveRole, roleKey);
                EnsureFutureExpiry(request.ExpiresAt);
                var grant = await CreateOrUpgradeGrantAsync(accessRequest, roleKey, request.ExpiresAt, actorId, manageResult, ct);
                accessRequest.Approve(actorId, grant.Id, request.Reason);
                await _auditService.AddAsync(CreateAuditEvent(accessRequest, actorId, PermissionAuditActions.AccessRequestApproved, before, Snapshot(accessRequest), grant.Id), ct);
                await _notificationService.AddAsync(new PermissionNotification(
                    accessRequest.WorkspaceId,
                    accessRequest.RequesterId,
                    PermissionNotificationTypes.AccessRequestApproved,
                    "Access request approved",
                    $"Your request for {roleKey} access was approved.",
                    actorId,
                    accessRequest.ResourceType,
                    accessRequest.ResourceId,
                    accessRequest.Id,
                    grant.Id,
                    "#permissions"), ct);
            }
            else
            {
                accessRequest.Deny(actorId, request.Reason);
                await _auditService.AddAsync(CreateAuditEvent(accessRequest, actorId, PermissionAuditActions.AccessRequestDenied, before, Snapshot(accessRequest)), ct);
                await _notificationService.AddAsync(new PermissionNotification(
                    accessRequest.WorkspaceId,
                    accessRequest.RequesterId,
                    PermissionNotificationTypes.AccessRequestDenied,
                    "Access request denied",
                    request.Reason,
                    actorId,
                    accessRequest.ResourceType,
                    accessRequest.ResourceId,
                    accessRequest.Id,
                    actionUrl: "#permissions"), ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return ToDto(accessRequest);
        }, cancellationToken);
    }

    public Task<AccessRequestDto> CancelAccessRequestAsync(
        Guid requestId,
        CancelAccessRequestRequest? request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var actorId = GetRequiredUserId();
            var accessRequest = await _accessRequestRepository.GetForUpdateAsync(requestId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Access request was not found.");
            var target = await ResolveTargetAsync(accessRequest.ResourceType, accessRequest.ResourceId, ct);
            var isRequester = accessRequest.RequesterId == actorId;
            if (!isRequester)
            {
                await EnsureCanManageTargetAsync(target, ct);
            }

            var before = Snapshot(accessRequest);
            accessRequest.Cancel(actorId, request?.Reason);
            await _auditService.AddAsync(CreateAuditEvent(accessRequest, actorId, PermissionAuditActions.AccessRequestCancelled, before, Snapshot(accessRequest)), ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return ToDto(accessRequest);
        }, cancellationToken);
    }

    private async Task<ResourceAccessGrant> CreateOrUpgradeGrantAsync(
        AccessRequest accessRequest,
        string roleKey,
        DateTimeOffset? expiresAt,
        Guid actorId,
        EffectivePermissionResult manageResult,
        CancellationToken cancellationToken)
    {
        var existingGrant = await _permissionRepository.GetSubjectGrantForUpdateAsync(
            accessRequest.WorkspaceId,
            accessRequest.ResourceType,
            accessRequest.ResourceId,
            accessRequest.SubjectType,
            accessRequest.SubjectId,
            cancellationToken);
        if (existingGrant is not null)
        {
            EnsureCanGrant(manageResult.EffectiveRole, existingGrant.RoleKey);
            var isActive = existingGrant.IsActive(DateTimeOffset.UtcNow);
            var existingRank = _permissionCatalog.GetRank(existingGrant.RoleKey);
            var requestedRank = _permissionCatalog.GetRank(roleKey);
            if (!isActive || requestedRank > existingRank)
            {
                var before = GrantSnapshot(existingGrant);
                existingGrant.ChangeRole(roleKey);
                existingGrant.ChangeExpiry(expiresAt);
                existingGrant.ChangeReason(accessRequest.Reason);
                await _auditService.AddAsync(
                    CreateGrantAuditEvent(accessRequest, actorId, PermissionAuditActions.GrantUpdated, before, GrantSnapshot(existingGrant), manageResult, existingGrant),
                    cancellationToken);
            }

            return existingGrant;
        }

        var grant = new ResourceAccessGrant(
            accessRequest.WorkspaceId,
            accessRequest.ResourceType,
            accessRequest.ResourceId,
            accessRequest.SubjectType,
            accessRequest.SubjectId,
            roleKey,
            actorId,
            expiresAt);
        grant.ChangeReason(accessRequest.Reason);
        await _permissionRepository.AddGrantAsync(grant, cancellationToken);
        await _auditService.AddAsync(
            CreateGrantAuditEvent(accessRequest, actorId, PermissionAuditActions.GrantCreated, null, GrantSnapshot(grant), manageResult, grant),
            cancellationToken);
        return grant;
    }

    private async Task EnsureRequesterCanAskForRoleAsync(
        Guid workspaceId,
        Guid actorId,
        string requestedRole,
        CancellationToken cancellationToken)
    {
        var workspaceRole = await _membershipQueryService.GetActiveRoleAsync(workspaceId, actorId, cancellationToken);
        if (workspaceRole is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace access is denied.");
        }

        if (!WorkspaceMemberRole.IsValid(workspaceRole))
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace permission is insufficient.");
        }

        if (_permissionCatalog.GetRank(requestedRole) > PermissionRole.EditorRank &&
            workspaceRole is not PermissionRole.Admin and not PermissionRole.Owner)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Only workspace admins or owners can request admin or owner access.");
        }
    }

    private async Task EnsureRequesterDoesNotAlreadyHaveRoleAsync(
        PermissionResourceTarget target,
        Guid actorId,
        string requestedRole,
        CancellationToken cancellationToken)
    {
        var actionKey = target.ResourceType == ResourceTypes.Document
            ? PermissionActions.DocumentView
            : PermissionActions.CollectionView;
        var effective = target.ResourceType == ResourceTypes.Document
            ? await _effectivePermissionService.AuthorizeDocumentAsync(target.ResourceId, actorId, actionKey, cancellationToken)
            : await _effectivePermissionService.AuthorizeCollectionAsync(target.ResourceId, actorId, actionKey, cancellationToken);
        if (effective.Reason == EffectivePermissionService.NoMembershipReason)
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace access is denied.");
        }

        if (_permissionCatalog.GetRank(effective.EffectiveRole) >= _permissionCatalog.GetRank(requestedRole))
        {
            throw new ApplicationErrorException(ErrorCodes.Conflict, "Requester already has the requested access or higher.");
        }
    }

    private async Task EnsureActiveWorkspaceMemberAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!await _groupRepository.UserIsWorkspaceMemberAsync(workspaceId, userId, cancellationToken))
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace access is denied.");
        }
    }

    private async Task<EffectivePermissionResult> EnsureCanManageTargetAsync(
        PermissionResourceTarget target,
        CancellationToken cancellationToken)
    {
        return target.ResourceType == ResourceTypes.Document
            ? await _scopedResourceAccessService.EnsureCanAccessDocumentAsync(target.ResourceId, PermissionActions.DocumentManagePermissions, cancellationToken)
            : await _scopedResourceAccessService.EnsureCanAccessCollectionAsync(target.ResourceId, PermissionActions.CollectionManagePermissions, cancellationToken);
    }

    private async Task<PermissionResourceTarget> ResolveTargetAsync(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        var normalized = PermissionResourceNormalizer.NormalizeScopedResourceType(resourceType);
        if (normalized == ResourceTypes.Document)
        {
            var document = await _resourceResolver.GetDocumentPermissionResourceAsync(resourceId, cancellationToken)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
            return new PermissionResourceTarget(ResourceTypes.Document, document.DocumentId, document.WorkspaceId);
        }

        var collection = await _resourceResolver.GetCollectionPermissionResourceAsync(resourceId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Collection was not found.");
        return new PermissionResourceTarget(ResourceTypes.Collection, collection.CollectionId, collection.WorkspaceId);
    }

    private void EnsureCanGrant(string? actorRole, string targetRole)
    {
        if (_permissionCatalog.GetRank(actorRole) < _permissionCatalog.GetRank(targetRole))
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Cannot grant a role above the actor's effective role.");
        }
    }

    private Guid GetRequiredUserId()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Authentication is required.");
        }

        return _currentUser.UserId.Value;
    }

    private static string NormalizeRole(string role)
    {
        var normalized = role.Trim().ToLowerInvariant();
        return ScopedPermissionRoles.IsSupported(normalized)
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "requested role is invalid.");
    }

    private static string NormalizeSubjectType(string subjectType)
    {
        var normalized = subjectType.Trim().ToLowerInvariant();
        return SubjectTypes.IsSupported(normalized)
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "subject type is invalid.");
    }

    private static string? NormalizeOptionalStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return AccessRequestStatus.IsSupported(normalized)
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "access request status is invalid.");
    }

    private static string NormalizeDecision(string decision)
    {
        var normalized = decision.Trim().ToLowerInvariant();
        return normalized is "approve" or "deny"
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "decision must be approve or deny.");
    }

    private static Guid ParseGuid(string value, string fieldName)
    {
        return Guid.TryParse(value, out var id)
            ? id
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, $"{fieldName} must be a valid UUID.");
    }

    private static void EnsureFutureExpiry(DateTimeOffset? expiresAt)
    {
        if (expiresAt.HasValue && expiresAt.Value <= DateTimeOffset.UtcNow)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "expiresAt must be in the future.");
        }
    }

    private static AccessRequestDto ToDto(AccessRequest request)
    {
        return new AccessRequestDto(
            request.Id.ToString(),
            request.WorkspaceId.ToString(),
            request.ResourceType,
            request.ResourceId.ToString(),
            request.RequesterId.ToString(),
            request.SubjectType,
            request.SubjectId.ToString(),
            request.RequestedRole,
            request.Reason,
            request.Status,
            request.DecidedBy?.ToString(),
            request.DecidedAt,
            request.DecisionReason,
            request.ResultingGrantId?.ToString(),
            request.CreatedAt,
            request.UpdatedAt);
    }

    private static PermissionAuditEvent CreateAuditEvent(
        AccessRequest request,
        Guid actorId,
        string action,
        object? before,
        object? after,
        Guid? grantId = null)
    {
        return new PermissionAuditEvent(
            request.WorkspaceId,
            actorId,
            action,
            request.ResourceType,
            request.ResourceId,
            request.SubjectType,
            request.SubjectId,
            before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            JsonSerializer.Serialize(new { accessRequestId = request.Id, grantId }, JsonOptions));
    }

    private static PermissionAuditEvent CreateGrantAuditEvent(
        AccessRequest request,
        Guid actorId,
        string action,
        object? before,
        object? after,
        EffectivePermissionResult manageResult,
        ResourceAccessGrant grant)
    {
        return new PermissionAuditEvent(
            request.WorkspaceId,
            actorId,
            action,
            request.ResourceType,
            request.ResourceId,
            grant.SubjectType,
            grant.SubjectId,
            before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            JsonSerializer.Serialize(new { manageResult.EffectiveRole, manageResult.Source, accessRequestId = request.Id }, JsonOptions));
    }

    private static object Snapshot(AccessRequest request)
    {
        return new
        {
            request.Id,
            request.Status,
            request.RequestedRole,
            request.DecidedBy,
            request.DecidedAt,
            request.DecisionReason,
            request.ResultingGrantId,
            request.UpdatedAt
        };
    }

    private static object GrantSnapshot(ResourceAccessGrant grant)
    {
        return new
        {
            grant.Id,
            grant.SubjectType,
            grant.SubjectId,
            grant.RoleKey,
            grant.ExpiresAt,
            grant.RevokedAt,
            grant.Reason
        };
    }

    private sealed record PermissionResourceTarget(string ResourceType, Guid ResourceId, Guid WorkspaceId);
}
