using System;

namespace NS.Module.Identity.Domain.Shared.Etos;

public class UserCreatedEto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
}


