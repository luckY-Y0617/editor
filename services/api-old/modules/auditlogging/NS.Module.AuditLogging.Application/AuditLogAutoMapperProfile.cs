using AutoMapper;
using NS.Module.AuditLogging.Application.Contracts.Dtos;
using NS.Module.AuditLogging.Domain;
using NS.Module.AuditLogging.Domain.Entities;

namespace NS.Module.AuditLogging.Application.Mapping;

public class AuditLogAutoMapperProfile : Profile
{
    public AuditLogAutoMapperProfile()
    {
        CreateMap<AuditLog, AuditLogDto>()
            .ForMember(dest => dest.HasException, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.Exceptions)))
            .ForMember(dest => dest.ExtraProperties, opt => opt.MapFrom(src => src.ExtraProperties));
        
        CreateMap<AuditLog, AuditLogDetailDto>()
            .IncludeBase<AuditLog, AuditLogDto>()
            .ForMember(dest => dest.Exceptions, opt => opt.MapFrom(src => src.Exceptions))
            .ForMember(dest => dest.Actions, opt => opt.MapFrom(src => src.Actions))
            .ForMember(dest => dest.EntityChanges, opt => opt.MapFrom(src => src.EntityChanges));
        
        CreateMap<AuditLogAction, AuditLogActionDto>();
        
        CreateMap<EntityChange, EntityChangeDto>()
            .ForMember(dest => dest.PropertyChanges, opt => opt.MapFrom(src => src.PropertyChanges));
        
        CreateMap<EntityPropertyChange, EntityPropertyChangeDto>();
        
        CreateMap<EntityChangeWithUsername, EntityChangeWithUsernameDto>()
            .ForMember(dest => dest.EntityChange, opt => opt.MapFrom(src => src.EntityChange))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName));
    }
}

