using NS.Module.Knowledge.Domain.Shared.Enums;

namespace NS.Module.Knowledge.Application.Contracts.KnowledgeBases.Dtos;

public class KnowledgeBaseCreateUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public KnowledgeBaseVisibility Visibility { get; set; } = KnowledgeBaseVisibility.Private;
    public string? Icon { get; set; }
    
    public bool AllowMembersCreateDoc { get; set; }
    
    public string? TeamId { get; set; }
}