using System;
using System.Collections.Generic;
using NS.Module.Identity.Domain.Shared.Enums;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Identity.Application.Contracts.Users.Dtos;

public class UserDto : EntityDto<Guid>
{
    public string UserName { get; set; } = string.Empty;
    
    public string? RealName { get; set; }
    public string? NickName { get; set; }
    
    public string? Email { get; set; }
    
    public string? PhoneNumber { get; set; }
    public GenderEnum? Gender { get; set; } = GenderEnum.Unknown;
    
    public List<string>? RoleNames { get; set; }
    
    public bool IsEnabled { get; set; }
    
    public Guid? TenantId { get; set; }
    
    public DateTime CreationTime { get; set; }
}


