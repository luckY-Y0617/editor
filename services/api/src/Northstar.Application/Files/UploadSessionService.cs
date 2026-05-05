using System.Globalization;
using System.Text.Json;
using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Files;
using Northstar.Domain.Files;

namespace Northstar.Application.Files;

public sealed class UploadSessionService : IUploadSessionService
{
    private readonly IFileRepository _fileRepository;
    private readonly IObjectStorage _objectStorage;
    private readonly IWorkspaceAccessService _accessService;
    private readonly IResourceWorkspaceResolver _workspaceResolver;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FilesOptions _options;

    public UploadSessionService(
        IFileRepository fileRepository,
        IObjectStorage objectStorage,
        IWorkspaceAccessService accessService,
        IResourceWorkspaceResolver workspaceResolver,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork,
        FilesOptions options)
    {
        _fileRepository = fileRepository;
        _objectStorage = objectStorage;
        _accessService = accessService;
        _workspaceResolver = workspaceResolver;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
        _options = options;
    }

    public Task<CreateUploadSessionResponse> CreateAsync(
        CreateUploadSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            ValidateCreateRequest(request);
            var workspaceId = await ResolveWorkspaceForCreateAsync(request, ct);
            await _accessService.EnsureCanEditWorkspaceAsync(workspaceId, ct);
            var actorId = await _accessService.GetRequiredUserIdAsync(ct);

            var idempotencyKey = request.IdempotencyKey.Trim();
            var existing = await _fileRepository.GetUploadSessionByIdempotencyKeyAsync(workspaceId, idempotencyKey, ct);
            if (existing is not null)
            {
                return ToCreateResponse(existing);
            }

            var sessionId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var objectKey = $"workspaces/{workspaceId}/files/{now:yyyy}/{now:MM}/{sessionId:N}";
            var session = new UploadSession(
                workspaceId,
                actorId,
                idempotencyKey,
                request.OriginalFilename.Trim(),
                request.MimeType.Trim().ToLowerInvariant(),
                request.ByteSize,
                _options.StorageProvider,
                _options.DefaultBucket,
                objectKey,
                now.AddMinutes(_options.UploadSessionMinutes),
                request.ChecksumSha256,
                request.BizType,
                UploadMode.Single,
                sessionId);

            await _fileRepository.AddUploadSessionAsync(session, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ToCreateResponse(session);
        }, cancellationToken);
    }

    public async Task<UploadSessionDto> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAndEnsureManageAsync(sessionId, cancellationToken);
        await ExpireIfNeededAsync(session, cancellationToken);
        return FileDtoMapper.ToDto(session);
    }

    public Task<UploadSessionDto> UploadContentAsync(
        Guid sessionId,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var session = await GetSessionAndEnsureManageAsync(sessionId, ct);
            await EnsureNotExpiredAsync(session, ct);
            session.MarkUploading();
            await _objectStorage.WriteUploadContentAsync(session, content, _options.MaxFileBytes, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return FileDtoMapper.ToDto(session);
        }, cancellationToken);
    }

    public Task<UploadSessionDto> CompleteAsync(
        Guid sessionId,
        CompleteUploadSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var session = await GetSessionAndEnsureManageAsync(sessionId, ct);
            await EnsureNotExpiredAsync(session, ct);
            var objectInfo = await _objectStorage.GetObjectInfoAsync(session, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.ValidationError, "Uploaded content was not found.");

            if (objectInfo.ByteSize != session.ByteSize)
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "Uploaded byte size does not match the session.");
            }

            if (!string.IsNullOrWhiteSpace(session.ChecksumSha256) &&
                !string.Equals(objectInfo.ChecksumSha256, session.ChecksumSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "Uploaded checksum does not match the session.");
            }

            session.Complete();
            await _unitOfWork.SaveChangesAsync(ct);
            return FileDtoMapper.ToDto(session);
        }, cancellationToken);
    }

    public Task<FinalizeUploadSessionResponse> FinalizeAsync(
        Guid sessionId,
        FinalizeUploadSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var session = await GetSessionAndEnsureManageAsync(sessionId, ct);
            await EnsureNotExpiredAsync(session, ct);

            if (session.Status == UploadSessionStatus.Finalized)
            {
                return await ToFinalizedResponseAsync(session, request, ct);
            }

            if (session.Status != UploadSessionStatus.Completed)
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "Only completed upload sessions can be finalized.");
            }

            var actorId = await _accessService.GetRequiredUserIdAsync(ct);
            var file = new StoredFile(
                session.WorkspaceId,
                actorId,
                session.StorageProvider,
                session.Bucket,
                session.ObjectKey,
                session.OriginalFilename,
                session.MimeType,
                session.ByteSize,
                session.ChecksumSha256);

            await _fileRepository.AddFileAsync(file, ct);
            session.Finalize(file.Id);
            await _fileRepository.AddOutboxEventAsync(FileOutboxFactory.FileFinalized(file), ct);

            var attachment = await CreateOptionalAttachmentAsync(file, request, actorId, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return new FinalizeUploadSessionResponse(
                FileDtoMapper.ToDto(file),
                attachment is null ? null : await GetAttachmentDtoAsync(attachment, ct));
        }, cancellationToken);
    }

    public Task<UploadSessionDto> GetProgressAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return GetAsync(sessionId, cancellationToken);
    }

    public Task<UploadSessionDto> AbortAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var session = await GetSessionAndEnsureManageAsync(sessionId, ct);
            session.Abort();
            await _unitOfWork.SaveChangesAsync(ct);
            return FileDtoMapper.ToDto(session);
        }, cancellationToken);
    }

    private async Task<Guid> ResolveWorkspaceForCreateAsync(
        CreateUploadSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.DocumentId))
        {
            var documentId = ParseGuid(request.DocumentId, "documentId");
            return await _workspaceResolver.GetWorkspaceIdForDocumentAsync(documentId, cancellationToken)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.WorkspaceId))
        {
            return ParseGuid(request.WorkspaceId, "workspaceId");
        }

        throw new ApplicationErrorException(ErrorCodes.ValidationError, "workspaceId or documentId is required.");
    }

    private async Task<UploadSession> GetSessionAndEnsureManageAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _fileRepository.GetUploadSessionAsync(sessionId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Upload session was not found.");
        var actorId = await _accessService.GetRequiredUserIdAsync(cancellationToken);
        if (session.OwnerId != actorId)
        {
            await _accessService.EnsureCanEditWorkspaceAsync(session.WorkspaceId, cancellationToken);
        }

        return session;
    }

    private async Task EnsureNotExpiredAsync(UploadSession session, CancellationToken cancellationToken)
    {
        if (!session.IsExpired(DateTimeOffset.UtcNow))
        {
            return;
        }

        session.Expire();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        throw new ApplicationErrorException(ErrorCodes.ValidationError, "Upload session has expired.");
    }

    private async Task ExpireIfNeededAsync(UploadSession session, CancellationToken cancellationToken)
    {
        if (!session.IsExpired(DateTimeOffset.UtcNow))
        {
            return;
        }

        session.Expire();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<DocumentAttachment?> CreateOptionalAttachmentAsync(
        StoredFile file,
        FinalizeUploadSessionRequest request,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentId))
        {
            return null;
        }

        var documentId = ParseGuid(request.DocumentId, "documentId");
        var location = await _fileRepository.GetDocumentLocationAsync(documentId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
        if (location.IsDeleted)
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
        }

        await _accessService.EnsureCanEditWorkspaceAsync(location.WorkspaceId, cancellationToken);
        if (location.WorkspaceId != file.WorkspaceId)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "File and document must belong to the same workspace.");
        }

        var relationType = NormalizeRelationType(request.RelationType);
        var existing = await _fileRepository.FindDocumentAttachmentAsync(documentId, file.Id, relationType, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var attachment = new DocumentAttachment(
            file.WorkspaceId,
            documentId,
            file.Id,
            relationType,
            FileDtoMapper.ToJson(request.Metadata),
            actorId);
        await _fileRepository.AddDocumentAttachmentAsync(attachment, cancellationToken);
        await _fileRepository.AddOutboxEventAsync(FileOutboxFactory.DocumentAttachmentCreated(attachment), cancellationToken);
        return attachment;
    }

    private async Task<FinalizeUploadSessionResponse> ToFinalizedResponseAsync(
        UploadSession session,
        FinalizeUploadSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (session.FinalizedFileId is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Conflict, "Upload session was finalized without a file reference.");
        }

        var file = await _fileRepository.GetFileAsync(session.FinalizedFileId.Value, cancellationToken: cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "File was not found.");
        DocumentAttachmentDto? attachment = null;
        if (!string.IsNullOrWhiteSpace(request.DocumentId))
        {
            var documentId = ParseGuid(request.DocumentId, "documentId");
            attachment = await _fileRepository.FindDocumentAttachmentDtoAsync(
                documentId,
                file.Id,
                NormalizeRelationType(request.RelationType),
                cancellationToken);
        }

        return new FinalizeUploadSessionResponse(FileDtoMapper.ToDto(file), attachment);
    }

    private async Task<DocumentAttachmentDto> GetAttachmentDtoAsync(
        DocumentAttachment attachment,
        CancellationToken cancellationToken)
    {
        return await _fileRepository.GetDocumentAttachmentAsync(
                attachment.DocumentId,
                attachment.Id,
                cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Attachment was not found after creation.");
    }

    private CreateUploadSessionResponse ToCreateResponse(UploadSession session)
    {
        return new CreateUploadSessionResponse(
            session.Id.ToString(),
            session.Status,
            session.UploadMode,
            _objectStorage.CreateUploadTarget(session),
            session.ExpiresAt);
    }

    private void ValidateCreateRequest(CreateUploadSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "idempotencyKey is required.");
        }

        if (string.IsNullOrWhiteSpace(request.OriginalFilename))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "originalFilename is required.");
        }

        if (string.IsNullOrWhiteSpace(request.MimeType))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "mimeType is required.");
        }

        if (request.ByteSize <= 0 || request.ByteSize > _options.MaxFileBytes)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "byteSize exceeds the configured file limit.");
        }

        if (!_options.AllowedMimeTypes.Contains(request.MimeType.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "mimeType is not allowed.");
        }

        var mode = string.IsNullOrWhiteSpace(request.UploadMode)
            ? UploadMode.Single
            : request.UploadMode.Trim().ToLowerInvariant();
        if (mode == UploadMode.Multipart)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Multipart upload is not enabled in Phase 6.");
        }

        if (mode != UploadMode.Single)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "uploadMode must be single.");
        }

        if (!string.IsNullOrWhiteSpace(request.ChecksumSha256) && !IsSha256(request.ChecksumSha256))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "checksumSha256 must be a SHA-256 hex string.");
        }
    }

    private static bool IsSha256(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length == 64 && trimmed.All(c => Uri.IsHexDigit(c));
    }

    private static string NormalizeRelationType(string? relationType)
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

    private static Guid ParseGuid(string value, string fieldName)
    {
        return Guid.TryParse(value, out var id)
            ? id
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, $"{fieldName} must be a valid UUID.");
    }
}
