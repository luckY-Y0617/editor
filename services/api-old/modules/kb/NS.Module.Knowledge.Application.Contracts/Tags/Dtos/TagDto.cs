using System;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Tags.Dtos;

public class TagDto : FullAuditedEntityDto<Guid>
{
    public Guid KnowledgeBaseId { get; set; }
    public string Name { get; set; } = default!;
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int UsageCount { get; set; }
}