using System.Text.Json;
using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Domain.Files;

namespace Northstar.Application.Files;

public sealed class FileReferenceService : IFileReferenceService
{
    private readonly IFileReferenceExtractor _extractor;
    private readonly IFileRepository _fileRepository;

    public FileReferenceService(
        IFileReferenceExtractor extractor,
        IFileRepository fileRepository)
    {
        _extractor = extractor;
        _fileRepository = fileRepository;
    }

    public async Task<bool> ValidateAndSyncDocumentReferencesAsync(
        Guid documentId,
        Guid workspaceId,
        JsonElement content,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var references = _extractor.Extract(content);
        if (references.Count == 0)
        {
            return false;
        }

        var changed = false;
        foreach (var reference in references)
        {
            var file = await _fileRepository.GetFileAsync(reference.FileId, cancellationToken: cancellationToken)
                ?? throw new ApplicationErrorException(
                    ErrorCodes.ValidationError,
                    $"Referenced file '{reference.FileId}' was not found.");

            if (file.WorkspaceId != workspaceId)
            {
                throw new ApplicationErrorException(
                    ErrorCodes.ValidationError,
                    $"Referenced file '{reference.FileId}' does not belong to this document workspace.");
            }

            var existing = await _fileRepository.FindDocumentAttachmentAsync(
                documentId,
                reference.FileId,
                reference.RelationType,
                cancellationToken);
            if (existing is not null)
            {
                continue;
            }

            var attachment = new DocumentAttachment(
                workspaceId,
                documentId,
                reference.FileId,
                reference.RelationType,
                metadata: null,
                createdBy: actorId);
            await _fileRepository.AddDocumentAttachmentAsync(attachment, cancellationToken);
            await _fileRepository.AddOutboxEventAsync(
                FileOutboxFactory.DocumentAttachmentCreated(attachment),
                cancellationToken);
            changed = true;
        }

        return changed;
    }
}
