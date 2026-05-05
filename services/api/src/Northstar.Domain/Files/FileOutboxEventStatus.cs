namespace Northstar.Domain.Files;

public static class FileOutboxEventStatus
{
    public const string Pending = "pending";
    public const string Published = "published";
    public const string Failed = "failed";
}
