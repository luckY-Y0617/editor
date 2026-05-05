using System;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Tags.Dtos;


public class TagCreateUpdateDto : EntityDto<Guid?>
{
    public Guid KnowledgeBaseId { get; set; }
    public string Name { get; set; } = default!;
    public string? Color { get; set; }
    public string? Icon { get; set; }
}