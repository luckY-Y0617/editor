using System.Text.Json;
using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Contracts.Security;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Application.Security;

public sealed class ScimService : IScimService
{
    private const string ScimProvider = "scim";
    private const string ListResponseSchema = "urn:ietf:params:scim:api:messages:2.0:ListResponse";
    private const string ServiceProviderConfigSchema = "urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig";
    private const string ResourceTypeSchema = "urn:ietf:params:scim:schemas:core:2.0:ResourceType";
    private const string SchemaSchema = "urn:ietf:params:scim:schemas:core:2.0:Schema";
    private const string UserSchema = "urn:ietf:params:scim:schemas:core:2.0:User";
    private const string GroupSchema = "urn:ietf:params:scim:schemas:core:2.0:Group";
    private const int DefaultPageSize = 100;
    private const int MaxPageSize = 100;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IScimBearerTokenAccessor _bearerTokenAccessor;
    private readonly IScimTokenRepository _scimTokenRepository;
    private readonly IScimProvisioningRepository _provisioningRepository;
    private readonly IShareLinkTokenService _tokenService;
    private readonly IPermissionAuditService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;

    public ScimService(
        IWorkspaceAccessService workspaceAccessService,
        IScimBearerTokenAccessor bearerTokenAccessor,
        IScimTokenRepository scimTokenRepository,
        IScimProvisioningRepository provisioningRepository,
        IShareLinkTokenService tokenService,
        IPermissionAuditService auditService,
        ICurrentUser currentUser,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork)
    {
        _workspaceAccessService = workspaceAccessService;
        _bearerTokenAccessor = bearerTokenAccessor;
        _scimTokenRepository = scimTokenRepository;
        _provisioningRepository = provisioningRepository;
        _tokenService = tokenService;
        _auditService = auditService;
        _currentUser = currentUser;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
    }

    public async Task<ScimServiceProviderConfigResponse> GetServiceProviderConfigAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanDiscoverScimAsync(workspaceId, cancellationToken);

