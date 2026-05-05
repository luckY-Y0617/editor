using System;

namespace NS.Module.Knowledge.Application.Contracts.Tags.Dtos;

public class GetTopTagsInput
{
    public Guid KnowledgeBaseId { get; set; }
    public int MaxCount { get; set; } = 20;
}