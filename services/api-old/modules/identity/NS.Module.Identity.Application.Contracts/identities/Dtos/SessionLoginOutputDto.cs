using System;
using System.Collections.Generic;

namespace NS.Module.Identity.Application.Contracts.identities.Dtos;

public class SessionLoginOutputDto
{
    public DateTime ExpireAt { get; set; }          // 会话过期（或滑动过期的上限）
    public AuthUserDto User { get; set; } = default!;
    public List<string> PermissionCodes { get; set; } = new();
}
