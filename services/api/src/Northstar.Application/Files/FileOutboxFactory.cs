using System.Text.Json;
using Northstar.Domain.Files;

namespace Northstar.Application.Files;

public static class FileOutboxFactory
{
    public static FileOutboxEvent FileFinalized(StoredFile file)
    {
        return new FileOutboxEvent(
            file.WorkspaceId,
            "file",
            file.Id,
            FileOutboxEventTypes.FileFinalized,
            JsonSerializer.Serialize(new
            {
                fileId = file.Id,
                workspaceId = file.WorkspaceId,
                byteSize = file.ByteSize,
                mimeType = file.MimeType
            }));
    }

    public static FileOutboxEvent DocumentAttachmentCreated(DocumentAttachment attachment)
    {
        return new FileOutboxEvent(
            attachment.WorkspaceId,
            "document_attachment",
            attachment.Id,
            FileOutboxEventTypes.DocumentAttachmentCreated,
            JsonSerializer.Serialize(new
            {
                attachmentId = attachment.Id,
                documentId = attachment.DocumentId,
                fileId = attachment.FileId,
                relationType = attachment.RelationType
            }));
    }

    public static FileOutboxEvent FileDeleted(StoredFile file)
    {
        return new FileOutboxEvent(
            file.WorkspaceId,
            "file",
            file.Id,
            FileOutboxEventTypes.FileDeleted,
            JsonSerializer.Serialize(new
            {
                fileId = file.Id,
                workspaceId = file.WorkspaceId
            }));
    }
}
