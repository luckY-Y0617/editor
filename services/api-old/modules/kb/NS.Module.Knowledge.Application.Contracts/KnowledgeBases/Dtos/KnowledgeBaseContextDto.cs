using NS.Module.Knowledge.Domain.Shared.Enums;

namespace NS.Module.Knowledge.Application.Contracts.KnowledgeBases.Dtos;

public class KnowledgeBaseContextDto
{
    public KnowledgeBaseDto KnowledgeBase { get; set; } = default!;
    public bool IsMember { get; set; }
    public KnowledgeBaseMemberRole? CurrentUserRole { get; set; }
}