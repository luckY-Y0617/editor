using Microsoft.AspNetCore.Mvc;
using Northstar.Application.Security;
using Northstar.Contracts.Security;

namespace Northstar.Api.Controllers;

[ApiController]
[Route("workspaces/{workspaceId:guid}/scim/v2")]
public sealed class ScimController : ControllerBase
{
    private readonly IScimService _scimService;

    public ScimController(IScimService scimService)
    {
        _scimService = scimService;
    }

    [HttpGet("ServiceProviderConfig")]
    [ProducesResponseType(typeof(ScimServiceProviderConfigResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScimServiceProviderConfigResponse>> GetServiceProviderConfig(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _scimService.GetServiceProviderConfigAsync(workspaceId, cancellationToken));
    }

    [HttpGet("Schemas")]
    [ProducesResponseType(typeof(ScimListResponse<ScimSchemaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScimListResponse<ScimSchemaDto>>> GetSchemas(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _scimService.GetSchemasAsync(workspaceId, cancellationToken));
    }

    [HttpGet("ResourceTypes")]
    [ProducesResponseType(typeof(ScimListResponse<ScimResourceTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScimListResponse<ScimResourceTypeDto>>> GetResourceTypes(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _scimService.GetResourceTypesAsync(workspaceId, cancellationToken));
    }

    [HttpGet("Users")]
    [ProducesResponseType(typeof(ScimListResponse<ScimUserResource>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScimListResponse<ScimUserResource>>> GetUsers(
        Guid workspaceId,
        [FromQuery] string? filter,
        [FromQuery] int? startIndex,
        [FromQuery] int? count,
        CancellationToken cancellationToken)
    {
        return Ok(await _scimService.GetUsersAsync(
            workspaceId,
            filter,
            startIndex,
            count,
            cancellationToken));
    }

    [HttpGet("Users/{userId}")]
    [ProducesResponseType(typeof(ScimUserResource), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScimUserResource>> GetUser(
        Guid workspaceId,
        string userId,
        CancellationToken cancellationToken)
    {
        return Ok(await _scimService.GetUserAsync(workspaceId, userId, cancellationToken));
    }

    [HttpPost("Users")]
    [ProducesResponseType(typeof(ScimUserResource), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScimUserResource>> CreateUser(
        Guid workspaceId,
        CreateScimUserRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _scimService.CreateUserAsync(workspaceId, request, cancellationToken));
    }

    [HttpPatch("Users/{userId}")]
    [ProducesResponseType(typeof(ScimUserResource), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScimUserResource>> PatchUser(
        Guid workspaceId,
        string userId,
        ScimPatchRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _scimService.PatchUserAsync(workspaceId, userId, request, cancellationToken));
    }

    [HttpPut("Users/{userId}")]
    [ProducesResponseType(typeof(ScimUserResource), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScimUserResource>> ReplaceUser(
        Guid workspaceId,
        string userId,
        CreateScimUserRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _scimService.ReplaceUserAsync(workspaceId, userId, request, cancellationToken));
    }

    [HttpDelete("Users/{userId}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteUser(
        Guid workspaceId,
        string userId,
        CancellationToken cancellationToken)
    {
        _ = userId;
        await _scimService.RejectUserProvisioningAsync(workspaceId, cancellationToken);
        return NoContent();
    }

    [HttpGet("Groups")]
    [ProducesResponseType(typeof(ScimListResponse<ScimGroupResource>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScimListResponse<ScimGroupResource>>> GetGroups(
        Guid workspaceId,
        [FromQuery] string? filter,
        [FromQuery] int? startIndex,
        [FromQuery] int? count,
        CancellationToken cancellationToken)
    {
        return Ok(await _scimService.GetGroupsAsync(
            workspaceId,
            filter,
            startIndex,
            count,
            cancellationToken));
    }

    [HttpGet("Groups/{groupId}")]
    [ProducesResponseType(typeof(ScimGroupResource), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScimGroupResource>> GetGroup(
        Guid workspaceId,
        string groupId,
        CancellationToken cancellationToken)
    {
        return Ok(await _scimService.GetGroupAsync(workspaceId, groupId, cancellationToken));
    }

    [HttpPost("Groups")]
    [ProducesResponseType(typeof(ScimGroupResource), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScimGroupResource>> CreateGroup(
        Guid workspaceId,
        CreateScimGroupRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _scimService.CreateGroupAsync(workspaceId, request, cancellationToken));
    }

    [HttpPatch("Groups/{groupId}")]
    [ProducesResponseType(typeof(ScimGroupResource), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScimGroupResource>> PatchGroup(
        Guid workspaceId,
        string groupId,
        ScimPatchRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _scimService.PatchGroupAsync(workspaceId, groupId, request, cancellationToken));
    }

    [HttpPut("Groups/{groupId}")]
    [ProducesResponseType(typeof(ScimGroupResource), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScimGroupResource>> ReplaceGroup(
        Guid workspaceId,
        string groupId,
        CreateScimGroupRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _scimService.ReplaceGroupAsync(workspaceId, groupId, request, cancellationToken));
    }

    [HttpDelete("Groups/{groupId}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteGroup(
        Guid workspaceId,
        string groupId,
        CancellationToken cancellationToken)
    {
        _ = groupId;
        await _scimService.RejectGroupProvisioningAsync(workspaceId, cancellationToken);
        return NoContent();
    }
}
