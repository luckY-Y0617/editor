using System.ComponentModel.DataAnnotations;
using NS.Module.Identity.Domain.Shared.Consts;
using NS.Module.Identity.Domain.Shared.Enums;
namespace NS.Module.Identity.Application.Contracts.Roles.Dtos;

public class RoleUpdateDto
{

    [Required]
    [StringLength(RoleConsts.RoleNameMaxLength)]
    public string RoleName { get; set; } = string.Empty;

    [StringLength(RoleConsts.DescriptionMaxLength)]
    public string? Description { get; set; }

}


