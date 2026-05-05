namespace Northstar.Application.Files;

public sealed class FilesOptions
{
    public const string SectionName = "Files";

    public string StorageProvider { get; init; } = "Local";
    public string LocalRootPath { get; init; } = "var/files";
    public string DefaultBucket { get; init; } = "northstar-local";
    public int UploadSessionMinutes { get; init; } = 60;
    public long MaxFileBytes { get; init; } = 52_428_800;
    public string[] AllowedMimeTypes { get; init; } =
    [
        "image/png",
        "image/jpeg",
        "image/webp",
        "application/pdf",
        "text/plain"
    ];
}
