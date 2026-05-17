using System.Text.Json;
using System.Text.Json.Nodes;
using Northstar.Application.Common;
using Northstar.Application.Knowledge;
using Northstar.Contracts.Common;
using Northstar.Contracts.Knowledge;
using Northstar.Contracts.Security;
using Northstar.Domain.Security;
using Northstar.Domain.Users;

namespace Northstar.Application.Security;

public sealed class ShareLinkService : IShareLinkService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IShareLinkRepository _shareLinkRepository;
    private readonly IShareLinkTokenService _tokenService;
    private readonly IShareLinkTokenProtector _tokenProtector;
    private readonly IResourceWorkspaceResolver _resourceResolver;
    private readonly IPermissionResourceDisplayResolver _resourceDisplayResolver;
    private readonly IResourcePermissionRepository _permissionRepository;
    private readonly IScopedResourceAccessService _scopedAccessService;
    private readonly IEffectivePermissionService _effectivePermissionService;
    private readonly IWorkspaceGroupRepository _groupRepository;
    private readonly IPermissionUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionCatalog _permissionCatalog;
    private readonly IPermissionAuditService _auditService;
    private readonly IShareLinkAccessAuditService _shareLinkAccessAuditService;
    private readonly IShareLinkAccessRepository _shareLinkAccessRepository;
    private readonly IPermissionNotificationFanoutService _notificationFanoutService;
    private readonly IAuthStepUpService _stepUpService;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKnowledgeQueryService _knowledgeQueryService;
    private readonly IPublicShareCollectionQueryService _publicCollectionQueryService;
    private readonly IPasswordHashService _passwordHashService;
    private readonly PermissionPublicShareOptions _publicShareOptions;

    public ShareLinkService(
        IShareLinkRepository shareLinkRepository,
        IShareLinkTokenService tokenService,
        IShareLinkTokenProtector tokenProtector,
        IResourceWorkspaceResolver resourceResolver,
        IPermissionResourceDisplayResolver resourceDisplayResolver,
        IResourcePermissionRepository permissionRepository,
        IScopedResourceAccessService scopedAccessService,
        IEffectivePermissionService effectivePermissionService,
        IWorkspaceGroupRepository groupRepository,
        IPermissionUserRepository userRepository,
        ICurrentUser currentUser,
        IPermissionCatalog permissionCatalog,
        IPermissionAuditService auditService,
        IShareLinkAccessAuditService shareLinkAccessAuditService,
        IShareLinkAccessRepository shareLinkAccessRepository,
        IPermissionNotificationFanoutService notificationFanoutService,
        IAuthStepUpService stepUpService,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork,
        IKnowledgeQueryService knowledgeQueryService,
        IPublicShareCollectionQueryService publicCollectionQueryService,
        IPasswordHashService passwordHashService,
        PermissionPublicShareOptions publicShareOptions)
    {
        _shareLinkRepository = shareLinkRepository;
        _tokenService = tokenService;
        _tokenProtector = tokenProtector;
        _resourceResolver = resourceResolver;
        _resourceDisplayResolver = resourceDisplayResolver;
        _permissionRepository = permissionRepository;
        _scopedAccessService = scopedAccessService;
        _effectivePermissionService = effectivePermissionService;
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _permissionCatalog = permissionCatalog;
        _auditService = auditService;
        _shareLinkAccessAuditService = shareLinkAccessAuditService;
        _shareLinkAccessRepository = shareLinkAccessRepository;
        _notificationFanoutService = notificationFanoutService;
        _stepUpService = stepUpService;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
        _knowledgeQueryService = knowledgeQueryService;
        _publicCollectionQueryService = publicCollectionQueryService;
        _passwordHashService = passwordHashService;
        _publicShareOptions = publicShareOptions;
    }

    public async Task<ShareLinksResponse> GetShareLinksAsync(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var target = await ResolveTargetAsync(resourceType, resourceId, cancellationToken);
        await EnsureCanShareAsync(target, cancellationToken);
        var links = await _shareLinkRepository.GetActiveByResourceAsync(
            target.WorkspaceId,
            target.ResourceType,
            target.ResourceId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        var policy = await _permissionRepository.GetPolicyAsync(
            target.WorkspaceId,
            target.ResourceType,
            target.ResourceId,
            cancellationToken);

        return new ShareLinksResponse(links.Select(link => ToDto(link, policy)).ToArray());
    }

    public async Task<LinkManagementListResponse> SearchShareLinksAsync(
        Guid workspaceId,
        string? resourceType,
        Guid? resourceId,
        string? audience,
        string? roleKey,
        string? status,
        string? q,
        int? offset,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "workspaceId is required.");
        }

        _ = await _scopedAccessService.GetRequiredUserIdAsync(cancellationToken);
        var normalizedOffset = Math.Max(0, offset ?? 0);
        var normalizedLimit = Math.Clamp(limit ?? 50, 1, 100);
        var normalizedResourceType = string.IsNullOrWhiteSpace(resourceType)
            ? null
            : PermissionResourceNormalizer.NormalizeShareableResourceType(resourceType);
        var normalizedAudience = NormalizeAudienceFilter(audience);
        var normalizedRole = NormalizeRoleFilter(roleKey);
        var normalizedStatus = NormalizeStatusFilter(status);
        var normalizedSearch = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        var links = await _shareLinkRepository.SearchAsync(
            new ShareLinkSearchQuery(
                workspaceId,
                normalizedResourceType,
                resourceId,
                normalizedAudience,
                normalizedRole),
            cancellationToken);
        var managementRows = await BuildManagementDtosAsync(links, cancellationToken);
        if (links.Count > 0 && managementRows.Count == 0)
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace permission is insufficient.");
        }

        var filtered = managementRows
            .Where(row => normalizedStatus is null || row.Status == normalizedStatus)
            .Where(row => MatchesSearch(row, normalizedSearch))
            .ToArray();

        return new LinkManagementListResponse(
            filtered.Skip(normalizedOffset).Take(normalizedLimit).ToArray(),
            normalizedOffset,
            normalizedLimit,
            filtered.Length,
            normalizedOffset + normalizedLimit < filtered.Length);
    }

    public async Task<ShareLinkDto> GetShareLinkAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default)
    {
        var link = await _shareLinkRepository.GetByIdAsync(shareLinkId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        var target = new PermissionResourceTarget(
            link.ResourceType,
            link.ResourceId,
            link.WorkspaceId);
        await EnsureCanShareAsync(target, cancellationToken);
        var policy = await _permissionRepository.GetPolicyAsync(
            target.WorkspaceId,
            target.ResourceType,
            target.ResourceId,
            cancellationToken);
        return ToDto(link, policy);
    }

    public async Task<LinkManagementDto> GetManagedShareLinkAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default)
    {
        var link = await _shareLinkRepository.GetByIdAsync(shareLinkId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        return await BuildManagementDtoAsync(link, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace permission is insufficient.");
    }

    public Task<CreateShareLinkResponse> CreateShareLinkAsync(
        string resourceType,
        Guid resourceId,
        CreateShareLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var target = await ResolveTargetAsync(resourceType, resourceId, ct);
            var shareResult = await EnsureCanShareAsync(target, ct);
            await _stepUpService.EnsureSatisfiedAsync(ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);
            var roleKey = NormalizeLinkRole(request.RoleKey);
            var audience = NormalizeAudience(request.Audience);
            var subjectEmail = NormalizeSubjectEmail(audience, request.SubjectEmail);
            EnsureCanGrant(shareResult.EffectiveRole, roleKey);
            EnsureExpiry(audience, request.ExpiresAt);
            EnsureAudienceRules(target, audience, roleKey, subjectEmail, request.ExpiresAt);
            var contentProtection = NormalizeContentProtection(target, audience, request.ContentProtection);
            EnsurePublicShareCreationPolicy(target, audience, roleKey, request.ExpiresAt, request.Password, contentProtection);
            var passwordHash = NormalizeAndHashPassword(audience, request.Password);

            var token = _tokenService.GenerateToken();
            var link = new ShareLink(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                _tokenService.HashToken(token),
                roleKey,
                audience,
                actorId,
                request.ExpiresAt,
                subjectEmail,
                passwordHash,
                _tokenProtector.Protect(token),
                SerializeContentProtectionForStorage(audience, contentProtection));
            await _shareLinkRepository.AddAsync(link, ct);
            await EnsureLinkPolicyAsync(target, audience, roleKey, actorId, shareResult, link.Id, ct);
            await _auditService.AddAsync(
                CreateAuditEvent(
                    target,
                    actorId,
                    PermissionAuditActions.ShareLinkCreated,
                    before: null,
                    after: ShareLinkSnapshot(link),
                    shareResult,
                    link),
                ct);
            await _notificationFanoutService.AddShareLinkCreatedAsync(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                link.Id,
                actorId,
                ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return new CreateShareLinkResponse(
                ToDto(link, status: "active"),
                token,
                BuildShareLinkResolveUrl(audience, token));
        }, cancellationToken);
    }

    public Task<LinkManagementDto> UpdateShareLinkAsync(
        Guid shareLinkId,
        UpdateShareLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var link = await _shareLinkRepository.GetForUpdateAsync(shareLinkId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
            var target = new PermissionResourceTarget(
                link.ResourceType,
                link.ResourceId,
                link.WorkspaceId);
            var management = await EnsureCanManageLinkAsync(link, ct);
            await _stepUpService.EnsureSatisfiedAsync(ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);

            var updated = false;
            if (!string.IsNullOrWhiteSpace(request.RoleKey))
            {
                var roleKey = NormalizeLinkRole(request.RoleKey);
                EnsureCanGrant(management.EffectiveRole, roleKey);
                EnsureAudienceRules(target, link.Audience, roleKey, link.SubjectEmail, link.ExpiresAt);
                if (link.RoleKey != roleKey)
                {
                    var before = ShareLinkSnapshot(link);
                    link.UpdateRole(roleKey);
                    await _auditService.AddAsync(
                        CreateAuditEvent(
                            target,
                            actorId,
                            PermissionAuditActions.ShareLinkRoleUpdated,
                            before,
                            ShareLinkSnapshot(link),
                            management,
                            link,
                            request.Reason),
                        ct);
                    updated = true;
                }
            }

            if (request.ExpiresAt.ValueKind != JsonValueKind.Undefined)
            {
                var expiresAt = ReadOptionalExpiry(request.ExpiresAt);
                EnsureExpiry(link.Audience, expiresAt);
                EnsureAudienceRules(target, link.Audience, link.RoleKey, link.SubjectEmail, expiresAt);
                if (link.ExpiresAt != expiresAt)
                {
                    var before = ShareLinkSnapshot(link);
                    link.UpdateExpiry(expiresAt);
                    await _auditService.AddAsync(
                        CreateAuditEvent(
                            target,
                            actorId,
                            PermissionAuditActions.ShareLinkExpiryUpdated,
                            before,
                            ShareLinkSnapshot(link),
                            management,
                            link,
                            request.Reason),
                        ct);
                    updated = true;
                }
            }

            if (updated)
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }

            return await BuildManagementDtoAsync(link, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace permission is insufficient.");
        }, cancellationToken);
    }

    public Task RevokeShareLinkAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var link = await _shareLinkRepository.GetForUpdateAsync(shareLinkId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
            var target = new PermissionResourceTarget(
                link.ResourceType,
                link.ResourceId,
                link.WorkspaceId);
            var shareResult = await EnsureCanManageLinkAsync(link, ct);
            await _stepUpService.EnsureSatisfiedAsync(ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);
            if (link.RevokedAt.HasValue)
            {
                return true;
            }

            var before = ShareLinkSnapshot(link);
            link.Revoke();
            await _auditService.AddAsync(
                CreateAuditEvent(
                    target,
                    actorId,
                    PermissionAuditActions.ShareLinkRevoked,
                    before,
                    ShareLinkSnapshot(link),
                    shareResult,
                    link),
                ct);
            await _notificationFanoutService.AddShareLinkRevokedAsync(
                target.WorkspaceId,
                target.ResourceType,
                target.ResourceId,
                link.Id,
                actorId,
                ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }

    public Task<LinkManagementDto> PauseShareLinkAsync(
        Guid shareLinkId,
        ShareLinkPauseRequest? request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var link = await _shareLinkRepository.GetForUpdateAsync(shareLinkId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
            var target = new PermissionResourceTarget(link.ResourceType, link.ResourceId, link.WorkspaceId);
            var management = await EnsureCanManageLinkAsync(link, ct);
            await _stepUpService.EnsureSatisfiedAsync(ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);
            if (!link.PausedAt.HasValue)
            {
                var before = ShareLinkSnapshot(link);
                link.Pause(actorId, request?.Reason);
                await _auditService.AddAsync(
                    CreateAuditEvent(
                        target,
                        actorId,
                        PermissionAuditActions.ShareLinkPaused,
                        before,
                        ShareLinkSnapshot(link),
                        management,
                        link,
                        request?.Reason),
                    ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            return await BuildManagementDtoAsync(link, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace permission is insufficient.");
        }, cancellationToken);
    }

    public Task<LinkManagementDto> ResumeShareLinkAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var link = await _shareLinkRepository.GetForUpdateAsync(shareLinkId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
            var target = new PermissionResourceTarget(link.ResourceType, link.ResourceId, link.WorkspaceId);
            var management = await EnsureCanManageLinkAsync(link, ct);
            await _stepUpService.EnsureSatisfiedAsync(ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);
            if (link.RevokedAt.HasValue)
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "revoked share links cannot be resumed.");
            }

            if (link.PausedAt.HasValue)
            {
                var before = ShareLinkSnapshot(link);
                link.Resume();
                await _auditService.AddAsync(
                    CreateAuditEvent(
                        target,
                        actorId,
                        PermissionAuditActions.ShareLinkResumed,
                        before,
                        ShareLinkSnapshot(link),
                        management,
                        link),
                    ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            return await BuildManagementDtoAsync(link, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace permission is insufficient.");
        }, cancellationToken);
    }

    public Task<CopyShareLinkResponse> CopyShareLinkAsync(
        Guid shareLinkId,
        ShareLinkCopyEventRequest? request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var link = await _shareLinkRepository.GetForUpdateAsync(shareLinkId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
            var target = new PermissionResourceTarget(link.ResourceType, link.ResourceId, link.WorkspaceId);
            var management = await EnsureCanManageLinkAsync(link, ct);
            if (link.RevokedAt.HasValue)
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "Revoked share links cannot be copied.");
            }

            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);
            var reissued = false;
            string token;
            if (!string.IsNullOrWhiteSpace(link.TokenCiphertext))
            {
                token = _tokenProtector.Unprotect(link.TokenCiphertext);
            }
            else
            {
                token = _tokenService.GenerateToken();
                link.ReplaceToken(_tokenService.HashToken(token), _tokenProtector.Protect(token));
                reissued = true;
            }

            await _auditService.AddAsync(
                CreateAuditEvent(
                    target,
                    actorId,
                    PermissionAuditActions.ShareLinkCopyRequested,
                    before: null,
                    after: new
                    {
                        link.Id,
                        link.WorkspaceId,
                        link.ResourceType,
                        link.ResourceId,
                        link.Audience,
                        link.RoleKey,
                        copiedValueType = NormalizeCopiedValueType(request?.CopiedValueType),
                        reissued
                    },
                    management,
                    link,
                    request?.Reason),
                ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return new CopyShareLinkResponse(link.Id.ToString(), BuildShareLinkResolveUrl(link.Audience, token), reissued);
        }, cancellationToken);
    }

    public Task RecordCopyEventAsync(
        Guid shareLinkId,
        ShareLinkCopyEventRequest? request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var link = await _shareLinkRepository.GetByIdAsync(shareLinkId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
            var target = new PermissionResourceTarget(link.ResourceType, link.ResourceId, link.WorkspaceId);
            var management = await EnsureCanManageLinkAsync(link, ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);
            await _auditService.AddAsync(
                CreateAuditEvent(
                    target,
                    actorId,
                    PermissionAuditActions.ShareLinkCopyRequested,
                    before: null,
                    after: new
                    {
                        link.Id,
                        link.WorkspaceId,
                        link.ResourceType,
                        link.ResourceId,
                        link.Audience,
                        link.RoleKey,
                        copiedValueType = NormalizeCopiedValueType(request?.CopiedValueType)
                    },
                    management,
                    link,
                    request?.Reason),
                ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }

    public async Task<ResolveShareLinkResponse> ResolveShareLinkAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Share link token is required.");
        }

        var link = await _shareLinkRepository.GetByTokenHashAsync(
            _tokenService.HashToken(token.Trim()),
            cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        var now = DateTimeOffset.UtcNow;
        if (!link.IsActive(now))
        {
            await _shareLinkAccessAuditService.RecordResolveAsync(
                token,
                ShareLinkAccessResults.Fail,
                GetInactiveFailureCategory(link, now),
                cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }

        if (link.Audience == ShareLinkAudiences.Public)
        {
            await _shareLinkAccessAuditService.RecordResolveAsync(
                token,
                ShareLinkAccessResults.Fail,
                "audience_mismatch",
                cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }

        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            await _shareLinkAccessAuditService.RecordResolveAsync(
                token,
                ShareLinkAccessResults.Fail,
                "audience_mismatch",
                cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Authentication is required.");
        }

        var policy = await _permissionRepository.GetPolicyAsync(
            link.WorkspaceId,
            link.ResourceType,
            link.ResourceId,
            cancellationToken);
        if (!await CanResolveLinkAsync(link, policy, _currentUser.UserId.Value, cancellationToken))
        {
            await _shareLinkAccessAuditService.RecordResolveAsync(
                token,
                ShareLinkAccessResults.Fail,
                policy is null || !PolicyMatchesAudience(link, policy) ? "policy_mismatch" : "forbidden",
                cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace access is denied.");
        }

        await _shareLinkAccessAuditService.RecordResolveAsync(
            token,
            ShareLinkAccessResults.Success,
            null,
            cancellationToken);
        return new ResolveShareLinkResponse(
            link.WorkspaceId.ToString(),
            link.ResourceType,
            link.ResourceId.ToString(),
            link.RoleKey,
            link.Audience,
            link.SubjectEmail,
            link.ExpiresAt);
    }

    public async Task<ResolvePublicShareLinkResponse> ResolvePublicShareLinkAsync(
        string token,
        string? passwordProof = null,
        CancellationToken cancellationToken = default)
    {
        var link = await GetActivePublicLinkAsync(token, expectedResourceType: null, passwordProof, cancellationToken);
        await _shareLinkAccessAuditService.RecordPublicAccessAsync(
            token,
            ShareLinkAccessEventTypes.Resolve,
            ShareLinkAccessResults.Success,
            null,
            cancellationToken);
        return ToPublicResolveDto(link);
    }

    public async Task<PublicShareDocumentResponse> GetPublicShareDocumentAsync(
        string token,
        string? passwordProof = null,
        CancellationToken cancellationToken = default)
    {
        var link = await GetActivePublicLinkAsync(token, ResourceTypes.Document, passwordProof, cancellationToken);
        var document = await _publicCollectionQueryService.GetDocumentAsync(link, link.ResourceId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");

        await _shareLinkAccessAuditService.RecordPublicAccessAsync(
            token,
            ShareLinkAccessEventTypes.Access,
            ShareLinkAccessResults.Success,
            null,
            cancellationToken,
            JsonSerializer.Serialize(new
            {
                category = "document_view",
                scopeType = link.ResourceType,
                resourceType = ResourceTypes.Document,
                documentId = link.ResourceId.ToString(),
                resourceId = link.ResourceId.ToString()
            }, JsonOptions));
        return new PublicShareDocumentResponse(
            ToPublicResolveDto(link),
            SanitizePublicDocumentDto(document));
    }

    public async Task<PublicShareTreeResponse> GetPublicShareTreeAsync(
        string token,
        string? passwordProof = null,
        CancellationToken cancellationToken = default)
    {
        var link = await GetActivePublicLinkAsync(token, expectedResourceType: null, passwordProof, cancellationToken);
        var tree = await _publicCollectionQueryService.GetTreeAsync(
            link,
            cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");

        await _shareLinkAccessAuditService.RecordPublicAccessAsync(
            token,
            ShareLinkAccessEventTypes.Access,
            ShareLinkAccessResults.Success,
            null,
            cancellationToken,
            JsonSerializer.Serialize(new
            {
                category = "tree_view",
                scopeType = link.ResourceType,
                resourceType = link.ResourceType,
                resourceId = link.ResourceId.ToString()
            }, JsonOptions));
        return tree with
        {
            ContentProtection = GetContentProtectionDto(link)
        };
    }

    public async Task<PublicShareDocumentResponse> GetPublicShareDocumentAsync(
        string token,
        Guid documentId,
        string? passwordProof = null,
        CancellationToken cancellationToken = default)
    {
        var link = await GetActivePublicLinkAsync(token, expectedResourceType: null, passwordProof, cancellationToken);
        var document = await _publicCollectionQueryService.GetDocumentAsync(link, documentId, cancellationToken);
        if (document is null)
        {
            await _shareLinkAccessAuditService.RecordPublicAccessAsync(
                token,
                ShareLinkAccessEventTypes.Access,
                ShareLinkAccessResults.Fail,
                "scope_denied",
                cancellationToken,
                JsonSerializer.Serialize(new
                {
                    category = "scope_denied",
                    scopeType = link.ResourceType,
                    resourceType = ResourceTypes.Document,
                    documentId = documentId.ToString(),
                    resourceId = documentId.ToString(),
                    denialReason = "scope_denied"
                }, JsonOptions));
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }

        await _shareLinkAccessAuditService.RecordPublicAccessAsync(
            token,
            ShareLinkAccessEventTypes.Access,
            ShareLinkAccessResults.Success,
            null,
            cancellationToken,
            JsonSerializer.Serialize(new
            {
                category = "document_view",
                scopeType = link.ResourceType,
                resourceType = ResourceTypes.Document,
                documentId = documentId.ToString(),
                resourceId = documentId.ToString()
            }, JsonOptions));
        return new PublicShareDocumentResponse(
            ToPublicResolveDto(link),
            SanitizePublicDocumentDto(document));
    }

    public async Task<PublicShareCollectionResponse> GetPublicShareCollectionAsync(
        string token,
        string? passwordProof = null,
        CancellationToken cancellationToken = default)
    {
        var link = await GetActivePublicLinkAsync(token, ResourceTypes.Collection, passwordProof, cancellationToken);
        var collection = await _publicCollectionQueryService.GetCollectionAsync(
            link.WorkspaceId,
            link.ResourceId,
            cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");

        await _shareLinkAccessAuditService.RecordPublicAccessAsync(
            token,
            ShareLinkAccessEventTypes.Access,
            ShareLinkAccessResults.Success,
            null,
            cancellationToken,
            JsonSerializer.Serialize(new
            {
                category = "tree_view",
                scopeType = link.ResourceType,
                resourceType = link.ResourceType,
                resourceId = link.ResourceId.ToString()
            }, JsonOptions));
        return new PublicShareCollectionResponse(
            ToPublicResolveDto(link),
            collection);
    }

    private async Task EnsureLinkPolicyAsync(
        PermissionResourceTarget target,
        string audience,
        string defaultLinkRole,
        Guid actorId,
        EffectivePermissionResult shareResult,
        Guid shareLinkId,
        CancellationToken cancellationToken)
    {
        var linkMode = audience switch
        {
            ShareLinkAudiences.Workspace => LinkModes.Internal,
            ShareLinkAudiences.External => LinkModes.External,
            ShareLinkAudiences.Public => LinkModes.Public,
            _ => throw new ApplicationErrorException(ErrorCodes.ValidationError, "share link audience is invalid.")
        };
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
                linkMode,
                defaultLinkRole,
                actorId);
            await _permissionRepository.AddPolicyAsync(policy, cancellationToken);
            await _auditService.AddAsync(
                CreatePolicyAuditEvent(target, actorId, before: null, after: PolicySnapshot(policy), shareResult, shareLinkId),
                cancellationToken);
            return;
        }

        if (policy.LinkMode != linkMode || policy.DefaultLinkRole != defaultLinkRole)
        {
            var before = PolicySnapshot(policy);
            policy.SetLinkMode(linkMode, defaultLinkRole);
            await _auditService.AddAsync(
                CreatePolicyAuditEvent(target, actorId, before, PolicySnapshot(policy), shareResult, shareLinkId),
                cancellationToken);
        }
    }

    private async Task<IReadOnlyList<LinkManagementDto>> BuildManagementDtosAsync(
        IReadOnlyList<ShareLink> links,
        CancellationToken cancellationToken)
    {
        var authorized = new List<(ShareLink Link, EffectivePermissionResult Result, bool CanManage)>();
        foreach (var link in links)
        {
            var access = await GetLinkManagementAccessAsync(link, cancellationToken);
            if (access is not null)
            {
                authorized.Add((link, access.Value.Result, access.Value.CanManage));
            }
        }

        if (authorized.Count == 0)
        {
            return Array.Empty<LinkManagementDto>();
        }

        var resourceGroups = authorized
            .GroupBy(item => item.Link.WorkspaceId)
            .ToArray();
        var titles = new Dictionary<(Guid WorkspaceId, string ResourceType, Guid ResourceId), string>();
        foreach (var group in resourceGroups)
        {
            var summaries = await _resourceDisplayResolver.GetDisplaySummariesAsync(
                group.Key,
                group
                    .Select(item => new PermissionResourceReference(item.Link.ResourceType, item.Link.ResourceId))
                    .Distinct()
                    .ToArray(),
                cancellationToken);
            foreach (var summary in summaries)
            {
                titles[(group.Key, summary.ResourceType, summary.ResourceId)] = summary.Title;
            }
        }

        var creatorIds = authorized
            .Select(item => item.Link.CreatedBy)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        var creators = creatorIds.Length == 0
            ? new Dictionary<Guid, PermissionUserIdentity>()
            : await _userRepository.GetIdentitiesAsync(creatorIds, cancellationToken);
        var analytics = await _shareLinkAccessRepository.GetSummaryRowsAsync(
            authorized[0].Link.WorkspaceId,
            authorized.Select(item => item.Link.Id).Distinct().ToArray(),
            DateTimeOffset.UtcNow.AddDays(-7),
            cancellationToken);

        var rows = new List<LinkManagementDto>(authorized.Count);
        foreach (var item in authorized)
        {
            var policy = await _permissionRepository.GetPolicyAsync(
                item.Link.WorkspaceId,
                item.Link.ResourceType,
                item.Link.ResourceId,
                cancellationToken);
            titles.TryGetValue((item.Link.WorkspaceId, item.Link.ResourceType, item.Link.ResourceId), out var title);
            var createdByDisplayName = item.Link.CreatedBy.HasValue &&
                creators.TryGetValue(item.Link.CreatedBy.Value, out var creator)
                    ? creator.DisplayName
                    : null;
            analytics.TryGetValue(item.Link.Id, out var summary);
            rows.Add(ToManagementDto(item.Link, item.Result, item.CanManage, title, createdByDisplayName, policy, summary));
        }

        return rows;
    }

    private async Task<LinkManagementDto?> BuildManagementDtoAsync(
        ShareLink link,
        CancellationToken cancellationToken)
    {
        var access = await GetLinkManagementAccessAsync(link, cancellationToken);
        if (access is null)
        {
            return null;
        }

        var summaries = await _resourceDisplayResolver.GetDisplaySummariesAsync(
            link.WorkspaceId,
            [new PermissionResourceReference(link.ResourceType, link.ResourceId)],
            cancellationToken);
        var creator = link.CreatedBy.HasValue
            ? (await _userRepository.GetIdentitiesAsync([link.CreatedBy.Value], cancellationToken))
                .GetValueOrDefault(link.CreatedBy.Value)
            : null;
        var policy = await _permissionRepository.GetPolicyAsync(
            link.WorkspaceId,
            link.ResourceType,
            link.ResourceId,
            cancellationToken);
        var stats = await _shareLinkAccessRepository.GetStatsAsync(link.WorkspaceId, link.Id, cancellationToken);
        var analytics = await _shareLinkAccessRepository.GetSummaryRowsAsync(
            link.WorkspaceId,
            [link.Id],
            DateTimeOffset.UtcNow.AddDays(-7),
            cancellationToken);
        analytics.TryGetValue(link.Id, out var summary);
        summary ??= stats is null
            ? null
            : new ShareLinkAccessSummaryRow(link.Id, stats.LastAccessedAt, stats.AccessCount, stats.UniqueVisitorCount, 0, 0);

        return ToManagementDto(
            link,
            access.Value.Result,
            access.Value.CanManage,
            summaries.FirstOrDefault()?.Title,
            creator?.DisplayName,
            policy,
            summary);
    }

    private async Task<EffectivePermissionResult> EnsureCanManageLinkAsync(
        ShareLink link,
        CancellationToken cancellationToken)
    {
        var access = await GetLinkManagementAccessAsync(link, cancellationToken);
        if (access is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace permission is insufficient.");
        }

        return access.Value.Result;
    }

    private async Task<LinkManagementAccess?> GetLinkManagementAccessAsync(
        ShareLink link,
        CancellationToken cancellationToken)
    {
        var userId = await _scopedAccessService.GetRequiredUserIdAsync(cancellationToken);
        var manageAction = link.ResourceType == ResourceTypes.Document
            ? PermissionActions.DocumentManagePermissions
            : link.ResourceType == ResourceTypes.Collection
                ? PermissionActions.CollectionManagePermissions
                : PermissionActions.WorkspaceManagePermissions;
        var shareAction = link.ResourceType == ResourceTypes.Document
            ? PermissionActions.DocumentShare
            : link.ResourceType == ResourceTypes.Collection
                ? PermissionActions.CollectionShare
                : PermissionActions.WorkspaceManagePermissions;
        var manageResult = link.ResourceType == ResourceTypes.Document
            ? await _effectivePermissionService.AuthorizeDocumentAsync(link.ResourceId, userId, manageAction, cancellationToken)
            : link.ResourceType == ResourceTypes.Collection
                ? await _effectivePermissionService.AuthorizeCollectionAsync(link.ResourceId, userId, manageAction, cancellationToken)
                : await _effectivePermissionService.AuthorizeWorkspaceAsync(link.WorkspaceId, userId, manageAction, cancellationToken);
        if (manageResult.Allowed)
        {
            return new LinkManagementAccess(manageResult, CanManage: true);
        }

        var shareResult = link.ResourceType == ResourceTypes.Document
            ? await _effectivePermissionService.AuthorizeDocumentAsync(link.ResourceId, userId, shareAction, cancellationToken)
            : link.ResourceType == ResourceTypes.Collection
                ? await _effectivePermissionService.AuthorizeCollectionAsync(link.ResourceId, userId, shareAction, cancellationToken)
                : await _effectivePermissionService.AuthorizeWorkspaceAsync(link.WorkspaceId, userId, shareAction, cancellationToken);
        if (shareResult.Allowed && link.CreatedBy == userId)
        {
            return new LinkManagementAccess(shareResult, CanManage: false);
        }

        return null;
    }

    private async Task<EffectivePermissionResult> EnsureCanShareAsync(
        PermissionResourceTarget target,
        CancellationToken cancellationToken)
    {
        if (target.ResourceType == ResourceTypes.Document)
        {
            return await _scopedAccessService.EnsureCanAccessDocumentAsync(
                target.ResourceId,
                PermissionActions.DocumentShare,
                cancellationToken);
        }

        if (target.ResourceType == ResourceTypes.Collection)
        {
            return await _scopedAccessService.EnsureCanAccessCollectionAsync(
                target.ResourceId,
                PermissionActions.CollectionShare,
                cancellationToken);
        }

        var userId = await _scopedAccessService.GetRequiredUserIdAsync(cancellationToken);
        var result = await _effectivePermissionService.AuthorizeWorkspaceAsync(
            target.WorkspaceId,
            userId,
            PermissionActions.WorkspaceManagePermissions,
            cancellationToken);
        if (!result.Allowed)
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace permission is insufficient.");
        }

        return result;
    }

    private async Task<PermissionResourceTarget> ResolveTargetAsync(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(resourceType))
        {
            var requestedResourceType = resourceType.Trim().ToLowerInvariant();
            if (requestedResourceType == "workspace")
            {
                throw PublicSharePolicyBlocked("PUBLIC_SHARE_WORKSPACE_UNSUPPORTED", "Workspace public sharing is not supported.");
            }
        }

        var normalizedResourceType = PermissionResourceNormalizer.NormalizeShareableResourceType(resourceType);
        if (normalizedResourceType == ResourceTypes.Library)
        {
            var library = await _resourceResolver.GetLibraryPermissionResourceAsync(resourceId, cancellationToken)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Library was not found.");
            return new PermissionResourceTarget(ResourceTypes.Library, library.LibraryId, library.WorkspaceId);
        }

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

    private void EnsureCanGrant(string? actorRole, string roleKey)
    {
        if (_permissionCatalog.GetRank(actorRole) < _permissionCatalog.GetRank(roleKey))
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Cannot create a link role above the actor's effective role.");
        }
    }

    private static string NormalizeLinkRole(string roleKey)
    {
        var normalized = roleKey.Trim().ToLowerInvariant();
        return normalized is PermissionRole.Viewer or PermissionRole.Commenter
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "roleKey must be viewer or commenter.");
    }

    private string NormalizeAudience(string? audience)
    {
        if (string.IsNullOrWhiteSpace(audience))
        {
            return ShareLinkAudiences.Workspace;
        }

        var normalized = audience.Trim().ToLowerInvariant();
        return normalized switch
        {
            ShareLinkAudiences.Workspace => normalized,
            ShareLinkAudiences.External => normalized,
            ShareLinkAudiences.Public => normalized,
            _ => throw new ApplicationErrorException(ErrorCodes.ValidationError, "share link audience is invalid.")
        };
    }

    private static string? NormalizeSubjectEmail(string audience, string? email)
    {
        if (audience != ShareLinkAudiences.External)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "subjectEmail is only supported for external links.");
            }

            return null;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "subjectEmail is required for external links.");
        }

        return email.Trim().ToLowerInvariant();
    }

    private static void EnsureExpiry(string audience, DateTimeOffset? expiresAt)
    {
        if (expiresAt.HasValue && expiresAt.Value <= DateTimeOffset.UtcNow)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "expiresAt must be in the future.");
        }
    }

    private void EnsureAudienceRules(
        PermissionResourceTarget target,
        string audience,
        string roleKey,
        string? subjectEmail,
        DateTimeOffset? expiresAt)
    {
        if (audience != ShareLinkAudiences.Public)
        {
            return;
        }

        if (_publicShareOptions.ViewerOnly && roleKey != PermissionRole.Viewer)
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_ROLE_NOT_ALLOWED", "Public share links can only use viewer role.");
        }

        if (!string.IsNullOrWhiteSpace(subjectEmail))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "subjectEmail is not supported for public links.");
        }

        if ((_publicShareOptions.RequireExpiry || !_publicShareOptions.AllowNoExpiry) && !expiresAt.HasValue)
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_EXPIRY_REQUIRED", "Public share links require expiresAt.");
        }

        if (expiresAt.HasValue)
        {
            var now = DateTimeOffset.UtcNow;
            var maxExpiresAt = now.Add(_publicShareOptions.EffectiveMaxExpiry(target.ResourceType));
            if (expiresAt.Value > maxExpiresAt)
            {
                throw PublicSharePolicyBlocked("PUBLIC_SHARE_EXPIRY_TOO_LONG", "Public share link expiry exceeds the configured maximum.");
            }
        }
    }

    private void EnsurePublicShareCreationPolicy(
        PermissionResourceTarget target,
        string audience,
        string roleKey,
        DateTimeOffset? expiresAt,
        string? password,
        ShareLinkContentProtectionDto contentProtection)
    {
        if (audience != ShareLinkAudiences.Public)
        {
            return;
        }

        if (!_publicShareOptions.Enabled)
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_DISABLED", "Public share links are not enabled.");
        }

        if (target.ResourceType == ResourceTypes.Document && !_publicShareOptions.AllowDocumentScope)
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_SCOPE_DISABLED", "Document public share links are disabled by policy.");
        }

        if (target.ResourceType == ResourceTypes.Collection && !_publicShareOptions.AllowCollectionScope)
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_SCOPE_DISABLED", "Collection public share links are disabled by policy.");
        }

        if (target.ResourceType == ResourceTypes.Library && !_publicShareOptions.AllowLibraryScope)
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_SCOPE_DISABLED", "Library public share links are disabled by policy.");
        }

        if (target.ResourceType is not ResourceTypes.Document and not ResourceTypes.Collection and not ResourceTypes.Library)
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_SCOPE_DISABLED", "Public share scope is not supported.");
        }

        if (_publicShareOptions.ViewerOnly && roleKey != PermissionRole.Viewer)
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_ROLE_NOT_ALLOWED", "Public share links can only use viewer role.");
        }

        if ((_publicShareOptions.RequireExpiry || !_publicShareOptions.AllowNoExpiry) && !expiresAt.HasValue)
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_EXPIRY_REQUIRED", "Public share links require expiresAt.");
        }

        if (expiresAt.HasValue && expiresAt.Value > DateTimeOffset.UtcNow.Add(_publicShareOptions.EffectiveMaxExpiry(target.ResourceType)))
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_EXPIRY_TOO_LONG", "Public share link expiry exceeds the configured maximum.");
        }

        if (_publicShareOptions.RequirePassword && string.IsNullOrWhiteSpace(password))
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_PASSWORD_REQUIRED", "Public share links require a password.");
        }

        if (_publicShareOptions.RequirePasswordForCollection &&
            target.ResourceType == ResourceTypes.Collection &&
            string.IsNullOrWhiteSpace(password))
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_PASSWORD_REQUIRED", "Collection public share links require a password.");
        }

        if (_publicShareOptions.RequirePasswordForLibrary &&
            target.ResourceType == ResourceTypes.Library &&
            string.IsNullOrWhiteSpace(password))
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_PASSWORD_REQUIRED", "Library public share links require a password.");
        }

        if (_publicShareOptions.DisallowDownloadForPublicLinks && !contentProtection.DisableDownload)
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_DOWNLOAD_DISABLED_REQUIRED", "Public share links must disable download.");
        }

        if (_publicShareOptions.RequireWatermarkForCollection &&
            target.ResourceType == ResourceTypes.Collection &&
            !contentProtection.WatermarkEnabled)
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_WATERMARK_REQUIRED", "Collection public share links require a watermark.");
        }

        if (_publicShareOptions.RequireWatermarkForLibrary &&
            target.ResourceType == ResourceTypes.Library &&
            !contentProtection.WatermarkEnabled)
        {
            throw PublicSharePolicyBlocked("PUBLIC_SHARE_WATERMARK_REQUIRED", "Library public share links require a watermark.");
        }
    }

    private ShareLinkContentProtectionDto NormalizeContentProtection(
        PermissionResourceTarget target,
        string audience,
        ShareLinkContentProtectionDto? requested)
    {
        if (audience != ShareLinkAudiences.Public)
        {
            return DefaultContentProtection();
        }

        var disableDownload = requested?.DisableDownload ?? _publicShareOptions.DefaultDisableDownload;
        var disablePrint = requested?.DisablePrint ?? _publicShareOptions.DefaultDisablePrint;
        var disableCopy = requested?.DisableCopy ?? _publicShareOptions.DefaultDisableCopy;
        var watermarkEnabled = requested?.WatermarkEnabled ?? _publicShareOptions.DefaultWatermarkEnabled;
        var watermarkText = SanitizeWatermarkText(requested?.WatermarkText, target);

        return new ShareLinkContentProtectionDto(
            disableDownload,
            disablePrint,
            disableCopy,
            watermarkEnabled,
            watermarkText);
    }

    private ShareLinkContentProtectionDto GetContentProtectionDto(ShareLink link)
    {
        if (link.Audience != ShareLinkAudiences.Public)
        {
            return DefaultContentProtection();
        }

        if (string.IsNullOrWhiteSpace(link.ContentProtectionJson))
        {
            return DefaultContentProtection();
        }

        try
        {
            var stored = JsonSerializer.Deserialize<StoredShareLinkContentProtection>(
                link.ContentProtectionJson,
                JsonOptions);
            if (stored is null)
            {
                return DefaultContentProtection();
            }

            return new ShareLinkContentProtectionDto(
                stored.DisableDownload ?? _publicShareOptions.DefaultDisableDownload,
                stored.DisablePrint ?? _publicShareOptions.DefaultDisablePrint,
                stored.DisableCopy ?? _publicShareOptions.DefaultDisableCopy,
                stored.WatermarkEnabled ?? _publicShareOptions.DefaultWatermarkEnabled,
                SanitizeWatermarkText(stored.WatermarkText, new PermissionResourceTarget(link.ResourceType, link.ResourceId, link.WorkspaceId)));
        }
        catch (JsonException)
        {
            return DefaultContentProtection();
        }
    }

    private ShareLinkContentProtectionDto DefaultContentProtection()
    {
        return new ShareLinkContentProtectionDto(
            _publicShareOptions.DefaultDisableDownload,
            _publicShareOptions.DefaultDisablePrint,
            _publicShareOptions.DefaultDisableCopy,
            _publicShareOptions.DefaultWatermarkEnabled,
            "Public link");
    }

    private static string? SerializeContentProtectionForStorage(
        string audience,
        ShareLinkContentProtectionDto contentProtection)
    {
        if (audience != ShareLinkAudiences.Public)
        {
            return null;
        }

        return JsonSerializer.Serialize(new StoredShareLinkContentProtection(
            contentProtection.DisableDownload,
            contentProtection.DisablePrint,
            contentProtection.DisableCopy,
            contentProtection.WatermarkEnabled,
            contentProtection.WatermarkText), JsonOptions);
    }

    private static string SanitizeWatermarkText(string? value, PermissionResourceTarget target)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "Public link" : value.Trim();
        if (text.Length > 80)
        {
            text = text[..80].Trim();
        }

        var lower = text.ToLowerInvariant();
        if (lower.Contains("token", StringComparison.Ordinal) ||
            lower.Contains("password", StringComparison.Ordinal) ||
            lower.Contains("hash", StringComparison.Ordinal) ||
            lower.Contains("proof", StringComparison.Ordinal) ||
            lower.Contains(target.ResourceId.ToString("D"), StringComparison.OrdinalIgnoreCase) ||
            lower.Contains(target.WorkspaceId.ToString("D"), StringComparison.OrdinalIgnoreCase))
        {
            return target.ResourceType == ResourceTypes.Collection ? "Shared folder" : "Public link";
        }

        return text;
    }

    private static ApplicationErrorException PublicSharePolicyBlocked(string reason, string message)
    {
        return new ApplicationErrorException(
            ErrorCodes.ValidationError,
            message,
            new
            {
                policyBlocked = true,
                reason
            });
    }

    private string? NormalizeAndHashPassword(string audience, string? password)
    {
        if (password is null)
        {
            return null;
        }

        if (audience != ShareLinkAudiences.Public)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "password is only supported for public links.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "password must not be empty.");
        }

        var normalized = password.Trim();
        if (normalized.Length > 256)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "password is too long.");
        }

        return _passwordHashService.HashPassword(CreateShareLinkPasswordUser(), normalized);
    }

    private ShareLinkDto ToDto(ShareLink link, ResourceAccessPolicy? policy = null, string? status = null)
    {
        return new ShareLinkDto(
            link.Id.ToString(),
            link.WorkspaceId.ToString(),
            link.ResourceType,
            link.ResourceId.ToString(),
            link.RoleKey,
            link.Audience,
            link.SubjectEmail,
            link.CreatedBy?.ToString(),
            link.CreatedAt,
            link.ExpiresAt,
            link.RevokedAt,
            link.PausedAt,
            link.HasPassword,
            GetContentProtectionDto(link),
            status ?? GetStatus(link, policy, DateTimeOffset.UtcNow));
    }

    private LinkManagementDto ToManagementDto(
        ShareLink link,
        EffectivePermissionResult managementResult,
        bool canManage,
        string? resourceTitle,
        string? createdByDisplayName,
        ResourceAccessPolicy? policy,
        ShareLinkAccessSummaryRow? analytics)
    {
        var status = GetStatus(link, policy, DateTimeOffset.UtcNow);
        return new LinkManagementDto(
            link.Id.ToString(),
            link.WorkspaceId.ToString(),
            link.ResourceType,
            link.ResourceId.ToString(),
            resourceTitle,
            link.RoleKey,
            link.Audience,
            link.SubjectEmail,
            link.CreatedBy?.ToString(),
            createdByDisplayName,
            link.CreatedAt,
            link.ExpiresAt,
            link.RevokedAt,
            link.PausedAt,
            link.PausedBy?.ToString(),
            link.PauseReason,
            link.HasPassword,
            GetContentProtectionDto(link),
            status,
            policy?.LinkMode,
            GetPolicyState(link, policy),
            analytics?.LastAccessedAt,
            analytics?.AccessCount ?? 0,
            analytics?.UniqueVisitorCount ?? 0,
            analytics?.RecentFailCount ?? 0,
            analytics?.ExternalOrPublicAccessCount ?? 0,
            canManage,
            canManage || link.CreatedBy.HasValue,
            canManage || link.CreatedBy.HasValue,
            canManage || link.CreatedBy.HasValue);
    }

    private static string GetStatus(ShareLink link, ResourceAccessPolicy? policy, DateTimeOffset now)
    {
        if (link.RevokedAt.HasValue)
        {
            return "revoked";
        }

        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value <= now)
        {
            return "expired";
        }

        if (link.PausedAt.HasValue)
        {
            return "paused";
        }

        return PolicyMatchesAudience(link, policy) ? "active" : "policy_paused";
    }

    private static string GetPolicyState(ShareLink link, ResourceAccessPolicy? policy)
    {
        if (policy is null)
        {
            return "missing";
        }

        if (PolicyMatchesAudience(link, policy))
        {
            return "matching";
        }

        return policy.LinkMode == LinkModes.Disabled ? "disabled" : "mismatch";
    }

    private static bool PolicyMatchesAudience(ShareLink link, ResourceAccessPolicy? policy)
    {
        var expectedLinkMode = link.Audience switch
        {
            ShareLinkAudiences.Workspace => LinkModes.Internal,
            ShareLinkAudiences.External => LinkModes.External,
            ShareLinkAudiences.Public => LinkModes.Public,
            _ => null
        };

        return expectedLinkMode is not null && policy?.LinkMode == expectedLinkMode;
    }

    private static string GetInactiveFailureCategory(ShareLink link, DateTimeOffset now)
    {
        if (link.RevokedAt.HasValue)
        {
            return "revoked";
        }

        if (link.PausedAt.HasValue)
        {
            return "paused";
        }

        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value <= now)
        {
            return "expired";
        }

        return "forbidden";
    }

    private static bool MatchesSearch(LinkManagementDto row, string? q)
    {
        if (q is null)
        {
            return true;
        }

        return Contains(row.Id, q) ||
            Contains(row.ResourceTitle, q) ||
            Contains(row.ResourceId, q) ||
            Contains(row.SubjectEmail, q) ||
            Contains(row.CreatedByDisplayName, q);
    }

    private static bool Contains(string? value, string q)
    {
        return value?.Contains(q, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string? NormalizeAudienceFilter(string? audience)
    {
        if (string.IsNullOrWhiteSpace(audience))
        {
            return null;
        }

        var normalized = audience.Trim().ToLowerInvariant();
        return ShareLinkAudiences.IsSupported(normalized)
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "share link audience is invalid.");
    }

    private static string? NormalizeRoleFilter(string? roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            return null;
        }

        return NormalizeLinkRole(roleKey);
    }

    private static string? NormalizeStatusFilter(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized is "active" or "expired" or "revoked" or "paused" or "policy_paused"
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "share link status is invalid.");
    }

    private static DateTimeOffset? ReadOptionalExpiry(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.String &&
            element.TryGetDateTimeOffset(out var expiresAt))
        {
            return expiresAt;
        }

        throw new ApplicationErrorException(ErrorCodes.ValidationError, "expiresAt is invalid.");
    }

    private static string NormalizeCopiedValueType(string? copiedValueType)
    {
        if (string.IsNullOrWhiteSpace(copiedValueType))
        {
            return "share_url";
        }

        var normalized = copiedValueType.Trim().ToLowerInvariant();
        return normalized is "created_url" or "link_id" or "metadata" or "share_url"
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "copiedValueType is invalid.");
    }

    private static string BuildShareLinkResolveUrl(string audience, string token)
    {
        return audience == ShareLinkAudiences.Public
            ? $"/api/v1/public/share-links/{token}/resolve"
            : $"/api/v1/share-links/{token}/resolve";
    }

    private async Task<ShareLink> GetActivePublicLinkAsync(
        string token,
        string? expectedResourceType,
        string? passwordProof,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Share link token is required.");
        }

        if (!_publicShareOptions.Enabled)
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }

        var link = await _shareLinkRepository.GetByTokenHashAsync(
            _tokenService.HashToken(token.Trim()),
            cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (link is null)
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }

        if (!link.IsActive(now))
        {
            await RecordPublicFailureAsync(token, GetInactiveFailureCategory(link, now), cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }

        if (link.Audience != ShareLinkAudiences.Public)
        {
            await RecordPublicFailureAsync(token, "audience_mismatch", cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }

        if ((expectedResourceType is not null && link.ResourceType != expectedResourceType) ||
            (link.ResourceType != ResourceTypes.Document &&
                link.ResourceType != ResourceTypes.Collection &&
                link.ResourceType != ResourceTypes.Library))
        {
            await RecordPublicFailureAsync(token, "resource_mismatch", cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }

        if (link.RoleKey != PermissionRole.Viewer ||
            !string.IsNullOrWhiteSpace(link.SubjectEmail) ||
            !link.ExpiresAt.HasValue)
        {
            await RecordPublicFailureAsync(token, "policy_mismatch", cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }

        if (!PublicPasswordProofIsValid(link, passwordProof))
        {
            await RecordPublicFailureAsync(token, "password_failed", cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }

        var policy = await _permissionRepository.GetPolicyAsync(
            link.WorkspaceId,
            link.ResourceType,
            link.ResourceId,
            cancellationToken);
        if (policy?.LinkMode != LinkModes.Public)
        {
            await RecordPublicFailureAsync(token, "policy_mismatch", cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }

        return link;
    }

    private bool PublicPasswordProofIsValid(ShareLink link, string? passwordProof)
    {
        if (!link.HasPassword)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(passwordProof) &&
            _passwordHashService.VerifyPassword(
                CreateShareLinkPasswordUser(),
                link.PasswordHash!,
                passwordProof.Trim());
    }

    private Task RecordPublicFailureAsync(string token, string failureCategory, CancellationToken cancellationToken)
    {
        return _shareLinkAccessAuditService.RecordPublicAccessAsync(
            token,
            ShareLinkAccessEventTypes.Resolve,
            ShareLinkAccessResults.Fail,
            failureCategory,
            cancellationToken);
    }

    private ResolvePublicShareLinkResponse ToPublicResolveDto(ShareLink link)
    {
        return new ResolvePublicShareLinkResponse(
            link.WorkspaceId.ToString(),
            link.ResourceType,
            link.ResourceId.ToString(),
            link.RoleKey,
            link.Audience,
            link.ExpiresAt!.Value,
            link.HasPassword,
            GetContentProtectionDto(link));
    }

    private static PublicShareDocumentDto ToPublicDocumentDto(KnowledgeDocumentDto document)
    {
        return new PublicShareDocumentDto(
            document.Id,
            document.Title,
            document.Status,
            document.UpdatedAt,
            document.Tags,
            SanitizePublicContent(document.Content),
            document.Revision);
    }

    private static PublicShareDocumentDto SanitizePublicDocumentDto(PublicShareDocumentDto document)
    {
        return document with
        {
            Content = SanitizePublicContent(document.Content)
        };
    }

    private static JsonElement SanitizePublicContent(JsonElement content)
    {
        var node = JsonNode.Parse(content.GetRawText());
        var sanitized = SanitizePublicContentNode(node) ?? new JsonObject
        {
            ["type"] = "doc",
            ["content"] = new JsonArray()
        };
        using var document = JsonDocument.Parse(sanitized.ToJsonString(JsonOptions));
        return document.RootElement.Clone();
    }

    private static JsonNode? SanitizePublicContentNode(JsonNode? node)
    {
        if (node is not JsonObject obj)
        {
            return node?.DeepClone();
        }

        var type = TryGetString(obj["type"]);
        if ((type == "image" || type == "imageBlock") && HasInternalPublicFileReference(obj))
        {
            return new JsonObject
            {
                ["type"] = "paragraph",
                ["content"] = new JsonArray(new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = "File preview is not available in this public view."
                })
            };
        }

        var copy = new JsonObject();
        foreach (var property in obj)
        {
            if (property.Key == "attrs" && property.Value is JsonObject attrs)
            {
                copy[property.Key] = SanitizePublicAttrs(attrs);
                continue;
            }

            if (property.Key == "content" && property.Value is JsonArray content)
            {
                var sanitizedContent = new JsonArray();
                foreach (var child in content)
                {
                    var sanitizedChild = SanitizePublicContentNode(child);
                    if (sanitizedChild is not null)
                    {
                        sanitizedContent.Add(sanitizedChild);
                    }
                }

                copy[property.Key] = sanitizedContent;
                continue;
            }

            copy[property.Key] = property.Value?.DeepClone();
        }

        return copy;
    }

    private static JsonObject SanitizePublicAttrs(JsonObject attrs)
    {
        var copy = new JsonObject();
        foreach (var property in attrs)
        {
            if (property.Key == "fileId")
            {
                continue;
            }

            if (property.Key is "src" or "href" &&
                TryGetString(property.Value) is { } value &&
                value.Contains("/api/v1/files/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            copy[property.Key] = property.Value?.DeepClone();
        }

        return copy;
    }

    private static bool HasInternalPublicFileReference(JsonObject obj)
    {
        if (obj["attrs"] is not JsonObject attrs)
        {
            return false;
        }

        return TryGetString(attrs["fileId"]) is { Length: > 0 } ||
            (TryGetString(attrs["src"]) is { } src &&
                src.Contains("/api/v1/files/", StringComparison.OrdinalIgnoreCase));
    }

    private static string? TryGetString(JsonNode? node)
    {
        try
        {
            return node?.GetValue<string>();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static User CreateShareLinkPasswordUser()
    {
        return new User("share-link-password", id: Guid.Empty);
    }

    private async Task<bool> CanResolveLinkAsync(
        ShareLink link,
        ResourceAccessPolicy? policy,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (link.Audience == ShareLinkAudiences.Workspace)
        {
            return policy?.LinkMode == LinkModes.Internal &&
                await _groupRepository.UserIsWorkspaceMemberAsync(link.WorkspaceId, userId, cancellationToken);
        }

        if (link.Audience == ShareLinkAudiences.External)
        {
            if (policy?.LinkMode != LinkModes.External || string.IsNullOrWhiteSpace(link.SubjectEmail))
            {
                return false;
            }

            var user = await _userRepository.GetIdentityAsync(userId, cancellationToken);
            return string.Equals(user?.Email, link.SubjectEmail, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static PermissionAuditEvent CreateAuditEvent(
        PermissionResourceTarget target,
        Guid actorId,
        string action,
        object? before,
        object? after,
        EffectivePermissionResult shareResult,
        ShareLink link,
        string? reason = null)
    {
        return new PermissionAuditEvent(
            target.WorkspaceId,
            actorId,
            action,
            target.ResourceType,
            target.ResourceId,
            SubjectTypes.ShareLink,
            link.Id,
            before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            JsonSerializer.Serialize(new
            {
                shareResult.EffectiveRole,
                shareResult.Source,
                shareLinkId = link.Id,
                link.RoleKey,
                link.Audience,
                link.SubjectEmail,
                link.HasPassword,
                contentProtectionJson = link.Audience == ShareLinkAudiences.Public ? link.ContentProtectionJson : null,
                link.ExpiresAt,
                reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()
            }, JsonOptions));
    }

    private static PermissionAuditEvent CreatePolicyAuditEvent(
        PermissionResourceTarget target,
        Guid actorId,
        object? before,
        object after,
        EffectivePermissionResult shareResult,
        Guid shareLinkId)
    {
        return new PermissionAuditEvent(
            target.WorkspaceId,
            actorId,
            PermissionAuditActions.PolicyUpdated,
            target.ResourceType,
            target.ResourceId,
            SubjectTypes.ShareLink,
            shareLinkId,
            before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            JsonSerializer.Serialize(after, JsonOptions),
            JsonSerializer.Serialize(new
            {
                shareResult.EffectiveRole,
                shareResult.Source,
                shareLinkId
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

    private static object ShareLinkSnapshot(ShareLink link)
    {
        return new
        {
            link.Id,
            link.WorkspaceId,
            link.ResourceType,
            link.ResourceId,
            link.RoleKey,
            link.Audience,
            link.SubjectEmail,
            link.HasPassword,
            contentProtectionJson = link.Audience == ShareLinkAudiences.Public ? link.ContentProtectionJson : null,
            link.CreatedBy,
            link.CreatedAt,
            link.ExpiresAt,
            link.RevokedAt,
            link.PausedAt,
            link.PausedBy,
            link.PauseReason
        };
    }

    private sealed record PermissionResourceTarget(
        string ResourceType,
        Guid ResourceId,
        Guid WorkspaceId);

    private readonly record struct LinkManagementAccess(
        EffectivePermissionResult Result,
        bool CanManage);

    private sealed record StoredShareLinkContentProtection(
        bool? DisableDownload,
        bool? DisablePrint,
        bool? DisableCopy,
        bool? WatermarkEnabled,
        string? WatermarkText);
}
