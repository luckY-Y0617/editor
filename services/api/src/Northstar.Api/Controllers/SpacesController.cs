using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Northstar.Application.Knowledge;
using Northstar.Contracts.Knowledge;

namespace Northstar.Api.Controllers;

[ApiController]
[Authorize]
[Route("spaces")]
public sealed class SpacesController : ControllerBase
{
    private readonly IKnowledgeMapService _knowledgeMapService;
    private readonly ISpaceTransferService _spaceTransferService;

    public SpacesController(
        IKnowledgeMapService knowledgeMapService,
        ISpaceTransferService spaceTransferService)
    {
        _knowledgeMapService = knowledgeMapService;
        _spaceTransferService = spaceTransferService;
    }

    [HttpGet("{spaceId:guid}/map")]
    [ProducesResponseType(typeof(KnowledgeMapResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<KnowledgeMapResponse>> GetMap(Guid spaceId, CancellationToken cancellationToken)
    {
        return Ok(await _knowledgeMapService.GetMapAsync(spaceId, cancellationToken));
    }

    [HttpGet("{spaceId:guid}/export")]
    [ProducesResponseType(typeof(ExportSpaceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExportSpaceResponse>> Export(
        Guid spaceId,
        [FromQuery] bool includeArchived = true,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _spaceTransferService.ExportAsync(spaceId, includeArchived, cancellationToken));
    }

    [HttpPost("{spaceId:guid}/import")]
    [ProducesResponseType(typeof(ImportSpaceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ImportSpaceResponse>> Import(
        Guid spaceId,
        ImportSpaceRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _spaceTransferService.ImportAsync(spaceId, request, cancellationToken));
    }
}
