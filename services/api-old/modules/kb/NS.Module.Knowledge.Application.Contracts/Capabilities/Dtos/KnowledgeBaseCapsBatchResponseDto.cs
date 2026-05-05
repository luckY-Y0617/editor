using System.Collections.Generic;

namespace NS.Module.Knowledge.Application.Contracts.Capabilities.Dtos;

public class KnowledgeBaseCapsBatchResponseDto
{
    public List<KnowledgeBaseCapsDto> Items { get; set; } = new();
}