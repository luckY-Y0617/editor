using System;
using NS.Module.Knowledge.Domain.Shared.Enums;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Documents.Dtos;

/// <summary>
/// 文档基本信息（列表/树节点用）
/// </summary>
public class DocumentDto : AuditedEntityDto<Guid>
{
    public Guid KnowledgeBaseId { get; set; }
    public Guid? ParentId { get; set; }
    public string Title { get; set; } = default!;
    public DocumentType Type { get; set; }
    public int Order { get; set; }
    public bool IsPinned { get; set; }
    public DocumentStatus Status { get; set; }

    public DateTime? LastContentUpdateTime { get; set; }
    public int WordCount { get; set; }
}