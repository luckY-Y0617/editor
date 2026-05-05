using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Workspaces;
using Northstar.Domain.Workspaces;

namespace Northstar.Application.Workspaces;

public sealed class WorkspaceMembersService : IWorkspaceMembersService
{
    private readonly IWorkspaceMemberRepository _repository;
    private readonly IWorkspaceAccessService _accessService;
    private readonly IAuthStepUpService _stepUpService;
    private readonly IUnitOfWork _unitOfWork;

    public WorkspaceMembersService(
        IWorkspaceMemberRepository repository,
        IWorkspaceAccessService accessService,
        IAuthStepUpService stepUpService,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _accessService = accessService;
        _stepUpService = stepUpService;
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkspaceMembersResponse> GetMembersAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        await EnsureWorkspaceExistsAsync(workspaceId, cancellationToken);
        await _accessService.EnsureCanManageWorkspaceAsync(workspaceId, cancellationToken);

        var members = await _repository.GetMembersAsync(workspaceId, cancellationToken);
        return new WorkspaceMembersResponse(members.Select(ToDto).ToArray());
    }

    public async Task<WorkspaceMemberDto> AddMemberAsync(
        Guid workspaceId,
        AddWorkspaceMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureWorkspaceExistsAsync(workspaceId, cancellationToken);
        await _accessService.EnsureCanManageWorkspaceAsync(workspaceId, cancellationToken);
        await _stepUpService.EnsureSatisfiedAsync(cancellationToken);

        var role = ValidateRole(request.Role);
        var email = NormalizeEmail(request.Email);
        var user = await _repository.FindUserByEmailAsync(email, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.ValidationError, "User email was not found.");

        var existing = await _repository.GetMemberAsync(workspaceId, user.Id, cancellationToken);
        if (existing is not null)
        {
            throw new ApplicationErrorException(ErrorCodes.Conflict, "User is already a workspace member.");
        }

        await _repository.AddMemberAsync(new WorkspaceMember(workspaceId, user.Id, role), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var added = await _repository.GetMemberAsync(workspaceId, user.Id, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Workspace member was not found after creation.");

        return new WorkspaceMemberDto(
            user.Id.ToString(),
            user.Email,
            user.DisplayName,
            added.Role,
            added.Status,
            added.JoinedAt);
    }

    public async Task<WorkspaceMemberDto> UpdateMemberAsync(
        Guid workspaceId,
        Guid userId,
        UpdateWorkspaceMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureWorkspaceExistsAsync(workspaceId, cancellationToken);
        await _accessService.EnsureCanManageWorkspaceAsync(workspaceId, cancellationToken);
        await _stepUpService.EnsureSatisfiedAsync(cancellationToken);

        var role = ValidateRole(request.Role);
        var member = await _repository.GetMemberAsync(workspaceId, userId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Workspace member was not found.");

        if (member.Role == WorkspaceMemberRole.Owner &&
            role != WorkspaceMemberRole.Owner &&
            await _repository.CountOwnersAsync(workspaceId, cancellationToken) <= 1)
        {
            throw new ApplicationErrorException(ErrorCodes.Conflict, "The last workspace owner cannot be downgraded.");
        }

        member.ChangeRole(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var members = await _repository.GetMembersAsync(workspaceId, cancellationToken);
        var updated = members.Single(member => member.UserId == userId);
        return ToDto(updated);
    }

    public async Task RemoveMemberAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureWorkspaceExistsAsync(workspaceId, cancellationToken);
        await _accessService.EnsureCanManageWorkspaceAsync(workspaceId, cancellationToken);
        await _stepUpService.EnsureSatisfiedAsync(cancellationToken);

        var member = await _repository.GetMemberAsync(workspaceId, userId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Workspace member was not found.");

        if (member.Role == WorkspaceMemberRole.Owner &&
            await _repository.CountOwnersAsync(workspaceId, cancellationToken) <= 1)
        {
            throw new ApplicationErrorException(ErrorCodes.Conflict, "The last workspace owner cannot be removed.");
        }

        _repository.RemoveMember(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureWorkspaceExistsAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        if (!await _repository.WorkspaceExistsAsync(workspaceId, cancellationToken))
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Workspace was not found.");
        }
    }

    private static WorkspaceMemberDto ToDto(WorkspaceMemberReadModel member)
    {
        return new WorkspaceMemberDto(
            member.UserId.ToString(),
            member.Email,
            member.DisplayName,
            member.Role,
            member.Status,
            member.JoinedAt);
    }

    private static string ValidateRole(string role)
    {
        var normalized = role.Trim().ToLowerInvariant();
        return WorkspaceMemberRole.IsValid(normalized)
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "Workspace member role is invalid.");
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Email is required.");
        }

        return email.Trim().ToLowerInvariant();
    }
}
