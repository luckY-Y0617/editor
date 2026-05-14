namespace Northstar.Application.Files;

public sealed class FilesOptions
{
    public const string SectionName = "Files";

    public string StorageProvider { get; init; } = "Local";
    public string LocalRootPath { get; init; } = "var/files";
    public string DefaultBucket { get; init; } = "northstar-local";
    public int UploadSessionMinutes { get; init; } = 60;
    public long MaxFileBytes { get; init; } = 52_428_800;
    public S3FilesOptions S3 { get; init; } = new();
    public string[] AllowedMimeTypes { get; init; } =
    [
        "image/png",
        "image/jpeg",
        "image/webp",
        "application/pdf",
        "text/plain"
    ];
}

public sealed class S3FilesOptions
{
    public string? Endpoint { get; init; }
    public string Region { get; init; } = "us-east-1";
    public string? AccessKey { get; init; }
    public string? SecretKey { get; init; }
    public bool ForcePathStyle { get; init; } = true;
    public bool UseHttp { get; init; }
    public int PresignedUploadMinutes { get; init; } = 15;
}
