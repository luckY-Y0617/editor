using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northstar.Application.Workspaces;
using Northstar.Contracts.Workspaces;

namespace Northstar.Api.Controllers;

[Authorize]
[ApiController]
[Route("workspaces")]
public sealed class WorkspacesController : ControllerBase
{
    private readonly IWorkspaceMembersService _membersService;
    private readonly IWorkspaceGroupService _groupService;
    private readonly IIamSyncService _iamSyncService;

    public WorkspacesController(
        IWorkspaceMembersService membersService,
        IWorkspaceGroupService groupService,
        IIamSyncService iamSyncService)
    {
        _membersService = membersService;
        _groupService = groupService;
        _iamSyncService = iamSyncService;
    }

    [HttpGet("{workspaceId:guid}/members")]
    [ProducesResponseType(typeof(WorkspaceMembersResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkspaceMembersResponse>> GetMembers(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _membersService.GetMembersAsync(workspaceId, cancellationToken));
    }

    [HttpPost("{workspaceId:guid}/members")]
    [ProducesResponseType(typeof(WorkspaceMemberDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkspaceMemberDto>> AddMember(
        Guid workspaceId,
        AddWorkspaceMemberRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _membersService.AddMemberAsync(workspaceId, request, cancellationToken));
    }

    [HttpPatch("{workspaceId:guid}/members/{userId:guid}")]
    [ProducesResponseType(typeof(WorkspaceMemberDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkspaceMemberDto>> UpdateMember(
        Guid workspaceId,
        Guid userId,
        UpdateWorkspaceMemberRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _membersService.UpdateMemberAsync(workspaceId, userId, request, cancellationToken));
    }

    [HttpDelete("{workspaceId:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMember(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _membersService.RemoveMemberAsync(workspaceId, userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{workspaceId:guid}/groups")]
    [ProducesResponseType(typeof(WorkspaceGroupsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkspaceGroupsResponse>> GetGroups(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _groupService.GetGroupsAsync(workspaceId, cancellationToken));
    }

    [HttpPost("{workspaceId:guid}/groups")]
    [ProducesResponseType(typeof(WorkspaceGroupDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkspaceGroupDto>> CreateGroup(
        Guid workspaceId,
        CreateWorkspaceGroupRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _groupService.CreateGroupAsync(workspaceId, request, cancellationToken));
    }

    [HttpGet("{workspaceId:guid}/groups/{groupId:guid}")]
    [ProducesResponseType(typeof(WorkspaceGroupDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkspaceGroupDetailDto>> GetGroup(
        Guid workspaceId,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        return Ok(await _groupService.GetGroupAsync(workspaceId, groupId, cancellationToken));
    }

    [HttpPatch("{workspaceId:guid}/groups/{groupId:guid}")]
    [ProducesResponseType(typeof(WorkspaceGroupDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkspaceGroupDto>> UpdateGroup(
        Guid workspaceId,
        Guid groupId,
        UpdateWorkspaceGroupRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _groupService.UpdateGroupAsync(workspaceId, groupId, request, cancellationToken));
    }

    [HttpDelete("{workspaceId:guid}/groups/{groupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ArchiveGroup(
        Guid workspaceId,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        await _groupService.ArchiveGroupAsync(workspaceId, groupId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{workspaceId:guid}/groups/{groupId:guid}/members")]
    [ProducesResponseType(typeof(WorkspaceGroupMemberDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkspaceGroupMemberDto>> AddGroupMember(
        Guid workspaceId,
        Guid groupId,
        AddWorkspaceGroupMemberRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _groupService.AddMemberAsync(workspaceId, groupId, request, cancellationToken));
    }

    [HttpDelete("{workspaceId:guid}/groups/{groupId:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveGroupMember(
        Guid workspaceId,
        Guid groupId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _groupService.RemoveMemberAsync(workspaceId, groupId, userId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{workspaceId:guid}/iam/sync")]
    [ProducesResponseType(typeof(IamSyncResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<IamSyncResponse>> SyncIam(
        Guid workspaceId,
        IamSyncRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _iamSyncService.SyncAsync(workspaceId, request, cancellationToken));
    }
}
