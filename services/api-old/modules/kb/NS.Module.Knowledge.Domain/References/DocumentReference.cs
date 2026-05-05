using System;
using NS.Module.Knowledge.Domain.Documents;
using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using NS.Module.Knowledge.Domain.Shared.Enums;
using Volo.Abp;

namespace NS.Module.Knowledge.Domain.References;

[SugarTable("kb_document_references")]
public class DocumentReference : FullAuditedEntity<Guid>, IMultiTenant, ISoftDelete
{
    [SugarColumn(IsNullable = true)]
    public Guid? TenantId { get; set; }

    [SugarColumn(IsNullable = false)]
    public Guid SourceDocumentId { get; protected set; }

    [SugarColumn(IsNullable = false)]
    public Guid TargetDocumentId { get; protected set; }

    [SugarColumn(IsNullable = false)]
    public DocumentReferenceType Type { get; protected set; }

    [SugarColumn(IsNullable = false)]
    public DateTime FirstFoundAt { get; protected set; }

    [SugarColumn(IsNullable = false)]
    public DateTime LastCheckedAt { get; protected set; }

    [SugarColumn(IsNullable = false)]
    public bool IsBroken { get; protected set; }

    [SugarColumn(Length = 512, IsNullable = true)]
    public string? Excerpt { get; protected set; }

    public DocumentReference()
    {
    }

    public DocumentReference(Guid sourceDocumentId, Guid targetDocumentId,
        DocumentReferenceType type, DateTime now, string? excerpt)
    {
        SourceDocumentId = sourceDocumentId;
        TargetDocumentId = targetDocumentId;
        Type = type;
        FirstFoundAt = now;
        LastCheckedAt = now;
        Excerpt = excerpt;
    }

    public void MarkChecked(DateTime checkedAt, bool isBroken, string? excerpt)
    {
        LastCheckedAt = checkedAt;
        IsBroken = isBroken;
        Excerpt = excerpt;
    }
}

