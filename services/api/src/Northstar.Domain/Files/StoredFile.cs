using Northstar.Domain.Shared;

namespace Northstar.Domain.Files;

public sealed class StoredFile
{
    private StoredFile()
    {
        StorageProvider = string.Empty;
        Bucket = string.Empty;
        ObjectKey = string.Empty;
        OriginalFilename = string.Empty;
        MimeType = string.Empty;
        Metadata = "{}";
        ScanStatus = FileScanStatus.Clean;
        ProcessingStatus = FileProcessingStatus.Ready;
    }

    public StoredFile(
        Guid workspaceId,
        Guid? uploadedBy,
        string storageProvider,
        string bucket,
        string objectKey,
        string originalFilename,
        string mimeType,
        long byteSize,
        string? checksumSha256 = null,
        int? width = null,
        int? height = null,
        string? metadata = null,
        Guid? id = null)
    {
        if (byteSize < 0)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "byteSize cannot be negative.");
        }

        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        UploadedBy = uploadedBy;
        StorageProvider = Required(storageProvider, nameof(storageProvider));
        Bucket = Required(bucket, nameof(bucket));
        ObjectKey = Required(objectKey, nameof(objectKey));
        OriginalFilename = Required(originalFilename, nameof(originalFilename));
        MimeType = Required(mimeType, nameof(mimeType));
        ByteSize = byteSize;
        ChecksumSha256 = string.IsNullOrWhiteSpace(checksumSha256) ? null : checksumSha256.Trim().ToLowerInvariant();
        Width = width;
        Height = height;
        Metadata = string.IsNullOrWhiteSpace(metadata) ? "{}" : metadata;
        ScanStatus = FileScanStatus.Clean;
        ProcessingStatus = FileProcessingStatus.Ready;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid? UploadedBy { get; private set; }
    public string StorageProvider { get; private set; }
    public string Bucket { get; private set; }
    public string ObjectKey { get; private set; }
    public string OriginalFilename { get; private set; }
    public string MimeType { get; private set; }
    public long ByteSize { get; private set; }
    public string? ChecksumSha256 { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public string Metadata { get; private set; }
    public string ScanStatus { get; private set; }
    public string ProcessingStatus { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public bool Delete()
    {
        if (DeletedAt.HasValue)
        {
            return false;
        }

        DeletedAt = DateTimeOffset.UtcNow;
        return true;
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
