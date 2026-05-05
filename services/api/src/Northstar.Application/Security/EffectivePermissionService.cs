using Northstar.Domain.Security;
using Northstar.Domain.Workspaces;

namespace Northstar.Application.Security;

public sealed class EffectivePermissionService : IEffectivePermissionService
{
    public const string WorkspaceSource = "workspace";
    public const string CollectionSource = "collection";
    public const string CollectionGroupSource = "collection_group";
    public const string CollectionShareLinkSource = "collection_share_link";
    public const string CollectionEmailInviteSource = "collection_email_invite";
    public const string DocumentSource = "document";
    public const string DocumentGroupSource = "document_group";
    public const string DocumentShareLinkSource = "document_share_link";
    public const string DocumentEmailInviteSource = "document_email_invite";
    public const string OwnerEscapeSource = "owner_escape";
    public const string AdminEscapeSource = "admin_escape";
    public const string NoMembershipReason = "workspace_membership_required";
    public const string PermissionDeniedReason = "permission_denied";
    public const string ResourceNotFoundReason = "resource_not_found";

    private readonly IWorkspaceMembershipQueryService _membershipQueryService;
    private readonly IPermissionCatalog _permissionCatalog;
    private readonly IResourceWorkspaceResolver _resourceWorkspaceResolver;
    private readonly IResourcePermissionRepository _resourcePermissionRepository;
    private readonly IWorkspaceGroupRepository _workspaceGroupRepository;
    private readonly IShareLinkRepository _shareLinkRepository;
    private readonly IShareLinkTokenService _shareLinkTokenService;
    private readonly IEmailInviteRepository _emailInviteRepository;
    private readonly IPermissionUserRepository _userRepository;

    public EffectivePermissionService(
        IWorkspaceMembershipQueryService membershipQueryService,
        IPermissionCatalog permissionCatalog,
        IResourceWorkspaceResolver resourceWorkspaceResolver,
        IResourcePermissionRepository resourcePermissionRepository,
        IWorkspaceGroupRepository workspaceGroupRepository,
        IShareLinkRepository shareLinkRepository,
        IShareLinkTokenService shareLinkTokenService,
        IEmailInviteRepository emailInviteRepository,
        IPermissionUserRepository userRepository)
    {
        _membershipQueryService = membershipQueryService;
        _permissionCatalog = permissionCatalog;
        _resourceWorkspaceResolver = resourceWorkspaceResolver;
        _resourcePermissionRepository = resourcePermissionRepository;
        _workspaceGroupRepository = workspaceGroupRepository;
        _shareLinkRepository = shareLinkRepository;
        _shareLinkTokenService = shareLinkTokenService;
        _emailInviteRepository = emailInviteRepository;
        _userRepository = userRepository;
    }

    public async Task<EffectivePermissionResult> AuthorizeWorkspaceAsync(
        Guid workspaceId,
        Guid userId,
        string actionKey,
        CancellationToken cancellationToken = default)
    {
        var role = await _membershipQueryService.GetActiveRoleAsync(workspaceId, userId, cancellationToken);
        if (role is null)
        {
            return new EffectivePermissionResult(false, null, WorkspaceSource, NoMembershipReason);
        }

        if (!WorkspaceMemberRole.IsValid(role))
        {
            return new EffectivePermissionResult(false, role, WorkspaceSource, PermissionDeniedReason);
        }

        if (!_permissionCatalog.RoleHasPermission(role, actionKey))
        {
            return new EffectivePermissionResult(false, role, WorkspaceSource, PermissionDeniedReason);
        }

        return new EffectivePermissionResult(true, role, WorkspaceSource, null);
    }

