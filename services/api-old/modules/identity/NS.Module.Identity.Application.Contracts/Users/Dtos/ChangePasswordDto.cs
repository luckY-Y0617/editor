using System;
using System.ComponentModel.DataAnnotations;
using NS.Module.Identity.Domain.Shared.Consts;

namespace NS.Module.Identity.Application.Contracts.Users.Dtos;

public class ChangePasswordDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(UserConsts.PasswordHashMaxLength)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(UserConsts.PasswordHashMaxLength)]
    public string NewPassword { get; set; } = string.Empty;
}


