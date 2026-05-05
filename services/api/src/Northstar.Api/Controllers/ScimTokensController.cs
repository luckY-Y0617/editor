using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northstar.Application.Security;
using Northstar.Contracts.Security;

namespace Northstar.Api.Controllers;

[Authorize]
[ApiController]
[Route("workspaces/{workspaceId:guid}/scim/tokens")]
public sealed class ScimTokensController : ControllerBase
{
    private readonly IScimTokenService _scimTokenService;

    public ScimTokensController(IScimTokenService scimTokenService)
    {
        _scimTokenService = scimTokenService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateScimTokenResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreateScimTokenResponse>> Create(
        Guid workspaceId,
        CreateScimTokenRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _scimTokenService.CreateAsync(workspaceId, request, cancellationToken));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ScimTokensResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScimTokensResponse>> Get(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _scimTokenService.GetAsync(workspaceId, cancellationToken));
    }

    [HttpDelete("{tokenId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke(
        Guid workspaceId,
        Guid tokenId,
        CancellationToken cancellationToken)
    {
        await _scimTokenService.RevokeAsync(workspaceId, tokenId, cancellationToken);
        return NoContent();
    }
}
