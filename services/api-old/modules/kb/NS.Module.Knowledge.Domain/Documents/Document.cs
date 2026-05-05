using System;
using System.Collections.Generic;
using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using NS.Module.Knowledge.Domain.Shared.Enums;
using NS.Module.Knowledge.Domain.References;
using NS.Module.Knowledge.Domain.Tags;
using NS.Module.Knowledge.Domain.Versions;

namespace NS.Module.Knowledge.Domain.Documents;

[SugarTable("kb_documents")]
public class Document : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    [SugarColumn(IsNullable = true)]
    public Guid? TenantId { get; set; }

    [SugarColumn(IsNullable = false)]
    public Guid KnowledgeBaseId { get; protected set; }

    [SugarColumn(IsNullable = true)]
    public Guid? ParentId { get; protected set; }

    [SugarColumn(Length = 256, IsNullable = false)]
    public string Title { get; protected set; } = default!;

    [SugarColumn(IsNullable = false)]
    public DocumentType Type { get; protected set; } = DocumentType.Normal;

    [SugarColumn(IsNullable = false)]
    public int Order { get; protected set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? LastContentUpdateTime { get; protected set; }

    #region 导航属性

    [Navigate(NavigateType.OneToOne, nameof(Id))]
    public DocumentContent? Content { get; set; }


    [Navigate(NavigateType.OneToMany, nameof(DocumentVersion.DocumentId))]
    public List<DocumentVersion>? Versions { get; set; } = null!;


    [Navigate(typeof(DocumentTag), nameof(DocumentTag.DocumentId), nameof(DocumentTag.TagId))]
    public List<Tag>? Tags { get; set; } = null!;

    // 出向引用（Document.Id → DocumentReference.SourceDocumentId）
    [Navigate(NavigateType.OneToMany, nameof(DocumentReference.SourceDocumentId))]
    public List<DocumentReference>? OutgoingReferences { get; set; } = new();


    // 入向引用（Document.Id → DocumentReference.TargetDocumentId）
    [Navigate(NavigateType.OneToMany, nameof(DocumentReference.TargetDocumentId))]
    public List<DocumentReference>? IncomingReferences { get; set; } = new();

    #endregion


    public Document()
    {
    }

    public Document(Guid knowledgeBaseId, string title, Guid? parentId = null)
    {
        KnowledgeBaseId = knowledgeBaseId;
        Title = title;
        ParentId = parentId;
    }

    public void Rename(string title)
    {
        Title = title;
    }

    public void MoveTo(Guid? newParentId, int newOrder)
    {
        ParentId = newParentId;
        Order = newOrder;
    }

    /// <summary>
    /// 设置文档类型
    /// </summary>
    public void SetType(DocumentType type)
    {
        Type = type;
    }
}
