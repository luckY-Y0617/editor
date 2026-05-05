using System;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Tags.Dtos;

public class GetTagListInput : PagedAndSortedResultRequestDto
{
    public Guid KnowledgeBaseId { get; set; }
    public string? Filter { get; set; }
}