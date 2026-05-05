using System;
using System.Collections.Generic;
using NS.Module.Identity.Application.Contracts.Teams.Dtos;

namespace NS.Module.Identity.Application.Contracts.identities.Dtos;

public class LoginOutputDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpireAt { get; set; }
    public AuthUserDto User { get; set; } = new();
    public List<string> PermissionCodes { get; set; } = new();
}

public class AuthUserDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Avatar { get; set; }

    public Guid? TenantId { get; set; }

    public List<string> Permissions { get; set; } = new();
    
    public List<UserTeamDto> Teams { get; set; } = new();
}



