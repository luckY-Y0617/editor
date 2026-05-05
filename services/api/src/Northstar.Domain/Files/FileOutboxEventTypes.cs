namespace Northstar.Domain.Files;

public static class FileOutboxEventTypes
{
    public const string FileFinalized = "file.finalized";
    public const string DocumentAttachmentCreated = "document_attachment.created";
    public const string FileDeleted = "file.deleted";
}
