using System.Text.Json;
using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Contracts.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class ResourcePermissionManagementService : IResourcePermissionManagementService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyList<string> AvailableRoles =
    [
        PermissionRole.Viewer,
        PermissionRole.Commenter,
        PermissionRole.Editor,
        PermissionRole.Admin,
        PermissionRole.Owner
    ];

    private readonly IResourcePermissionRepository _permissionRepository;
    private readonly IWorkspaceGroupRepository _groupRepository;
    private readonly IResourceWorkspaceResolver _resourceResolver;
    private readonly IScopedResourceAccessService _resourceAccessService;
    private readonly IEffectivePermissionQueryService _effectivePermissionQueryService;
    private readonly IPermissionCatalog _permissionCatalog;
    private readonly IPermissionAuditService _auditService;
    private readonly IPermissionNotificationFanoutService _notificationFanoutService;
    private readonly IAuthStepUpService _stepUpService;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;

    public ResourcePermissionManagementService(
        IResourcePermissionRepository permissionRepository,
        IWorkspaceGroupRepository groupRepository,
        IResourceWorkspaceResolver resourceResolver,
        IScopedResourceAccessService resourceAccessService,
        IEffectivePermissionQueryService effectivePermissionQueryService,
        IPermissionCatalog permissionCatalog,
        IPermissionAuditService auditService,
        IPermissionNotificationFanoutService notificationFanoutService,
        IAuthStepUpService stepUpService,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork)
    {
        _permissionRepository = permissionRepository;
        _groupRepository = groupRepository;
        _resourceResolver = resourceResolver;
        _resourceAccessService = resourceAccessService;
        _effectivePermissionQueryService = effectivePermissionQueryService;
        _permissionCatalog = permissionCatalog;
        _auditService = auditService;
        _notificationFanoutService = notificationFanoutService;
        _stepUpService = stepUpService;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
    }

    public async Task<ResourcePermissionsResponse> GetResourcePermissionsAsync(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var target = await ResolveTargetAsync(resourceType, resourceId, cancellationToken);
        await EnsureCanManagePermissionsAsync(target, cancellationToken);
        return await BuildResponseAsync(target, cancellationToken);
    }

    public Task<ResourcePermissionsResponse> UpdatePolicyAsync(
        string resourceType,
        Guid resourceId,
        UpdateResourcePolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var target = await ResolveTargetAsync(resourceType, resourceId, ct);
            var manageResult = await EnsureCanManagePermissionsAsync(target, ct);
            await _stepUpService.EnsureSatisfiedAsync(ct);
            var actorId = await _resourceAccessService.GetRequiredUserIdAsync(ct);
            var inheritanceMode = NormalizeInheritanceMode(request.InheritanceMode);
            var linkMode = string.IsNullOrWhiteSpace(request.LinkMode)
                ? LinkModes.Disabled
                : request.LinkMode.Trim().ToLowerInvariant();
            if (linkMode == LinkModes.Public)
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "Public share links are not enabled.");
            }

            if (linkMode != LinkModes.Disabled && linkMode != LinkModes.Internal && linkMode != LinkModes.External)
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "linkMode is invalid.");
            }
            var defaultLinkRole = linkMode is LinkModes.Internal or LinkModes.External
                ? NormalizeOptionalLinkRole(request.DefaultLinkRole)
                : null;

            var policy = await _permissionRepository.GetPolicyForUpdateAsync(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                ct);
            var before = policy is null ? null : PolicySnapshot(policy);

            if (policy is null)
            {
                policy = new ResourceAccessPolicy(
                    target.WorkspaceId,
                    target.ResourceType,
                    target.ResourceId,
                    inheritanceMode,
                    linkMode,
                    defaultLinkRole,
                    createdBy: actorId);
                await _permissionRepository.AddPolicyAsync(policy, ct);
            }
            else
            {
                policy.SetInheritanceMode(inheritanceMode);
                policy.SetLinkMode(linkMode, defaultLinkRole);
            }

            await _auditService.AddAsync(
                CreateAuditEvent(
                    target,
                    actorId,
                    PermissionAuditActions.PolicyUpdated,
                    before,
                    PolicySnapshot(policy),
                    manageResult),
                ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return await BuildResponseAsync(target, ct);
        }, cancellationToken);
    }

    public Task<PermissionGrantDto> CreateGrantAsync(
        string resourceType,
        Guid resourceId,
        CreatePermissionGrantRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var target = await ResolveTargetAsync(resourceType, resourceId, ct);
            var manageResult = await EnsureCanManagePermissionsAsync(target, ct);
            await _stepUpService.EnsureSatisfiedAsync(ct);
            var actorId = await _resourceAccessService.GetRequiredUserIdAsync(ct);
            var subjectType = NormalizeSubjectType(request.SubjectType);
            var subjectId = ParseGuid(request.SubjectId, "subjectId");
            var roleKey = NormalizeScopedRole(request.RoleKey);
            EnsureCanGrant(manageResult.EffectiveRole, roleKey);
            EnsureFutureExpiry(request.ExpiresAt);

            await EnsureSubjectExistsAsync(target.WorkspaceId, subjectType, subjectId, ct);

            var existingGrant = await _permissionRepository.GetSubjectGrantForUpdateAsync(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                subjectType,
                subjectId,
                ct);
            if (existingGrant is not null)
            {
                if (existingGrant.IsActive(DateTimeOffset.UtcNow))
                {
                    throw new ApplicationErrorException(ErrorCodes.Conflict, "A grant already exists for this subject.");
                }

                EnsureCanGrant(manageResult.EffectiveRole, existingGrant.RoleKey);
                var before = GrantSnapshot(existingGrant);
                existingGrant.ChangeRole(roleKey);
                existingGrant.ChangeExpiry(request.ExpiresAt);
                existingGrant.ChangeReason(request.Reason);
                await _auditService.AddAsync(
                    CreateAuditEvent(
                        target,
                        actorId,
                        PermissionAuditActions.GrantUpdated,
                        before,
                        GrantSnapshot(existingGrant),
                        manageResult,
                        existingGrant.SubjectType,
                        existingGrant.SubjectId),
                    ct);
                await AddGrantNotificationAsync(
                    existingGrant,
                    actorId,
                    target,
                    PermissionNotificationTypes.GrantUpdated,
                    "Permission updated",
                    $"Your access was updated to {existingGrant.RoleKey}.",
                    ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ToDto(existingGrant);
            }

            var grant = new ResourceAccessGrant(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                subjectType,
                subjectId,
                roleKey,
                actorId,
                request.ExpiresAt);
            grant.ChangeReason(request.Reason);
            await _permissionRepository.AddGrantAsync(grant, ct);
            await _auditService.AddAsync(
                CreateAuditEvent(
                    target,
                    actorId,
                    PermissionAuditActions.GrantCreated,
                    before: null,
                    after: GrantSnapshot(grant),
                    manageResult,
                    grant.SubjectType,
                    grant.SubjectId),
                ct);
            await AddGrantNotificationAsync(
                grant,
                actorId,
                target,
                PermissionNotificationTypes.GrantCreated,
                "Permission granted",
                $"You were granted {grant.RoleKey} access.",
                ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ToDto(grant);
        }, cancellationToken);
    }

    public Task<PermissionGrantDto> UpdateGrantAsync(
        string resourceType,
        Guid resourceId,
        Guid grantId,
        UpdatePermissionGrantRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var target = await ResolveTargetAsync(resourceType, resourceId, ct);
            var manageResult = await EnsureCanManagePermissionsAsync(target, ct);
            await _stepUpService.EnsureSatisfiedAsync(ct);
            var actorId = await _resourceAccessService.GetRequiredUserIdAsync(ct);
            var grant = await _permissionRepository.GetGrantForUpdateAsync(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                grantId,
                ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Permission grant was not found.");
            if (grant.RevokedAt.HasValue)
            {
                throw new ApplicationErrorException(ErrorCodes.Conflict, "Revoked grants cannot be updated.");
            }

            var before = GrantSnapshot(grant);
            EnsureCanGrant(manageResult.EffectiveRole, grant.RoleKey);
            var expiryPatch = ReadExpiryPatch(request.ExpiresAt);
            if (expiryPatch.HasValue)
            {
                EnsureFutureExpiry(expiryPatch.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.RoleKey))
            {
                var roleKey = NormalizeScopedRole(request.RoleKey);
                EnsureCanGrant(manageResult.EffectiveRole, roleKey);
                grant.ChangeRole(roleKey);
            }

            if (expiryPatch.Present)
            {
                grant.ChangeExpiry(expiryPatch.Value);
            }

            grant.ChangeReason(request.Reason);

            await _auditService.AddAsync(
                CreateAuditEvent(
                    target,
                    actorId,
                    PermissionAuditActions.GrantUpdated,
                    before,
                    GrantSnapshot(grant),
                    manageResult,
                    grant.SubjectType,
                    grant.SubjectId),
                ct);
            await AddGrantNotificationAsync(
                grant,
                actorId,
                target,
                PermissionNotificationTypes.GrantUpdated,
                "Permission updated",
                $"Your access was updated to {grant.RoleKey}.",
                ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ToDto(grant);
        }, cancellationToken);
    }

    public Task RevokeGrantAsync(
        string resourceType,
        Guid resourceId,
        Guid grantId,
        RevokePermissionGrantRequest? request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var target = await ResolveTargetAsync(resourceType, resourceId, ct);
            var manageResult = await EnsureCanManagePermissionsAsync(target, ct);
            await _stepUpService.EnsureSatisfiedAsync(ct);
            var actorId = await _resourceAccessService.GetRequiredUserIdAsync(ct);
            var grant = await _permissionRepository.GetGrantForUpdateAsync(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                grantId,
                ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Permission grant was not found.");
            if (grant.RevokedAt.HasValue)
            {
                return true;
            }

            var before = GrantSnapshot(grant);
            grant.Revoke(actorId, request?.Reason);
            await _auditService.AddAsync(
                CreateAuditEvent(
                    target,
                    actorId,
                    PermissionAuditActions.GrantRevoked,
                    before,
                    GrantSnapshot(grant),
                    manageResult,
                    grant.SubjectType,
                    grant.SubjectId),
                ct);
            await AddGrantNotificationAsync(
                grant,
                actorId,
                target,
                PermissionNotificationTypes.GrantRevoked,
                "Permission revoked",
                "Your direct access was revoked.",
                ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }

    private async Task<ResourcePermissionsResponse> BuildResponseAsync(
        PermissionResourceTarget target,
        CancellationToken cancellationToken)
    {
        var policy = await _permissionRepository.GetPolicyAsync(
            target.WorkspaceId,
            target.ResourceType,
            target.ResourceId,
            cancellationToken);
        var grants = await _permissionRepository.GetGrantsAsync(
            target.WorkspaceId,
            target.ResourceType,
            target.ResourceId,
            cancellationToken);
        var effective = await _effectivePermissionQueryService.GetEffectivePermissionAsync(
            target.ResourceType,
            target.ResourceId,
            cancellationToken);

        return new ResourcePermissionsResponse(
            target.ResourceType,
            target.ResourceId.ToString(),
            ToDto(policy),
            grants.Select(ToDto).ToArray(),
            effective,
            await ResolveInheritedFromAsync(target, policy, cancellationToken),
            AvailableRoles);
    }

    private async Task<string> ResolveInheritedFromAsync(
        PermissionResourceTarget target,
        ResourceAccessPolicy? policy,
        CancellationToken cancellationToken)
    {
        if (policy is not null)
        {
            return target.ResourceType;
        }

        if (target.ResourceType == ResourceTypes.Document && target.CollectionId.HasValue)
        {
            var collectionPolicy = await _permissionRepository.GetPolicyAsync(
                target.WorkspaceId,
                ResourceTypes.Collection,
                target.CollectionId.Value,
                cancellationToken);
            if (collectionPolicy is not null)
            {
                return ResourceTypes.Collection;
            }
        }

        return ResourceTypes.Workspace;
    }

    private async Task<PermissionResourceTarget> ResolveTargetAsync(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        var normalizedResourceType = PermissionResourceNormalizer.NormalizeScopedResourceType(resourceType);
        if (normalizedResourceType == ResourceTypes.Document)
        {
            var document = await _resourceResolver.GetDocumentPermissionResourceAsync(resourceId, cancellationToken)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
            return new PermissionResourceTarget(
                ResourceTypes.Document,
                document.DocumentId,
                document.WorkspaceId,
                document.CollectionId);
        }

        var collection = await _resourceResolver.GetCollectionPermissionResourceAsync(resourceId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Collection was not found.");
        return new PermissionResourceTarget(
            ResourceTypes.Collection,
            collection.CollectionId,
            collection.WorkspaceId,
            null);
    }

    private Task<EffectivePermissionResult> EnsureCanManagePermissionsAsync(
        PermissionResourceTarget target,
        CancellationToken cancellationToken)
    {
        return target.ResourceType == ResourceTypes.Document
            ? _resourceAccessService.EnsureCanAccessDocumentAsync(
                target.ResourceId,
                PermissionActions.DocumentManagePermissions,
                cancellationToken)
            : _resourceAccessService.EnsureCanAccessCollectionAsync(
                target.ResourceId,
                PermissionActions.CollectionManagePermissions,
                cancellationToken);
    }

    private void EnsureCanGrant(string? actorRole, string targetRole)
    {
        if (_permissionCatalog.GetRank(actorRole) < _permissionCatalog.GetRank(targetRole))
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Cannot grant a role above the actor's effective role.");
        }
    }

    private Task AddGrantNotificationAsync(
        ResourceAccessGrant grant,
        Guid actorId,
        PermissionResourceTarget target,
        string type,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        return _notificationFanoutService.AddGrantNotificationAsync(
            grant,
            actorId,
            type,
            title,
            body,
            cancellationToken);
    }

    private static string NormalizeInheritanceMode(string mode)
    {
        var normalized = mode.Trim().ToLowerInvariant();
        return InheritanceModes.IsSupported(normalized)
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "inheritanceMode is invalid.");
    }

    private static string NormalizeSubjectType(string subjectType)
    {
        var normalized = subjectType.Trim().ToLowerInvariant();
        return normalized is SubjectTypes.User or SubjectTypes.Group
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "Only user and group grants are supported.");
    }

    private async Task EnsureSubjectExistsAsync(
        Guid workspaceId,
        string subjectType,
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        if (subjectType == SubjectTypes.User)
        {
            if (!await _groupRepository.UserIsWorkspaceMemberAsync(workspaceId, subjectId, cancellationToken))
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "Grant user must be an active workspace member.");
            }

            return;
        }

        var group = await _groupRepository.GetGroupAsync(workspaceId, subjectId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.ValidationError, "Grant group was not found in this workspace.");
        if (group.IsArchived)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Archived groups cannot receive grants.");
        }
    }

    private static string NormalizeScopedRole(string role)
    {
        var normalized = role.Trim().ToLowerInvariant();
        return ScopedPermissionRoles.IsSupported(normalized)
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "roleKey is invalid.");
    }

    private static string? NormalizeOptionalLinkRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        var normalized = role.Trim().ToLowerInvariant();
        return normalized is PermissionRole.Viewer or PermissionRole.Commenter
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "defaultLinkRole must be viewer or commenter.");
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

    private static ExpiryPatch ReadExpiryPatch(JsonElement expiresAt)
    {
        return expiresAt.ValueKind switch
        {
            JsonValueKind.Undefined => new ExpiryPatch(false, null),
            JsonValueKind.Null => new ExpiryPatch(true, null),
            JsonValueKind.String when expiresAt.TryGetDateTimeOffset(out var value) => new ExpiryPatch(true, value),
            _ => throw new ApplicationErrorException(ErrorCodes.ValidationError, "expiresAt must be an ISO 8601 timestamp or null.")
        };
    }

    private static PermissionPolicyDto ToDto(ResourceAccessPolicy? policy)
    {
        return policy is null
            ? new PermissionPolicyDto(InheritanceModes.Inherit, LinkModes.Disabled, null)
            : new PermissionPolicyDto(policy.InheritanceMode, policy.LinkMode, policy.DefaultLinkRole);
    }

    private static PermissionGrantDto ToDto(ResourceAccessGrant grant)
    {
        return new PermissionGrantDto(
            grant.Id.ToString(),
            grant.SubjectType,
            grant.SubjectId.ToString(),
            grant.RoleKey,
            grant.GrantedBy?.ToString(),
            grant.GrantedAt,
            grant.ExpiresAt,
            grant.Reason);
    }

    private static PermissionAuditEvent CreateAuditEvent(
        PermissionResourceTarget target,
        Guid actorId,
        string action,
        object? before,
        object? after,
        EffectivePermissionResult manageResult,
        string? subjectType = null,
        Guid? subjectId = null)
    {
        return new PermissionAuditEvent(
            target.WorkspaceId,
            actorId,
            action,
            target.ResourceType,
            target.ResourceId,
            subjectType,
            subjectId,
            before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            JsonSerializer.Serialize(new { manageResult.EffectiveRole, manageResult.Source }, JsonOptions));
    }

    private static object PolicySnapshot(ResourceAccessPolicy policy)
    {
        return new
        {
            policy.Id,
            policy.InheritanceMode,
            policy.LinkMode,
            policy.DefaultLinkRole,
            policy.UpdatedAt
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
            grant.RevokedBy,
            grant.Reason
        };
    }

    private sealed record PermissionResourceTarget(
        string ResourceType,
        Guid ResourceId,
        Guid WorkspaceId,
        Guid? CollectionId);

    private readonly record struct ExpiryPatch(bool Present, DateTimeOffset? Value)
    {
        public bool HasValue => Value.HasValue;
    }
}
