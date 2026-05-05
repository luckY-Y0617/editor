namespace Northstar.Domain.Files;

public static class UploadSessionStatus
{
    public const string Initiated = "initiated";
    public const string Uploading = "uploading";
    public const string Completed = "completed";
    public const string Aborted = "aborted";
    public const string Expired = "expired";
    public const string Failed = "failed";
    public const string Finalized = "finalized";
}
