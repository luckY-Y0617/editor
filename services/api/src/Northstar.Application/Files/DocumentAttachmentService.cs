using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Files;
using Northstar.Domain.Files;
using Northstar.Domain.Security;

namespace Northstar.Application.Files;

public sealed class DocumentAttachmentService : IDocumentAttachmentService
{
    private readonly IFileRepository _fileRepository;
    private readonly IScopedResourceAccessService _scopedAccessService;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;

    public DocumentAttachmentService(
        IFileRepository fileRepository,
        IScopedResourceAccessService scopedAccessService,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork)
    {
        _fileRepository = fileRepository;
        _scopedAccessService = scopedAccessService;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
    }

    public async Task<DocumentAttachmentsResponse> GetAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var location = await GetActiveDocumentLocationAsync(documentId, cancellationToken);
        await _scopedAccessService.EnsureCanAccessDocumentAnyAsync(
            documentId,
            [PermissionActions.AttachmentView, PermissionActions.DocumentView],
            cancellationToken);
        var attachments = await _fileRepository.GetDocumentAttachmentsAsync(documentId, cancellationToken);
        return new DocumentAttachmentsResponse(attachments);
    }

    public Task<DocumentAttachmentDto> AttachAsync(
        Guid documentId,
        AttachFileToDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var location = await GetActiveDocumentLocationAsync(documentId, ct);
            await _scopedAccessService.EnsureCanAccessDocumentAnyAsync(
                documentId,
                [PermissionActions.AttachmentCreate, PermissionActions.DocumentEdit],
                ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);
            var fileId = ParseGuid(request.FileId, "fileId");
            var file = await _fileRepository.GetFileAsync(fileId, cancellationToken: ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "File was not found.");
            if (file.WorkspaceId != location.WorkspaceId)
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "File and document must belong to the same workspace.");
            }

            var relationType = NormalizeRelationType(request.RelationType);
            var existing = await _fileRepository.FindDocumentAttachmentDtoAsync(
                documentId,
                fileId,
                relationType,
                ct);
            if (existing is not null)
            {
                return existing;
            }

            var attachment = new DocumentAttachment(
                location.WorkspaceId,
                documentId,
                fileId,
                relationType,
                FileDtoMapper.ToJson(request.Metadata),
                actorId);
            await _fileRepository.AddDocumentAttachmentAsync(attachment, ct);
            await _fileRepository.AddOutboxEventAsync(FileOutboxFactory.DocumentAttachmentCreated(attachment), ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return await _fileRepository.GetDocumentAttachmentAsync(documentId, attachment.Id, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Attachment was not found after creation.");
        }, cancellationToken);
    }

    public Task DeleteAsync(
        Guid documentId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var location = await GetActiveDocumentLocationAsync(documentId, ct);
            await _scopedAccessService.EnsureCanAccessDocumentAnyAsync(
                documentId,
                [PermissionActions.AttachmentDelete, PermissionActions.DocumentEdit],
                ct);
            var attachment = await _fileRepository.GetDocumentAttachmentEntityAsync(documentId, attachmentId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Attachment was not found.");

            _fileRepository.RemoveDocumentAttachment(attachment);
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }

    private async Task<DocumentLocation> GetActiveDocumentLocationAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var location = await _fileRepository.GetDocumentLocationAsync(documentId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
        if (location.IsDeleted)
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
        }

        return location;
    }

    private static Guid ParseGuid(string value, string fieldName)
    {
        return Guid.TryParse(value, out var id)
            ? id
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, $"{fieldName} must be a valid UUID.");
    }

    private static string NormalizeRelationType(string relationType)
    {
        var value = string.IsNullOrWhiteSpace(relationType)
            ? DocumentAttachmentRelationType.Attachment
            : relationType.Trim();
        if (!DocumentAttachmentRelationType.IsValid(value))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "relationType is invalid.");
        }

        return value;
    }
}