    public async Task<EffectivePermissionResult> AuthorizeCollectionAsync(
        Guid collectionId,
        Guid userId,
        string actionKey,
        CancellationToken cancellationToken = default,
        string? shareToken = null)
    {
        var resource = await _resourceWorkspaceResolver.GetCollectionPermissionResourceAsync(
            collectionId,
            cancellationToken);
        if (resource is null)
        {
            return new EffectivePermissionResult(false, null, CollectionSource, ResourceNotFoundReason);
        }

        return await AuthorizeScopedResourceAsync(
            resource.WorkspaceId,
            ResourceTypes.Collection,
            resource.CollectionId,
            userId,
            actionKey,
            CollectionSource,
            shareToken,
            cancellationToken);
    }

    public async Task<EffectivePermissionResult> AuthorizeDocumentAsync(
        Guid documentId,
        Guid userId,
        string actionKey,
        CancellationToken cancellationToken = default,
        string? shareToken = null)
    {
        var resource = await _resourceWorkspaceResolver.GetDocumentPermissionResourceAsync(
            documentId,
            cancellationToken);
        if (resource is null)
        {
            return new EffectivePermissionResult(false, null, DocumentSource, ResourceNotFoundReason);
        }

        return await AuthorizeDocumentResourceAsync(
            resource.WorkspaceId,
            resource.DocumentId,
            resource.CollectionId,
            userId,
            actionKey,
            shareToken,
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, EffectivePermissionResult>> AuthorizeDocumentsAsync(
        IReadOnlyCollection<Guid> documentIds,
        Guid userId,
        string actionKey,
        CancellationToken cancellationToken = default)
    {
        var distinctDocumentIds = documentIds.Distinct().ToArray();
        var results = distinctDocumentIds.ToDictionary(
            documentId => documentId,
            _ => new EffectivePermissionResult(false, null, DocumentSource, ResourceNotFoundReason));
        if (distinctDocumentIds.Length == 0)
        {
            return results;
        }

        var resources = await _resourceWorkspaceResolver.GetDocumentPermissionResourcesAsync(
            distinctDocumentIds,
            cancellationToken);

        foreach (var workspaceResources in resources.GroupBy(resource => resource.WorkspaceId))
        {
            var workspaceId = workspaceResources.Key;
            var workspaceRole = await _membershipQueryService.GetActiveRoleAsync(
                workspaceId,
                userId,
                cancellationToken);
            if (workspaceRole is null)
            {
                foreach (var resource in workspaceResources)
                {
                    results[resource.DocumentId] = new EffectivePermissionResult(false, null, DocumentSource, NoMembershipReason);
                }

                continue;
            }

            if (!WorkspaceMemberRole.IsValid(workspaceRole))
            {
                foreach (var resource in workspaceResources)
                {
                    results[resource.DocumentId] = new EffectivePermissionResult(false, workspaceRole, DocumentSource, PermissionDeniedReason);
                }

                continue;
            }

            var workspaceResourceList = workspaceResources.ToArray();
            var now = DateTimeOffset.UtcNow;
            var groupIds = await _workspaceGroupRepository.GetActiveGroupIdsForUserAsync(
                workspaceId,
                userId,
                now,
                cancellationToken);
            var scopedResults = await AuthorizeDocumentResourcesForWorkspaceAsync(
                workspaceId,
                workspaceRole,
                workspaceResourceList,
                userId,
                actionKey,
                groupIds,
                now,
                cancellationToken);

            foreach (var result in scopedResults)
            {
                results[result.Key] = result.Value;
            }
        }

        return results;
    }

    public async Task<EffectivePermissionResult> AuthorizeDocumentIncludingDeletedAsync(
        Guid documentId,
        Guid userId,
        string actionKey,
        CancellationToken cancellationToken = default,
        string? shareToken = null)
    {
        var resource = await _resourceWorkspaceResolver.GetDocumentPermissionResourceIncludingDeletedAsync(
            documentId,
            cancellationToken);
        if (resource is null)
        {
            return new EffectivePermissionResult(false, null, DocumentSource, ResourceNotFoundReason);
        }

        return await AuthorizeDocumentResourceAsync(
            resource.WorkspaceId,
            resource.DocumentId,
            resource.CollectionId,
            userId,
            actionKey,
            shareToken,
            cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, EffectivePermissionResult>> AuthorizeDocumentResourcesForWorkspaceAsync(
        Guid workspaceId,
        string workspaceRole,
        IReadOnlyList<DocumentPermissionResource> resources,
        Guid userId,
        string actionKey,
        IReadOnlyCollection<Guid> groupIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var documentIds = resources.Select(resource => resource.DocumentId).Distinct().ToArray();
        var collectionIds = resources
            .Select(resource => resource.CollectionId)
            .Where(collectionId => collectionId.HasValue)
            .Select(collectionId => collectionId!.Value)
            .Distinct()
            .ToArray();

        var documentPolicies = (await _resourcePermissionRepository.GetPoliciesForResourcesAsync(
                workspaceId,
                ResourceTypes.Document,
                documentIds,
                cancellationToken))
            .ToDictionary(policy => policy.ResourceId);
        var collectionPolicies = (await _resourcePermissionRepository.GetPoliciesForResourcesAsync(
                workspaceId,
                ResourceTypes.Collection,
                collectionIds,
                cancellationToken))
            .ToDictionary(policy => policy.ResourceId);
        var documentUserGrants = GetLatestGrantByResource(await _resourcePermissionRepository.GetActiveUserGrantsForResourcesAsync(
            workspaceId,
            ResourceTypes.Document,
            documentIds,
            userId,
            now,
            cancellationToken));
        var collectionUserGrants = GetLatestGrantByResource(await _resourcePermissionRepository.GetActiveUserGrantsForResourcesAsync(
            workspaceId,
            ResourceTypes.Collection,
            collectionIds,
            userId,
            now,
            cancellationToken));
        var documentGroupGrants = GroupGrantsByResource(await _resourcePermissionRepository.GetActiveGroupGrantsForResourcesAsync(
            workspaceId,
            ResourceTypes.Document,
            documentIds,
            groupIds,
            now,
            cancellationToken));
        var collectionGroupGrants = GroupGrantsByResource(await _resourcePermissionRepository.GetActiveGroupGrantsForResourcesAsync(
            workspaceId,
            ResourceTypes.Collection,
            collectionIds,
            groupIds,
            now,
            cancellationToken));

        var results = new Dictionary<Guid, EffectivePermissionResult>();
        foreach (var resource in resources)
        {
            documentPolicies.TryGetValue(resource.DocumentId, out var documentPolicy);
            var documentInheritanceMode = documentPolicy?.InheritanceMode ?? InheritanceModes.Inherit;
            var parentCandidate = BuildDocumentParentCandidate(
                workspaceRole,
                resource.CollectionId,
                documentPolicy,
                collectionPolicies,
                collectionUserGrants,
                collectionGroupGrants);
            documentUserGrants.TryGetValue(resource.DocumentId, out var documentGrant);
            documentGroupGrants.TryGetValue(resource.DocumentId, out var groupedDocumentGrants);
            var candidate = SelectHighest(
                parentCandidate,
                ToCandidate(documentGrant?.RoleKey, DocumentSource),
                BuildHighestGrantCandidate(groupedDocumentGrants ?? Array.Empty<ResourceAccessGrant>(), DocumentGroupSource));

            if (candidate.Role is null)
            {
                results[resource.DocumentId] = new EffectivePermissionResult(
                    false,
                    null,
                    DocumentSource,
                    PermissionDeniedReason,
                    documentInheritanceMode);
                continue;
            }

            if (!_permissionCatalog.RoleHasPermission(candidate.Role, actionKey))
            {
                results[resource.DocumentId] = new EffectivePermissionResult(
                    false,
                    candidate.Role,
                    candidate.Source,
                    PermissionDeniedReason,
                    documentInheritanceMode);
                continue;
            }

            results[resource.DocumentId] = new EffectivePermissionResult(
                true,
                candidate.Role,
                candidate.Source,
                null,
                documentInheritanceMode);
        }

        return results;
    }

    private async Task<EffectivePermissionResult> AuthorizeScopedResourceAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid userId,
        string actionKey,
        string directGrantSource,
        string? shareToken,
        CancellationToken cancellationToken)
    {
        var workspaceRole = await _membershipQueryService.GetActiveRoleAsync(
            workspaceId,
            userId,
            cancellationToken);
        if (workspaceRole is not null && !WorkspaceMemberRole.IsValid(workspaceRole))
        {
            return new EffectivePermissionResult(false, workspaceRole, directGrantSource, PermissionDeniedReason);
        }

        var policy = await _resourcePermissionRepository.GetPolicyAsync(
            workspaceId,
            resourceType,
            resourceId,
            cancellationToken);
        var inheritanceMode = policy?.InheritanceMode ?? InheritanceModes.Inherit;
        var now = DateTimeOffset.UtcNow;
        IReadOnlyList<Guid> groupIds = workspaceRole is null
            ? Array.Empty<Guid>()
            : await _workspaceGroupRepository.GetActiveGroupIdsForUserAsync(
                workspaceId,
                userId,
                now,
                cancellationToken);
        var directGrant = workspaceRole is null
            ? null
            : await _resourcePermissionRepository.GetActiveUserGrantAsync(
                workspaceId,
                resourceType,
                resourceId,
                userId,
                now,
                cancellationToken);
        IReadOnlyList<ResourceAccessGrant> groupGrants = workspaceRole is null
            ? Array.Empty<ResourceAccessGrant>()
            : await _resourcePermissionRepository.GetActiveGroupGrantsAsync(
                workspaceId,
                resourceType,
                resourceId,
                groupIds,
                now,
                cancellationToken);
        var shareLinkRole = await GetShareLinkRoleAsync(
            workspaceId,
            resourceType,
            resourceId,
            policy,
            userId,
            workspaceRole,
            shareToken,
            now,
            cancellationToken);
        var inviteRole = await GetAcceptedEmailInviteRoleAsync(
            workspaceId,
            resourceType,
            resourceId,
            policy,
            userId,
            now,
            cancellationToken);

        var parentCandidate = workspaceRole is null
            ? new EffectiveRoleCandidate(null, WorkspaceSource)
            : BuildWorkspaceCandidate(workspaceRole, inheritanceMode);
        var candidate = SelectHighest(
            parentCandidate,
            ToCandidate(directGrant?.RoleKey, directGrantSource),
            BuildHighestGrantCandidate(groupGrants, CollectionGroupSource),
            ToCandidate(shareLinkRole, CollectionShareLinkSource),
            ToCandidate(inviteRole, CollectionEmailInviteSource));
        if (candidate.Role is null)
        {
            return new EffectivePermissionResult(
                false,
                null,
                directGrantSource,
                workspaceRole is null ? NoMembershipReason : PermissionDeniedReason,
                inheritanceMode);
        }

        if (!_permissionCatalog.RoleHasPermission(candidate.Role, actionKey))
        {
            return new EffectivePermissionResult(
                false,
                candidate.Role,
                candidate.Source,
                PermissionDeniedReason,
                inheritanceMode);
        }

        return new EffectivePermissionResult(
            true,
            candidate.Role,
            candidate.Source,
            null,
            inheritanceMode);
    }

    private async Task<EffectivePermissionResult> AuthorizeDocumentResourceAsync(
        Guid workspaceId,
        Guid documentId,
        Guid? collectionId,
        Guid userId,
        string actionKey,
        string? shareToken,
        CancellationToken cancellationToken)
    {
        var workspaceRole = await _membershipQueryService.GetActiveRoleAsync(
            workspaceId,
            userId,
            cancellationToken);
        if (workspaceRole is not null && !WorkspaceMemberRole.IsValid(workspaceRole))
        {
            return new EffectivePermissionResult(false, workspaceRole, DocumentSource, PermissionDeniedReason);
        }

        var now = DateTimeOffset.UtcNow;
        var documentPolicy = await _resourcePermissionRepository.GetPolicyAsync(
            workspaceId,
            ResourceTypes.Document,
            documentId,
            cancellationToken);
        var documentInheritanceMode = documentPolicy?.InheritanceMode ?? InheritanceModes.Inherit;
        IReadOnlyList<Guid> groupIds = workspaceRole is null
            ? Array.Empty<Guid>()
            : await _workspaceGroupRepository.GetActiveGroupIdsForUserAsync(
                workspaceId,
                userId,
                now,
                cancellationToken);
        var documentGrant = workspaceRole is null
            ? null
            : await _resourcePermissionRepository.GetActiveUserGrantAsync(
                workspaceId,
                ResourceTypes.Document,
                documentId,
                userId,
                now,
                cancellationToken);
        IReadOnlyList<ResourceAccessGrant> documentGroupGrants = workspaceRole is null
            ? Array.Empty<ResourceAccessGrant>()
            : await _resourcePermissionRepository.GetActiveGroupGrantsAsync(
                workspaceId,
                ResourceTypes.Document,
                documentId,
                groupIds,
                now,
                cancellationToken);
        var documentShareLinkRole = await GetShareLinkRoleAsync(
            workspaceId,
            ResourceTypes.Document,
            documentId,
            documentPolicy,
            userId,
            workspaceRole,
            shareToken,
            now,
            cancellationToken);
        var documentInviteRole = await GetAcceptedEmailInviteRoleAsync(
            workspaceId,
            ResourceTypes.Document,
            documentId,
            documentPolicy,
            userId,
            now,
            cancellationToken);

        var parentCandidate = await BuildDocumentParentCandidateAsync(
            workspaceId,
            workspaceRole,
            collectionId,
            userId,
            groupIds,
            now,
            documentPolicy,
            shareToken,
            cancellationToken);
        var candidate = SelectHighest(
            parentCandidate,
            ToCandidate(documentGrant?.RoleKey, DocumentSource),
            BuildHighestGrantCandidate(documentGroupGrants, DocumentGroupSource),
            ToCandidate(documentShareLinkRole, DocumentShareLinkSource),
            ToCandidate(documentInviteRole, DocumentEmailInviteSource));

        if (candidate.Role is null)
        {
            return new EffectivePermissionResult(
                false,
                null,
                DocumentSource,
                workspaceRole is null ? NoMembershipReason : PermissionDeniedReason,
                documentInheritanceMode);
        }

        if (!_permissionCatalog.RoleHasPermission(candidate.Role, actionKey))
        {
            return new EffectivePermissionResult(
                false,
                candidate.Role,
                candidate.Source,
                PermissionDeniedReason,
                documentInheritanceMode);
        }

        return new EffectivePermissionResult(
            true,
            candidate.Role,
            candidate.Source,
            null,
            documentInheritanceMode);
    }

    private async Task<EffectiveRoleCandidate> BuildDocumentParentCandidateAsync(
        Guid workspaceId,
        string? workspaceRole,
        Guid? collectionId,
        Guid userId,
        IReadOnlyCollection<Guid> groupIds,
        DateTimeOffset now,
        ResourceAccessPolicy? documentPolicy,
        string? shareToken,
        CancellationToken cancellationToken)
    {
        if (documentPolicy?.InheritanceMode == InheritanceModes.Restricted)
        {
            return workspaceRole is null
                ? new EffectiveRoleCandidate(null, WorkspaceSource)
                : BuildEscapeCandidate(workspaceRole);
        }

        var workspaceCandidate = workspaceRole is null
            ? new EffectiveRoleCandidate(null, WorkspaceSource)
            : BuildWorkspaceCandidate(workspaceRole, InheritanceModes.Inherit);
        if (!collectionId.HasValue)
        {
            return workspaceCandidate;
        }

        var collectionPolicy = await _resourcePermissionRepository.GetPolicyAsync(
            workspaceId,
            ResourceTypes.Collection,
            collectionId.Value,
            cancellationToken);
        var collectionInheritanceMode = collectionPolicy?.InheritanceMode ?? InheritanceModes.Inherit;
        var collectionGrant = workspaceRole is null
            ? null
            : await _resourcePermissionRepository.GetActiveUserGrantAsync(
                workspaceId,
                ResourceTypes.Collection,
                collectionId.Value,
                userId,
                now,
                cancellationToken);
        IReadOnlyList<ResourceAccessGrant> collectionGroupGrants = workspaceRole is null
            ? Array.Empty<ResourceAccessGrant>()
            : await _resourcePermissionRepository.GetActiveGroupGrantsAsync(
                workspaceId,
                ResourceTypes.Collection,
                collectionId.Value,
                groupIds,
                now,
                cancellationToken);
        var collectionShareLinkRole = await GetShareLinkRoleAsync(
            workspaceId,
            ResourceTypes.Collection,
            collectionId.Value,
            collectionPolicy,
            userId,
            workspaceRole,
            shareToken,
            now,
            cancellationToken);
        var collectionInviteRole = await GetAcceptedEmailInviteRoleAsync(
            workspaceId,
            ResourceTypes.Collection,
            collectionId.Value,
            collectionPolicy,
            userId,
            now,
            cancellationToken);

        var collectionParentCandidate = collectionInheritanceMode == InheritanceModes.Restricted && workspaceRole is not null
            ? BuildEscapeCandidate(workspaceRole)
            : workspaceCandidate;

        return SelectHighest(
            collectionParentCandidate,
            ToCandidate(collectionGrant?.RoleKey, CollectionSource),
            BuildHighestGrantCandidate(collectionGroupGrants, CollectionGroupSource),
            ToCandidate(collectionShareLinkRole, CollectionShareLinkSource),
            ToCandidate(collectionInviteRole, CollectionEmailInviteSource));
    }

    private EffectiveRoleCandidate BuildDocumentParentCandidate(
        string workspaceRole,
        Guid? collectionId,
        ResourceAccessPolicy? documentPolicy,
        IReadOnlyDictionary<Guid, ResourceAccessPolicy> collectionPolicies,
        IReadOnlyDictionary<Guid, ResourceAccessGrant> collectionUserGrants,
        IReadOnlyDictionary<Guid, IReadOnlyList<ResourceAccessGrant>> collectionGroupGrants)
    {
        if (documentPolicy?.InheritanceMode == InheritanceModes.Restricted)
        {
            return BuildEscapeCandidate(workspaceRole);
        }

        var workspaceCandidate = BuildWorkspaceCandidate(workspaceRole, InheritanceModes.Inherit);
        if (!collectionId.HasValue)
        {
            return workspaceCandidate;
        }

        collectionPolicies.TryGetValue(collectionId.Value, out var collectionPolicy);
        var collectionInheritanceMode = collectionPolicy?.InheritanceMode ?? InheritanceModes.Inherit;
        collectionUserGrants.TryGetValue(collectionId.Value, out var collectionGrant);
        collectionGroupGrants.TryGetValue(collectionId.Value, out var groupedCollectionGrants);
        var collectionParentCandidate = collectionInheritanceMode == InheritanceModes.Restricted
            ? BuildEscapeCandidate(workspaceRole)
            : workspaceCandidate;

        return SelectHighest(
            collectionParentCandidate,
            ToCandidate(collectionGrant?.RoleKey, CollectionSource),
            BuildHighestGrantCandidate(groupedCollectionGrants ?? Array.Empty<ResourceAccessGrant>(), CollectionGroupSource));
    }

    private async Task<string?> GetShareLinkRoleAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        ResourceAccessPolicy? policy,
        Guid userId,
        string? workspaceRole,
        string? shareToken,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(shareToken))
        {
            return null;
        }

        var tokenHash = _shareLinkTokenService.HashToken(shareToken.Trim());
        var link = await _shareLinkRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (link is null ||
            !link.IsActive(now) ||
            link.WorkspaceId != workspaceId ||
            link.ResourceType != resourceType ||
            link.ResourceId != resourceId)
        {
            return null;
        }

        if (link.Audience == ShareLinkAudiences.Workspace)
        {
            return workspaceRole is not null && policy?.LinkMode == LinkModes.Internal
                ? link.RoleKey
                : null;
        }

        if (link.Audience == ShareLinkAudiences.External)
        {
            if (policy?.LinkMode != LinkModes.External || string.IsNullOrWhiteSpace(link.SubjectEmail))
            {
                return null;
            }

            var user = await _userRepository.GetIdentityAsync(userId, cancellationToken);
            return string.Equals(user?.Email, link.SubjectEmail, StringComparison.OrdinalIgnoreCase)
                ? link.RoleKey
                : null;
        }

        return null;
    }

    private async Task<string?> GetAcceptedEmailInviteRoleAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        ResourceAccessPolicy? policy,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (policy?.LinkMode != LinkModes.External)
        {
            return null;
        }

        var user = await _userRepository.GetIdentityAsync(userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(user?.Email))
        {
            return null;
        }

        return await _emailInviteRepository.GetAcceptedRoleForEmailAsync(
            workspaceId,
            resourceType,
            resourceId,
            user.Email,
            now,
            cancellationToken);
    }

