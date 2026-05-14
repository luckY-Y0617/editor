using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Files;
using Northstar.Domain.Security;

namespace Northstar.Application.Files;

public sealed class FileService : IFileService
{
    private readonly IFileRepository _fileRepository;
    private readonly IObjectStorage _objectStorage;
    private readonly IWorkspaceAccessService _accessService;
    private readonly IScopedResourceAccessService _scopedAccessService;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;

    public FileService(
        IFileRepository fileRepository,
        IObjectStorage objectStorage,
        IWorkspaceAccessService accessService,
        IScopedResourceAccessService scopedAccessService,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork)
    {
        _fileRepository = fileRepository;
        _objectStorage = objectStorage;
        _accessService = accessService;
        _scopedAccessService = scopedAccessService;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
    }

    public async Task<FileDto> GetAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetFileAsync(fileId, cancellationToken: cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "File was not found.");
        await EnsureCanAccessFileAsync(
            file.Id,
            file.WorkspaceId,
            PermissionActions.FileDownload,
            [PermissionActions.FileDownload, PermissionActions.AttachmentView, PermissionActions.DocumentView],
            cancellationToken);
        return FileDtoMapper.ToDto(file);
    }

    public async Task<FileContentResult> OpenContentAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetFileAsync(fileId, cancellationToken: cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "File was not found.");
        await EnsureCanAccessFileAsync(
            file.Id,
            file.WorkspaceId,
            PermissionActions.FileDownload,
            [PermissionActions.FileDownload, PermissionActions.AttachmentView, PermissionActions.DocumentView],
            cancellationToken);
        var stream = await _objectStorage.OpenReadAsync(file, cancellationToken);
        return new FileContentResult(FileDtoMapper.ToDto(file), stream);
    }

    public Task DeleteAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var file = await _fileRepository.GetFileAsync(fileId, includeDeleted: true, cancellationToken: ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "File was not found.");
            if (file.DeletedAt.HasValue)
            {
                return true;
            }

            await EnsureCanAccessFileAsync(
                file.Id,
                file.WorkspaceId,
                PermissionActions.FileDelete,
                [PermissionActions.FileDelete, PermissionActions.AttachmentDelete, PermissionActions.DocumentEdit],
                ct);
            var activeAttachmentCount = await _fileRepository.CountActiveAttachmentsAsync(file.Id, ct);
            if (activeAttachmentCount > 0)
            {
                throw new ApplicationErrorException(ErrorCodes.Conflict, "File still has active document attachments.");
            }

            if (file.Delete())
            {
                await _fileRepository.AddOutboxEventAsync(FileOutboxFactory.FileDeleted(file), ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            return true;
        }, cancellationToken);
    }

    private async Task EnsureCanAccessFileAsync(
        Guid fileId,
        Guid workspaceId,
        string workspaceAction,
        IReadOnlyList<string> attachedDocumentActions,
        CancellationToken cancellationToken)
    {
        var documentIds = await _fileRepository.GetActiveAttachmentDocumentIdsAsync(fileId, cancellationToken);
        if (documentIds.Count == 0)
        {
            await _accessService.EnsureCanAccessWorkspaceAsync(workspaceId, workspaceAction, cancellationToken);
            return;
        }

        ApplicationErrorException? lastError = null;
        foreach (var documentId in documentIds)
        {
            try
            {
                await _scopedAccessService.EnsureCanAccessDocumentAnyAsync(
                    documentId,
                    attachedDocumentActions,
                    cancellationToken);
                return;
            }
            catch (ApplicationErrorException error) when (
                error.Code is ErrorCodes.Forbidden or ErrorCodes.NotFound)
            {
                lastError = error;
            }
        }

        throw lastError ?? new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace permission is insufficient.");
    }
}
