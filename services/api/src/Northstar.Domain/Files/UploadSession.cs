using Northstar.Domain.Shared;

namespace Northstar.Domain.Files;

public sealed class UploadSession
{
    private UploadSession()
    {
        IdempotencyKey = string.Empty;
        OriginalFilename = string.Empty;
        MimeType = string.Empty;
        StorageProvider = string.Empty;
        Bucket = string.Empty;
        ObjectKey = string.Empty;
        UploadMode = Northstar.Domain.Files.UploadMode.Single;
        Status = UploadSessionStatus.Initiated;
    }

    public UploadSession(
        Guid workspaceId,
        Guid ownerId,
        string idempotencyKey,
        string originalFilename,
        string mimeType,
        long byteSize,
        string storageProvider,
        string bucket,
        string objectKey,
        DateTimeOffset expiresAt,
        string? checksumSha256 = null,
        string? bizType = null,
        string uploadMode = Northstar.Domain.Files.UploadMode.Single,
        Guid? id = null)
    {
        if (byteSize <= 0)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "byteSize must be positive.");
        }

        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        OwnerId = ownerId;
        IdempotencyKey = Required(idempotencyKey, nameof(idempotencyKey));
        OriginalFilename = Required(originalFilename, nameof(originalFilename));
        MimeType = Required(mimeType, nameof(mimeType));
        ByteSize = byteSize;
        ChecksumSha256 = string.IsNullOrWhiteSpace(checksumSha256) ? null : checksumSha256.Trim().ToLowerInvariant();
        BizType = string.IsNullOrWhiteSpace(bizType) ? null : bizType.Trim();
        StorageProvider = Required(storageProvider, nameof(storageProvider));
        Bucket = Required(bucket, nameof(bucket));
        ObjectKey = Required(objectKey, nameof(objectKey));
        UploadMode = Required(uploadMode, nameof(uploadMode));
        Status = UploadSessionStatus.Initiated;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid OwnerId { get; private set; }
    public string IdempotencyKey { get; private set; }
    public string OriginalFilename { get; private set; }
    public string MimeType { get; private set; }
    public long ByteSize { get; private set; }
    public string? ChecksumSha256 { get; private set; }
    public string? BizType { get; private set; }
    public string StorageProvider { get; private set; }
    public string Bucket { get; private set; }
    public string ObjectKey { get; private set; }
    public string UploadMode { get; private set; }
    public string? MultipartUploadId { get; private set; }
    public long? ChunkSize { get; private set; }
    public int? TotalParts { get; private set; }
    public string Status { get; private set; }
    public Guid? FinalizedFileId { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsExpired(DateTimeOffset now)
    {
        return now >= ExpiresAt && Status is not UploadSessionStatus.Finalized;
    }

    public void MarkUploading()
    {
        EnsureCanUpload();
        Status = UploadSessionStatus.Uploading;
        Touch();
    }

    public void Complete()
    {
        EnsureNotTerminal();
        Status = UploadSessionStatus.Completed;
        Touch();
    }

    public void Finalize(Guid fileId)
    {
        if (Status == UploadSessionStatus.Finalized)
        {
            return;
        }

        if (Status != UploadSessionStatus.Completed)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "Only completed upload sessions can be finalized.");
        }

        Status = UploadSessionStatus.Finalized;
        FinalizedFileId = fileId;
        FinalizedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Abort()
    {
        if (Status == UploadSessionStatus.Finalized)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "Finalized upload sessions cannot be aborted.");
        }

        if (Status == UploadSessionStatus.Aborted)
        {
            return;
        }

        Status = UploadSessionStatus.Aborted;
        Touch();
    }

    public void Expire()
    {
        if (Status is UploadSessionStatus.Finalized or UploadSessionStatus.Aborted)
        {
            return;
        }

        Status = UploadSessionStatus.Expired;
        Touch();
    }

    private void EnsureCanUpload()
    {
        EnsureNotTerminal();
        if (Status == UploadSessionStatus.Completed)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "Completed upload sessions cannot accept more content.");
        }
    }

    private void EnsureNotTerminal()
    {
        if (Status is UploadSessionStatus.Aborted or UploadSessionStatus.Expired or UploadSessionStatus.Failed)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, $"Upload session is {Status}.");
        }

        if (Status == UploadSessionStatus.Finalized)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "Upload session is already finalized.");
        }
    }

    private void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, $"{parameterName} is required.");
        }

        return value.Trim();
    }
}
