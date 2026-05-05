using AutoMapper;
using NS.Module.TenantManagement.Application.Contracts.Dtos;
using NS.Module.TenantManagement.Domain;

namespace NS.Module.TenantManagement.Application;

public class TenantAutoMapperProfile : Profile
{
    public TenantAutoMapperProfile()
    {
        CreateMap<TenantAggregateRoot, TenantGetOutputDto>()
            .ForMember(d => d.DbType, opt => opt.MapFrom(s => s.DbType));

        CreateMap<TenantAggregateRoot, TenantGetListOutputDto>()
            .ForMember(d => d.DbType, opt => opt.MapFrom(s => s.DbType));

        CreateMap<TenantConnectionString, TenantConnectionStringDto>();
    }
}