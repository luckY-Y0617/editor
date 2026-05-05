using System.Text.Json;
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
    private readonly IResourceWorkspaceResolver _resourceResolver;
    private readonly IResourcePermissionRepository _permissionRepository;
    private readonly IScopedResourceAccessService _scopedAccessService;
    private readonly IWorkspaceGroupRepository _groupRepository;
    private readonly IPermissionUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionCatalog _permissionCatalog;
    private readonly IPermissionAuditService _auditService;
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
        IResourceWorkspaceResolver resourceResolver,
        IResourcePermissionRepository permissionRepository,
        IScopedResourceAccessService scopedAccessService,
        IWorkspaceGroupRepository groupRepository,
        IPermissionUserRepository userRepository,
        ICurrentUser currentUser,
        IPermissionCatalog permissionCatalog,
        IPermissionAuditService auditService,
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
        _resourceResolver = resourceResolver;
        _permissionRepository = permissionRepository;
        _scopedAccessService = scopedAccessService;
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _permissionCatalog = permissionCatalog;
        _auditService = auditService;
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

        return new ShareLinksResponse(links.Select(ToDto).ToArray());
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
                passwordHash);
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
                ToDto(link),
                token,
                audience == ShareLinkAudiences.Public
                    ? $"/api/v1/public/share-links/{token}/resolve"
                    : $"/api/v1/share-links/{token}/resolve");
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
            var shareResult = await EnsureCanShareAsync(target, ct);
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
        if (!link.IsActive(DateTimeOffset.UtcNow))
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }

        if (link.Audience == ShareLinkAudiences.Public)
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }

        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Authentication is required.");
        }

        var policy = await _permissionRepository.GetPolicyAsync(
            link.WorkspaceId,
            link.ResourceType,
            link.ResourceId,
            cancellationToken);
        if (!await CanResolveLinkAsync(link, policy, _currentUser.UserId.Value, cancellationToken))
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace access is denied.");
        }

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
        return ToPublicResolveDto(link);
    }

    public async Task<PublicShareDocumentResponse> GetPublicShareDocumentAsync(
        string token,
        string? passwordProof = null,
        CancellationToken cancellationToken = default)
    {
        var link = await GetActivePublicLinkAsync(token, ResourceTypes.Document, passwordProof, cancellationToken);
        var document = await _knowledgeQueryService.GetDocumentAsync(link.ResourceId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");

        return new PublicShareDocumentResponse(
            ToPublicResolveDto(link),
            ToPublicDocumentDto(document));
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
            ShareLinkAudiences.Public when _publicShareOptions.Enabled => normalized,
            ShareLinkAudiences.Public => throw new ApplicationErrorException(ErrorCodes.ValidationError, "Public share links are not enabled."),
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
        if (audience == ShareLinkAudiences.Public && !expiresAt.HasValue)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Public share links require expiresAt.");
        }

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

        if (roleKey != PermissionRole.Viewer)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Public share links can only use viewer role.");
        }

        if (!string.IsNullOrWhiteSpace(subjectEmail))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "subjectEmail is not supported for public links.");
        }

        if (!expiresAt.HasValue)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Public share links require expiresAt.");
        }

        var now = DateTimeOffset.UtcNow;
        var maxExpiresAt = now.Add(_publicShareOptions.EffectiveMaxExpiry());
        if (!expiresAt.HasValue || expiresAt.Value > maxExpiresAt)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Public share link expiry exceeds the configured maximum.");
        }
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

    private static ShareLinkDto ToDto(ShareLink link)
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
            link.HasPassword);
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
        if (link is null ||
            !link.IsActive(now) ||
            link.Audience != ShareLinkAudiences.Public ||
            (expectedResourceType is not null && link.ResourceType != expectedResourceType) ||
            (link.ResourceType != ResourceTypes.Document && link.ResourceType != ResourceTypes.Collection) ||
            link.RoleKey != PermissionRole.Viewer ||
            !string.IsNullOrWhiteSpace(link.SubjectEmail) ||
            !link.ExpiresAt.HasValue)
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }

        EnsurePublicPasswordProof(link, passwordProof);

        var policy = await _permissionRepository.GetPolicyAsync(
            link.WorkspaceId,
            link.ResourceType,
            link.ResourceId,
            cancellationToken);
        if (policy?.LinkMode != LinkModes.Public)
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }

        return link;
    }

    private void EnsurePublicPasswordProof(ShareLink link, string? passwordProof)
    {
        if (!link.HasPassword)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(passwordProof) ||
            !_passwordHashService.VerifyPassword(
                CreateShareLinkPasswordUser(),
                link.PasswordHash!,
                passwordProof.Trim()))
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Share link was not found.");
        }
    }

    private static ResolvePublicShareLinkResponse ToPublicResolveDto(ShareLink link)
    {
        return new ResolvePublicShareLinkResponse(
            link.WorkspaceId.ToString(),
            link.ResourceType,
            link.ResourceId.ToString(),
            link.RoleKey,
            link.Audience,
            link.ExpiresAt!.Value,
            link.HasPassword);
    }

    private static PublicShareDocumentDto ToPublicDocumentDto(KnowledgeDocumentDto document)
    {
        return new PublicShareDocumentDto(
            document.Id,
            document.Title,
            document.Status,
            document.UpdatedAt,
            document.Tags,
            document.Content,
            document.Revision);
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
        ShareLink link)
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
                link.ExpiresAt
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
            link.CreatedBy,
            link.CreatedAt,
            link.ExpiresAt,
            link.RevokedAt
        };
    }

    private sealed record PermissionResourceTarget(
        string ResourceType,
        Guid ResourceId,
        Guid WorkspaceId);
}
