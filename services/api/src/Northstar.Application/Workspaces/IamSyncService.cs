using System.Text.Json;
using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Workspaces;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Application.Workspaces;

public sealed class IamSyncService : IIamSyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IIamSyncRepository _repository;
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IEffectivePermissionService _effectivePermissionService;
    private readonly IPermissionAuditService _auditService;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;

    public IamSyncService(
        IIamSyncRepository repository,
        IWorkspaceAccessService workspaceAccessService,
        IEffectivePermissionService effectivePermissionService,
        IPermissionAuditService auditService,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _workspaceAccessService = workspaceAccessService;
        _effectivePermissionService = effectivePermissionService;
        _auditService = auditService;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
    }

    public Task<IamSyncResponse> SyncAsync(
        Guid workspaceId,
        IamSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            if (!await _repository.WorkspaceExistsAsync(workspaceId, ct))
            {
                throw new ApplicationErrorException(ErrorCodes.NotFound, "Workspace was not found.");
            }

            await _workspaceAccessService.EnsureCanManageWorkspaceAsync(workspaceId, ct);
            var actorId = await _workspaceAccessService.GetRequiredUserIdAsync(ct);
            var manageResult = await _effectivePermissionService.AuthorizeWorkspaceAsync(
                workspaceId,
                actorId,
                PermissionActions.WorkspaceManageMembers,
                ct);

            var provider = NormalizeProvider(request.Provider);
            var counters = new IamSyncCounters();
            var normalizedUsers = NormalizeUsers(workspaceId, request.Users, counters);
            var normalizedGroups = NormalizeGroups(workspaceId, request.Groups, counters);
            var syncedUsers = new Dictionary<string, User>(StringComparer.Ordinal);
            var userMappings = new List<IamSyncUserMappingDto>();

            foreach (var syncUser in normalizedUsers.Values)
            {
                var existingUser = await _repository.GetUserByExternalAsync(
                    provider,
                    syncUser.ExternalSubjectId,
                    ct);
                var created = false;
                if (existingUser is null && syncUser.Email is not null)
                {
                    existingUser = await _repository.GetUserByEmailAsync(syncUser.Email, ct);
                }

                var before = existingUser is null ? null : UserSnapshot(existingUser);
                var workspaceMemberCreated = false;
                bool userChanged;
                User user;
                if (existingUser is null)
                {
                    user = new User(
                        syncUser.DisplayName,
                        syncUser.Email,
                        externalProvider: provider,
                        externalSubjectId: syncUser.ExternalSubjectId);
                    await _repository.AddUserAsync(user, ct);
                    counters.UsersCreated++;
                    created = true;
                    userChanged = true;
                }
                else
                {
                    user = existingUser;
                    userChanged = user.ApplyExternalProfile(
                        provider,
                        syncUser.ExternalSubjectId,
                        syncUser.DisplayName,
                        syncUser.Email);
                    if (userChanged)
                    {
                        counters.UsersUpdated++;
                    }
                }

                var workspaceMember = await _repository.GetWorkspaceMemberForUpdateAsync(
                    workspaceId,
                    user.Id,
                    ct);
                if (workspaceMember is null)
                {
                    await _repository.AddWorkspaceMemberAsync(
                        new WorkspaceMember(workspaceId, user.Id, syncUser.WorkspaceRole),
                        ct);
                    counters.WorkspaceMembersCreated++;
                    workspaceMemberCreated = true;
                }

                if (userChanged || workspaceMemberCreated)
                {
                    await _auditService.AddAsync(
                        CreateAuditEvent(
                            workspaceId,
                            actorId,
                            PermissionAuditActions.IamUserMapped,
                            before,
                            UserSnapshot(user),
                            manageResult,
                            SubjectTypes.User,
                            user.Id,
                            new
                            {
                                provider,
                                syncUser.ExternalSubjectId,
                                workspaceMemberCreated
                            }),
                        ct);
                }
                else
                {
                    counters.Skipped++;
                }

                syncedUsers[syncUser.ExternalSubjectId] = user;
                userMappings.Add(new IamSyncUserMappingDto(
                    syncUser.ExternalSubjectId,
                    user.Id.ToString(),
                    created,
                    workspaceMemberCreated));
            }

            var groupMappings = new List<IamSyncGroupMappingDto>();
            foreach (var syncGroup in normalizedGroups.Values)
            {
                foreach (var memberSubjectId in syncGroup.MemberExternalSubjectIds)
                {
                    if (!syncedUsers.ContainsKey(memberSubjectId))
                    {
                        throw new ApplicationErrorException(
                            ErrorCodes.ValidationError,
                            "Group members must reference users in the same IAM sync payload.");
                    }
                }

                var existingGroup = await _repository.GetGroupByExternalForUpdateAsync(
                    workspaceId,
                    provider,
                    syncGroup.ExternalGroupId,
                    ct);
                var groupCreated = false;
                WorkspaceGroup group;
                object? before = null;
                if (existingGroup is null)
                {
                    await EnsureUniqueGroupNameAsync(workspaceId, syncGroup.Name, null, ct);
                    group = new WorkspaceGroup(
                        workspaceId,
                        syncGroup.Name,
                        syncGroup.Description,
                        GroupTypes.Dynamic,
                        actorId,
                        provider,
                        syncGroup.ExternalGroupId,
                        DateTimeOffset.UtcNow);
                    await _repository.AddGroupAsync(group, ct);
                    counters.GroupsCreated++;
                    groupCreated = true;
                    await _auditService.AddAsync(
                        CreateAuditEvent(
                            workspaceId,
                            actorId,
                            PermissionAuditActions.IamGroupSynced,
                            before: null,
                            after: GroupSnapshot(group),
                            manageResult,
                            SubjectTypes.Group,
                            group.Id,
                            new { provider, syncGroup.ExternalGroupId, created = true }),
                        ct);
                }
                else
                {
                    group = existingGroup;
                    before = GroupSnapshot(group);
                    await EnsureUniqueGroupNameAsync(workspaceId, syncGroup.Name, group.Id, ct);
                    var changed = group.SyncExternal(
                        syncGroup.Name,
                        syncGroup.Description,
                        provider,
                        syncGroup.ExternalGroupId,
                        DateTimeOffset.UtcNow);
                    if (changed)
                    {
                        counters.GroupsUpdated++;
                        await _auditService.AddAsync(
                            CreateAuditEvent(
                                workspaceId,
                                actorId,
                                PermissionAuditActions.IamGroupSynced,
                                before,
                                GroupSnapshot(group),
                                manageResult,
                                SubjectTypes.Group,
                                group.Id,
                                new { provider, syncGroup.ExternalGroupId, created = false }),
                            ct);
                    }
                }

                var membershipResult = await SyncGroupMembersAsync(
                    workspaceId,
                    actorId,
                    manageResult,
                    provider,
                    group,
                    syncGroup,
                    syncedUsers,
                    counters,
                    ct);

                if (!groupCreated &&
                    before is not null &&
                    membershipResult.MembersAdded == 0 &&
                    membershipResult.MembersRemoved == 0)
                {
                    counters.Skipped++;
                }

                groupMappings.Add(new IamSyncGroupMappingDto(
                    syncGroup.ExternalGroupId,
                    group.Id.ToString(),
                    groupCreated,
                    membershipResult.MembersAdded,
                    membershipResult.MembersRemoved));
            }

            await _unitOfWork.SaveChangesAsync(ct);

            return new IamSyncResponse(
                counters.ToDto(),
                userMappings,
                groupMappings);
        }, cancellationToken);
    }

    private async Task<IamMembershipSyncResult> SyncGroupMembersAsync(
        Guid workspaceId,
        Guid actorId,
        EffectivePermissionResult manageResult,
        string provider,
        WorkspaceGroup group,
        NormalizedIamGroup syncGroup,
        IReadOnlyDictionary<string, User> syncedUsers,
        IamSyncCounters counters,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var desiredUserIds = syncGroup.MemberExternalSubjectIds
            .Select(subjectId => syncedUsers[subjectId].Id)
            .ToHashSet();
        var existingMembers = await _repository.GetGroupMembersForUpdateAsync(group.Id, cancellationToken);
        var existingByUserId = existingMembers.ToDictionary(member => member.UserId);
        var membersAdded = 0;
        var membersRemoved = 0;

        foreach (var desiredUserId in desiredUserIds)
        {
            if (existingByUserId.TryGetValue(desiredUserId, out var existing))
            {
                if (existing.IsActive(now) && existing.ExpiresAt is null)
                {
                    counters.Skipped++;
                    continue;
                }

                var before = GroupMemberSnapshot(existing);
                existing.ChangeExpiry(null);
                counters.MembersAdded++;
                membersAdded++;
                await _auditService.AddAsync(
                    CreateAuditEvent(
                        workspaceId,
                        actorId,
                        PermissionAuditActions.IamGroupMemberAdded,
                        before,
                        GroupMemberSnapshot(existing),
                        manageResult,
                        SubjectTypes.User,
                        desiredUserId,
                        new { provider, groupId = group.Id, syncGroup.ExternalGroupId, renewed = true }),
                    cancellationToken);
                continue;
            }

            var member = new WorkspaceGroupMember(group.Id, desiredUserId, actorId);
            await _repository.AddGroupMemberAsync(member, cancellationToken);
            counters.MembersAdded++;
            membersAdded++;
            await _auditService.AddAsync(
                CreateAuditEvent(
                    workspaceId,
                    actorId,
                    PermissionAuditActions.IamGroupMemberAdded,
                    before: null,
                    after: GroupMemberSnapshot(member),
                    manageResult,
                    SubjectTypes.User,
                    desiredUserId,
                    new { provider, groupId = group.Id, syncGroup.ExternalGroupId, renewed = false }),
                cancellationToken);
        }

        foreach (var existing in existingMembers.Where(member => !desiredUserIds.Contains(member.UserId)))
        {
            var before = GroupMemberSnapshot(existing);
            existing.Remove();
            counters.MembersRemoved++;
            membersRemoved++;
            await _auditService.AddAsync(
                CreateAuditEvent(
                    workspaceId,
                    actorId,
                    PermissionAuditActions.IamGroupMemberRemoved,
                    before,
                    GroupMemberSnapshot(existing),
                    manageResult,
                    SubjectTypes.User,
                    existing.UserId,
                    new { provider, groupId = group.Id, syncGroup.ExternalGroupId }),
                cancellationToken);
        }

        return new IamMembershipSyncResult(membersAdded, membersRemoved);
    }

    private async Task EnsureUniqueGroupNameAsync(
        Guid workspaceId,
        string name,
        Guid? exceptGroupId,
        CancellationToken cancellationToken)
    {
        if (await _repository.ActiveGroupNameExistsAsync(workspaceId, name, exceptGroupId, cancellationToken))
        {
            throw new ApplicationErrorException(ErrorCodes.Conflict, "A workspace group with this name already exists.");
        }
    }

    private static Dictionary<string, NormalizedIamUser> NormalizeUsers(
        Guid workspaceId,
        IReadOnlyList<IamSyncUserRequest>? users,
        IamSyncCounters counters)
    {
        var normalized = new Dictionary<string, NormalizedIamUser>(StringComparer.Ordinal);
        foreach (var user in users ?? Array.Empty<IamSyncUserRequest>())
        {
            EnsureWorkspaceMatch(workspaceId, user.WorkspaceId);
            var subjectId = NormalizeExternalId(user.ExternalSubjectId, "externalSubjectId");
            if (normalized.ContainsKey(subjectId))
            {
                counters.Skipped++;
                continue;
            }

            normalized[subjectId] = new NormalizedIamUser(
                subjectId,
                NormalizeOptionalEmail(user.Email),
                NormalizeDisplayName(user.DisplayName),
                NormalizeWorkspaceRole(user.WorkspaceRole));
        }

        return normalized;
    }

    private static Dictionary<string, NormalizedIamGroup> NormalizeGroups(
        Guid workspaceId,
        IReadOnlyList<IamSyncGroupRequest>? groups,
        IamSyncCounters counters)
    {
        var normalized = new Dictionary<string, NormalizedIamGroup>(StringComparer.Ordinal);
        foreach (var group in groups ?? Array.Empty<IamSyncGroupRequest>())
        {
            EnsureWorkspaceMatch(workspaceId, group.WorkspaceId);
            var externalGroupId = NormalizeExternalId(group.ExternalGroupId, "externalGroupId");
            if (normalized.ContainsKey(externalGroupId))
            {
                counters.Skipped++;
                continue;
            }

            var members = new List<string>();
            var seenMembers = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rawMember in group.Members ?? Array.Empty<string>())
            {
                var memberSubjectId = NormalizeExternalId(rawMember, "member externalSubjectId");
                if (!seenMembers.Add(memberSubjectId))
                {
                    counters.Skipped++;
                    continue;
                }

                members.Add(memberSubjectId);
            }

            normalized[externalGroupId] = new NormalizedIamGroup(
                externalGroupId,
                NormalizeDisplayName(group.Name),
                NormalizeOptional(group.Description),
                members);
        }

        return normalized;
    }

    private static void EnsureWorkspaceMatch(Guid workspaceId, string? payloadWorkspaceId)
    {
        if (string.IsNullOrWhiteSpace(payloadWorkspaceId))
        {
            return;
        }

        if (!Guid.TryParse(payloadWorkspaceId, out var parsed) || parsed != workspaceId)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Payload workspaceId must match the route workspaceId.");
        }
    }

    private static string NormalizeProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "provider is required.");
        }

        return provider.Trim().ToLowerInvariant();
    }

    private static string NormalizeExternalId(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, $"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "displayName is required.");
        }

        return displayName.Trim();
    }

    private static string? NormalizeOptionalEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeWorkspaceRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return WorkspaceMemberRole.Viewer;
        }

        var normalized = role.Trim().ToLowerInvariant();
        if (normalized == WorkspaceMemberRole.Owner || !WorkspaceMemberRole.IsValid(normalized))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "workspaceRole must be admin, editor, or viewer.");
        }

        return normalized;
    }

    private static PermissionAuditEvent CreateAuditEvent(
        Guid workspaceId,
        Guid actorId,
        string action,
        object? before,
        object? after,
        EffectivePermissionResult manageResult,
        string? subjectType,
        Guid? subjectId,
        object? extraMetadata)
    {
        return new PermissionAuditEvent(
            workspaceId,
            actorId,
            action,
            ResourceTypes.Workspace,
            workspaceId,
            subjectType,
            subjectId,
            before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            JsonSerializer.Serialize(new
            {
                manageResult.EffectiveRole,
                manageResult.Source,
                extraMetadata
            }, JsonOptions));
    }

    private static object UserSnapshot(User user)
    {
        return new
        {
            user.Id,
            user.Email,
            user.DisplayName,
            user.ExternalProvider,
            user.ExternalSubjectId,
            user.UpdatedAt
        };
    }

    private static object GroupSnapshot(WorkspaceGroup group)
    {
        return new
        {
            group.Id,
            group.WorkspaceId,
            group.Name,
            group.Description,
            group.Type,
            group.ArchivedAt,
            group.ExternalProvider,
            group.ExternalGroupId,
            group.ExternalSyncedAt,
            group.UpdatedAt
        };
    }

    private static object GroupMemberSnapshot(WorkspaceGroupMember member)
    {
        return new
        {
            member.Id,
            member.GroupId,
            member.UserId,
            member.AddedBy,
            member.AddedAt,
            member.ExpiresAt,
            member.RemovedAt
        };
    }

    private sealed record NormalizedIamUser(
        string ExternalSubjectId,
        string? Email,
        string DisplayName,
        string WorkspaceRole);

    private sealed record NormalizedIamGroup(
        string ExternalGroupId,
        string Name,
        string? Description,
        IReadOnlyList<string> MemberExternalSubjectIds);

    private sealed record IamMembershipSyncResult(int MembersAdded, int MembersRemoved);

    private sealed class IamSyncCounters
    {
        public int UsersCreated { get; set; }
        public int UsersUpdated { get; set; }
        public int WorkspaceMembersCreated { get; set; }
        public int GroupsCreated { get; set; }
        public int GroupsUpdated { get; set; }
        public int MembersAdded { get; set; }
        public int MembersRemoved { get; set; }
        public int Skipped { get; set; }

        public IamSyncCountsDto ToDto()
        {
            return new IamSyncCountsDto(
                UsersCreated + WorkspaceMembersCreated + GroupsCreated + MembersAdded,
                UsersUpdated + GroupsUpdated,
                MembersRemoved,
                Skipped,
                UsersCreated,
                UsersUpdated,
                WorkspaceMembersCreated,
                GroupsCreated,
                GroupsUpdated,
                MembersAdded,
                MembersRemoved);
        }
    }
}
