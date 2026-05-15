using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class PermissionNotificationFanoutService : IPermissionNotificationFanoutService
{
    private readonly IAccessRequestRepository _accessRequestRepository;
    private readonly IResourcePermissionRepository _permissionRepository;
    private readonly IWorkspaceGroupRepository _groupRepository;
    private readonly IPermissionNotificationPreferenceRepository _preferenceRepository;
    private readonly IPermissionNotificationService _notificationService;
    private readonly IPermissionCatalog _permissionCatalog;

    public PermissionNotificationFanoutService(
        IAccessRequestRepository accessRequestRepository,
        IResourcePermissionRepository permissionRepository,
        IWorkspaceGroupRepository groupRepository,
        IPermissionNotificationPreferenceRepository preferenceRepository,
        IPermissionNotificationService notificationService,
        IPermissionCatalog permissionCatalog)
    {
        _accessRequestRepository = accessRequestRepository;
        _permissionRepository = permissionRepository;
        _groupRepository = groupRepository;
        _preferenceRepository = preferenceRepository;
        _notificationService = notificationService;
        _permissionCatalog = permissionCatalog;
    }

    public Task AddAccessRequestCreatedAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid accessRequestId,
        Guid actorId,
        string requestedRole,
        CancellationToken cancellationToken = default)
    {
        return AddResourceManagerNotificationsAsync(
            workspaceId,
            resourceType,
            resourceId,
            actorId,
            PermissionNotificationTypes.AccessRequestCreated,
            "Access request pending",
            $"A workspace member requested {requestedRole} access.",
            RecipientPermissionMode.ManageOnly,
            accessRequestId: accessRequestId,
            permissionGrantId: null,
            extraRecipientIds: [],
            dedupeSeed: null,
            cancellationToken: cancellationToken);
    }

    public async Task AddGrantNotificationAsync(
        ResourceAccessGrant grant,
        Guid actorId,
        string type,
        string title,
        string body,
        CancellationToken cancellationToken = default)
    {
        var recipients = await GetGrantRecipientIdsAsync(grant, actorId, cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        await _notificationService.AddRangeAsync(
            recipients.Select(recipientId => new PermissionNotification(
                grant.WorkspaceId,
                recipientId,
                type,
                title,
                body,
                actorId,
                grant.ResourceType,
                grant.ResourceId,
                permissionGrantId: grant.Id,
                actionUrl: "#permissions")),
            cancellationToken);
    }

    public Task AddShareLinkCreatedAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid shareLinkId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        return AddResourceManagerNotificationsAsync(
            workspaceId,
            resourceType,
            resourceId,
            actorId,
            PermissionNotificationTypes.ShareLinkCreated,
            "Share link created",
            "A share link was created for this resource.",
            RecipientPermissionMode.ShareOrManage,
            accessRequestId: null,
            permissionGrantId: null,
            extraRecipientIds: [actorId],
            dedupeSeed: $"share-link:{shareLinkId}:created",
            includeActor: true,
            cancellationToken: cancellationToken);
    }

    public Task AddShareLinkRevokedAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid shareLinkId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        return AddResourceManagerNotificationsAsync(
            workspaceId,
            resourceType,
            resourceId,
            actorId,
            PermissionNotificationTypes.ShareLinkRevoked,
            "Share link revoked",
            "A share link was revoked for this resource.",
            RecipientPermissionMode.ShareOrManage,
            accessRequestId: null,
            permissionGrantId: null,
            extraRecipientIds: [actorId],
            dedupeSeed: $"share-link:{shareLinkId}:revoked",
            includeActor: true,
            cancellationToken: cancellationToken);
    }

    public Task AddEmailInviteCreatedAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid inviteId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        return AddResourceManagerNotificationsAsync(
            workspaceId,
            resourceType,
            resourceId,
            actorId,
            PermissionNotificationTypes.EmailInviteCreated,
            "Email invite created",
            "An email invite was created for this resource.",
            RecipientPermissionMode.ShareOrManage,
            accessRequestId: null,
            permissionGrantId: null,
            extraRecipientIds: [],
            dedupeSeed: $"email-invite:{inviteId}:created",
            cancellationToken: cancellationToken);
    }

    public Task AddEmailInviteAcceptedAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid inviteId,
        Guid actorId,
        Guid? invitedBy,
        CancellationToken cancellationToken = default)
    {
        return AddResourceManagerNotificationsAsync(
            workspaceId,
            resourceType,
            resourceId,
            actorId,
            PermissionNotificationTypes.EmailInviteAccepted,
            "Email invite accepted",
            "An email invite was accepted for this resource.",
            RecipientPermissionMode.ShareOrManage,
            accessRequestId: null,
            permissionGrantId: null,
            extraRecipientIds: ExtraRecipients(invitedBy),
            dedupeSeed: $"email-invite:{inviteId}:accepted",
            cancellationToken: cancellationToken);
    }

    public Task AddEmailInviteRevokedAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid inviteId,
        Guid actorId,
        Guid? invitedBy,
        Guid? acceptedBy,
        CancellationToken cancellationToken = default)
    {
        return AddResourceManagerNotificationsAsync(
            workspaceId,
            resourceType,
            resourceId,
            actorId,
            PermissionNotificationTypes.EmailInviteRevoked,
            "Email invite revoked",
            "An email invite was revoked for this resource.",
            RecipientPermissionMode.ShareOrManage,
            accessRequestId: null,
            permissionGrantId: null,
            extraRecipientIds: ExtraRecipients(invitedBy, acceptedBy),
            dedupeSeed: $"email-invite:{inviteId}:revoked",
            cancellationToken: cancellationToken);
    }

    public Task AddEmailInviteDeliveryFailedAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid inviteId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        return AddResourceManagerNotificationsAsync(
            workspaceId,
            resourceType,
            resourceId,
            actorId,
            PermissionNotificationTypes.EmailInviteDeliveryFailed,
            "Email invite delivery failed",
            "Email invite delivery failed for this resource.",
            RecipientPermissionMode.ShareOrManage,
            accessRequestId: null,
            permissionGrantId: null,
            extraRecipientIds: [],
            dedupeSeed: $"email-invite:{inviteId}:delivery-failed",
            cancellationToken: cancellationToken);
    }

    private async Task AddResourceManagerNotificationsAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid actorId,
        string type,
        string title,
        string body,
        RecipientPermissionMode permissionMode,
        Guid? accessRequestId,
        Guid? permissionGrantId,
        IReadOnlyCollection<Guid> extraRecipientIds,
        string? dedupeSeed,
        bool includeActor = false,
        CancellationToken cancellationToken = default)
    {
        var recipients = await GetRecipientIdsAsync(
            workspaceId,
            resourceType,
            resourceId,
            actorId,
            permissionMode,
            extraRecipientIds,
            includeActor,
            cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        await _notificationService.AddRangeAsync(
            recipients.Select(recipientId => new PermissionNotification(
                workspaceId,
                recipientId,
                type,
                title,
                body,
                actorId,
                resourceType,
                resourceId,
                accessRequestId,
                permissionGrantId,
                actionUrl: "#permissions",
                dedupeKey: dedupeSeed is null ? null : $"{dedupeSeed}:{recipientId}")),
            cancellationToken);
    }

    private async Task<IReadOnlyList<Guid>> GetRecipientIdsAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid actorId,
        RecipientPermissionMode permissionMode,
        IReadOnlyCollection<Guid> extraRecipientIds,
        bool includeActor,
        CancellationToken cancellationToken)
    {
        var recipients = new HashSet<Guid>(await _accessRequestRepository.GetWorkspaceManagerUserIdsAsync(
            workspaceId,
            cancellationToken));

        var now = DateTimeOffset.UtcNow;
        var manageAction = resourceType == ResourceTypes.Document
            ? PermissionActions.DocumentManagePermissions
            : PermissionActions.CollectionManagePermissions;
        var shareAction = resourceType == ResourceTypes.Document
            ? PermissionActions.DocumentShare
            : PermissionActions.CollectionShare;
        var directGrants = await _permissionRepository.GetGrantsAsync(
            workspaceId,
            resourceType,
            resourceId,
            cancellationToken);

        foreach (var grant in directGrants)
        {
            if (!grant.IsActive(now) || !RoleMatches(grant.RoleKey, permissionMode, shareAction, manageAction))
            {
                continue;
            }

            if (grant.SubjectType == SubjectTypes.User)
            {
                if (await _groupRepository.UserIsWorkspaceMemberAsync(workspaceId, grant.SubjectId, cancellationToken))
                {
                    recipients.Add(grant.SubjectId);
                }

                continue;
            }

            if (grant.SubjectType == SubjectTypes.Group)
            {
                var groupMemberIds = await _groupRepository.GetActiveGroupMemberUserIdsAsync(
                    workspaceId,
                    grant.SubjectId,
                    now,
                    cancellationToken);
                foreach (var groupMemberId in groupMemberIds)
                {
                    recipients.Add(groupMemberId);
                }
            }
        }

        foreach (var recipientId in extraRecipientIds)
        {
            recipients.Add(recipientId);
        }

        var filtered = new List<Guid>();
        foreach (var recipientId in recipients.Where(recipientId => includeActor || recipientId != actorId))
        {
            if (!await IsMutedAsync(recipientId, workspaceId, resourceType, resourceId, cancellationToken))
            {
                filtered.Add(recipientId);
            }
        }

        return filtered;
    }

    private async Task<IReadOnlyList<Guid>> GetGrantRecipientIdsAsync(
        ResourceAccessGrant grant,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var recipients = new HashSet<Guid>();
        if (grant.SubjectType == SubjectTypes.User)
        {
            if (await _groupRepository.UserIsWorkspaceMemberAsync(grant.WorkspaceId, grant.SubjectId, cancellationToken))
            {
                recipients.Add(grant.SubjectId);
            }
        }
        else if (grant.SubjectType == SubjectTypes.Group)
        {
            var groupMemberIds = await _groupRepository.GetActiveGroupMemberUserIdsAsync(
                grant.WorkspaceId,
                grant.SubjectId,
                now,
                cancellationToken);
            foreach (var groupMemberId in groupMemberIds)
            {
                recipients.Add(groupMemberId);
            }
        }

        var filtered = new List<Guid>();
        foreach (var recipientId in recipients.Where(recipientId => recipientId != actorId))
        {
            if (!await IsMutedAsync(recipientId, grant.WorkspaceId, grant.ResourceType, grant.ResourceId, cancellationToken))
            {
                filtered.Add(recipientId);
            }
        }

        return filtered;
    }

    private async Task<bool> IsMutedAsync(
        Guid recipientId,
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        var preferences = await _preferenceRepository.GetForUserWorkspaceAsync(
            recipientId,
            workspaceId,
            cancellationToken);
        var resourcePreference = preferences.FirstOrDefault(preference =>
            preference.ResourceType == resourceType &&
            preference.ResourceId == resourceId);
        if (resourcePreference is not null)
        {
            return resourcePreference.Muted;
        }

        return preferences.Any(preference =>
            preference.ResourceType is null &&
            preference.ResourceId is null &&
            preference.Muted);
    }

    private static IReadOnlyCollection<Guid> ExtraRecipients(params Guid?[] userIds)
    {
        return userIds
            .Where(userId => userId.HasValue)
            .Select(userId => userId!.Value)
            .ToArray();
    }

    private bool RoleMatches(
        string roleKey,
        RecipientPermissionMode permissionMode,
        string shareAction,
        string manageAction)
    {
        return permissionMode switch
        {
            RecipientPermissionMode.ManageOnly => _permissionCatalog.RoleHasPermission(roleKey, manageAction),
            RecipientPermissionMode.ShareOrManage => _permissionCatalog.RoleHasPermission(roleKey, shareAction) ||
                _permissionCatalog.RoleHasPermission(roleKey, manageAction),
            _ => false
        };
    }

    private enum RecipientPermissionMode
    {
        ManageOnly,
        ShareOrManage
    }
}
