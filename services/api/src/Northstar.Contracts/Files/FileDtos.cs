using System.Text.Json;

namespace Northstar.Contracts.Files;

public sealed record CreateUploadSessionRequest(
    string IdempotencyKey,
    string OriginalFilename,
    string MimeType,
    long ByteSize,
    string? ChecksumSha256,
    string? BizType,
    string UploadMode,
    string? WorkspaceId,
    string? DocumentId);

public sealed record UploadTargetDto(
    string Type,
    string Method,
    string Url,
    IReadOnlyDictionary<string, string> Headers);

public sealed record CreateUploadSessionResponse(
    string SessionId,
    string Status,
    string UploadMode,
    UploadTargetDto UploadTarget,
    DateTimeOffset ExpiresAt);

public sealed record UploadSessionDto(
    string Id,
    string WorkspaceId,
    string OwnerId,
    string IdempotencyKey,
    string OriginalFilename,
    string MimeType,
    long ByteSize,
    string? ChecksumSha256,
    string? BizType,
    string Status,
    string UploadMode,
    string? FinalizedFileId,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CompleteUploadSessionRequest();

public sealed record FinalizeUploadSessionRequest(
    string? DocumentId,
    string? RelationType,
    JsonElement? Metadata);

public sealed record FinalizeUploadSessionResponse(
    FileDto File,
    DocumentAttachmentDto? Attachment);

public sealed record FileDto(
    string Id,
    string WorkspaceId,
    string? UploadedBy,
    string StorageProvider,
    string Bucket,
    string ObjectKey,
    string OriginalFilename,
    string MimeType,
    long ByteSize,
    string? ChecksumSha256,
    int? Width,
    int? Height,
    JsonElement Metadata,
    string ScanStatus,
    string ProcessingStatus,
    DateTimeOffset CreatedAt);

public sealed record DocumentAttachmentDto(
    string Id,
    string WorkspaceId,
    string DocumentId,
    string FileId,
    string RelationType,
    JsonElement Metadata,
    DateTimeOffset CreatedAt,
    FileDto File);

public sealed record AttachFileToDocumentRequest(
    string FileId,
    string RelationType,
    JsonElement? Metadata);

public sealed record DocumentAttachmentsResponse(
    IReadOnlyList<DocumentAttachmentDto> Attachments);
