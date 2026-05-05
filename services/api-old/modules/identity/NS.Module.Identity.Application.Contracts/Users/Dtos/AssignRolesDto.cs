using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NS.Module.Identity.Application.Contracts.Users.Dtos;

public class AssignRolesDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public List<Guid> RoleIds { get; set; } = new();
}


