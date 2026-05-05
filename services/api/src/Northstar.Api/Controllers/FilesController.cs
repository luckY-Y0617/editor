using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northstar.Application.Common;
using Northstar.Application.Files;
using Northstar.Contracts.Common;
using Northstar.Contracts.Files;

namespace Northstar.Api.Controllers;

[ApiController]
[Authorize]
[Route("files")]
public sealed class FilesController : ControllerBase
{
    private readonly IUploadSessionService _uploadSessionService;
    private readonly IFileService _fileService;

    public FilesController(
        IUploadSessionService uploadSessionService,
        IFileService fileService)
    {
        _uploadSessionService = uploadSessionService;
        _fileService = fileService;
    }

    [HttpPost("uploads/sessions")]
    [ProducesResponseType(typeof(CreateUploadSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreateUploadSessionResponse>> CreateUploadSession(
        CreateUploadSessionRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _uploadSessionService.CreateAsync(request, cancellationToken));
    }

    [HttpGet("uploads/sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(UploadSessionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UploadSessionDto>> GetUploadSession(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        return Ok(await _uploadSessionService.GetAsync(sessionId, cancellationToken));
    }

    [HttpPut("uploads/sessions/{sessionId:guid}/content")]
    [ProducesResponseType(typeof(UploadSessionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UploadSessionDto>> UploadContent(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        return Ok(await _uploadSessionService.UploadContentAsync(sessionId, Request.Body, cancellationToken));
    }

    [HttpPost("uploads/sessions/{sessionId:guid}/complete")]
    [ProducesResponseType(typeof(UploadSessionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UploadSessionDto>> CompleteUploadSession(
        Guid sessionId,
        CompleteUploadSessionRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _uploadSessionService.CompleteAsync(sessionId, request, cancellationToken));
    }

    [HttpPost("uploads/sessions/{sessionId:guid}/finalize")]
    [ProducesResponseType(typeof(FinalizeUploadSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinalizeUploadSessionResponse>> FinalizeUploadSession(
        Guid sessionId,
        FinalizeUploadSessionRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _uploadSessionService.FinalizeAsync(sessionId, request, cancellationToken));
    }

    [HttpGet("uploads/sessions/{sessionId:guid}/progress")]
    [ProducesResponseType(typeof(UploadSessionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UploadSessionDto>> GetUploadSessionProgress(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        return Ok(await _uploadSessionService.GetProgressAsync(sessionId, cancellationToken));
    }

    [HttpPost("uploads/sessions/{sessionId:guid}/abort")]
    [ProducesResponseType(typeof(UploadSessionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UploadSessionDto>> AbortUploadSession(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        return Ok(await _uploadSessionService.AbortAsync(sessionId, cancellationToken));
    }

    [HttpPost("uploads/sessions/{sessionId:guid}/parts/presign")]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult PresignUploadParts(Guid sessionId)
    {
        throw new ApplicationErrorException(
            ErrorCodes.ValidationError,
            "Multipart upload is not enabled in Phase 6.");
    }

    [HttpGet("{fileId:guid}")]
    [ProducesResponseType(typeof(FileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FileDto>> GetFile(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        return Ok(await _fileService.GetAsync(fileId, cancellationToken));
    }

    [HttpGet("{fileId:guid}/content")]
    public async Task<IActionResult> GetFileContent(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var result = await _fileService.OpenContentAsync(fileId, cancellationToken);
        return File(result.Content, result.File.MimeType, result.File.OriginalFilename, enableRangeProcessing: true);
    }

    [HttpDelete("{fileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteFile(Guid fileId, CancellationToken cancellationToken)
    {
        await _fileService.DeleteAsync(fileId, cancellationToken);
        return NoContent();
    }
}
