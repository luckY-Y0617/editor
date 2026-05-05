using System;
using System.Collections.Generic;

namespace NS.Module.Identity.Domain.Shared.Etos;

public class UserRoleAssignedEto
{
    public Guid UserId { get; set; }
    public List<Guid> RoleIds { get; set; } = new();
}


