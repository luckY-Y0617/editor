namespace Northstar.Domain.Files;

public static class DocumentAttachmentRelationType
{
    public const string Attachment = "attachment";
    public const string InlineImage = "inline_image";
    public const string Cover = "cover";

    public static bool IsValid(string relationType)
    {
        return relationType is Attachment or InlineImage or Cover;
    }
}
