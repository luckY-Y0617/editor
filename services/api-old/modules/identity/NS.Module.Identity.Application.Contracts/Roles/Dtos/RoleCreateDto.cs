using System.ComponentModel.DataAnnotations;
using NS.Module.Identity.Domain.Shared.Consts;

namespace NS.Module.Identity.Application.Contracts.Roles.Dtos;

public class RoleCreateDto
{
    [Required]
    [StringLength(RoleConsts.RoleNameMaxLength)]
    public string RoleName { get; set; } = string.Empty;

    [Required]
    [StringLength(RoleConsts.RoleCodeMaxLength)]
    public string RoleCode { get; set; } = string.Empty;

    [StringLength(RoleConsts.DescriptionMaxLength)]
    public string? Description { get; set; }

}


