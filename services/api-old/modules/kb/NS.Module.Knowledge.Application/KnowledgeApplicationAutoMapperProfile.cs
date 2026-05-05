using AutoMapper;
using NS.Module.Knowledge.Application.Contracts.Comments.Dtos;
using NS.Module.Knowledge.Application.Contracts.KnowledgeBases.Dtos;
using NS.Module.Knowledge.Application.Contracts.Documents.Dtos;
using NS.Module.Knowledge.Application.Contracts.Members.Dtos;
using NS.Module.Knowledge.Application.Contracts.Tags.Dtos;
using NS.Module.Knowledge.Application.Contracts.Versions.Dtos;
using NS.Module.Knowledge.Domain.Comments;
using NS.Module.Knowledge.Domain.Documents;
using NS.Module.Knowledge.Domain.KnowledgeBases;
using NS.Module.Knowledge.Domain.Members;
using NS.Module.Knowledge.Domain.Tags;
using NS.Module.Knowledge.Domain.Versions;


namespace NS.Module.Knowledge.Application
{
    public class KnowledgeApplicationAutoMapperProfile : Profile
    {
        public KnowledgeApplicationAutoMapperProfile()
        {
            CreateMap<KnowledgeBase, KnowledgeBaseDto>();

            CreateMap<Document, DocumentDto>();

            CreateMap<Document, DocumentTreeNodeDto>();

            CreateMap<Document, DocumentMetaDto>();

            CreateMap<Document, DocumentDetailDto>()
                .ForMember(d => d.Content, opt => opt.Ignore())
                .ForMember(d => d.Tags, opt => opt.Ignore());

            CreateMap<Document, DocumentBreadcrumbItemDto>()
                .ForMember(d => d.Title, opt => opt.MapFrom(s => s.Title));

            CreateMap<DocumentVersion, DocumentVersionDto>();
            
            CreateMap<KnowledgeBaseMember, KnowledgeBaseMemberDto>();

            CreateMap<Tag, TagDto>();

            CreateMap<Comment, CommentDto>();
        }
    }
}
