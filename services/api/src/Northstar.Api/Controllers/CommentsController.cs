using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northstar.Application.Knowledge;
using Northstar.Contracts.Knowledge;

namespace Northstar.Api.Controllers;

[ApiController]
[Authorize]
[Route("documents/{documentId:guid}/comments")]
public sealed class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CommentThreadsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommentThreadsResponse>> List(
        Guid documentId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _commentService.ListThreadsAsync(documentId, cancellationToken, GetShareToken(shareToken)));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CommentThreadDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommentThreadDto>> Create(
        Guid documentId,
        CreateCommentThreadRequest request,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _commentService.CreateThreadAsync(documentId, request, cancellationToken, GetShareToken(shareToken)));
    }

    [HttpPost("{threadId:guid}/messages")]
    [ProducesResponseType(typeof(CommentThreadDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommentThreadDto>> AddMessage(
        Guid documentId,
        Guid threadId,
        AddCommentMessageRequest request,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _commentService.AddMessageAsync(documentId, threadId, request, cancellationToken, GetShareToken(shareToken)));
    }

    [HttpPost("{threadId:guid}/resolve")]
    [ProducesResponseType(typeof(CommentThreadDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommentThreadDto>> Resolve(
        Guid documentId,
        Guid threadId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _commentService.ResolveThreadAsync(documentId, threadId, cancellationToken, GetShareToken(shareToken)));
    }

    [HttpPost("{threadId:guid}/reopen")]
    [ProducesResponseType(typeof(CommentThreadDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommentThreadDto>> Reopen(
        Guid documentId,
        Guid threadId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _commentService.ReopenThreadAsync(documentId, threadId, cancellationToken, GetShareToken(shareToken)));
    }

    private string? GetShareToken(string? queryToken)
    {
        if (!string.IsNullOrWhiteSpace(queryToken))
        {
            return queryToken;
        }

        return Request.Headers.TryGetValue("X-Share-Link-Token", out var values)
            ? values.FirstOrDefault()
            : null;
    }
}
