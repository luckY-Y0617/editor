using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Northstar.Application.Bootstrap;
using Northstar.Contracts.Knowledge;

namespace Northstar.Api.Controllers;

[ApiController]
[Authorize]
[Route("bootstrap")]
public sealed class BootstrapController : ControllerBase
{
    private readonly IBootstrapService _bootstrapService;

    public BootstrapController(IBootstrapService bootstrapService)
    {
        _bootstrapService = bootstrapService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(BootstrapResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BootstrapResponse>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _bootstrapService.GetBootstrapAsync(cancellationToken));
    }
}
