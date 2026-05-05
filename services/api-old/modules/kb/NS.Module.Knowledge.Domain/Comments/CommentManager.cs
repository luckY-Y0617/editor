using System;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace NS.Module.Knowledge.Domain.Comments;
public class CommentManager : DomainService
{
    public Comment Create(
        Guid documentId,
        string content,
        string? positionJson,
        Guid? parentId = null)
    {
        Check.NotNull(documentId, nameof(documentId));
        Check.NotNullOrWhiteSpace(content, nameof(content));

        return new Comment(
            documentId,
            content,
            positionJson,
            parentId
        );
    }

    public void UpdateContent(Comment comment, string content)
    {
        Check.NotNull(comment, nameof(comment));
        comment.UpdateContent(content);
    }
}