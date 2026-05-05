using System;
using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace NS.Module.Knowledge.Domain.Tags;

[SugarTable("kb_tags")]
public class Tag : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    [SugarColumn(IsNullable = true)]
    public Guid? TenantId { get; protected set; }

    [SugarColumn(IsNullable = false)]
    public Guid KnowledgeBaseId { get; protected set; }

    [SugarColumn(Length = 64, IsNullable = false)]
    public string Name { get; protected set; } = default!;

    [SugarColumn(Length = 32, IsNullable = true)]
    public string? Color { get; protected set; }

    [SugarColumn(Length = 64, IsNullable = true)]
    public string? Icon { get; protected set; }

    public Tag()
    {
    }

    public Tag(Guid knowledgeBaseId, string name)
    {
        KnowledgeBaseId = knowledgeBaseId;
        Name = name;
    }

    public void SetColor(string? color)
    {
        Color = color;
    }

    public void SetIcon(string? icon)
    {
        Icon = icon;
    }
    
    public void Rename(string name)
    {
        Name = name;
    }


}
