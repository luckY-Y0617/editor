using System;
using NS.Module.Knowledge.Domain.Shared.Enums;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.KnowledgeBases.Dtos;

public class KnowledgeBaseDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string? TeamId { get; set; }
    public KnowledgeBaseVisibility Visibility { get; set; }
    public string? Icon { get; set; }
    public Guid? CoverImageId { get; set; }
    public int SortOrder { get; set; }
}