        return new ScimServiceProviderConfigResponse(
            [ServiceProviderConfigSchema],
            "/api/v1/workspaces/{workspaceId}/scim/v2",
            new ScimSupportedFeature(true),
            new ScimBulkFeature(false, 0, 0),
            new ScimFilterFeature(true, MaxPageSize),
            new ScimSupportedFeature(false),
            new ScimSupportedFeature(false),
            new ScimSupportedFeature(false),
            [
                new ScimAuthenticationSchemeDto(
                    "Northstar SCIM bearer token",
                    "Dedicated workspace-scoped SCIM bearer tokens are accepted for SCIM endpoints. Owner/admin Northstar API tokens are accepted for management discovery.",
                    "oauthbearertoken",
                    true)
            ]);
    }

    public async Task<ScimListResponse<ScimSchemaDto>> GetSchemasAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanDiscoverScimAsync(workspaceId, cancellationToken);

        var resources = new[]
        {
            new ScimSchemaDto(
                [SchemaSchema],
                UserSchema,
                "User",
                "Northstar SCIM User schema for minimal workspace provisioning.",
                []),
            new ScimSchemaDto(
                [SchemaSchema],
                GroupSchema,
                "Group",
                "Northstar SCIM Group schema for minimal workspace provisioning.",
                [])
        };

        return new ScimListResponse<ScimSchemaDto>(
            [ListResponseSchema],
            resources.Length,
            resources.Length,
            1,
            resources);
    }

    public async Task<ScimListResponse<ScimResourceTypeDto>> GetResourceTypesAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanDiscoverScimAsync(workspaceId, cancellationToken);

        var resources = new[]
        {
            new ScimResourceTypeDto(
                "User",
                "User",
                "/Users",
                "Northstar SCIM User provisioning endpoint.",
                UserSchema,
                []),
            new ScimResourceTypeDto(
                "Group",
                "Group",
                "/Groups",
                "Northstar SCIM Group provisioning endpoint.",
                GroupSchema,
                [])
        };

        return new ScimListResponse<ScimResourceTypeDto>(
            [ListResponseSchema],
            resources.Length,
            resources.Length,
            1,
            resources);
    }

    public async Task<ScimListResponse<ScimUserResource>> GetUsersAsync(
        Guid workspaceId,
        string? filter,
        int? startIndex,
        int? count,
        CancellationToken cancellationToken = default)
    {
        await EnsureScimBearerTokenAsync(workspaceId, cancellationToken);
        var page = NormalizePage(startIndex, count);
        var parsedFilter = ParseFilter(filter, "userName", "externalId");
        var userName = parsedFilter is not null &&
            string.Equals(parsedFilter.Field, "userName", StringComparison.OrdinalIgnoreCase)
            ? NormalizeEmail(parsedFilter.Value, "userName")
            : null;
        var externalId = parsedFilter is not null &&
            string.Equals(parsedFilter.Field, "externalId", StringComparison.OrdinalIgnoreCase)
            ? NormalizeRequired(parsedFilter.Value, "externalId")
            : null;

        var total = await _provisioningRepository.CountUsersAsync(
            workspaceId,
            ScimProvider,
            userName,
            externalId,
            cancellationToken);
        var users = await _provisioningRepository.GetUsersAsync(
            workspaceId,
            ScimProvider,
            userName,
            externalId,
            page.Skip,
            page.Take,
            cancellationToken);

        return new ScimListResponse<ScimUserResource>(
            [ListResponseSchema],
            total,
            users.Count,
            page.StartIndex,
            users.Select(user => ToUserResource(workspaceId, user)).ToArray());
    }

    public async Task<ScimUserResource> GetUserAsync(
        Guid workspaceId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureScimBearerTokenAsync(workspaceId, cancellationToken);
        var parsedUserId = ParseGuid(userId, "userId");
        var user = await _provisioningRepository.GetUserForUpdateAsync(
            workspaceId,
            parsedUserId,
            ScimProvider,
            cancellationToken);
        if (user is null)
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "SCIM user was not found.");
        }

        return ToUserResource(workspaceId, user);
    }

    public Task<ScimUserResource> CreateUserAsync(
        Guid workspaceId,
        CreateScimUserRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            await EnsureScimBearerTokenAsync(workspaceId, ct);
            EnsureActiveRequest(request.Active);
            var normalized = NormalizeUserRequest(request);

            var existingUser = await _provisioningRepository.GetUserByExternalForUpdateAsync(
                ScimProvider,
                normalized.ExternalId,
                ct);
            if (existingUser is null)
            {
                existingUser = await _provisioningRepository.GetUserByEmailForUpdateAsync(
                    normalized.UserName,
                    ct);
            }

            var before = existingUser is null ? null : UserSnapshot(existingUser);
            var userChanged = false;
            var userCreated = false;
            User user;
            if (existingUser is null)
            {
                user = new User(
                    normalized.DisplayName,
                    normalized.UserName,
                    externalProvider: ScimProvider,
                    externalSubjectId: normalized.ExternalId);
                await _provisioningRepository.AddUserAsync(user, ct);
                userCreated = true;
                userChanged = true;
            }
            else
            {
                user = existingUser;
                userChanged = user.ApplyExternalProfile(
                    ScimProvider,
                    normalized.ExternalId,
                    normalized.DisplayName,
                    normalized.UserName);
            }

            var member = await _provisioningRepository.GetWorkspaceMemberForUpdateAsync(
                workspaceId,
                user.Id,
                ct);
            var workspaceMemberCreated = false;
            if (member is null)
            {
                member = new WorkspaceMember(workspaceId, user.Id, WorkspaceMemberRole.Viewer);
                await _provisioningRepository.AddWorkspaceMemberAsync(member, ct);
                workspaceMemberCreated = true;
            }

            if (userChanged || workspaceMemberCreated)
            {
                await _auditService.AddAsync(
                    CreateAuditEvent(
                        workspaceId,
                        PermissionAuditActions.IamUserMapped,
                        before,
                        UserSnapshot(user),
                        SubjectTypes.User,
                        user.Id,
                        new
                        {
                            provider = ScimProvider,
                            externalSubjectId = normalized.ExternalId,
                            userCreated,
                            workspaceMemberCreated,
                            workspaceRole = workspaceMemberCreated ? WorkspaceMemberRole.Viewer : member.Role
                        }),
                    ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return ToUserResource(workspaceId, new ScimProvisionedUser(user, member));
        }, cancellationToken);
    }

    public Task<ScimUserResource> PatchUserAsync(
        Guid workspaceId,
        string userId,
        ScimPatchRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            await EnsureScimBearerTokenAsync(workspaceId, ct);
            var parsedUserId = ParseGuid(userId, "userId");
            var provisionedUser = await _provisioningRepository.GetUserForUpdateAsync(
                workspaceId,
                parsedUserId,
                ScimProvider,
                ct);
            if (provisionedUser is null)
            {
                throw new ApplicationErrorException(ErrorCodes.NotFound, "SCIM user was not found.");
            }

            var user = provisionedUser.User;
            var before = UserSnapshot(user);
            var patch = NormalizeUserPatch(user, request);
            var changed = user.ApplyExternalProfile(
                ScimProvider,
                user.ExternalSubjectId!,
                patch.DisplayName,
                patch.UserName);
            if (changed)
            {
                await _auditService.AddAsync(
                    CreateAuditEvent(
                        workspaceId,
                        PermissionAuditActions.IamUserMapped,
                        before,
                        UserSnapshot(user),
                        SubjectTypes.User,
                        user.Id,
                        new { provider = ScimProvider, externalSubjectId = user.ExternalSubjectId, patched = true }),
                    ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return ToUserResource(workspaceId, provisionedUser);
        }, cancellationToken);
    }

    public Task<ScimUserResource> ReplaceUserAsync(
        Guid workspaceId,
        string userId,
        CreateScimUserRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            await EnsureScimBearerTokenAsync(workspaceId, ct);
            EnsureActiveRequest(request.Active);
            var parsedUserId = ParseGuid(userId, "userId");
            var provisionedUser = await _provisioningRepository.GetUserForUpdateAsync(
                workspaceId,
                parsedUserId,
                ScimProvider,
                ct);
            if (provisionedUser is null)
            {
                throw new ApplicationErrorException(ErrorCodes.NotFound, "SCIM user was not found.");
            }

            var normalized = NormalizeUserRequest(request);
            var user = provisionedUser.User;
            if (!string.Equals(user.ExternalSubjectId, normalized.ExternalId, StringComparison.Ordinal))
            {
                throw new ApplicationErrorException(
                    ErrorCodes.ValidationError,
                    "SCIM user externalId cannot be changed by replace.");
            }

            var before = UserSnapshot(user);
            var changed = user.ApplyExternalProfile(
                ScimProvider,
                normalized.ExternalId,
                normalized.DisplayName,
                normalized.UserName);
            if (changed)
            {
                await _auditService.AddAsync(
                    CreateAuditEvent(
                        workspaceId,
                        PermissionAuditActions.IamUserMapped,
                        before,
                        UserSnapshot(user),
                        SubjectTypes.User,
                        user.Id,
                        new { provider = ScimProvider, externalSubjectId = user.ExternalSubjectId, replaced = true }),
                    ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return ToUserResource(workspaceId, provisionedUser);
        }, cancellationToken);
    }

    public async Task RejectUserProvisioningAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        await EnsureScimBearerTokenAsync(workspaceId, cancellationToken);
        throw new ApplicationErrorException(
            ErrorCodes.ValidationError,
            "SCIM user delete/deactivate is not implemented. Users are not hard-deleted through SCIM.");
    }

    public async Task<ScimListResponse<ScimGroupResource>> GetGroupsAsync(
        Guid workspaceId,
        string? filter,
        int? startIndex,
        int? count,
        CancellationToken cancellationToken = default)
    {
        await EnsureScimBearerTokenAsync(workspaceId, cancellationToken);
        var page = NormalizePage(startIndex, count);
        var parsedFilter = ParseFilter(filter, "displayName", "externalId");
        var displayName = parsedFilter is not null &&
            string.Equals(parsedFilter.Field, "displayName", StringComparison.OrdinalIgnoreCase)
            ? NormalizeRequired(parsedFilter.Value, "displayName")
            : null;
        var externalId = parsedFilter is not null &&
            string.Equals(parsedFilter.Field, "externalId", StringComparison.OrdinalIgnoreCase)
            ? NormalizeRequired(parsedFilter.Value, "externalId")
            : null;

        var total = await _provisioningRepository.CountGroupsAsync(
            workspaceId,
            ScimProvider,
            displayName,
            externalId,
            cancellationToken);
        var groups = await _provisioningRepository.GetGroupsAsync(
            workspaceId,
            ScimProvider,
            displayName,
            externalId,
            page.Skip,
            page.Take,
            cancellationToken);

        return new ScimListResponse<ScimGroupResource>(
            [ListResponseSchema],
            total,
            groups.Count,
            page.StartIndex,
            groups.Select(group => ToGroupResource(workspaceId, group)).ToArray());
    }

    public async Task<ScimGroupResource> GetGroupAsync(
        Guid workspaceId,
        string groupId,
        CancellationToken cancellationToken = default)
    {
        await EnsureScimBearerTokenAsync(workspaceId, cancellationToken);
        var parsedGroupId = ParseGuid(groupId, "groupId");
        var group = await GetScimGroupAsync(workspaceId, parsedGroupId, cancellationToken);
        return ToGroupResource(workspaceId, group);
    }

    public Task<ScimGroupResource> CreateGroupAsync(
        Guid workspaceId,
        CreateScimGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            await EnsureScimBearerTokenAsync(workspaceId, ct);
            var normalized = NormalizeGroupRequest(request);
            var now = DateTimeOffset.UtcNow;
            var group = await _provisioningRepository.GetGroupByExternalForUpdateAsync(
                workspaceId,
                ScimProvider,
                normalized.ExternalId,
                ct);

            var before = group is null ? null : GroupSnapshot(group);
            var groupCreated = false;
            var groupChanged = false;
            if (group is null)
            {
                await EnsureUniqueGroupNameAsync(workspaceId, normalized.DisplayName, null, ct);
                group = new WorkspaceGroup(
                    workspaceId,
                    normalized.DisplayName,
                    type: GroupTypes.Dynamic,
                    externalProvider: ScimProvider,
                    externalGroupId: normalized.ExternalId,
                    externalSyncedAt: now);
                await _provisioningRepository.AddGroupAsync(group, ct);
                groupCreated = true;
                groupChanged = true;
            }
            else
            {
                EnsureScimManaged(group);
                await EnsureUniqueGroupNameAsync(workspaceId, normalized.DisplayName, group.Id, ct);
                groupChanged = group.SyncExternal(
                    normalized.DisplayName,
                    group.Description,
                    ScimProvider,
                    normalized.ExternalId,
                    now);
            }

            var membershipResult = normalized.Members is null
                ? new ScimGroupMembershipSyncResult(0, 0)
                : await SyncGroupMembersAsync(workspaceId, group, normalized.Members, ct);

            if (groupChanged)
            {
                await _auditService.AddAsync(
                    CreateAuditEvent(
                        workspaceId,
                        PermissionAuditActions.IamGroupSynced,
                        before,
                        GroupSnapshot(group),
                        SubjectTypes.Group,
                        group.Id,
                        new { provider = ScimProvider, externalGroupId = normalized.ExternalId, created = groupCreated }),
                    ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            var provisionedGroup = await GetScimGroupAsync(workspaceId, group.Id, ct);
            return ToGroupResource(workspaceId, provisionedGroup);
        }, cancellationToken);
    }

    public Task<ScimGroupResource> PatchGroupAsync(
        Guid workspaceId,
        string groupId,
        ScimPatchRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            await EnsureScimBearerTokenAsync(workspaceId, ct);
            var parsedGroupId = ParseGuid(groupId, "groupId");
            var group = await _provisioningRepository.GetGroupForUpdateAsync(
                workspaceId,
                parsedGroupId,
                ct);
            if (group is null)
            {
                throw new ApplicationErrorException(ErrorCodes.NotFound, "SCIM group was not found.");
            }

            EnsureScimManaged(group);
            var patch = NormalizeGroupPatch(group, request);
            var before = GroupSnapshot(group);
            var groupChanged = false;
            if (patch.DisplayName is not null)
            {
                await EnsureUniqueGroupNameAsync(workspaceId, patch.DisplayName, group.Id, ct);
                groupChanged = group.SyncExternal(
                    patch.DisplayName,
                    group.Description,
                    ScimProvider,
                    group.ExternalGroupId!,
                    DateTimeOffset.UtcNow);
            }

            var membershipResult = patch.Members is null
                ? new ScimGroupMembershipSyncResult(0, 0)
                : await SyncGroupMembersAsync(workspaceId, group, patch.Members, ct);

            if (groupChanged)
            {
                await _auditService.AddAsync(
                    CreateAuditEvent(
                        workspaceId,
                        PermissionAuditActions.IamGroupSynced,
                        before,
                        GroupSnapshot(group),
                        SubjectTypes.Group,
                        group.Id,
                        new { provider = ScimProvider, externalGroupId = group.ExternalGroupId, patched = true }),
                    ct);
            }

            _ = membershipResult;
            await _unitOfWork.SaveChangesAsync(ct);
            var updatedGroup = await GetScimGroupAsync(workspaceId, group.Id, ct);
            return ToGroupResource(workspaceId, updatedGroup);
        }, cancellationToken);
    }

    public Task<ScimGroupResource> ReplaceGroupAsync(
        Guid workspaceId,
        string groupId,
        CreateScimGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            await EnsureScimBearerTokenAsync(workspaceId, ct);
            var parsedGroupId = ParseGuid(groupId, "groupId");
            var group = await _provisioningRepository.GetGroupForUpdateAsync(
                workspaceId,
                parsedGroupId,
                ct);
            if (group is null)
            {
                throw new ApplicationErrorException(ErrorCodes.NotFound, "SCIM group was not found.");
            }

            EnsureScimManaged(group);
            var normalized = NormalizeGroupRequest(request);
            if (!string.Equals(group.ExternalGroupId, normalized.ExternalId, StringComparison.Ordinal))
            {
                throw new ApplicationErrorException(
                    ErrorCodes.ValidationError,
                    "SCIM group externalId cannot be changed by replace.");
            }

            await EnsureUniqueGroupNameAsync(workspaceId, normalized.DisplayName, group.Id, ct);
            var before = GroupSnapshot(group);
            var groupChanged = group.SyncExternal(
                normalized.DisplayName,
                group.Description,
                ScimProvider,
                normalized.ExternalId,
                DateTimeOffset.UtcNow);
            var membershipResult = normalized.Members is null
                ? new ScimGroupMembershipSyncResult(0, 0)
                : await SyncGroupMembersAsync(workspaceId, group, normalized.Members, ct);

            if (groupChanged)
            {
                await _auditService.AddAsync(
                    CreateAuditEvent(
                        workspaceId,
                        PermissionAuditActions.IamGroupSynced,
                        before,
                        GroupSnapshot(group),
                        SubjectTypes.Group,
                        group.Id,
                        new { provider = ScimProvider, externalGroupId = group.ExternalGroupId, replaced = true }),
                    ct);
            }

            _ = membershipResult;
            await _unitOfWork.SaveChangesAsync(ct);
            var updatedGroup = await GetScimGroupAsync(workspaceId, group.Id, ct);
            return ToGroupResource(workspaceId, updatedGroup);
        }, cancellationToken);
    }

    public async Task RejectGroupProvisioningAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        await EnsureScimBearerTokenAsync(workspaceId, cancellationToken);
        throw new ApplicationErrorException(
            ErrorCodes.ValidationError,
            "SCIM group delete/deactivate is not implemented. Groups are not hard-deleted through SCIM.");
    }

    private async Task<ScimProvisionedGroup> GetScimGroupAsync(
        Guid workspaceId,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var group = await _provisioningRepository.GetGroupForUpdateAsync(
            workspaceId,
            groupId,
            cancellationToken);
        if (group is null || !string.Equals(group.ExternalProvider, ScimProvider, StringComparison.Ordinal))
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "SCIM group was not found.");
        }

        var members = await _provisioningRepository.GetGroupMembersForUpdateAsync(group.Id, cancellationToken);
        return new ScimProvisionedGroup(group, members);
    }

    private async Task<ScimGroupMembershipSyncResult> SyncGroupMembersAsync(
        Guid workspaceId,
        WorkspaceGroup group,
        IReadOnlyList<ScimGroupMemberDto> requestedMembers,
        CancellationToken cancellationToken)
    {
        var desiredUserIds = NormalizeGroupMemberIds(requestedMembers);
        var users = await _provisioningRepository.GetUsersByIdsAsync(
            workspaceId,
            ScimProvider,
            desiredUserIds,
            cancellationToken);
        if (users.Count != desiredUserIds.Count)
        {
            throw new ApplicationErrorException(
                ErrorCodes.ValidationError,
                "SCIM group members must reference SCIM-managed users in the same workspace.");
        }

        var existingMembers = await _provisioningRepository.GetGroupMembersForUpdateAsync(group.Id, cancellationToken);
        var existingByUserId = existingMembers.ToDictionary(member => member.Member.UserId);
        var now = DateTimeOffset.UtcNow;
        var membersAdded = 0;
        var membersRemoved = 0;

        foreach (var desiredUserId in desiredUserIds)
        {
            if (existingByUserId.TryGetValue(desiredUserId, out var existing))
            {
                if (existing.Member.IsActive(now) && existing.Member.ExpiresAt is null)
                {
                    continue;
                }

                var before = GroupMemberSnapshot(existing.Member);
                existing.Member.ChangeExpiry(null);
                membersAdded++;
                await AddGroupMemberAuditAsync(
                    workspaceId,
                    group,
                    desiredUserId,
                    PermissionAuditActions.IamGroupMemberAdded,
                    before,
                    GroupMemberSnapshot(existing.Member),
                    renewed: true,
                    cancellationToken);
                continue;
            }

            var member = new WorkspaceGroupMember(group.Id, desiredUserId);
            await _provisioningRepository.AddGroupMemberAsync(member, cancellationToken);
            membersAdded++;
            await AddGroupMemberAuditAsync(
                workspaceId,
                group,
                desiredUserId,
                PermissionAuditActions.IamGroupMemberAdded,
                null,
                GroupMemberSnapshot(member),
                false,
                cancellationToken);
        }

        foreach (var existing in existingMembers.Where(member => !desiredUserIds.Contains(member.Member.UserId)))
        {
            var before = GroupMemberSnapshot(existing.Member);
            existing.Member.Remove();
            membersRemoved++;
            await AddGroupMemberAuditAsync(
                workspaceId,
                group,
                existing.Member.UserId,
                PermissionAuditActions.IamGroupMemberRemoved,
                before,
                GroupMemberSnapshot(existing.Member),
                renewed: false,
                cancellationToken);
        }

        return new ScimGroupMembershipSyncResult(membersAdded, membersRemoved);
    }

    private Task AddGroupMemberAuditAsync(
        Guid workspaceId,
        WorkspaceGroup group,
        Guid userId,
        string action,
        object? before,
        object? after,
        bool renewed,
        CancellationToken cancellationToken)
    {
        return _auditService.AddAsync(
            CreateAuditEvent(
                workspaceId,
                action,
                before,
                after,
                SubjectTypes.User,
                userId,
                new { provider = ScimProvider, groupId = group.Id, externalGroupId = group.ExternalGroupId, renewed }),
            cancellationToken);
    }

    private async Task EnsureCanDiscoverScimAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (await TryValidateScimBearerTokenAsync(workspaceId, cancellationToken))
        {
            return;
        }

        if (_currentUser.IsAuthenticated)
        {
            await _workspaceAccessService.EnsureCanManageWorkspaceAsync(workspaceId, cancellationToken);
            return;
        }

        throw new ApplicationErrorException(ErrorCodes.Unauthorized, "SCIM authentication is required.");
    }

    private async Task EnsureScimBearerTokenAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (await TryValidateScimBearerTokenAsync(workspaceId, cancellationToken))
        {
            return;
        }

        throw new ApplicationErrorException(ErrorCodes.Unauthorized, "SCIM authentication is required.");
    }

    private async Task<bool> TryValidateScimBearerTokenAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var bearerToken = _bearerTokenAccessor.GetBearerToken();
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return false;
        }

        var tokenHash = _tokenService.HashToken(bearerToken);
        var scimToken = await _scimTokenRepository.GetByTokenHashForUpdateAsync(tokenHash, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (scimToken is null ||
            scimToken.WorkspaceId != workspaceId ||
            !scimToken.IsActive(now))
        {
            return false;
        }

        scimToken.MarkUsed(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureUniqueGroupNameAsync(
        Guid workspaceId,
        string name,
        Guid? exceptGroupId,
        CancellationToken cancellationToken)
    {
        if (await _provisioningRepository.ActiveGroupNameExistsAsync(workspaceId, name, exceptGroupId, cancellationToken))
        {
            throw new ApplicationErrorException(ErrorCodes.Conflict, "A workspace group with this name already exists.");
        }
    }

    private static ScimUserResource ToUserResource(Guid workspaceId, ScimProvisionedUser provisionedUser)
    {
        var user = provisionedUser.User;
        var userName = user.Email ?? user.ExternalSubjectId ?? user.Id.ToString();
        return new ScimUserResource(
            [UserSchema],
            user.Id.ToString(),
            user.ExternalSubjectId,
            userName,
            user.DisplayName,
            new ScimNameDto(user.DisplayName, null, null),
            provisionedUser.WorkspaceMember.Status == WorkspaceMemberStatus.Active,
            new ScimMetaDto(
                "User",
                user.CreatedAt,
                user.UpdatedAt,
                $"/api/v1/workspaces/{workspaceId}/scim/v2/Users/{user.Id}"));
    }

    private static ScimGroupResource ToGroupResource(Guid workspaceId, ScimProvisionedGroup provisionedGroup)
    {
        var group = provisionedGroup.Group;
        var now = DateTimeOffset.UtcNow;
        var members = provisionedGroup.Members
            .Where(member => member.Member.IsActive(now))
            .Select(member => new ScimGroupMemberDto(
                member.User.Id.ToString(),
                member.User.DisplayName))
            .ToArray();

        return new ScimGroupResource(
            [GroupSchema],
            group.Id.ToString(),
            group.ExternalGroupId,
            group.Name,
            members,
            new ScimMetaDto(
                "Group",
                group.CreatedAt,
                group.UpdatedAt,
                $"/api/v1/workspaces/{workspaceId}/scim/v2/Groups/{group.Id}"));
    }

    private static NormalizedScimUser NormalizeUserRequest(CreateScimUserRequest request)
    {
        var userName = NormalizeEmail(request.UserName, "userName");
        var externalId = NormalizeRequired(request.ExternalId, "externalId");
        var displayName = NormalizeDisplayName(request.DisplayName, request.Name, userName);
        return new NormalizedScimUser(userName, externalId, displayName);
    }

    private static NormalizedScimGroup NormalizeGroupRequest(CreateScimGroupRequest request)
    {
        return new NormalizedScimGroup(
            NormalizeRequired(request.ExternalId, "externalId"),
            NormalizeRequired(request.DisplayName, "displayName"),
            request.Members);
    }

    private static NormalizedScimUserPatch NormalizeUserPatch(User user, ScimPatchRequest request)
    {
        var userName = user.Email ?? user.ExternalSubjectId ?? user.Id.ToString();
        var displayName = user.DisplayName;
        foreach (var operation in EnsureOperations(request))
        {
            EnsurePatchOperation(operation.Op);
            ApplyUserPatchOperation(operation, ref userName, ref displayName);
        }

        return new NormalizedScimUserPatch(
            NormalizeEmail(userName, "userName"),
            NormalizeRequired(displayName, "displayName"));
    }

    private static NormalizedScimGroupPatch NormalizeGroupPatch(WorkspaceGroup group, ScimPatchRequest request)
    {
        string? displayName = null;
        IReadOnlyList<ScimGroupMemberDto>? members = null;
        foreach (var operation in EnsureOperations(request))
        {
            EnsurePatchOperation(operation.Op);
            ApplyGroupPatchOperation(operation, ref displayName, ref members);
        }

        return new NormalizedScimGroupPatch(
            displayName is null ? null : NormalizeRequired(displayName, "displayName"),
            members);
    }

    private static void ApplyUserPatchOperation(
        ScimPatchOperationDto operation,
        ref string userName,
        ref string displayName)
    {
        var path = NormalizePatchPath(operation.Path);
        if (path is null)
        {
            if (operation.Value.ValueKind != JsonValueKind.Object)
            {
                throw UnsupportedPatch();
            }

            if (operation.Value.TryGetProperty("userName", out var userNameElement))
            {
                userName = GetString(userNameElement, "userName");
            }

            if (operation.Value.TryGetProperty("displayName", out var displayNameElement))
            {
                displayName = GetString(displayNameElement, "displayName");
            }

            if (operation.Value.TryGetProperty("name", out var nameElement))
            {
                displayName = GetNameDisplay(nameElement);
            }

            if (operation.Value.TryGetProperty("active", out var activeElement))
            {
                EnsureActiveRequest(GetBoolean(activeElement, "active"));
            }

            return;
        }

        switch (path)
        {
            case "username":
                userName = GetString(operation.Value, "userName");
                return;
            case "displayname":
                displayName = GetString(operation.Value, "displayName");
                return;
            case "name":
            case "name.formatted":
                displayName = path == "name"
                    ? GetNameDisplay(operation.Value)
                    : GetString(operation.Value, "name.formatted");
                return;
            case "active":
                EnsureActiveRequest(GetBoolean(operation.Value, "active"));
                return;
            default:
                throw UnsupportedPatch();
        }
    }

    private static void ApplyGroupPatchOperation(
        ScimPatchOperationDto operation,
        ref string? displayName,
        ref IReadOnlyList<ScimGroupMemberDto>? members)
    {
        var path = NormalizePatchPath(operation.Path);
        if (path is null)
        {
            if (operation.Value.ValueKind != JsonValueKind.Object)
            {
                throw UnsupportedPatch();
            }

            if (operation.Value.TryGetProperty("displayName", out var displayNameElement))
            {
                displayName = GetString(displayNameElement, "displayName");
            }

            if (operation.Value.TryGetProperty("members", out var membersElement))
            {
                members = GetMembers(membersElement);
            }

            return;
        }

        switch (path)
        {
            case "displayname":
                displayName = GetString(operation.Value, "displayName");
                return;
            case "members":
                members = GetMembers(operation.Value);
                return;
            default:
                throw UnsupportedPatch();
        }
    }

    private static IReadOnlyList<ScimPatchOperationDto> EnsureOperations(ScimPatchRequest request)
    {
        if (request.Operations is null || request.Operations.Count == 0)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "SCIM PATCH operations are required.");
        }

        return request.Operations;
    }

    private static void EnsurePatchOperation(string? op)
    {
        if (!string.Equals(op, "replace", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(op, "add", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Only SCIM add and replace PATCH operations are supported.");
        }
    }

    private static string? NormalizePatchPath(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? null
            : path.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }

    private static IReadOnlySet<Guid> NormalizeGroupMemberIds(IReadOnlyList<ScimGroupMemberDto> members)
    {
        var userIds = new HashSet<Guid>();
        foreach (var member in members)
        {
            userIds.Add(ParseGuid(member.Value, "member.value"));
        }

        return userIds;
    }

    private static IReadOnlyList<ScimGroupMemberDto> GetMembers(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "members must be an array.");
        }

        return value.EnumerateArray()
            .Select(member =>
            {
                if (member.ValueKind == JsonValueKind.String)
                {
                    return new ScimGroupMemberDto(GetString(member, "member"), null);
                }

                if (member.ValueKind == JsonValueKind.Object &&
                    member.TryGetProperty("value", out var valueElement))
                {
                    var display = member.TryGetProperty("display", out var displayElement) &&
                        displayElement.ValueKind == JsonValueKind.String
                            ? displayElement.GetString()
                            : null;
                    return new ScimGroupMemberDto(GetString(valueElement, "member.value"), display);
                }

                throw new ApplicationErrorException(ErrorCodes.ValidationError, "member value is required.");
            })
            .ToArray();
    }

    private static ScimFilter? ParseFilter(string? filter, params string[] allowedFields)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return null;
        }

        var normalized = filter.Trim();
        var operatorIndex = normalized.IndexOf(" eq ", StringComparison.OrdinalIgnoreCase);
        if (operatorIndex <= 0)
        {
            throw UnsupportedFilter();
        }

        var field = normalized[..operatorIndex].Trim();
        if (!allowedFields.Any(allowed => string.Equals(allowed, field, StringComparison.OrdinalIgnoreCase)))
        {
            throw UnsupportedFilter();
        }

        var value = normalized[(operatorIndex + 4)..].Trim();
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            value = value[1..^1];
        }

        return new ScimFilter(field, value);
    }

    private static PageRequest NormalizePage(int? startIndex, int? count)
    {
        var normalizedStart = Math.Max(1, startIndex ?? 1);
        var normalizedCount = Math.Clamp(count ?? DefaultPageSize, 1, MaxPageSize);
        return new PageRequest(normalizedStart, normalizedStart - 1, normalizedCount);
    }

    private static string NormalizeEmail(string? value, string fieldName)
    {
        return NormalizeRequired(value, fieldName).ToLowerInvariant();
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, $"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string NormalizeDisplayName(string? displayName, ScimNameDto? name, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(name?.Formatted))
        {
            return name.Formatted.Trim();
        }

        return fallback;
    }

    private static string GetNameDisplay(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            return GetString(value, "name");
        }

        if (value.ValueKind == JsonValueKind.Object &&
            value.TryGetProperty("formatted", out var formatted))
        {
            return GetString(formatted, "name.formatted");
        }

        throw new ApplicationErrorException(ErrorCodes.ValidationError, "name.formatted is required.");
    }

    private static string GetString(JsonElement value, string fieldName)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, $"{fieldName} must be a string.");
        }

        return NormalizeRequired(value.GetString(), fieldName);
    }

    private static bool GetBoolean(JsonElement value, string fieldName)
    {
        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, $"{fieldName} must be a boolean.");
        }

        return value.GetBoolean();
    }

    private static void EnsureActiveRequest(bool? active)
    {
        if (active == false)
        {
            throw new ApplicationErrorException(
                ErrorCodes.ValidationError,
                "SCIM user deactivation is not implemented; users are not hard-deleted through SCIM.");
        }
    }

    private static Guid ParseGuid(string value, string fieldName)
    {
        if (!Guid.TryParse(value, out var parsed))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, $"{fieldName} must be a valid identifier.");
        }

        return parsed;
    }

    private static void EnsureScimManaged(WorkspaceGroup group)
    {
        if (!string.Equals(group.ExternalProvider, ScimProvider, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(group.ExternalGroupId))
        {
            throw new ApplicationErrorException(
                ErrorCodes.ValidationError,
                "SCIM cannot mutate local non-SCIM-managed groups.");
        }
    }

    private static ApplicationErrorException UnsupportedFilter()
    {
        return new ApplicationErrorException(ErrorCodes.ValidationError, "Unsupported SCIM filter.");
    }

    private static ApplicationErrorException UnsupportedPatch()
    {
        return new ApplicationErrorException(ErrorCodes.ValidationError, "Unsupported SCIM PATCH operation.");
    }

    private static PermissionAuditEvent CreateAuditEvent(
        Guid workspaceId,
        string action,
        object? before,
        object? after,
        string? subjectType,
        Guid? subjectId,
        object? extraMetadata)
    {
        return new PermissionAuditEvent(
            workspaceId,
            actorId: null,
            action,
            ResourceTypes.Workspace,
            workspaceId,
            subjectType,
            subjectId,
            before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            JsonSerializer.Serialize(new { extraMetadata }, JsonOptions));
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

    private sealed record NormalizedScimUser(
        string UserName,
        string ExternalId,
        string DisplayName);

    private sealed record NormalizedScimUserPatch(
        string UserName,
        string DisplayName);

    private sealed record NormalizedScimGroup(
        string ExternalId,
        string DisplayName,
        IReadOnlyList<ScimGroupMemberDto>? Members);

    private sealed record NormalizedScimGroupPatch(
        string? DisplayName,
        IReadOnlyList<ScimGroupMemberDto>? Members);

    private sealed record ScimFilter(string Field, string Value);

    private sealed record PageRequest(int StartIndex, int Skip, int Take);

    private sealed record ScimGroupMembershipSyncResult(int MembersAdded, int MembersRemoved);
}
