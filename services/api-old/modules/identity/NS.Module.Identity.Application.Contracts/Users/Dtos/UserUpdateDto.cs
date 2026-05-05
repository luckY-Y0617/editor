using System.ComponentModel.DataAnnotations;
using NS.Module.Identity.Domain.Shared.Consts;

namespace NS.Module.Identity.Application.Contracts.Users.Dtos;

public class UserUpdateDto
{
    public string? Username { get; set; }
    
    [EmailAddress]
    [StringLength(UserConsts.EmailMaxLength)]
    public string? Email { get; set; }

    [StringLength(UserConsts.PhoneNumberMaxLength)]
    public string? PhoneNumber { get; set; }

}


