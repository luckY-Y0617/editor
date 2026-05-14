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

    [HttpGet("{documentId:guid}/versions")]
    [ProducesResponseType(typeof(DocumentVersionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentVersionsResponse>> GetVersions(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return Ok(await _documentService.GetVersionsAsync(documentId, cancellationToken));
    }

    [HttpGet("{documentId:guid}/versions/{versionId:guid}")]
    [ProducesResponseType(typeof(DocumentVersionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentVersionResponse>> GetVersion(
        Guid documentId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        return Ok(await _documentService.GetVersionAsync(documentId, versionId, cancellationToken));
    }

    [HttpPost("{documentId:guid}/versions/publish")]
    [ProducesResponseType(typeof(PublishDocumentVersionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PublishDocumentVersionResponse>> PublishVersion(
        Guid documentId,
        PublishDocumentVersionRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _documentService.PublishVersionAsync(documentId, request, cancellationToken));
    }

    [HttpPost("{documentId:guid}/versions/unpublish")]
    [ProducesResponseType(typeof(UnpublishDocumentVersionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UnpublishDocumentVersionResponse>> UnpublishVersion(
        Guid documentId,
        UnpublishDocumentVersionRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _documentService.UnpublishVersionAsync(documentId, request, cancellationToken));
    }

    [HttpPost("{documentId:guid}/versions/compare")]
    [ProducesResponseType(typeof(CompareDocumentVersionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompareDocumentVersionsResponse>> CompareVersions(
        Guid documentId,
        CompareDocumentVersionsRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _documentService.CompareVersionsAsync(documentId, request, cancellationToken));
    }

    [HttpPost("{documentId:guid}/versions/{versionId:guid}/restore")]
    [ProducesResponseType(typeof(RestoreDocumentVersionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RestoreDocumentVersionResponse>> RestoreVersion(
        Guid documentId,
        Guid versionId,
        RestoreDocumentVersionRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _documentService.RestoreVersionAsync(documentId, versionId, request, cancellationToken));
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
