using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NS.Module.Identity.Application.Contracts.Roles.Dtos;

public class AssignPermissionsDto
{
    [Required]
    public List<string> PermissionCodes { get; set; } = new();
}

