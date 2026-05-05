using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NS.Module.Identity.Domain.Shared.Consts;

namespace NS.Module.Identity.Application.Contracts.Users.Dtos;

public class UserCreateDto
{
    [Required]
    [StringLength(UserConsts.UserNameMaxLength)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [StringLength(UserConsts.PasswordHashMaxLength)]
    public string Password { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(UserConsts.EmailMaxLength)]
    public string? Email { get; set; }

    [StringLength(UserConsts.PhoneNumberMaxLength)]
    public string? PhoneNumber { get; set; }


    public List<Guid> RoleIds { get; set; } = new();


    public bool IsEnabled { get; set; } = true;
}