    private EffectiveRoleCandidate BuildWorkspaceCandidate(string workspaceRole, string inheritanceMode)
    {
        if (inheritanceMode == InheritanceModes.Inherit)
        {
            return new EffectiveRoleCandidate(workspaceRole, WorkspaceSource);
        }

        return BuildEscapeCandidate(workspaceRole);
    }

    private static EffectiveRoleCandidate BuildEscapeCandidate(string workspaceRole)
    {
        return workspaceRole switch
        {
            PermissionRole.Owner => new EffectiveRoleCandidate(workspaceRole, OwnerEscapeSource),
            PermissionRole.Admin => new EffectiveRoleCandidate(workspaceRole, AdminEscapeSource),
            _ => new EffectiveRoleCandidate(null, WorkspaceSource)
        };
    }

    private static EffectiveRoleCandidate ToCandidate(string? role, string source)
    {
        return new EffectiveRoleCandidate(role, source);
    }

    private EffectiveRoleCandidate BuildHighestGrantCandidate(
        IReadOnlyList<ResourceAccessGrant> grants,
        string source)
    {
        var candidate = new EffectiveRoleCandidate(null, source);
        foreach (var grant in grants)
        {
            candidate = SelectHighest(candidate, ToCandidate(grant.RoleKey, source));
        }

        return candidate;
    }

    private static IReadOnlyDictionary<Guid, ResourceAccessGrant> GetLatestGrantByResource(
        IReadOnlyList<ResourceAccessGrant> grants)
    {
        return grants
            .GroupBy(grant => grant.ResourceId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(grant => grant.GrantedAt).First());
    }

    private static IReadOnlyDictionary<Guid, IReadOnlyList<ResourceAccessGrant>> GroupGrantsByResource(
        IReadOnlyList<ResourceAccessGrant> grants)
    {
        return grants
            .GroupBy(grant => grant.ResourceId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ResourceAccessGrant>)group.ToArray());
    }

    private EffectiveRoleCandidate SelectHighest(params EffectiveRoleCandidate[] candidates)
    {
        var winner = candidates[0];
        foreach (var candidate in candidates.Skip(1))
        {
            var winnerRank = _permissionCatalog.GetRank(winner.Role);
            var candidateRank = _permissionCatalog.GetRank(candidate.Role);
            if (candidateRank >= winnerRank && candidate.Role is not null)
            {
                winner = candidate;
            }
        }

        return winner;
    }

    private sealed record EffectiveRoleCandidate(string? Role, string Source);
}
