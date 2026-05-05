using System;
using System.Collections.Generic;
using NS.Module.Knowledge.Domain.Shared.Enums;
using NS.Module.Knowledge.Application.Contracts.Tags.Dtos;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Documents.Dtos;

public class DocumentMetaDto : EntityDto<Guid>
{
    // ===== 核心元数据 =====
    public Guid KnowledgeBaseId { get; set; }
    public string Title { get; set; } = default!;
    public Guid? ParentId { get; set; }
    public DocumentType Type { get; set; }
    public int Order { get; set; }
    public bool IsDeleted { get; set; }

    // ===== 审计字段 =====
    public DateTime CreationTime { get; set; }
    public Guid? CreatorId { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public Guid? LastModifierId { get; set; }

    // ===== 关联信息 =====
    public List<TagDto> Tags { get; set; } = new();

    // ===== 统计信息（关键）=====
    public int CommentCount { get; set; }
    public int VersionCount { get; set; }
}
