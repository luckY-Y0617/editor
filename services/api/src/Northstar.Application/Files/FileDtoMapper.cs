using System.Text.Json;
using Northstar.Contracts.Files;
using Northstar.Domain.Files;

namespace Northstar.Application.Files;

public static class FileDtoMapper
{
    public static UploadSessionDto ToDto(UploadSession session)
    {
        return new UploadSessionDto(
            session.Id.ToString(),
            session.WorkspaceId.ToString(),
            session.OwnerId.ToString(),
            session.IdempotencyKey,
            session.OriginalFilename,
            session.MimeType,
            session.ByteSize,
            session.ChecksumSha256,
            session.BizType,
            session.Status,
            session.UploadMode,
            session.FinalizedFileId?.ToString(),
            session.ExpiresAt,
            session.CreatedAt,
            session.UpdatedAt);
    }

    public static FileDto ToDto(StoredFile file)
    {
        return new FileDto(
            file.Id.ToString(),
            file.WorkspaceId.ToString(),
            file.UploadedBy?.ToString(),
            file.OriginalFilename,
            file.MimeType,
            file.ByteSize,
            file.ChecksumSha256,
            file.Width,
            file.Height,
            ParseJson(file.Metadata),
            file.ScanStatus,
            file.ProcessingStatus,
            file.CreatedAt);
    }

    public static JsonElement ParseJson(string? json)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        return document.RootElement.Clone();
    }

    public static string ToJson(JsonElement? element)
    {
        return element.HasValue && element.Value.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null
            ? JsonSerializer.Serialize(element.Value)
            : "{}";
    }
}
