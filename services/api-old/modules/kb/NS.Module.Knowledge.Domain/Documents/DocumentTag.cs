using System;
using NS.Module.Knowledge.Domain.Tags;
using SqlSugar;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace NS.Module.Knowledge.Domain.Documents;

[SugarTable("kb_document_tags")]
public class DocumentTag : Entity<Guid>, IMultiTenant
{
    [SugarColumn(IsNullable = true)]
    public Guid? TenantId { get; set; }

    [SugarColumn(IsNullable = false)]
    public Guid DocumentId { get; protected set; }

    [SugarColumn(IsNullable = false)]
    public Guid TagId { get; protected set; }

    [SugarColumn(IsNullable = false)]
    public DateTime BindTime { get; protected set; }

    [Navigate(NavigateType.OneToOne, nameof(DocumentId))]
    public Document? Document { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(TagId))]
    public Tag? Tag { get; set; }

    public DocumentTag()
    {
    }

    public DocumentTag(Guid documentId, Guid tagId, DateTime bindTime)
    {
        DocumentId = documentId;
        TagId = tagId;
        BindTime = bindTime;
    }
}
