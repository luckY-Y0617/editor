using System;
using System.Collections.Generic;
using NS.Module.Knowledge.Domain.Members;
using NS.Module.Knowledge.Domain.Shared.Enums;
using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace NS.Module.Knowledge.Domain.KnowledgeBases;

[SugarTable("kb_knowledge_bases")]
public class KnowledgeBase : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    [SugarColumn(IsNullable = true)]
    public Guid? TenantId { get; set; }
    
    [SugarColumn(IsNullable = false)]
    public Guid OwnerId { get; protected set; }
    
    [SugarColumn(IsNullable = true)]
    public Guid? TeamId { get; set; }

    [SugarColumn(Length = 128)]
    public string Name { get; protected set; } = default!;

    [SugarColumn(Length = 64)]
    public string? Code { get; protected set; }

    [SugarColumn(Length = 512, IsNullable = true)]
    public string? Description { get; protected set; }
    
    public KnowledgeBaseVisibility Visibility { get; protected set; } = KnowledgeBaseVisibility.Private;

    [SugarColumn(Length = 128, IsNullable = true)]
    public string? Icon { get; protected set; }
    
    [SugarColumn(IsNullable = false)]
    public bool AllowMembersCreateDoc { get; protected set; } = true;
    
    public int SortOrder { get; protected set; }


    // // 导航属性
    //
    // [Navigate(NavigateType.OneToMany, nameof(Documents) + "." + nameof(Documents.Document.KnowledgeBaseId))]
    // public List<Documents.Document> Documents { get; set; } = new();
    
    [Navigate(NavigateType.OneToMany, nameof(KnowledgeBaseMember.KnowledgeBaseId))]
    public List<KnowledgeBaseMember> Members { get; set; } = null!;

    public KnowledgeBase()
    {
    }

    public KnowledgeBase(string name, Guid ownerId) 
    {
        Name = name;
        OwnerId = ownerId;
    }

    public void SetDescription(string? description)
    {
        Description = description;
    }

    public void SetVisibility(KnowledgeBaseVisibility visibility)
    {
        Visibility = visibility;
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }

    public void SetIcon(string? icon)
    {
        Icon = icon;
    }

    public void SetCode(string code)
    {
        Code = code;
    }
    
    public void SetAllowMembersCreateDoc(bool value)
    {
        AllowMembersCreateDoc = value;
    }

    public void SetTeamId(Guid? teamId)
    {
        TeamId = teamId;
    }

    public void SetOwnerId(Guid ownerId)
    {
        OwnerId = ownerId;
    }

    public void SetName(string name)
    {
        Name = name;
    }
}

