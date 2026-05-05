using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Northstar.Application.Knowledge;
using Northstar.Contracts.Knowledge;

namespace Northstar.Api.Controllers;

[ApiController]
[Authorize]
[Route("search")]
public sealed class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResponse>> Search(
        [FromQuery] string? q,
        [FromQuery] Guid spaceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _searchService.SearchAsync(q, spaceId, cancellationToken));
    }
}
