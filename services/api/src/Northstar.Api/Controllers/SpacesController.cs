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
    private readonly ICollectionService _collectionService;
    private readonly IKnowledgeMapService _knowledgeMapService;
    private readonly ISpaceTransferService _spaceTransferService;

    public SpacesController(
        ICollectionService collectionService,
        IKnowledgeMapService knowledgeMapService,
        ISpaceTransferService spaceTransferService)
    {
        _collectionService = collectionService;
        _knowledgeMapService = knowledgeMapService;
        _spaceTransferService = spaceTransferService;
    }

    [HttpGet("{spaceId:guid}/map")]
    [ProducesResponseType(typeof(KnowledgeMapResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<KnowledgeMapResponse>> GetMap(Guid spaceId, CancellationToken cancellationToken)
    {
        return Ok(await _knowledgeMapService.GetMapAsync(spaceId, cancellationToken));
    }

    [HttpPost("{spaceId:guid}/collections")]
    [ProducesResponseType(typeof(CollectionMutationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CollectionMutationResponse>> CreateCollection(
        Guid spaceId,
        CreateCollectionRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _collectionService.CreateAsync(spaceId, request, cancellationToken));
    }

    [HttpPatch("{spaceId:guid}/collections/{collectionId:guid}")]
    [ProducesResponseType(typeof(CollectionMutationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CollectionMutationResponse>> UpdateCollection(
        Guid spaceId,
        Guid collectionId,
        UpdateCollectionRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _collectionService.UpdateAsync(spaceId, collectionId, request, cancellationToken));
    }

    [HttpPut("{spaceId:guid}/collections/order")]
    [ProducesResponseType(typeof(KnowledgeMapResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<KnowledgeMapResponse>> ReorderCollections(
        Guid spaceId,
        ReorderCollectionsRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _collectionService.ReorderAsync(spaceId, request, cancellationToken));
    }

    [HttpDelete("{spaceId:guid}/collections/{collectionId:guid}")]
    [ProducesResponseType(typeof(KnowledgeMapResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<KnowledgeMapResponse>> DeleteCollection(
        Guid spaceId,
        Guid collectionId,
        CancellationToken cancellationToken)
    {
        return Ok(await _collectionService.DeleteAsync(spaceId, collectionId, cancellationToken));
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
