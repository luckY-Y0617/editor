using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Northstar.Application.Files;
using Northstar.Application.Knowledge;
using Northstar.Contracts.Files;
using Northstar.Contracts.Knowledge;

namespace Northstar.Api.Controllers;

[ApiController]
[Authorize]
[Route("documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentContextService _contextService;
    private readonly IDocumentActivityService _activityService;
    private readonly IDocumentAttachmentService _attachmentService;

    public DocumentsController(
        IDocumentService documentService,
        IDocumentContextService contextService,
        IDocumentActivityService activityService,
        IDocumentAttachmentService attachmentService)
    {
        _documentService = documentService;
        _contextService = contextService;
        _activityService = activityService;
        _attachmentService = attachmentService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateDocumentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreateDocumentResponse>> Create(
        CreateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _documentService.CreateAsync(request, cancellationToken));
    }

    [HttpGet("{documentId:guid}")]
    [ProducesResponseType(typeof(GetDocumentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetDocumentResponse>> Get(
        Guid documentId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _documentService.GetAsync(documentId, cancellationToken, GetShareToken(shareToken)));
    }

    [HttpPatch("{documentId:guid}")]
    [ProducesResponseType(typeof(UpdateDocumentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UpdateDocumentResponse>> Update(
        Guid documentId,
        UpdateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _documentService.UpdateAsync(documentId, request, cancellationToken));
    }

    [HttpPatch("{documentId:guid}/location")]
    [ProducesResponseType(typeof(MoveDocumentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MoveDocumentResponse>> Move(
        Guid documentId,
        MoveDocumentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _documentService.MoveAsync(documentId, request, cancellationToken));
    }

    [HttpPatch("{documentId:guid}/archive")]
    [ProducesResponseType(typeof(MoveDocumentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MoveDocumentResponse>> Archive(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return Ok(await _documentService.ArchiveAsync(documentId, cancellationToken));
    }

    [HttpPatch("{documentId:guid}/restore")]
    [ProducesResponseType(typeof(MoveDocumentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MoveDocumentResponse>> Restore(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return Ok(await _documentService.RestoreAsync(documentId, cancellationToken));
    }

    [HttpDelete("{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid documentId, CancellationToken cancellationToken)
    {
        await _documentService.DeleteAsync(documentId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{documentId:guid}/context")]
    [ProducesResponseType(typeof(DocumentContextResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentContextResponse>> GetContext(
        Guid documentId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _contextService.GetAsync(documentId, cancellationToken, GetShareToken(shareToken)));
    }

    [HttpGet("{documentId:guid}/activity")]
    [ProducesResponseType(typeof(DocumentActivityResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentActivityResponse>> GetActivity(
        Guid documentId,
        [FromQuery] string? shareToken,
        CancellationToken cancellationToken)
    {
        return Ok(await _activityService.GetAsync(documentId, cancellationToken, GetShareToken(shareToken)));
    }

    [HttpGet("{documentId:guid}/attachments")]
    [ProducesResponseType(typeof(DocumentAttachmentsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentAttachmentsResponse>> GetAttachments(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return Ok(await _attachmentService.GetAsync(documentId, cancellationToken));
    }

    [HttpPost("{documentId:guid}/attachments")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentAttachmentDto>> AttachFile(
        Guid documentId,
        AttachFileToDocumentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _attachmentService.AttachAsync(documentId, request, cancellationToken));
    }

    [HttpDelete("{documentId:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAttachment(
        Guid documentId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        await _attachmentService.DeleteAsync(documentId, attachmentId, cancellationToken);
        return NoContent();
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
