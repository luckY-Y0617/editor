using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northstar.Application.Security;
using Northstar.Contracts.Security;

namespace Northstar.Api.Controllers;

[Authorize]
[ApiController]
[Route("share-links")]
public sealed class ShareLinksController : ControllerBase
{
    private readonly IShareLinkService _shareLinkService;

    public ShareLinksController(IShareLinkService shareLinkService)
    {
        _shareLinkService = shareLinkService;
    }

    [HttpGet("{token}/resolve")]
    [ProducesResponseType(typeof(ResolveShareLinkResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResolveShareLinkResponse>> Resolve(
        string token,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkService.ResolveShareLinkAsync(token, cancellationToken));
    }
}
