using System;

namespace NS.Module.Identity.Domain.Shared.Etos;

public class UserPasswordChangedEto
{
    public Guid UserId { get; set; }
    public DateTime ChangedAt { get; set; }
}


