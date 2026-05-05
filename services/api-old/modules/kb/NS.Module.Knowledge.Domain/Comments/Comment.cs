using System;
using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Check = Volo.Abp.Check;

namespace NS.Module.Knowledge.Domain.Comments;

[SugarTable("kb_comments")]
public class Comment : FullAuditedAggregateRoot<Guid>
{
    public Guid DocumentId { get; private set; }
    public Guid? ParentId { get; private set; }

    public string Content { get; private set; } = null!;

    [SugarColumn(IsNullable = true)]
    public string? PositionJson { get; private set; }

    public Comment() {}

    public Comment(
        Guid documentId,
        string content,
        string? positionJson,
        Guid? parentId)
    {
        DocumentId = documentId;
        ParentId = parentId;
        SetContent(content);
        PositionJson = positionJson;
    }

    public void UpdateContent(string content)
    {
        SetContent(content);
    }

    private void SetContent(string content)
    {
        Check.NotNullOrWhiteSpace(content, nameof(content));
        Content = content.Trim();
    }
}