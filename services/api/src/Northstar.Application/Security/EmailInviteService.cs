using System.Text.Json;
using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Contracts.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class EmailInviteService : IEmailInviteService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IEmailInviteRepository _inviteRepository;
    private readonly IShareLinkTokenService _tokenService;
    private readonly IResourceWorkspaceResolver _resourceResolver;
    private readonly IResourcePermissionRepository _permissionRepository;
    private readonly IScopedResourceAccessService _scopedAccessService;
    private readonly IPermissionUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionCatalog _permissionCatalog;
    private readonly IPermissionAuditService _auditService;
    private readonly IPermissionNotificationFanoutService _notificationFanoutService;
    private readonly IAuthStepUpService _stepUpService;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailInviteDeliveryOutboxRepository _deliveryOutboxRepository;
    private readonly IEmailInviteDeliveryOutboxProcessor _deliveryOutboxProcessor;
    private readonly EmailInviteDeliveryOptions _deliveryOptions;

    public EmailInviteService(
        IEmailInviteRepository inviteRepository,
        IShareLinkTokenService tokenService,
        IResourceWorkspaceResolver resourceResolver,
        IResourcePermissionRepository permissionRepository,
        IScopedResourceAccessService scopedAccessService,
        IPermissionUserRepository userRepository,
        ICurrentUser currentUser,
        IPermissionCatalog permissionCatalog,
        IPermissionAuditService auditService,
        IPermissionNotificationFanoutService notificationFanoutService,
        IAuthStepUpService stepUpService,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork,
        IEmailInviteDeliveryOutboxRepository deliveryOutboxRepository,
        IEmailInviteDeliveryOutboxProcessor deliveryOutboxProcessor,
        EmailInviteDeliveryOptions deliveryOptions)
    {
        _inviteRepository = inviteRepository;
        _tokenService = tokenService;
        _resourceResolver = resourceResolver;
        _permissionRepository = permissionRepository;
        _scopedAccessService = scopedAccessService;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _permissionCatalog = permissionCatalog;
        _auditService = auditService;
        _notificationFanoutService = notificationFanoutService;
        _stepUpService = stepUpService;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
        _deliveryOutboxRepository = deliveryOutboxRepository;
        _deliveryOutboxProcessor = deliveryOutboxProcessor;
        _deliveryOptions = deliveryOptions;
    }

    public async Task<EmailInvitesResponse> GetInvitesAsync(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var target = await ResolveTargetAsync(resourceType, resourceId, cancellationToken);
        await EnsureCanShareAsync(target, cancellationToken);
        var invites = await _inviteRepository.GetByResourceAsync(
            target.WorkspaceId,
            target.ResourceType,
            target.ResourceId,
            cancellationToken);
        var now = DateTimeOffset.UtcNow;
        return new EmailInvitesResponse(invites.Select(invite => ToDto(invite, now)).ToArray());
    }

    public Task<CreateEmailInviteResponse> CreateInviteAsync(
        string resourceType,
        Guid resourceId,
        CreateEmailInviteRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var target = await ResolveTargetAsync(resourceType, resourceId, ct);
            var shareResult = await EnsureCanShareAsync(target, ct);
            await _stepUpService.EnsureSatisfiedAsync(ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);
            var email = NormalizeEmail(request.Email);
            var roleKey = NormalizeInviteRole(request.RoleKey);
            EnsureCanGrant(shareResult.EffectiveRole, roleKey);
            EnsureFutureExpiry(request.ExpiresAt);

            var existingPending = await _inviteRepository.GetPendingForUpdateAsync(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                email,
                ct);
            if (existingPending is not null)
            {
                if (existingPending.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    throw new ApplicationErrorException(ErrorCodes.Conflict, "A pending invite already exists for this email.");
                }

                var expiredBefore = InviteSnapshot(existingPending);
                existingPending.Expire();
                await _auditService.AddAsync(
                    CreateAuditEvent(
                        target,
                        actorId,
                        PermissionAuditActions.EmailInviteExpired,
                        expiredBefore,
                        InviteSnapshot(existingPending),
                        shareResult,
                        existingPending),
                    ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            var token = _tokenService.GenerateToken();
            var invite = new ResourceEmailInvite(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                email,
                _tokenService.HashToken(token),
                roleKey,
                request.ExpiresAt,
                actorId);

            await _inviteRepository.AddAsync(invite, ct);
            await EnsureExternalLinkPolicyAsync(target, roleKey, actorId, shareResult, invite.Id, ct);
            var acceptUrl = BuildAcceptUrl(token);
            var delivery = await QueueAndAttemptDeliveryAsync(invite, acceptUrl, ct);
            invite.MarkDelivery(delivery.Status, delivery.Provider, delivery.AttemptedAt, delivery.ErrorCode);
            await _auditService.AddAsync(
                CreateAuditEvent(
                    target,
                    actorId,
                    PermissionAuditActions.EmailInviteCreated,
                    before: null,
                    after: InviteSnapshot(invite),
                    shareResult,
                    invite),
                ct);
            await _notificationFanoutService.AddEmailInviteCreatedAsync(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                invite.Id,
                actorId,
                ct);
            if (invite.DeliveryStatus == EmailInviteDeliveryStatuses.Failed)
            {
                await _notificationFanoutService.AddEmailInviteDeliveryFailedAsync(
                    target.WorkspaceId,
                    target.ResourceType,
                    target.ResourceId,
                    invite.Id,
                    actorId,
                    ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            return new CreateEmailInviteResponse(
                ToDto(invite, DateTimeOffset.UtcNow),
                token,
                acceptUrl,
                ToDeliveryDto(invite));
        }, cancellationToken);
    }

    public Task RevokeInviteAsync(
        Guid inviteId,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var invite = await _inviteRepository.GetForUpdateAsync(inviteId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Invite was not found.");
            var target = new PermissionResourceTarget(invite.ResourceType, invite.ResourceId, invite.WorkspaceId);
            var shareResult = await EnsureCanShareAsync(target, ct);
            await _stepUpService.EnsureSatisfiedAsync(ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);
            if (invite.Status == EmailInviteStatuses.Revoked)
            {
                return true;
            }

            var before = InviteSnapshot(invite);
            invite.Revoke(actorId);
            await _auditService.AddAsync(
                CreateAuditEvent(
                    target,
                    actorId,
                    PermissionAuditActions.EmailInviteRevoked,
                    before,
                    InviteSnapshot(invite),
                    shareResult,
                    invite),
                ct);
            await _notificationFanoutService.AddEmailInviteRevokedAsync(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                invite.Id,
                actorId,
                invite.InvitedBy,
                invite.AcceptedBy,
                ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }

    public Task<CreateEmailInviteResponse> RetryInviteAsync(
        Guid inviteId,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var existing = await _inviteRepository.GetForUpdateAsync(inviteId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Invite was not found.");
            var target = new PermissionResourceTarget(existing.ResourceType, existing.ResourceId, existing.WorkspaceId);
            var shareResult = await EnsureCanShareAsync(target, ct);
            await _stepUpService.EnsureSatisfiedAsync(ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);
            var now = DateTimeOffset.UtcNow;
            if (existing.Status != EmailInviteStatuses.Pending || existing.ExpiresAt <= now)
            {
                throw new ApplicationErrorException(ErrorCodes.Conflict, "Only active pending invites can be retried.");
            }

            if (existing.DeliveryStatus != EmailInviteDeliveryStatuses.Failed)
            {
                throw new ApplicationErrorException(ErrorCodes.Conflict, "Only failed email invites can be retried.");
            }

            var before = InviteSnapshot(existing);
            existing.Revoke(actorId);
            await _auditService.AddAsync(
                CreateAuditEvent(
                    target,
                    actorId,
                    PermissionAuditActions.EmailInviteRevoked,
                    before,
                    InviteSnapshot(existing),
                    shareResult,
                    existing),
                ct);

            var token = _tokenService.GenerateToken();
            var replacement = new ResourceEmailInvite(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                existing.Email,
                _tokenService.HashToken(token),
                existing.RoleKey,
                existing.ExpiresAt,
                actorId);

            await _inviteRepository.AddAsync(replacement, ct);
            await EnsureExternalLinkPolicyAsync(target, replacement.RoleKey, actorId, shareResult, replacement.Id, ct);
            var acceptUrl = BuildAcceptUrl(token);
            var delivery = await QueueAndAttemptDeliveryAsync(replacement, acceptUrl, ct);
            replacement.MarkDelivery(delivery.Status, delivery.Provider, delivery.AttemptedAt, delivery.ErrorCode);
            await _auditService.AddAsync(
                CreateAuditEvent(
                    target,
                    actorId,
                    PermissionAuditActions.EmailInviteCreated,
                    before: null,
                    after: InviteSnapshot(replacement),
                    shareResult,
                    replacement),
                ct);
            await _notificationFanoutService.AddEmailInviteCreatedAsync(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                replacement.Id,
                actorId,
                ct);
            if (replacement.DeliveryStatus == EmailInviteDeliveryStatuses.Failed)
            {
                await _notificationFanoutService.AddEmailInviteDeliveryFailedAsync(
                    target.WorkspaceId,
                    target.ResourceType,
                    target.ResourceId,
                    replacement.Id,
                    actorId,
                    ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return new CreateEmailInviteResponse(
                ToDto(replacement, DateTimeOffset.UtcNow),
                token,
                acceptUrl,
                ToDeliveryDto(replacement));
        }, cancellationToken);
    }

    public async Task<ResolveEmailInviteResponse> ResolveInviteAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var invite = await GetInviteForAuthenticatedEmailAsync(token, forUpdate: false, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Invite was not found.");
        if (!invite.IsPendingActive(DateTimeOffset.UtcNow))
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Invite was not found.");
        }

        return new ResolveEmailInviteResponse(
            invite.WorkspaceId.ToString(),
            invite.ResourceType,
            invite.ResourceId.ToString(),
            invite.Email,
            invite.RoleKey,
            invite.Status,
            invite.ExpiresAt);
    }

    public Task<AcceptEmailInviteResponse> AcceptInviteAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var actorId = await GetRequiredUserIdAsync(ct);
            var invite = await GetInviteForAuthenticatedEmailAsync(token, forUpdate: true, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Invite was not found.");
            var target = new PermissionResourceTarget(invite.ResourceType, invite.ResourceId, invite.WorkspaceId);
            var actorResult = new EffectivePermissionResult(
                true,
                invite.RoleKey,
                target.ResourceType == ResourceTypes.Document
                    ? EffectivePermissionService.DocumentEmailInviteSource
                    : EffectivePermissionService.CollectionEmailInviteSource,
                null);
            var now = DateTimeOffset.UtcNow;
            if (invite.Status == EmailInviteStatuses.Accepted && invite.AcceptedBy == actorId && invite.ExpiresAt > now)
            {
                return new AcceptEmailInviteResponse(ToDto(invite, now));
            }

            if (invite.Status != EmailInviteStatuses.Pending)
            {
                throw new ApplicationErrorException(ErrorCodes.NotFound, "Invite was not found.");
            }

            if (invite.ExpiresAt <= now)
            {
                var expiredBefore = InviteSnapshot(invite);
                invite.Expire();
                await _auditService.AddAsync(
                    CreateAuditEvent(
                        target,
                        actorId,
                        PermissionAuditActions.EmailInviteExpired,
                        expiredBefore,
                        InviteSnapshot(invite),
                        actorResult,
                        invite),
                    ct);
                await _unitOfWork.SaveChangesAsync(ct);
                throw new ApplicationErrorException(ErrorCodes.NotFound, "Invite was not found.");
            }

            var before = InviteSnapshot(invite);
            invite.Accept(actorId);
            await _auditService.AddAsync(
                CreateAuditEvent(
                    target,
                    actorId,
                    PermissionAuditActions.EmailInviteAccepted,
                    before,
                    InviteSnapshot(invite),
                    actorResult,
                    invite),
                ct);
            await _notificationFanoutService.AddEmailInviteAcceptedAsync(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                invite.Id,
                actorId,
                invite.InvitedBy,
                ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return new AcceptEmailInviteResponse(ToDto(invite, now));
        }, cancellationToken);
    }

    private async Task<EmailInviteDeliveryResult> QueueAndAttemptDeliveryAsync(
        ResourceEmailInvite invite,
        string acceptUrl,
        CancellationToken cancellationToken)
    {
        if (!_deliveryOptions.Enabled)
        {
            return new EmailInviteDeliveryResult(
                EmailInviteDeliveryStatuses.Disabled,
                NormalizeDeliveryProvider(_deliveryOptions.Provider),
                null);
        }

        var outbox = new EmailInviteDeliveryOutboxItem(
            invite.WorkspaceId,
            invite.Id,
            invite.Email,
            NormalizeDeliveryProvider(_deliveryOptions.Provider),
            Math.Max(1, _deliveryOptions.MaxAttempts),
            DateTimeOffset.UtcNow);
        await _deliveryOutboxRepository.AddAsync(outbox, cancellationToken);
        return await _deliveryOutboxProcessor.ProcessAsync(outbox, invite, acceptUrl, cancellationToken);
    }

    private string BuildAcceptUrl(string token)
    {
        var path = $"/api/v1/permissions/email-invites/{token}/accept";
        if (string.IsNullOrWhiteSpace(_deliveryOptions.PublicBaseUrl))
        {
            return path;
        }

        return $"{_deliveryOptions.PublicBaseUrl.TrimEnd('/')}{path}";
    }

    private async Task<ResourceEmailInvite?> GetInviteForAuthenticatedEmailAsync(
        string token,
        bool forUpdate,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Invite token is required.");
        }

        var actorId = await GetRequiredUserIdAsync(cancellationToken);
        var invite = forUpdate
            ? await _inviteRepository.GetByTokenHashForUpdateAsync(_tokenService.HashToken(token.Trim()), cancellationToken)
            : await _inviteRepository.GetByTokenHashAsync(_tokenService.HashToken(token.Trim()), cancellationToken);
        if (invite is null)
        {
            return null;
        }

        var user = await _userRepository.GetIdentityAsync(actorId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Current user was not found.");
        if (!string.Equals(user.Email, invite.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Invite access is denied.");
        }

        return invite;
    }

    private async Task EnsureExternalLinkPolicyAsync(
        PermissionResourceTarget target,
        string defaultLinkRole,
        Guid actorId,
        EffectivePermissionResult shareResult,
        Guid inviteId,
        CancellationToken cancellationToken)
    {
        var policy = await _permissionRepository.GetPolicyForUpdateAsync(
            target.WorkspaceId,
            target.ResourceType,
            target.ResourceId,
            cancellationToken);
        if (policy is null)
        {
            policy = new ResourceAccessPolicy(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                InheritanceModes.Inherit,
                LinkModes.External,
                defaultLinkRole,
                actorId);
            await _permissionRepository.AddPolicyAsync(policy, cancellationToken);
            await _auditService.AddAsync(
                CreatePolicyAuditEvent(target, actorId, before: null, after: PolicySnapshot(policy), shareResult, inviteId),
                cancellationToken);
            return;
        }

        if (policy.LinkMode != LinkModes.External || policy.DefaultLinkRole != defaultLinkRole)
        {
            var before = PolicySnapshot(policy);
            policy.SetLinkMode(LinkModes.External, defaultLinkRole);
            await _auditService.AddAsync(
                CreatePolicyAuditEvent(target, actorId, before, PolicySnapshot(policy), shareResult, inviteId),
                cancellationToken);
        }
    }

    private async Task<EffectivePermissionResult> EnsureCanShareAsync(
        PermissionResourceTarget target,
        CancellationToken cancellationToken)
    {
        return target.ResourceType == ResourceTypes.Document
            ? await _scopedAccessService.EnsureCanAccessDocumentAsync(
                target.ResourceId,
                PermissionActions.DocumentShare,
                cancellationToken)
            : await _scopedAccessService.EnsureCanAccessCollectionAsync(
                target.ResourceId,
                PermissionActions.CollectionShare,
                cancellationToken);
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
            return new PermissionResourceTarget(ResourceTypes.Document, document.DocumentId, document.WorkspaceId);
        }

        var collection = await _resourceResolver.GetCollectionPermissionResourceAsync(resourceId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Collection was not found.");
        return new PermissionResourceTarget(ResourceTypes.Collection, collection.CollectionId, collection.WorkspaceId);
    }

    private async Task<Guid> GetRequiredUserIdAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Authentication is required.");
        }

        return await Task.FromResult(_currentUser.UserId.Value);
    }

    private void EnsureCanGrant(string? actorRole, string roleKey)
    {
        if (_permissionCatalog.GetRank(actorRole) < _permissionCatalog.GetRank(roleKey))
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Cannot invite a role above the actor's effective role.");
        }
    }

    private static string NormalizeInviteRole(string roleKey)
    {
        var normalized = roleKey.Trim().ToLowerInvariant();
        return normalized is PermissionRole.Viewer or PermissionRole.Commenter
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "roleKey must be viewer or commenter.");
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "email is required.");
        }

        return email.Trim().ToLowerInvariant();
    }

    private static void EnsureFutureExpiry(DateTimeOffset expiresAt)
    {
        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "expiresAt must be in the future.");
        }
    }

    private static EmailInviteDto ToDto(ResourceEmailInvite invite, DateTimeOffset now)
    {
        var status = invite.Status == EmailInviteStatuses.Pending && invite.ExpiresAt <= now
            ? EmailInviteStatuses.Expired
            : invite.Status;
        return new EmailInviteDto(
            invite.Id.ToString(),
            invite.WorkspaceId.ToString(),
            invite.ResourceType,
            invite.ResourceId.ToString(),
            invite.Email,
            invite.RoleKey,
            status,
            invite.InvitedBy?.ToString(),
            invite.AcceptedBy?.ToString(),
            invite.RevokedBy?.ToString(),
            invite.CreatedAt,
            invite.ExpiresAt,
            invite.AcceptedAt,
            invite.RevokedAt,
            invite.ExpiredAt,
            invite.DeliveryStatus,
            invite.DeliveryProvider,
            invite.DeliveryAttemptedAt,
            invite.DeliveryErrorCode);
    }

    private static EmailInviteDeliveryDto ToDeliveryDto(ResourceEmailInvite invite)
    {
        return new EmailInviteDeliveryDto(
            invite.DeliveryStatus,
            invite.DeliveryProvider,
            invite.DeliveryAttemptedAt,
            invite.DeliveryErrorCode);
    }

    private static PermissionAuditEvent CreateAuditEvent(
        PermissionResourceTarget target,
        Guid actorId,
        string action,
        object? before,
        object? after,
        EffectivePermissionResult shareResult,
        ResourceEmailInvite invite)
    {
        return new PermissionAuditEvent(
            target.WorkspaceId,
            actorId,
            action,
            target.ResourceType,
            target.ResourceId,
            SubjectTypes.EmailInvite,
            invite.Id,
            before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            JsonSerializer.Serialize(new
            {
                shareResult.EffectiveRole,
                shareResult.Source,
                emailInviteId = invite.Id,
                invite.Email,
                invite.RoleKey,
                invite.Status,
                invite.ExpiresAt,
                invite.DeliveryStatus,
                invite.DeliveryProvider,
                invite.DeliveryAttemptedAt,
                invite.DeliveryErrorCode
            }, JsonOptions));
    }

    private static PermissionAuditEvent CreatePolicyAuditEvent(
        PermissionResourceTarget target,
        Guid actorId,
        object? before,
        object after,
        EffectivePermissionResult shareResult,
        Guid inviteId)
    {
        return new PermissionAuditEvent(
            target.WorkspaceId,
            actorId,
            PermissionAuditActions.PolicyUpdated,
            target.ResourceType,
            target.ResourceId,
            SubjectTypes.EmailInvite,
            inviteId,
            before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            JsonSerializer.Serialize(after, JsonOptions),
            JsonSerializer.Serialize(new
            {
                shareResult.EffectiveRole,
                shareResult.Source,
                emailInviteId = inviteId
            }, JsonOptions));
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

    private static object InviteSnapshot(ResourceEmailInvite invite)
    {
        return new
        {
            invite.Id,
            invite.WorkspaceId,
            invite.ResourceType,
            invite.ResourceId,
            invite.Email,
            invite.RoleKey,
            invite.Status,
            invite.InvitedBy,
            invite.AcceptedBy,
            invite.RevokedBy,
            invite.CreatedAt,
            invite.ExpiresAt,
            invite.AcceptedAt,
            invite.RevokedAt,
            invite.ExpiredAt,
            invite.DeliveryStatus,
            invite.DeliveryProvider,
            invite.DeliveryAttemptedAt,
            invite.DeliveryErrorCode
        };
    }

    private static string NormalizeDeliveryProvider(string? provider)
    {
        return string.IsNullOrWhiteSpace(provider)
            ? "noop"
            : provider.Trim().ToLowerInvariant();
    }

    private sealed record PermissionResourceTarget(
        string ResourceType,
        Guid ResourceId,
        Guid WorkspaceId);
}
