using AutoMapper;
using NS.Module.Identity.Application.Contracts.identities.Dtos;
using NS.Module.Identity.Application.Contracts.Roles.Dtos;
using NS.Module.Identity.Application.Contracts.Teams.Dtos;
using NS.Module.Identity.Application.Contracts.Users.Dtos;
using NS.Module.Identity.Domain.Identities;
using NS.Module.Identity.Domain.Roles;
using NS.Module.Identity.Domain.Teams;
using NS.Module.Identity.Domain.Users;

namespace NS.Module.Identity.Application;

public class IdentityApplicationAutoMapperProfile : Profile
{
    public IdentityApplicationAutoMapperProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.RoleNames, opt => opt.MapFrom(x =>
                x.Roles.Select(role => role.NormalizedRoleName)
            ))
            .ForMember(dest => dest.NickName, opt => opt.MapFrom(x =>
                 x.Profile.NickName
            ))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(x =>
                x.Profile.Gender));

        CreateMap<User, AuthUserDto>();

        CreateMap<Role, RoleDto>()
            .ForMember(
                dest => dest.PermissionCodes,
                opt => opt.MapFrom(x =>
                    x.Permissions.Select(p => p.PermissionCode)));

        CreateMap<Team, TeamDto>()
            .ForMember(dest => dest.Members, opt => opt.MapFrom(x => x.Members));

        CreateMap<TeamMember, TeamMemberDto>();

        CreateMap<User, UserLookupDto>();

        CreateMap<LoginLog, LoginLogDto>();
    }
}

