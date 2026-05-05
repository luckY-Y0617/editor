using System.Text.Json;
using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Workspaces;
using Northstar.Domain.Security;

namespace Northstar.Application.Workspaces;

public sealed class WorkspaceGroupService : IWorkspaceGroupService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IWorkspaceGroupRepository _repository;
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IEffectivePermissionService _effectivePermissionService;
    private readonly IPermissionAuditService _auditService;
    private readonly IPermissionNotificationService _notificationService;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;

    public WorkspaceGroupService(
        IWorkspaceGroupRepository repository,
        IWorkspaceAccessService workspaceAccessService,
        IEffectivePermissionService effectivePermissionService,
        IPermissionAuditService auditService,
        IPermissionNotificationService notificationService,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _workspaceAccessService = workspaceAccessService;
        _effectivePermissionService = effectivePermissionService;
        _auditService = auditService;
        _notificationService = notificationService;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkspaceGroupsResponse> GetGroupsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageGroupsAsync(workspaceId, cancellationToken);
        var groups = await _repository.GetGroupsAsync(workspaceId, cancellationToken);
        return new WorkspaceGroupsResponse(groups.Select(ToDto).ToArray());
    }

    public async Task<WorkspaceGroupDetailDto> GetGroupAsync(
        Guid workspaceId,
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageGroupsAsync(workspaceId, cancellationToken);
        var detail = await _repository.GetGroupDetailAsync(workspaceId, groupId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Workspace group was not found.");
        return ToDetailDto(detail);
    }

    public Task<WorkspaceGroupDto> CreateGroupAsync(
        Guid workspaceId,
        CreateWorkspaceGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var manageResult = await EnsureCanManageGroupsAsync(workspaceId, ct);
            var actorId = await _workspaceAccessService.GetRequiredUserIdAsync(ct);
            var type = NormalizeGroupType(request.Type);
            if (type != GroupTypes.Static)
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "Dynamic groups are reserved for future IAM sync.");
            }

            var name = NormalizeName(request.Name);
            await EnsureUniqueNameAsync(workspaceId, name, exceptGroupId: null, ct);
            var group = new WorkspaceGroup(
                workspaceId,
                name,
                request.Description,
                type,
                actorId);

            await _repository.AddGroupAsync(group, ct);
            await _auditService.AddAsync(
                CreateGroupAuditEvent(
                    workspaceId,
                    actorId,
                    PermissionAuditActions.GroupCreated,
                    before: null,
                    after: GroupSnapshot(group),
                    manageResult,
                    group.Id),
                ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var detail = await _repository.GetGroupDetailAsync(workspaceId, group.Id, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Workspace group was not found after creation.");
            return ToDto(detail.Group);
        }, cancellationToken);
    }

    public Task<WorkspaceGroupDto> UpdateGroupAsync(
        Guid workspaceId,
        Guid groupId,
        UpdateWorkspaceGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var manageResult = await EnsureCanManageGroupsAsync(workspaceId, ct);
            var actorId = await _workspaceAccessService.GetRequiredUserIdAsync(ct);
            var group = await GetMutableGroupAsync(workspaceId, groupId, ct);
            var before = GroupSnapshot(group);
            var name = NormalizeName(request.Name);
            await EnsureUniqueNameAsync(workspaceId, name, groupId, ct);

            group.Update(name, request.Description);
            await _auditService.AddAsync(
                CreateGroupAuditEvent(
                    workspaceId,
                    actorId,
                    PermissionAuditActions.GroupUpdated,
                    before,
                    GroupSnapshot(group),
                    manageResult,
                    group.Id),
                ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var detail = await _repository.GetGroupDetailAsync(workspaceId, group.Id, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Workspace group was not found after update.");
            return ToDto(detail.Group);
        }, cancellationToken);
    }

    public Task ArchiveGroupAsync(
        Guid workspaceId,
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var manageResult = await EnsureCanManageGroupsAsync(workspaceId, ct);
            var actorId = await _workspaceAccessService.GetRequiredUserIdAsync(ct);
            var group = await GetMutableGroupAsync(workspaceId, groupId, ct);
            var before = GroupSnapshot(group);

            group.Archive();
            await _auditService.AddAsync(
                CreateGroupAuditEvent(
                    workspaceId,
                    actorId,
                    PermissionAuditActions.GroupArchived,
                    before,
                    GroupSnapshot(group),
                    manageResult,
                    group.Id),
                ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }

    public Task<WorkspaceGroupMemberDto> AddMemberAsync(
        Guid workspaceId,
        Guid groupId,
        AddWorkspaceGroupMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var manageResult = await EnsureCanManageGroupsAsync(workspaceId, ct);
            var actorId = await _workspaceAccessService.GetRequiredUserIdAsync(ct);
            var group = await GetMutableGroupAsync(workspaceId, groupId, ct);
            var userId = ParseGuid(request.UserId, "userId");
            if (!await _repository.UserIsWorkspaceMemberAsync(workspaceId, userId, ct))
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "User must be an active workspace member.");
            }
            EnsureFutureExpiry(request.ExpiresAt);

            var existing = await _repository.GetActiveMemberForUpdateAsync(group.Id, userId, ct);
            if (existing is not null)
            {
                if (existing.IsActive(DateTimeOffset.UtcNow))
                {
                    throw new ApplicationErrorException(ErrorCodes.Conflict, "User is already a group member.");
                }

                var before = GroupMemberSnapshot(existing);
                existing.ChangeExpiry(request.ExpiresAt);
                await _auditService.AddAsync(
                    CreateGroupAuditEvent(
                        workspaceId,
                        actorId,
                        PermissionAuditActions.GroupMemberAdded,
                        before,
                        GroupMemberSnapshot(existing),
                        manageResult,
                        group.Id,
                        new { existing.UserId, renewed = true }),
                    ct);
                await _notificationService.AddAsync(
                    new PermissionNotification(
                        workspaceId,
                        userId,
                        PermissionNotificationTypes.GroupMemberAdded,
                        "Added to workspace group",
                        $"You were added to {group.Name}.",
                        actorId,
                        ResourceTypes.Workspace,
                        workspaceId,
                        actionUrl: "#permissions"),
                    ct);
                await _unitOfWork.SaveChangesAsync(ct);

                var renewedDetail = await _repository.GetGroupDetailAsync(workspaceId, group.Id, ct)
                    ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Workspace group was not found after member add.");
                return ToMemberDto(renewedDetail.Members.Single(member => member.UserId == userId));
            }

            var member = new WorkspaceGroupMember(group.Id, userId, actorId, request.ExpiresAt);
            await _repository.AddMemberAsync(member, ct);
            await _auditService.AddAsync(
                CreateGroupAuditEvent(
                    workspaceId,
                    actorId,
                    PermissionAuditActions.GroupMemberAdded,
                    before: null,
                    after: GroupMemberSnapshot(member),
                    manageResult,
                    group.Id,
                    new { member.UserId }),
                ct);
            await _notificationService.AddAsync(
                new PermissionNotification(
                    workspaceId,
                    userId,
                    PermissionNotificationTypes.GroupMemberAdded,
                    "Added to workspace group",
                    $"You were added to {group.Name}.",
                    actorId,
                    ResourceTypes.Workspace,
                    workspaceId,
                    actionUrl: "#permissions"),
                ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var detail = await _repository.GetGroupDetailAsync(workspaceId, group.Id, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Workspace group was not found after member add.");
            return ToMemberDto(detail.Members.Single(member => member.UserId == userId));
        }, cancellationToken);
    }

    public Task RemoveMemberAsync(
        Guid workspaceId,
        Guid groupId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var manageResult = await EnsureCanManageGroupsAsync(workspaceId, ct);
            var actorId = await _workspaceAccessService.GetRequiredUserIdAsync(ct);
            var group = await GetMutableGroupAsync(workspaceId, groupId, ct);
            var member = await _repository.GetActiveMemberForUpdateAsync(group.Id, userId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Workspace group member was not found.");
            var before = GroupMemberSnapshot(member);

            member.Remove();
            await _auditService.AddAsync(
                CreateGroupAuditEvent(
                    workspaceId,
                    actorId,
                    PermissionAuditActions.GroupMemberRemoved,
                    before,
                    GroupMemberSnapshot(member),
                    manageResult,
                    group.Id,
                    new { member.UserId }),
                ct);
            await _notificationService.AddAsync(
                new PermissionNotification(
                    workspaceId,
                    userId,
                    PermissionNotificationTypes.GroupMemberRemoved,
                    "Removed from workspace group",
                    $"You were removed from {group.Name}.",
                    actorId,
                    ResourceTypes.Workspace,
                    workspaceId,
                    actionUrl: "#permissions"),
                ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }

    private async Task<EffectivePermissionResult> EnsureCanManageGroupsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        if (!await _repository.WorkspaceExistsAsync(workspaceId, cancellationToken))
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Workspace was not found.");
        }

        await _workspaceAccessService.EnsureCanManageWorkspaceAsync(workspaceId, cancellationToken);
        var actorId = await _workspaceAccessService.GetRequiredUserIdAsync(cancellationToken);
        return await _effectivePermissionService.AuthorizeWorkspaceAsync(
            workspaceId,
            actorId,
            PermissionActions.WorkspaceManageMembers,
            cancellationToken);
    }

    private async Task<WorkspaceGroup> GetMutableGroupAsync(
        Guid workspaceId,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var group = await _repository.GetGroupForUpdateAsync(workspaceId, groupId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Workspace group was not found.");
        if (group.IsExternal)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "External groups are read-only.");
        }

        if (group.IsArchived)
        {
            throw new ApplicationErrorException(ErrorCodes.Conflict, "Archived groups cannot be changed.");
        }

        return group;
    }

    private async Task EnsureUniqueNameAsync(
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

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Group name is required.");
        }

        return name.Trim();
    }

    private static string NormalizeGroupType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return GroupTypes.Static;
        }

        var normalized = type.Trim().ToLowerInvariant();
        return GroupTypes.IsSupported(normalized)
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "Group type is invalid.");
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

    private static WorkspaceGroupDto ToDto(WorkspaceGroupReadModel group)
    {
        return new WorkspaceGroupDto(
            group.Id.ToString(),
            group.WorkspaceId.ToString(),
            group.Name,
            group.Description,
            group.Type,
            group.ArchivedAt.HasValue,
            group.ExternalProvider,
            group.ExternalGroupId,
            group.ExternalSyncedAt,
            group.MembersCount,
            group.CreatedAt,
            group.UpdatedAt);
    }

    private static WorkspaceGroupDetailDto ToDetailDto(WorkspaceGroupDetailReadModel detail)
    {
        return new WorkspaceGroupDetailDto(
            ToDto(detail.Group),
            detail.Members.Select(ToMemberDto).ToArray());
    }

    private static WorkspaceGroupMemberDto ToMemberDto(WorkspaceGroupMemberReadModel member)
    {
        return new WorkspaceGroupMemberDto(
            member.Id.ToString(),
            member.UserId.ToString(),
            member.Email,
            member.DisplayName,
            member.AddedBy?.ToString(),
            member.AddedAt,
            member.ExpiresAt);
    }

    private static PermissionAuditEvent CreateGroupAuditEvent(
        Guid workspaceId,
        Guid actorId,
        string action,
        object? before,
        object? after,
        EffectivePermissionResult manageResult,
        Guid groupId,
        object? extraMetadata = null)
    {
        return new PermissionAuditEvent(
            workspaceId,
            actorId,
            action,
            ResourceTypes.Workspace,
            workspaceId,
            SubjectTypes.Group,
            groupId,
            before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            JsonSerializer.Serialize(new
            {
                manageResult.EffectiveRole,
                manageResult.Source,
                groupId,
                extraMetadata
            }, JsonOptions));
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
}
