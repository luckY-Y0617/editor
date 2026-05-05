namespace NS.Module.Knowledge.Application.Contracts.KnowledgeBases.Dtos;

public class KnowledgeBaseGetListInput
{
    public string? Filter { get; set; }
    
    public string? TeamId { get; set; }
}