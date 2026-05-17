using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Northstar.Api.Security;
using Northstar.Application.Security;
using Northstar.Contracts.Security;

namespace Northstar.Api.Controllers;

[AllowAnonymous]
[ApiController]
[EnableRateLimiting(PublicShareRateLimitPolicyNames.PublicShareLinks)]
[Route("public/share-links")]
public sealed class PublicShareLinksController : ControllerBase
{
    private const string PasswordHeaderName = "X-Share-Link-Password";

    private readonly IShareLinkService _shareLinkService;

    public PublicShareLinksController(IShareLinkService shareLinkService)
    {
        _shareLinkService = shareLinkService;
    }

    [HttpGet("{token}/resolve")]
    [ProducesResponseType(typeof(ResolvePublicShareLinkResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResolvePublicShareLinkResponse>> Resolve(
        string token,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkService.ResolvePublicShareLinkAsync(
            token,
            GetPasswordProof(),
            cancellationToken));
    }

    [HttpGet("{token}/document")]
    [ProducesResponseType(typeof(PublicShareDocumentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicShareDocumentResponse>> GetDocument(
        string token,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkService.GetPublicShareDocumentAsync(
            token,
            GetPasswordProof(),
            cancellationToken));
    }

    [HttpGet("{token}/tree")]
    [ProducesResponseType(typeof(PublicShareTreeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicShareTreeResponse>> GetTree(
        string token,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkService.GetPublicShareTreeAsync(
            token,
            GetPasswordProof(),
            cancellationToken));
    }

    [HttpGet("{token}/documents/{documentId:guid}")]
    [ProducesResponseType(typeof(PublicShareDocumentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicShareDocumentResponse>> GetScopedDocument(
        string token,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkService.GetPublicShareDocumentAsync(
            token,
            documentId,
            GetPasswordProof(),
            cancellationToken));
    }

    [HttpGet("{token}/collection")]
    [ProducesResponseType(typeof(PublicShareCollectionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicShareCollectionResponse>> GetCollection(
        string token,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkService.GetPublicShareCollectionAsync(
            token,
            GetPasswordProof(),
            cancellationToken));
    }

    private string? GetPasswordProof()
    {
        return Request.Headers.TryGetValue(PasswordHeaderName, out var value)
            ? value.FirstOrDefault()
            : null;
    }
}
